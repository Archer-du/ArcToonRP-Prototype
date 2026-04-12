using ArcToon.Buffers;
using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.Lighting
{
    /// <summary>
    /// Renders the directional light shadow atlas with cascade support.
    /// Tiles per light = cascadeCount, orthographic culling projection, shadow pancaking enabled.
    /// Extra: owns ShadowCascadeBufferData and uploads cascade data + CASCADE_BLEND_SOFT keyword.
    /// </summary>
    public class DirectionalShadowAtlasRenderer : ShadowAtlasRenderer
    {
        private static readonly ShadowTileBufferData[] directionalShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades];

        private static readonly ShadowCascadeBufferData[] cascadeShadowData =
            new ShadowCascadeBufferData[RenderPipelineInfo.MaxCascades];

        private readonly RenderInfo[] directionalRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades];

        private ShadowMapTileData directionalTileData;

        // Extra resource: cascade data buffer
        private GraphicsBuffer cascadeShadowDataBuffer;

        // ─── Abstract implementation ───

        protected override string SampleName => "Directional Shadows";
        protected override int GetAtlasSize() => (int)settings.directionalCascadeShadow.atlasSize;
        protected override int GetActiveLightOrCasterCount() => collector.shadowedDirectionalLightCount;
        protected override bool UsePancaking => true;
        protected override int AtlasSizePropertyID => InternalShader.PropertyID.DirectionalShadowAtlasSize;
        protected override int AtlasTexturePropertyID => InternalShader.PropertyID.DirectionalShadowAtlas;
        protected override int DataBufferPropertyID => InternalShader.PropertyID.DirectionalShadowData;

        public override void SetupResources(ShadowResources resources)
        {
            atlas = collector.shadowedDirectionalLightCount > 0
                ? resources.directionalShadowAtlas
                : resources.defaultShadowTexture;
            dataBuffer = resources.directionalShadowData;
            cascadeShadowDataBuffer = resources.cascadeShadowData;
        }

        protected override void OnBuildRendererLists(int count, ScriptableRenderContext context)
        {
            int atlasSize = GetAtlasSize();
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            int tiles = count * cascadeCount;
            directionalTileData = new ShadowMapTileData(atlasSize, tiles);

            for (int i = 0; i < count; i++)
            {
                BuildRendererListForIndex(i, context);
            }
        }

        protected override void BuildRendererListForIndex(int shadowedDirectionalLightIndex,
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
                ref RenderInfo info = ref directionalRenderInfo[
                    shadowedDirectionalLightIndex * RenderPipelineInfo.MaxCascades + i];
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex, i, cascadeCount, ratios,
                    directionalTileData.tileSize, lightShadowData.nearPlaneOffset, out info.view,
                    out info.projection, out ShadowSplitData splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSplitDataPerLight[splitOffset + i] = splitData;
                if (shadowedDirectionalLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center
                    // with a surface fragment square radius
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

        protected override void RenderTilesForIndex(int shadowedDirectionalLightIndex, CommandBuffer cmd)
        {
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
            float tileScale = 1.0f / directionalTileData.splitCount;
            cmd.SetGlobalDepthBias(0f,
                collector.ShadowMapDataDirectionals[shadowedDirectionalLightIndex].slopeScaleBias);
            for (int i = 0; i < cascadeCount; i++)
            {
                RenderInfo info = directionalRenderInfo[
                    shadowedDirectionalLightIndex * RenderPipelineInfo.MaxCascades + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = cmd.SetTileViewport(tileIndex, directionalTileData.splitCount,
                    directionalTileData.tileSize);

                directionalShadowData[tileIndex] = new ShadowTileBufferData(
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                cmd.SetViewProjectionMatrices(info.view, info.projection);
                cmd.DrawRendererList(info.handle);
            }
        }

        protected override void OnUploadBufferData(CommandBuffer cmd, int activeLightCount)
        {
            int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
            cmd.SetBufferData(cascadeShadowDataBuffer, cascadeShadowData,
                0, 0, cascadeCount);
            cmd.SetBufferData(dataBuffer, directionalShadowData,
                0, 0, activeLightCount * cascadeCount);
        }

        protected override void OnPostRender(CommandBuffer cmd)
        {
            cmd.SetKeyword(InternalShader.GlobalKeyword.CASCADE_BLEND_SOFT,
                settings.directionalCascadeShadow.blendMode == ShadowSettings.CascadeBlendMode.Soft);
        }

        /// <summary>
        /// Override SetGlobalBindings to also bind cascade data buffer,
        /// cascade count and shadow distance fade.
        /// </summary>
        public override void SetGlobalBindings(CommandBuffer cmd)
        {
            base.SetGlobalBindings(cmd);
            cmd.SetGlobalBuffer(InternalShader.PropertyID.ShadowCascadeData, cascadeShadowDataBuffer);

            cmd.SetGlobalInt(InternalShader.PropertyID.CascadeCount,
                collector.shadowedDirectionalLightCount > 0
                    ? settings.directionalCascadeShadow.cascadeCount
                    : -1);

            float f = 1f - settings.directionalCascadeShadow.edgeFade;
            cmd.SetGlobalVector(InternalShader.PropertyID.ShadowDistanceFade,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        }
    }
}
