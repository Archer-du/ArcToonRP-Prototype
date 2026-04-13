using System.Collections.Generic;
using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.System;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.Lighting
{
    /// <summary>
    /// Thin orchestrator for all shadow atlas renderers.
    /// Owns shared NativeArrays for culling data and coordinates the lifecycle:
    /// Initialize → SetupResources → BuildRendererLists → RenderShadowMap.
    /// </summary>
    public class ShadowMapRenderer
    {
        #region Atlas Renderers

        private readonly ShadowAtlasRenderer[] allAtlases;

        #endregion

        #region Shared State

        private CullingResults cullingResults;
        private ShadowSettings settings;
        private PerLightDataCollector collector;

        // Shared culling data (indexed by visibleLightIndex, shared across all shadow types)
        private NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;
        private NativeArray<ShadowSplitData> shadowSplitDataPerLight;

        #endregion

        public ShadowMapRenderer()
        {
            allAtlases = new ShadowAtlasRenderer[]
            {
                new DirectionalShadowAtlasRenderer(),
                new PerObjectShadowAtlasRenderer(),
                new SpotShadowAtlasRenderer(),
                new PointShadowAtlasRenderer()
            };
        }

        public void Initialize(CullingResults cullingResults, Camera camera,
            ShadowSettings settings, PerLightDataCollector collector)
        {
            this.cullingResults = cullingResults;
            this.settings = settings;
            this.collector = collector;

            cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(
                cullingResults.visibleLights.Length, Allocator.Temp);
            shadowSplitDataPerLight = new NativeArray<ShadowSplitData>(
                cullingInfoPerLight.Length * RenderPipelineInfo.MaxTilesPerLight,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Initialize all atlas renderers with shared state
            foreach (var atlas in allAtlases)
            {
                atlas.Initialize(cullingResults, camera, settings, collector,
                    cullingInfoPerLight, shadowSplitDataPerLight);
            }
        }

        /// <summary>
        /// Bind RTHandle and GraphicsBuffer references from ShadowResources.
        /// Called after Initialize, before BuildRendererLists.
        /// </summary>
        public void SetupResources(ShadowResources resources)
        {
            foreach (var atlas in allAtlases)
            {
                atlas.SetupResources(resources);
            }
        }

        /// <summary>
        /// Create RendererLists for all shadow casters and perform shadow caster culling.
        /// Called after SetupResources, before RenderShadowMap.
        /// </summary>
        public void BuildRendererLists(ScriptableRenderContext context)
        {
            foreach (var atlas in allAtlases)
            {
                atlas.BuildRendererLists(context);
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
            // Render all shadow atlases (order: directional, perObject, spot, point)
            foreach (var atlas in allAtlases)
            {
                atlas.RenderAtlas(commandBuffer);
            }

            commandBuffer.SetGlobalDepthBias(0f, 0f);

            // Bind all atlas textures and data buffers as global shader resources
            foreach (var atlas in allAtlases)
            {
                atlas.SetGlobalBindings(commandBuffer);
            }

            // Shadow filter keywords (shared across all shadow types)
            SetFilterKeywords(commandBuffer);

            // Shadow mask keywords (shared across all shadow types)
            commandBuffer.SetKeywords(
                new[] { InternalShader.GlobalKeyword.SHADOW_MASK_ALWAYS, InternalShader.GlobalKeyword.SHADOW_MASK_DISTANCE },
                collector.useShadowMask
                    ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1
                    : -1
            );

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void SetFilterKeywords(CommandBuffer commandBuffer)
        {
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

            if (settings.filterQuality is ShadowSettings.FilterQuality.PoissonDisk
                or ShadowSettings.FilterQuality.PCSS)
            {
                commandBuffer.SetGlobalFloat(InternalShader.PropertyID.PoissonFilterRadius,
                    settings.poissonFilterRadius);
            }
        }
    }
}