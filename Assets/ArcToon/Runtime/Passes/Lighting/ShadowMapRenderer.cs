using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class ShadowMapRenderer
    {
        struct RenderInfo
        {
            public RendererListHandle handle;

            public Matrix4x4 view, projection;
            public float width, height;
        }

        struct ShadowMapTileData
        {
            public int atlasSize;
            public int tileCount;
            public int splitCount;
            public int tileSize;

            public ShadowMapTileData(int atlasSize, int tileCount)
            {
                this.atlasSize = atlasSize;
                this.tileCount = tileCount;
                splitCount = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4;
                tileSize = atlasSize / splitCount;
            }
        }

        #region Global

        private CullingResults cullingResults;

        private ShadowSettings settings;

        private CommandBuffer commandBuffer;

        private Camera camera;

        private PerLightDataCollector collector;

        private PerObjectShadowCasterManager perObjectShadowCasterManager;

        private const int maxTilesPerLight = 6;

        private static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");

        private static readonly GlobalKeyword[] shadowMaskKeywords =
        {
            GlobalKeyword.Create("_SHADOW_MASK_ALWAYS"),
            GlobalKeyword.Create("_SHADOW_MASK_DISTANCE"),
        };

        private static readonly GlobalKeyword[] filterKeywords =
        {
            GlobalKeyword.Create("_PCF3X3"),
            GlobalKeyword.Create("_PCF5X5"),
            GlobalKeyword.Create("_PCF7X7"),
        };

        NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;

        NativeArray<ShadowSplitData> shadowSplitDataPerLight;

        #endregion

        #region Directional Light

        private const int maxCascades = 4;

        private static Matrix4x4[] directionalShadowVPMatrices =
            new Matrix4x4[PerLightDataCollector.maxShadowedDirectionalLightCount * maxCascades];

        private static int directionalShadowVPMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");

        private static readonly ShadowCascadeBufferData[] cascadeShadowData =
            new ShadowCascadeBufferData[maxCascades];

        private static int cascadeShadowDataID = Shader.PropertyToID("_ShadowCascadeData");

        private static Vector4 directionalAtlasSizes;
        private static int directionalShadowAtlasSizeID = Shader.PropertyToID("_DirectionalShadowAtlasSize");

        private static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");

        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");

        private static readonly GlobalKeyword[] cascadeBlendKeywords =
        {
            GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
        };

        private TextureHandle directionalAtlas;

        private BufferHandle cascadeShadowDataHandle;
        private BufferHandle directionalShadowMatricesHandle;

        private RenderInfo[] directionalRenderInfo =
            new RenderInfo[PerLightDataCollector.maxShadowedDirectionalLightCount * maxCascades];

        private ShadowMapTileData directionalTileData;

        #endregion

        #region Per Object Shadow

        private static readonly PerObjectShadowBufferData[] perObjectShadowData =
            new PerObjectShadowBufferData[PerLightDataCollector.maxPerObjectShadowCasterCount *
                                          PerLightDataCollector.maxShadowedDirectionalLightCount];

        private static int perObjectShadowDataID = Shader.PropertyToID("_PerObjectShadowData");

        private static Vector4 perObjectAtlasSizes;
        private static int perObjectAtlasSizeID = Shader.PropertyToID("_PerObjectAtlasSize");

        private static int perObjectShadowAtlasID = Shader.PropertyToID("_PerObjectShadowAtlas");

        private TextureHandle perObjectAtlas;

        private BufferHandle perObjectShadowDataHandle;

        private RenderInfo[] perObjectRenderInfo =
            new RenderInfo[PerLightDataCollector.maxPerObjectShadowCasterCount *
                           PerLightDataCollector.maxShadowedDirectionalLightCount];

        private ShadowMapTileData perObjectTileData;

        #endregion

        #region Spot Light

        private static readonly SpotShadowBufferData[] spotShadowData =
            new SpotShadowBufferData[PerLightDataCollector.maxShadowedSpotLightCount];

        private static int spotShadowDataID = Shader.PropertyToID("_SpotShadowData");

        private static Vector4 spotAtlasSizes;
        private static int spotShadowAtlasSizeID = Shader.PropertyToID("_SpotShadowAtlasSize");

        private static int spotShadowAtlasID = Shader.PropertyToID("_SpotShadowAtlas");

        private TextureHandle spotAtlas;

        private BufferHandle spotShadowDataHandle;

        private RenderInfo[] spotRenderInfo =
            new RenderInfo[PerLightDataCollector.maxShadowedSpotLightCount];

        private ShadowMapTileData spotTileData;

        #endregion

        #region Point Light

        private static readonly PointShadowBufferData[] pointShadowData =
            new PointShadowBufferData[PerLightDataCollector.maxShadowedPointLightCount * 6];

        private static int pointShadowDataID = Shader.PropertyToID("_PointShadowData");

        private static Vector4 pointAtlasSizes;
        private static int pointShadowAtlasSizeID = Shader.PropertyToID("_PointShadowAtlasSize");

        private static int pointShadowAtlasID = Shader.PropertyToID("_PointShadowAtlas");

        public TextureHandle pointAtlas;

        public BufferHandle pointShadowDataHandle;

        private RenderInfo[] pointRenderInfo =
            new RenderInfo[PerLightDataCollector.maxShadowedPointLightCount * maxTilesPerLight];

        private ShadowMapTileData pointTileData;

        #endregion

        public void RenderShadowMap(RenderGraphContext context)
        {
            commandBuffer = context.cmd;

            if (collector.shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadowMap();
            }

            if (collector.enabledPerObjectShadowCasterCount > 0)
            {
                RenderPerObjectShadowMap();
            }

            if (collector.shadowedSpotLightCount > 0)
            {
                RenderSpotShadowMap();
            }

            if (collector.shadowedPointLightCount > 0)
            {
                RenderPointShadowMap();
            }

            commandBuffer.SetGlobalDepthBias(0f, 0f);
            commandBuffer.SetGlobalBuffer(
                cascadeShadowDataID, cascadeShadowDataHandle);
            commandBuffer.SetGlobalBuffer(
                directionalShadowVPMatricesID, directionalShadowMatricesHandle);
            commandBuffer.SetGlobalBuffer(spotShadowDataID, spotShadowDataHandle);
            commandBuffer.SetGlobalBuffer(pointShadowDataID, pointShadowDataHandle);
            commandBuffer.SetGlobalBuffer(perObjectShadowDataID, perObjectShadowDataHandle);

            commandBuffer.SetGlobalTexture(dirShadowAtlasID, directionalAtlas);
            commandBuffer.SetGlobalTexture(spotShadowAtlasID, spotAtlas);
            commandBuffer.SetGlobalTexture(pointShadowAtlasID, pointAtlas);
            commandBuffer.SetGlobalTexture(perObjectShadowAtlasID, perObjectAtlas);

            commandBuffer.SetKeywords(filterKeywords, (int)settings.filterQuality - 1);

            commandBuffer.SetKeywords(shadowMaskKeywords,
                collector.useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

            commandBuffer.SetGlobalInt(cascadeCountId,
                collector.shadowedDirectionalLightCount > 0 ? settings.directionalCascadeShadow.cascadeCount : -1);

            float f = 1f - settings.directionalCascadeShadow.edgeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeID,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public void Setup(CullingResults cullingResults, Camera camera,
            ShadowSettings settings, PerLightDataCollector collector, PerObjectShadowCasterManager manager)
        {
            this.cullingResults = cullingResults;
            this.settings = settings;
            this.collector = collector;
            this.camera = camera;
            this.perObjectShadowCasterManager = manager;

            cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(
                cullingResults.visibleLights.Length, Allocator.Temp);
            shadowSplitDataPerLight = new NativeArray<ShadowSplitData>(
                cullingInfoPerLight.Length * maxTilesPerLight,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        public ShadowMapHandles Record(
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            ScriptableRenderContext context)
        {
            int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
            var desc = new TextureDesc(atlasSize, atlasSize)
            {
                depthBufferBits = DepthBits.Depth32,
                isShadowMap = true,
                name = "Directional Shadow Atlas"
            };
            directionalAtlas = collector.shadowedDirectionalLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            cascadeShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxCascades, ShadowCascadeBufferData.stride)
                {
                    name = "Shadow Cascades",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            directionalShadowMatricesHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(
                    new BufferDesc(PerLightDataCollector.maxShadowedDirectionalLightCount * maxCascades, 4 * 16)
                    {
                        name = "Directional Shadow Matrices",
                        target = GraphicsBuffer.Target.Structured
                    })
            );

            atlasSize = (int)settings.perObjectShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Per Object Shadow Atlas";
            perObjectAtlas = collector.enabledPerObjectShadowCasterCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            perObjectShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(
                    new BufferDesc(
                        PerLightDataCollector.maxPerObjectShadowCasterCount *
                        PerLightDataCollector.maxShadowedDirectionalLightCount, PerObjectShadowBufferData.stride)
                    {
                        name = "Per Object Shadow Data",
                        target = GraphicsBuffer.Target.Structured
                    })
            );

            atlasSize = (int)settings.spotShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Spot Shadow Atlas";
            spotAtlas = collector.shadowedSpotLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            spotShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(
                    new BufferDesc(PerLightDataCollector.maxShadowedSpotLightCount, SpotShadowBufferData.stride)
                    {
                        name = "Spot Shadow Data",
                        target = GraphicsBuffer.Target.Structured
                    })
            );

            atlasSize = (int)settings.pointShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Point Shadow Atlas";
            pointAtlas = collector.shadowedPointLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            pointShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(PerLightDataCollector.maxShadowedPointLightCount * 6,
                    PointShadowBufferData.stride)
                {
                    name = "Point Shadow Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            BuildRendererLists(renderGraph, builder, context);

            return new ShadowMapHandles(directionalAtlas, spotAtlas, pointAtlas, perObjectAtlas,
                cascadeShadowDataHandle, directionalShadowMatricesHandle, spotShadowDataHandle, pointShadowDataHandle,
                perObjectShadowDataHandle);
        }


        void BuildRendererLists(
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            ScriptableRenderContext context)
        {
            if (collector.shadowedDirectionalLightCount > 0)
            {
                int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
                int tiles =
                    collector.shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount;
                directionalTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedDirectionalLightCount; i++)
                {
                    BuildDirectionalRendererList(i, renderGraph, builder);
                }
            }

            if (collector.enabledPerObjectShadowCasterCount > 0)
            {
                int atlasSize = (int)settings.perObjectShadow.atlasSize;
                int tiles =
                    collector.enabledPerObjectShadowCasterCount * collector.shadowedDirectionalLightCount;
                perObjectTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.enabledPerObjectShadowCasterCount; i++)
                {
                    BuildPerObjectRendererList(i, renderGraph, builder);
                }
            }

            if (collector.shadowedSpotLightCount > 0)
            {
                int atlasSize = (int)settings.spotShadow.atlasSize;
                int tiles = collector.shadowedSpotLightCount;
                spotTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedSpotLightCount; i++)
                {
                    BuildSpotShadowsRendererList(i, renderGraph, builder);
                }
            }

            if (collector.shadowedPointLightCount > 0)
            {
                int atlasSize = (int)settings.pointShadow.atlasSize;
                int tiles = collector.shadowedPointLightCount * 6;
                pointTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedPointLightCount; i++)
                {
                    BuildPointShadowsRendererList(i, renderGraph, builder);
                }
            }

            if (collector.shadowedDirectionalLightCount + collector.shadowedSpotLightCount +
                collector.shadowedPointLightCount > 0)
            {
                context.CullShadowCasters(
                    cullingResults,
                    new ShadowCastersCullingInfos
                    {
                        perLightInfos = cullingInfoPerLight,
                        splitBuffer = shadowSplitDataPerLight
                    });
            }
        }

        void BuildDirectionalRendererList(
            int shadowedDirectionalLightIndex,
            RenderGraph renderGraph,
            RenderGraphBuilder builder)
        {
            var lightShadowData = collector.ShadowMapDataDirectionals[shadowedDirectionalLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            Vector3 ratios = settings.directionalCascadeShadow.CascadeRatios;
            float cullingFactor = Mathf.Max(0f, 1f - settings.directionalCascadeShadow.edgeFade);
            int splitOffset = lightShadowData.visibleLightIndex * maxTilesPerLight;
            for (int i = 0; i < cascadeCount; i++)
            {
                ref RenderInfo info = ref directionalRenderInfo[shadowedDirectionalLightIndex * maxCascades + i];
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex, i, cascadeCount, ratios,
                    directionalTileData.tileSize, lightShadowData.nearPlaneOffset, out info.view,
                    out info.projection, out ShadowSplitData splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSplitDataPerLight[splitOffset + i] = splitData;
                if (shadowedDirectionalLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center with a surface fragment square radius
                    cascadeShadowData[i] = new ShadowCascadeBufferData(
                        splitData.cullingSphere,
                        directionalTileData.tileSize, settings.filterSize);
                }

                info.handle = builder.UseRendererList(renderGraph.CreateShadowRendererList(ref shadowSettings));
            }

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Orthographic,
                    splitRange = new RangeInt(splitOffset, cascadeCount)
                };
        }

        private ShaderTagId shadowCasterId = new ShaderTagId("ShadowCaster");

        void BuildPerObjectRendererList(
            int enabledPerObjectShadowCasterIndex,
            RenderGraph renderGraph,
            RenderGraphBuilder builder)
        {
            var casterShadowMapData = collector.ShadowMapDataPerObjectCasters[enabledPerObjectShadowCasterIndex];
            int shadowedDirectionalLightCount = collector.shadowedDirectionalLightCount;
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                var lightShadowData = collector.ShadowMapDataDirectionals[i];
                ref RenderInfo info = ref perObjectRenderInfo[
                    enabledPerObjectShadowCasterIndex * PerLightDataCollector.maxShadowedDirectionalLightCount + i];
                cullingResults.ComputePerObjectShadowMatricesAndCullingPrimitives(
                    casterShadowMapData.visibleCasterIndex, lightShadowData.visibleLightIndex,
                    camera, perObjectShadowCasterManager,
                    out info.view, out info.projection, out info.width, out info.height);

                info.handle = builder.UseRendererList(renderGraph.CreateRendererList(
                    new RendererListDesc(shadowCasterId, cullingResults, camera)
                    {
                        sortingCriteria = SortingCriteria.CommonOpaque,
                        renderQueueRange = RenderQueueRange.all,
                    })
                );
            }
        }

        void BuildSpotShadowsRendererList(
            int shadowedSpotLightIndex, RenderGraph renderGraph, RenderGraphBuilder builder)
        {
            var lightShadowData = collector.ShadowMapDataSpots[shadowedSpotLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            ref RenderInfo info = ref spotRenderInfo[shadowedSpotLightIndex];
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
                lightShadowData.visibleLightIndex, out info.view, out info.projection,
                out ShadowSplitData splitData);

            int splitOffset = lightShadowData.visibleLightIndex * maxTilesPerLight;
            shadowSplitDataPerLight[splitOffset] = splitData;

            info.handle = builder.UseRendererList(renderGraph.CreateShadowRendererList(ref shadowSettings));

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Perspective,
                    splitRange = new RangeInt(splitOffset, 1)
                };
        }

        void BuildPointShadowsRendererList(
            int shadowedPointLightIndex, RenderGraph renderGraph, RenderGraphBuilder builder)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            float texelSize = 2f / pointTileData.tileSize;
            float filterSize = texelSize * settings.filterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            float fovBias = Mathf.Atan(1f + normalBiasScale + filterSize) * Mathf.Rad2Deg * 2f - 90f;

            int splitOffset = lightShadowData.visibleLightIndex * maxTilesPerLight;
            for (int i = 0; i < 6; i++)
            {
                ref RenderInfo info =
                    ref pointRenderInfo[shadowedPointLightIndex * maxTilesPerLight + i];
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex, (CubemapFace)i, fovBias,
                    out info.view, out info.projection,
                    out ShadowSplitData splitData);
                shadowSplitDataPerLight[splitOffset + i] = splitData;

                info.handle = builder.UseRendererList(renderGraph.CreateShadowRendererList(ref shadowSettings));
            }

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Perspective,
                    splitRange = new RangeInt(splitOffset, 6)
                };
        }

        void RenderDirectionalShadowMap()
        {
            int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
            directionalAtlasSizes.x = directionalAtlasSizes.y = 1f / atlasSize;
            directionalAtlasSizes.z = directionalAtlasSizes.w = atlasSize;

            commandBuffer.BeginSample("Directional Shadows");
            commandBuffer.SetRenderTarget(
                directionalAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 1f);
            for (int i = 0; i < collector.shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(directionalShadowAtlasSizeID, directionalAtlasSizes);
            commandBuffer.SetBufferData(
                cascadeShadowDataHandle, cascadeShadowData,
                0, 0, settings.directionalCascadeShadow.cascadeCount);

            commandBuffer.SetBufferData(
                directionalShadowMatricesHandle, directionalShadowVPMatrices,
                0, 0, collector.shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount);
            commandBuffer.SetKeywords(
                cascadeBlendKeywords, (int)settings.directionalCascadeShadow.blendMode - 1
            );
            commandBuffer.EndSample("Directional Shadows");
        }

        void RenderPerObjectShadowMap()
        {
            int atlasSize = (int)settings.perObjectShadow.atlasSize;
            perObjectAtlasSizes.x = perObjectAtlasSizes.y = 1f / atlasSize;
            perObjectAtlasSizes.z = perObjectAtlasSizes.w = atlasSize;

            commandBuffer.BeginSample("Per Object Shadows");
            commandBuffer.SetRenderTarget(
                perObjectAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 0f);
            for (int i = 0; i < collector.enabledPerObjectShadowCasterCount; i++)
            {
                RenderPerObjectShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(perObjectAtlasSizeID, perObjectAtlasSizes);
            commandBuffer.SetBufferData(
                perObjectShadowDataHandle, perObjectShadowData,
                0, 0, collector.enabledPerObjectShadowCasterCount * collector.shadowedDirectionalLightCount);
            
            commandBuffer.EndSample("Per Object Shadows");
        }

        void RenderSpotShadowMap()
        {
            int atlasSize = (int)settings.spotShadow.atlasSize;
            spotAtlasSizes.x = spotAtlasSizes.y = 1f / atlasSize;
            spotAtlasSizes.z = spotAtlasSizes.w = atlasSize;

            commandBuffer.BeginSample("Spot Shadows");

            commandBuffer.SetRenderTarget(
                spotAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 0f);
            for (int i = 0; i < collector.shadowedSpotLightCount; i++)
            {
                RenderSpotShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(spotShadowAtlasSizeID, spotAtlasSizes);
            commandBuffer.SetBufferData(spotShadowDataHandle, spotShadowData,
                0, 0, collector.shadowedSpotLightCount);

            commandBuffer.EndSample("Spot Shadows");
        }

        void RenderPointShadowMap()
        {
            int atlasSize = (int)settings.pointShadow.atlasSize;
            pointAtlasSizes.x = pointAtlasSizes.y = 1f / atlasSize;
            pointAtlasSizes.z = pointAtlasSizes.w = atlasSize;

            commandBuffer.BeginSample("Point Shadows");

            commandBuffer.SetRenderTarget(
                pointAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 0f);
            for (int i = 0; i < collector.shadowedPointLightCount; i++)
            {
                RenderPointShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(pointShadowAtlasSizeID, pointAtlasSizes);
            commandBuffer.SetBufferData(pointShadowDataHandle, pointShadowData,
                0, 0, collector.shadowedPointLightCount * 6);

            commandBuffer.EndSample("Point Shadows");
        }

        void RenderDirectionalShadowSplitTile(int shadowedDirectionalLightIndex)
        {
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
            float tileScale = 1.0f / directionalTileData.splitCount;
            commandBuffer.SetGlobalDepthBias(0f,
                collector.ShadowMapDataDirectionals[shadowedDirectionalLightIndex].slopeScaleBias);
            for (int i = 0; i < cascadeCount; i++)
            {
                RenderInfo info = directionalRenderInfo[shadowedDirectionalLightIndex * maxCascades + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = commandBuffer.SetTileViewport(tileIndex, directionalTileData.splitCount,
                    directionalTileData.tileSize);

                directionalShadowVPMatrices[tileIndex] =
                    ShadowMapHelpers.ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale);

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }

        void RenderPerObjectShadowSplitTile(int enabledPerObjectShadowCasterIndex)
        {
            int tileCount = collector.shadowedDirectionalLightCount;
            int tileOffset = enabledPerObjectShadowCasterIndex * tileCount;
            float tileScale = 1.0f / perObjectTileData.splitCount;
            // TODO: config
            commandBuffer.SetGlobalDepthBias(0f, 5f);
            for (int i = 0; i < tileCount; i++)
            {
                RenderInfo info =
                    perObjectRenderInfo[
                        enabledPerObjectShadowCasterIndex * PerLightDataCollector.maxShadowedDirectionalLightCount + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = commandBuffer.SetTileViewport(tileIndex, perObjectTileData.splitCount,
                    perObjectTileData.tileSize);

                float normalBias = Mathf.Max(info.width / perObjectTileData.tileSize,
                    info.height / perObjectTileData.tileSize);
                perObjectShadowData[tileIndex] = new PerObjectShadowBufferData(
                    normalBias, settings.filterSize,
                    ShadowMapHelpers.ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawPerObjectShadowRenderer(perObjectShadowCasterManager,
                    collector.ShadowMapDataPerObjectCasters[enabledPerObjectShadowCasterIndex].visibleCasterIndex);
                // commandBuffer.DrawRendererList(info.handle);
            }
        }

        void RenderSpotShadowSplitTile(int shadowedSpotLightIndex)
        {
            var lightShadowData = collector.ShadowMapDataSpots[shadowedSpotLightIndex];
            int tileIndex = shadowedSpotLightIndex;

            RenderInfo info = spotRenderInfo[shadowedSpotLightIndex];
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1 in case of shadow map)
            float texelSize = 2f / (spotTileData.tileSize * info.projection.m00);
            float filterSize = texelSize * settings.filterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            Vector2 offset = commandBuffer.SetTileViewport(tileIndex, spotTileData.splitCount, spotTileData.tileSize);
            float tileScale = 1f / spotTileData.splitCount;

            spotShadowData[tileIndex] = new SpotShadowBufferData(
                offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                ShadowMapHelpers.ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

            commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            commandBuffer.DrawRendererList(info.handle);
        }

        void RenderPointShadowSplitTile(int shadowedPointLightIndex)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            int tileOffset = shadowedPointLightIndex * 6;
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1, cot\frac{FOV}{2} = 1 in case of point shadow map)
            float texelSize = 2f / pointTileData.tileSize;
            float filterSize = texelSize * settings.filterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            float tileScale = 1.0f / pointTileData.splitCount;
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            for (int i = 0; i < 6; i++)
            {
                RenderInfo info = pointRenderInfo[shadowedPointLightIndex * maxTilesPerLight + i];
                // Undo the front face culling effect
                info.view.m11 = -info.view.m11;
                info.view.m12 = -info.view.m12;
                info.view.m13 = -info.view.m13;

                int tileIndex = tileOffset + i;
                Vector2 offset =
                    commandBuffer.SetTileViewport(tileIndex, pointTileData.splitCount, pointTileData.tileSize);

                pointShadowData[tileIndex] = new PointShadowBufferData(
                    offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                    ShadowMapHelpers.ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }
    }
}