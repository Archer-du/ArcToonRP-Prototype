using System.Collections.Generic;
using ArcToon.Data;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes.Lighting
{
    public class ShadowMapRenderer
    {
        struct RenderInfo
        {
            public RendererList handle;

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

        NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;

        NativeArray<ShadowSplitData> shadowSplitDataPerLight;

        private ShadowResources resources;

        #endregion

        #region Directional Light

        private static readonly ShadowTileBufferData[] directionalShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades];

        private static readonly ShadowCascadeBufferData[] cascadeShadowData =
            new ShadowCascadeBufferData[RenderPipelineInfo.MaxCascades];

        private static Vector4 directionalAtlasSizes;

        private RTHandle directionalAtlas;

        private GraphicsBuffer cascadeShadowDataBuffer;
        private GraphicsBuffer directionalShadowDataBuffer;

        private RenderInfo[] directionalRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades];

        private ShadowMapTileData directionalTileData;

        #endregion

        #region Per Object Shadow

        private static readonly ShadowTileBufferData[] perObjectShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                                     RenderPipelineInfo.MaxShadowedDirectionalLightCount];

        private static Vector4 perObjectAtlasSizes;

        private RTHandle perObjectAtlas;

        private GraphicsBuffer perObjectShadowDataBuffer;

        private RenderInfo[] perObjectRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                           RenderPipelineInfo.MaxShadowedDirectionalLightCount];

        private ShadowMapTileData perObjectTileData;

        #endregion

        #region Spot Light

        private static readonly ShadowTileBufferData[] spotShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedSpotLightCount];

        private static Vector4 spotAtlasSizes;

        private RTHandle spotAtlas;

        private GraphicsBuffer spotShadowDataBuffer;

        private RenderInfo[] spotRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedSpotLightCount];

        private ShadowMapTileData spotTileData;

        #endregion

        #region Point Light

        private static readonly ShadowTileBufferData[] pointShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedPointLightCount * 6];

        private static Vector4 pointAtlasSizes;

        private RTHandle pointAtlas;

        private GraphicsBuffer pointShadowDataBuffer;

        private RenderInfo[] pointRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedPointLightCount * RenderPipelineInfo.MaxTilesPerLight];

        private ShadowMapTileData pointTileData;

        #endregion
        
        public void Initialize(CullingResults cullingResults, Camera camera,
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
                cullingInfoPerLight.Length * RenderPipelineInfo.MaxTilesPerLight,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }
        
        /// <summary>
        /// Bind RTHandle and GraphicsBuffer references from ShadowResources.
        /// Selects default shadow texture when no casters are present.
        /// Called after Initialize, before BuildRendererLists.
        /// </summary>
        public void SetupResources(ShadowResources resources)
        {
            this.resources = resources;

            // Shadow Atlases: use real atlas or default shadow texture
            directionalAtlas = collector.shadowedDirectionalLightCount > 0
                ? resources.directionalShadowAtlas
                : resources.defaultShadowTexture;

            perObjectAtlas = collector.enabledPerObjectShadowCasterCount > 0
                ? resources.perObjectShadowAtlas
                : resources.defaultShadowTexture;

            spotAtlas = collector.shadowedSpotLightCount > 0
                ? resources.spotShadowAtlas
                : resources.defaultShadowTexture;

            pointAtlas = collector.shadowedPointLightCount > 0
                ? resources.pointShadowAtlas
                : resources.defaultShadowTexture;

            // Shadow Data Buffers
            cascadeShadowDataBuffer = resources.cascadeShadowData;
            directionalShadowDataBuffer = resources.directionalShadowData;
            perObjectShadowDataBuffer = resources.perObjectShadowData;
            spotShadowDataBuffer = resources.spotShadowData;
            pointShadowDataBuffer = resources.pointShadowData;
        }

        /// <summary>
        /// Create RendererLists for all shadow casters and perform shadow caster culling.
        /// Called after SetupResources, before RenderShadowMap.
        /// </summary>
        public void BuildRendererLists(ScriptableRenderContext context)
        {
            if (collector.shadowedDirectionalLightCount > 0)
            {
                int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
                int tiles =
                    collector.shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount;
                directionalTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedDirectionalLightCount; i++)
                {
                    BuildDirectionalRendererList(i, context);
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
                    BuildPerObjectRendererList(i, context);
                }
            }

            if (collector.shadowedSpotLightCount > 0)
            {
                int atlasSize = (int)settings.spotShadow.atlasSize;
                int tiles = collector.shadowedSpotLightCount;
                spotTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedSpotLightCount; i++)
                {
                    BuildSpotShadowsRendererList(i, context);
                }
            }

            if (collector.shadowedPointLightCount > 0)
            {
                int atlasSize = (int)settings.pointShadow.atlasSize;
                int tiles = collector.shadowedPointLightCount * 6;
                pointTileData = new ShadowMapTileData(atlasSize, tiles);

                for (int i = 0; i < collector.shadowedPointLightCount; i++)
                {
                    BuildPointShadowsRendererList(i, context);
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

        public void RenderShadowMap(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            this.commandBuffer = commandBuffer;

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
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.ShadowCascadeData, cascadeShadowDataBuffer);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.DirectionalShadowData, directionalShadowDataBuffer);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.SpotShadowData, spotShadowDataBuffer);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PointShadowData, pointShadowDataBuffer);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PerObjectShadowData, perObjectShadowDataBuffer);

            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DirectionalShadowAtlas, directionalAtlas);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.SpotShadowAtlas, spotAtlas);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.PointShadowAtlas, pointAtlas);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.PerObjectShadowAtlas, perObjectAtlas);

            Dictionary<ShadowSettings.FilterQuality, GlobalKeyword> filterKeywords = new()
            {
                { ShadowSettings.FilterQuality.PCF3x3, InternalShader.GlobalKeyword.PCF3X3 },
                { ShadowSettings.FilterQuality.PCF5x5, InternalShader.GlobalKeyword.PCF5X5 },
                { ShadowSettings.FilterQuality.PCF7x7, InternalShader.GlobalKeyword.PCF7X7 },
                { ShadowSettings.FilterQuality.PoissonDisk, InternalShader.GlobalKeyword.POISSON_DISK },
                { ShadowSettings.FilterQuality.PCSS, InternalShader.GlobalKeyword.PCSS },
            };
            foreach (var filter in filterKeywords)
            {
                commandBuffer.SetKeyword(filter.Value, settings.filterQuality == filter.Key);
            }

            if (settings.filterQuality is ShadowSettings.FilterQuality.PoissonDisk or ShadowSettings.FilterQuality.PCSS)
            {
                commandBuffer.SetGlobalFloat(InternalShader.PropertyID.PoissonFilterRadius, settings.poissonFilterRadius);
            }

            commandBuffer.SetKeywords(
                new[] { InternalShader.GlobalKeyword.SHADOW_MASK_ALWAYS, InternalShader.GlobalKeyword.SHADOW_MASK_DISTANCE },
                collector.useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1
            );

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.CascadeCount,
                collector.shadowedDirectionalLightCount > 0 ? settings.directionalCascadeShadow.cascadeCount : -1);

            float f = 1f - settings.directionalCascadeShadow.edgeFade;
            commandBuffer.SetGlobalVector(InternalShader.PropertyID.ShadowDistanceFade,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void BuildDirectionalRendererList(
            int shadowedDirectionalLightIndex,
            ScriptableRenderContext context)
        {
            var lightShadowData = collector.ShadowMapDataDirectionals[shadowedDirectionalLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            Vector3 ratios = settings.directionalCascadeShadow.CascadeRatios;
            float cullingFactor = Mathf.Max(0f, 1f - settings.directionalCascadeShadow.edgeFade);
            int splitOffset = lightShadowData.visibleLightIndex * RenderPipelineInfo.MaxTilesPerLight;
            for (int i = 0; i < cascadeCount; i++)
            {
                ref RenderInfo info = ref directionalRenderInfo[shadowedDirectionalLightIndex * RenderPipelineInfo.MaxCascades + i];
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
                        directionalTileData.tileSize, settings.FilterSize);
                }

                info.handle = context.CreateShadowRendererList(ref shadowSettings);
            }

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Orthographic,
                    splitRange = new RangeInt(splitOffset, cascadeCount)
                };
        }

        private void BuildPerObjectRendererList(
            int enabledPerObjectShadowCasterIndex,
            ScriptableRenderContext context)
        {
            var casterShadowMapData = collector.ShadowMapDataPerObjectCasters[enabledPerObjectShadowCasterIndex];
            int shadowedDirectionalLightCount = collector.shadowedDirectionalLightCount;
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                var lightShadowData = collector.ShadowMapDataDirectionals[i];
                ref RenderInfo info = ref perObjectRenderInfo[
                    enabledPerObjectShadowCasterIndex * RenderPipelineInfo.MaxShadowedDirectionalLightCount + i];
                cullingResults.ComputePerObjectShadowMatricesAndCullingPrimitives(
                    casterShadowMapData.visibleCasterIndex, lightShadowData.visibleLightIndex,
                    camera, perObjectShadowCasterManager,
                    out info.view, out info.projection, out info.width, out info.height);

                info.handle = context.CreateRendererList(
                    new RendererListDesc(InternalShader.TagId.ShadowCaster, cullingResults, camera)
                    {
                        sortingCriteria = SortingCriteria.CommonOpaque,
                        renderQueueRange = RenderQueueRange.all,
                    }
                );
            }
        }

        private void BuildSpotShadowsRendererList(
            int shadowedSpotLightIndex, ScriptableRenderContext context)
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

            int splitOffset = lightShadowData.visibleLightIndex * RenderPipelineInfo.MaxTilesPerLight;
            shadowSplitDataPerLight[splitOffset] = splitData;

            info.handle = context.CreateShadowRendererList(ref shadowSettings);

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Perspective,
                    splitRange = new RangeInt(splitOffset, 1)
                };
        }

        private void BuildPointShadowsRendererList(
            int shadowedPointLightIndex, ScriptableRenderContext context)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1, cot\frac{FOV}{2} = 1 in case of point shadow map)
            float texelSize = 2f / pointTileData.tileSize;
            float filterSize = texelSize * settings.FilterSize;
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            float fovBias = Mathf.Atan(1f + normalBiasScale + filterSize) * Mathf.Rad2Deg * 2f - 90f;

            int splitOffset = lightShadowData.visibleLightIndex * RenderPipelineInfo.MaxTilesPerLight;
            for (int i = 0; i < 6; i++)
            {
                ref RenderInfo info =
                    ref pointRenderInfo[shadowedPointLightIndex * RenderPipelineInfo.MaxTilesPerLight + i];
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex, (CubemapFace)i, fovBias,
                    out info.view, out info.projection,
                    out ShadowSplitData splitData);
                shadowSplitDataPerLight[splitOffset + i] = splitData;

                info.handle = context.CreateShadowRendererList(ref shadowSettings);
            }

            cullingInfoPerLight[lightShadowData.visibleLightIndex] =
                new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Perspective,
                    splitRange = new RangeInt(splitOffset, 6)
                };
        }

        private void RenderDirectionalShadowMap()
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
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.ShadowPancaking, 1f);
            for (int i = 0; i < collector.shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.DirectionalShadowAtlasSize, directionalAtlasSizes);
            commandBuffer.SetBufferData(
                cascadeShadowDataBuffer, cascadeShadowData,
                0, 0, settings.directionalCascadeShadow.cascadeCount);

            commandBuffer.SetBufferData(
                directionalShadowDataBuffer, directionalShadowData,
                0, 0, collector.shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount);
            commandBuffer.SetKeyword(InternalShader.GlobalKeyword.CASCADE_BLEND_SOFT, settings.directionalCascadeShadow.blendMode == ShadowSettings.CascadeBlendMode.Soft);
            commandBuffer.EndSample("Directional Shadows");
        }

        private void RenderPerObjectShadowMap()
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
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.ShadowPancaking, 0f);
            for (int i = 0; i < collector.enabledPerObjectShadowCasterCount; i++)
            {
                RenderPerObjectShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.PerObjectAtlasSize, perObjectAtlasSizes);
            commandBuffer.SetBufferData(
                perObjectShadowDataBuffer, perObjectShadowData,
                0, 0, collector.enabledPerObjectShadowCasterCount * collector.shadowedDirectionalLightCount);
            
            commandBuffer.EndSample("Per Object Shadows");
        }

        private void RenderSpotShadowMap()
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
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.ShadowPancaking, 0f);
            for (int i = 0; i < collector.shadowedSpotLightCount; i++)
            {
                RenderSpotShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.SpotShadowAtlasSize, spotAtlasSizes);
            commandBuffer.SetBufferData(spotShadowDataBuffer, spotShadowData,
                0, 0, collector.shadowedSpotLightCount);

            commandBuffer.EndSample("Spot Shadows");
        }

        private void RenderPointShadowMap()
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
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.ShadowPancaking, 0f);
            for (int i = 0; i < collector.shadowedPointLightCount; i++)
            {
                RenderPointShadowSplitTile(i);
            }

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.PointShadowAtlasSize, pointAtlasSizes);
            commandBuffer.SetBufferData(pointShadowDataBuffer, pointShadowData,
                0, 0, collector.shadowedPointLightCount * 6);

            commandBuffer.EndSample("Point Shadows");
        }

        private void RenderDirectionalShadowSplitTile(int shadowedDirectionalLightIndex)
        {
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
            float tileScale = 1.0f / directionalTileData.splitCount;
            commandBuffer.SetGlobalDepthBias(0f,
                collector.ShadowMapDataDirectionals[shadowedDirectionalLightIndex].slopeScaleBias);
            for (int i = 0; i < cascadeCount; i++)
            {
                RenderInfo info = directionalRenderInfo[shadowedDirectionalLightIndex * RenderPipelineInfo.MaxCascades + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = commandBuffer.SetTileViewport(tileIndex, directionalTileData.splitCount,
                    directionalTileData.tileSize);

                directionalShadowData[tileIndex] = new ShadowTileBufferData(
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }

        private void RenderPerObjectShadowSplitTile(int enabledPerObjectShadowCasterIndex)
        {
            int tileCount = collector.shadowedDirectionalLightCount;
            int tileOffset = enabledPerObjectShadowCasterIndex * tileCount;
            float tileScale = 1.0f / perObjectTileData.splitCount;
            // TODO: config
            commandBuffer.SetGlobalDepthBias(0f, 5f);
            for (int i = 0; i < tileCount; i++)
            {
                RenderInfo info =
                    perObjectRenderInfo[enabledPerObjectShadowCasterIndex * RenderPipelineInfo.MaxShadowedDirectionalLightCount + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = commandBuffer.SetTileViewport(tileIndex, perObjectTileData.splitCount,
                    perObjectTileData.tileSize);

                float normalBias = Mathf.Max(info.width / perObjectTileData.tileSize,
                    info.height / perObjectTileData.tileSize);
                float normalBiasFactor = normalBias * (settings.FilterSize * 1.4142136f);
                perObjectShadowData[tileIndex] = new ShadowTileBufferData(
                    offset, tileScale, perObjectAtlasSizes.x, normalBiasFactor,
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawPerObjectShadowRenderer(perObjectShadowCasterManager,
                    collector.ShadowMapDataPerObjectCasters[enabledPerObjectShadowCasterIndex].visibleCasterIndex);
                // commandBuffer.DrawRendererList(info.handle);
            }
        }

        private void RenderSpotShadowSplitTile(int shadowedSpotLightIndex)
        {
            var lightShadowData = collector.ShadowMapDataSpots[shadowedSpotLightIndex];
            int tileIndex = shadowedSpotLightIndex;

            RenderInfo info = spotRenderInfo[shadowedSpotLightIndex];
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1 in case of shadow map)
            float texelSize = 2f / (spotTileData.tileSize * info.projection.m00);
            float filterSize = texelSize * settings.FilterSize;
            float normalBiasFactor = filterSize * 1.4142136f;
            Vector2 offset = commandBuffer.SetTileViewport(tileIndex, spotTileData.splitCount, spotTileData.tileSize);
            float tileScale = 1f / spotTileData.splitCount;

            spotShadowData[tileIndex] = new ShadowTileBufferData(
                offset, tileScale, spotAtlasSizes.x, normalBiasFactor,
                ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

            commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            commandBuffer.DrawRendererList(info.handle);
        }

        private void RenderPointShadowSplitTile(int shadowedPointLightIndex)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            int tileOffset = shadowedPointLightIndex * 6;
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1, cot\frac{FOV}{2} = 1 in case of point shadow map)
            float texelSize = 2f / pointTileData.tileSize;
            float filterSize = texelSize * settings.FilterSize;
            float normalBiasFactor = filterSize * 1.4142136f;
            float tileScale = 1.0f / pointTileData.splitCount;
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);

            for (int i = 0; i < 6; i++)
            {
                RenderInfo info = pointRenderInfo[shadowedPointLightIndex * RenderPipelineInfo.MaxTilesPerLight + i];
                // Undo the front face culling effect
                info.view.m11 = -info.view.m11;
                info.view.m12 = -info.view.m12;
                info.view.m13 = -info.view.m13;

                int tileIndex = tileOffset + i;
                Vector2 offset = commandBuffer.SetTileViewport(tileIndex, pointTileData.splitCount, pointTileData.tileSize);

                pointShadowData[tileIndex] = new ShadowTileBufferData(
                    offset, tileScale, pointAtlasSizes.x, normalBiasFactor,
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
                commandBuffer.DrawRendererList(info.handle);
            }
        }
        
        private static Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
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
    }
}