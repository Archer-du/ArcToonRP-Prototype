using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class ShadowRenderer
    {
        public TextureHandle directionalAtlas;
        public TextureHandle spotAtlas;
        public TextureHandle pointAtlas;

        public BufferHandle cascadeShadowDataHandle;
        public BufferHandle directionalShadowMatricesHandle;

        public BufferHandle spotShadowDataHandle;

        public BufferHandle pointShadowDataHandle;

        NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;

        NativeArray<ShadowSplitData> shadowSplitDataPerLight;

        struct RenderInfo
        {
            public RendererListHandle handle;

            public Matrix4x4 view, projection;
        }

        private RenderInfo[] directionalRenderInfo =
            new RenderInfo[maxShadowedDirectionalLightCount * maxCascades];

        private RenderInfo[] spotRenderInfo =
            new RenderInfo[maxShadowedSpotLightCount];

        private RenderInfo[] pointRenderInfo =
            new RenderInfo[maxShadowedPointLightCount * maxTilesPerLight];

        // TODO: adapt to record
        public ShadowMapHandles GetShadowMapHandles(
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
            directionalAtlas = shadowedDirectionalLightCount > 0
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
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedDirectionalLightCount * maxCascades, 4 * 16)
                {
                    name = "Directional Shadow Matrices",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            atlasSize = (int)settings.spotShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Spot Shadow Atlas";
            spotAtlas = shadowedSpotLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            spotShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedSpotLightCount, SpotShadowBufferData.stride)
                {
                    name = "Spot Shadow Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            atlasSize = (int)settings.pointShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Point Shadow Atlas";
            pointAtlas = shadowedPointLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            pointShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedPointLightCount * 6, PointShadowBufferData.stride)
                {
                    name = "Point Shadow Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            
            BuildRendererLists(renderGraph, builder, context);

            return new ShadowMapHandles(directionalAtlas, spotAtlas, pointAtlas,
                cascadeShadowDataHandle, directionalShadowMatricesHandle, spotShadowDataHandle, pointShadowDataHandle);
        }


        int directionalSplit, directionalTileSize;
        int spotSplit, spotTileSize;
        int pointSplit, pointTileSize;

        private const int maxTilesPerLight = 6;

        void BuildRendererLists(
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            ScriptableRenderContext context)
        {
            if (shadowedDirectionalLightCount > 0)
            {
                int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
                int tiles =
                    shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount;
                directionalSplit = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
                directionalTileSize = atlasSize / directionalSplit;

                for (int i = 0; i < shadowedDirectionalLightCount; i++)
                {
                    BuildDirectionalRendererList(i, renderGraph, builder);
                }
            }

            if (shadowedSpotLightCount > 0)
            {
                int atlasSize = (int)settings.spotShadow.atlasSize;
                int tiles = shadowedSpotLightCount;
                spotSplit = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
                spotTileSize = atlasSize / spotSplit;

                for (int i = 0; i < shadowedSpotLightCount; i++)
                {
                    BuildSpotShadowsRendererList(i, renderGraph, builder);
                }
            }

            if (shadowedPointLightCount > 0)
            {
                int atlasSize = (int)settings.pointShadow.atlasSize;
                int tiles = shadowedPointLightCount * 6;
                pointSplit = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
                pointTileSize = atlasSize / pointSplit;

                for (int i = 0; i < shadowedPointLightCount; i++)
                {
                    BuildPointShadowsRendererList(i, renderGraph, builder);
                }
            }

            if (shadowedDirectionalLightCount + shadowedSpotLightCount + shadowedPointLightCount > 0)
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
            ShadowMapDataDirectional lightShadowData = shadowMapDataDirectionals[shadowedDirectionalLightIndex];
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
                    directionalTileSize, lightShadowData.nearPlaneOffset, out info.view,
                    out info.projection, out ShadowSplitData splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSplitDataPerLight[splitOffset + i] = splitData;
                if (shadowedDirectionalLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center with a surface fragment square radius
                    cascadeShadowData[i] = new ShadowCascadeBufferData(
                        splitData.cullingSphere,
                        directionalTileSize, settings.filterSize);
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

        void BuildSpotShadowsRendererList(
            int shadowedSpotLightIndex, RenderGraph renderGraph, RenderGraphBuilder builder)
        {
            ShadowMapDataSpot lightShadowData = shadowMapDataSpots[shadowedSpotLightIndex];
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
            ShadowMapDataPoint lightShadowData = shadowMapDataPoints[shadowedPointLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            float texelSize = 2f / pointTileSize;
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

        public CommandBuffer commandBuffer;

        private CullingResults cullingResults;

        private ShadowSettings settings;

        static readonly GlobalKeyword[] shadowMaskKeywords =
        {
            GlobalKeyword.Create("_SHADOW_MASK_ALWAYS"),
            GlobalKeyword.Create("_SHADOW_MASK_DISTANCE"),
        };

        private bool useShadowMask;

        private static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");

        static readonly GlobalKeyword[] filterKeywords =
        {
            GlobalKeyword.Create("_PCF3X3"),
            GlobalKeyword.Create("_PCF5X5"),
            GlobalKeyword.Create("_PCF7X7"),
        };

        // ----------------- directional shadow -----------------

        private static Matrix4x4[] directionalShadowVPMatrices =
            new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

        private static int directionalShadowVPMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");

        Vector4 directionalAtlasSizes;
        private static int directionalShadowAtlasSizeID = Shader.PropertyToID("_DirectionalShadowAtlasSize");

        private const int maxShadowedDirectionalLightCount = 4;
        private int shadowedDirectionalLightCount;

        private static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");

        private static readonly ShadowCascadeBufferData[] cascadeShadowData =
            new ShadowCascadeBufferData[maxCascades];

        private static int cascadeShadowDataID = Shader.PropertyToID("_ShadowCascadeData");

        private const int maxCascades = 4;

        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");


        static readonly GlobalKeyword[] cascadeBlendKeywords =
        {
            GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
        };


        // ----------------- spot shadow -----------------
        private static readonly SpotShadowBufferData[] spotShadowData =
            new SpotShadowBufferData[maxShadowedSpotLightCount];

        private static int spotShadowDataID = Shader.PropertyToID("_SpotShadowData");

        private static int spotShadowAtlasID = Shader.PropertyToID("_SpotShadowAtlas");

        Vector4 spotAtlasSizes;
        private static int spotShadowAtlasSizeID = Shader.PropertyToID("_SpotShadowAtlasSize");

        private const int maxShadowedSpotLightCount = 16;
        private int shadowedSpotLightCount;


        // ----------------- point shadow -----------------
        private static readonly PointShadowBufferData[] pointShadowData =
            new PointShadowBufferData[maxShadowedPointLightCount * 6];

        private static int pointShadowDataID = Shader.PropertyToID("_PointShadowData");

        private static int pointShadowAtlasID = Shader.PropertyToID("_PointShadowAtlas");

        Vector4 pointAtlasSizes;
        private static int pointShadowAtlasSizeID = Shader.PropertyToID("_PointShadowAtlasSize");

        private const int maxShadowedPointLightCount = 2;
        private int shadowedPointLightCount;

        public void Setup(CullingResults cullingResults,
            ShadowSettings settings
        )
        {
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedSpotLightCount = shadowedPointLightCount = 0;

            useShadowMask = false;

            cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(
                cullingResults.visibleLights.Length, Allocator.Temp);
            shadowSplitDataPerLight = new NativeArray<ShadowSplitData>(
                cullingInfoPerLight.Length * maxTilesPerLight,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[maxShadowedDirectionalLightCount];

        public Vector4 ReservePerLightShadowDataDirectional(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                // only baked shadows are used
                if (shadowedDirectionalLightCount >= maxShadowedDirectionalLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    // a trick to only sample baked shadow
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedDirectionalLightIndex = shadowedDirectionalLightCount++;
                shadowMapDataDirectionals[shadowedDirectionalLightIndex] =
                    new ShadowMapDataDirectional
                    {
                        visibleLightIndex = visibleLightIndex,
                        // TODO: interpreting light settings differently than their original purpose, use additional data instead
                        slopeScaleBias = light.shadowBias,
                        nearPlaneOffset = light.shadowNearPlane,
                    };
                return new Vector4(
                    light.shadowStrength,
                    shadowedDirectionalLightIndex * settings.directionalCascadeShadow.cascadeCount,
                    light.shadowNormalBias, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[maxShadowedSpotLightCount];

        public Vector4 ReservePerLightShadowDataSpot(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedSpotLightCount >= maxShadowedSpotLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedSpotLightIndex = shadowedSpotLightCount++;
                shadowMapDataSpots[shadowedSpotLightIndex] = new ShadowMapDataSpot
                {
                    visibleLightIndex = visibleLightIndex,
                    // TODO: interpreting light settings differently than their original purpose, use additional data instead
                    slopeScaleBias = light.shadowBias,
                    normalBias = light.shadowNormalBias,
                };
                return new Vector4(
                    light.shadowStrength, shadowedSpotLightIndex, 0, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        struct ShadowMapDataPoint
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataPoint[] shadowMapDataPoints =
            new ShadowMapDataPoint[maxShadowedPointLightCount];

        public Vector4 ReservePerLightShadowDataPoint(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedPointLightCount >= maxShadowedPointLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedPointLightIndex = shadowedPointLightCount++;
                shadowMapDataPoints[shadowedPointLightIndex] = new ShadowMapDataPoint
                {
                    visibleLightIndex = visibleLightIndex,
                    // TODO: interpreting light settings differently than their original purpose, use additional data instead
                    slopeScaleBias = light.shadowBias,
                    normalBias = light.shadowNormalBias,
                };
                return new Vector4(
                    light.shadowStrength, shadowedPointLightIndex * 6, 0, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }


        public void RenderShadowMap(RenderGraphContext context)
        {
            commandBuffer = context.cmd;

            if (shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadowMap();
            }

            if (shadowedSpotLightCount > 0)
            {
                RenderSpotShadowMap();
            }

            if (shadowedPointLightCount > 0)
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

            commandBuffer.SetGlobalTexture(dirShadowAtlasID, directionalAtlas);
            commandBuffer.SetGlobalTexture(spotShadowAtlasID, spotAtlas);
            commandBuffer.SetGlobalTexture(pointShadowAtlasID, pointAtlas);

            SetKeywords(filterKeywords, (int)settings.filterQuality - 1);

            SetKeywords(shadowMaskKeywords,
                useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

            commandBuffer.SetGlobalInt(cascadeCountId,
                shadowedDirectionalLightCount > 0 ? settings.directionalCascadeShadow.cascadeCount : -1);

            float f = 1f - settings.directionalCascadeShadow.edgeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeID,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
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
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(directionalShadowAtlasSizeID, directionalAtlasSizes);
            commandBuffer.SetBufferData(
                cascadeShadowDataHandle, cascadeShadowData,
                0, 0, settings.directionalCascadeShadow.cascadeCount);

            commandBuffer.SetBufferData(
                directionalShadowMatricesHandle, directionalShadowVPMatrices,
                0, 0, shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount);
            SetKeywords(
                cascadeBlendKeywords, (int)settings.directionalCascadeShadow.blendMode - 1
            );
            commandBuffer.EndSample("Directional Shadows");
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
            for (int i = 0; i < shadowedSpotLightCount; i++)
            {
                RenderSpotShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(spotShadowAtlasSizeID, spotAtlasSizes);
            commandBuffer.SetBufferData(spotShadowDataHandle, spotShadowData,
                0, 0, shadowedSpotLightCount);
            
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
            for (int i = 0; i < shadowedPointLightCount; i++)
            {
                RenderPointShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(pointShadowAtlasSizeID, pointAtlasSizes);
            commandBuffer.SetBufferData(pointShadowDataHandle, pointShadowData,
                0, 0, shadowedPointLightCount * 6);
            
            commandBuffer.EndSample("Point Shadows");
        }

        void RenderDirectionalShadowSplitTile(int shadowedDirectionalLightIndex)
        {
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
            float tileScale = 1.0f / directionalSplit;
            commandBuffer.SetGlobalDepthBias(0f, shadowMapDataDirectionals[shadowedDirectionalLightIndex].slopeScaleBias);
            for (int i = 0; i < cascadeCount; i++)
            {
                RenderInfo info = directionalRenderInfo[shadowedDirectionalLightIndex * maxCascades + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = SetTileViewport(tileIndex, directionalSplit, directionalTileSize);
                
                directionalShadowVPMatrices[tileIndex] =
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale);

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }

        void RenderSpotShadowSplitTile(int shadowedSpotLightIndex)
        {
            ShadowMapDataSpot lightShadowData = shadowMapDataSpots[shadowedSpotLightIndex];
            int tileIndex = shadowedSpotLightIndex;

            RenderInfo info = spotRenderInfo[shadowedSpotLightIndex];
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1 in case of shadow map)
            float texelSize = 2f / (spotTileSize * info.projection.m00);
            float filterSize = texelSize * settings.filterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            Vector2 offset = SetTileViewport(tileIndex, spotSplit, spotTileSize);
            float tileScale = 1f / spotSplit;

            spotShadowData[tileIndex] = new SpotShadowBufferData(
                offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

            commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            commandBuffer.DrawRendererList(info.handle);
        }

        void RenderPointShadowSplitTile(int shadowedPointLightIndex)
        {
            ShadowMapDataPoint lightShadowData = shadowMapDataPoints[shadowedPointLightIndex];
            int tileOffset = shadowedPointLightIndex * 6;
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1, cot\frac{FOV}{2} = 1 in case of point shadow map)
            float texelSize = 2f / pointTileSize;
            float filterSize = texelSize * settings.filterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            float tileScale = 1.0f / pointSplit;
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            for (int i = 0; i < 6; i++)
            {
                RenderInfo info = pointRenderInfo[shadowedPointLightIndex * maxTilesPerLight + i];
                // Undo the front face culling effect
                info.view.m11 = -info.view.m11;
                info.view.m12 = -info.view.m12;
                info.view.m13 = -info.view.m13;

                int tileIndex = tileOffset + i;
                Vector2 offset = SetTileViewport(tileIndex, pointSplit, pointTileSize);

                pointShadowData[tileIndex] = new PointShadowBufferData(
                    offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }

        Vector2 SetTileViewport(int tileIndex, int split, float tileSize)
        {
            var offset = new Vector2(tileIndex % split, tileIndex / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }

        // TODO: extract
        void SetKeywords(GlobalKeyword[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                commandBuffer.SetKeyword(keywords[i], i == enabledIndex);
            }
        }
    }
}