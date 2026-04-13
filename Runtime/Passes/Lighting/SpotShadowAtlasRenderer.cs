using ArcToon.Buffers;
using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.System;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.Lighting
{
    /// <summary>
    /// Renders the spot light shadow atlas.
    /// 1 tile per spot light, perspective culling projection.
    /// </summary>
    public class SpotShadowAtlasRenderer : ShadowAtlasRenderer
    {
        private static readonly ShadowTileBufferData[] spotShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedSpotLightCount];

        private readonly RenderInfo[] spotRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedSpotLightCount];

        private ShadowMapTileData spotTileData;

        // ─── Abstract implementation ───

        protected override string SampleName => "Spot Shadows";
        protected override int GetAtlasSize() => (int)settings.spotShadow.atlasSize;
        protected override int GetActiveLightOrCasterCount() => collector.shadowedSpotLightCount;
        protected override bool UsePancaking => false;
        protected override int AtlasSizePropertyID => InternalShader.PropertyID.SpotShadowAtlasSize;
        protected override int AtlasTexturePropertyID => InternalShader.PropertyID.SpotShadowAtlas;
        protected override int DataBufferPropertyID => InternalShader.PropertyID.SpotShadowData;

        public override void SetupResources(ShadowResources resources)
        {
            atlas = collector.shadowedSpotLightCount > 0
                ? resources.spotShadowAtlas
                : resources.defaultShadowTexture;
            dataBuffer = resources.spotShadowData;
        }

        protected override void OnBuildRendererLists(int count, ScriptableRenderContext context)
        {
            int atlasSize = GetAtlasSize();
            int tiles = count;
            spotTileData = new ShadowMapTileData(atlasSize, tiles);

            for (int i = 0; i < count; i++)
            {
                BuildRendererListForIndex(i, context);
            }
        }

        protected override void BuildRendererListForIndex(int shadowedSpotLightIndex,
            ScriptableRenderContext context)
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

        protected override void RenderTilesForIndex(int shadowedSpotLightIndex, CommandBuffer cmd)
        {
            var lightShadowData = collector.ShadowMapDataSpots[shadowedSpotLightIndex];
            int tileIndex = shadowedSpotLightIndex;

            RenderInfo info = spotRenderInfo[shadowedSpotLightIndex];
            // m00 = cot(FOV/2) / Aspect (Aspect = 1 for shadow map)
            float texelSize = 2f / (spotTileData.tileSize * info.projection.m00);
            float filterSize = texelSize * settings.FilterSize;
            float normalBiasFactor = filterSize * 1.4142136f;
            Vector2 offset = cmd.SetTileViewport(tileIndex, spotTileData.splitCount, spotTileData.tileSize);
            float tileScale = 1f / spotTileData.splitCount;

            spotShadowData[tileIndex] = new ShadowTileBufferData(
                offset, tileScale, 1f / GetAtlasSize(), normalBiasFactor,
                ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

            cmd.SetViewProjectionMatrices(info.view, info.projection);
            cmd.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            cmd.DrawRendererList(info.handle);
        }

        protected override void OnUploadBufferData(CommandBuffer cmd, int activeLightCount)
        {
            cmd.SetBufferData(dataBuffer, spotShadowData, 0, 0, activeLightCount);
        }
    }
}
