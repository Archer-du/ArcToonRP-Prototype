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
    /// Renders the point light shadow atlas.
    /// 6 tiles per point light (cubemap faces), perspective culling projection.
    /// Requires view matrix Y-flip to undo front face culling effect.
    /// </summary>
    public class PointShadowAtlasRenderer : ShadowAtlasRenderer
    {
        private static readonly ShadowTileBufferData[] pointShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxShadowedPointLightCount * 6];

        private readonly RenderInfo[] pointRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxShadowedPointLightCount * RenderPipelineInfo.MaxTilesPerLight];

        private ShadowMapTileData pointTileData;

        // ─── Abstract implementation ───

        protected override string SampleName => "Point Shadows";
        protected override int GetAtlasSize() => (int)settings.pointShadow.atlasSize;
        protected override int GetActiveLightOrCasterCount() => collector.shadowedPointLightCount;
        protected override bool UsePancaking => false;
        protected override int AtlasSizePropertyID => InternalShader.PropertyID.PointShadowAtlasSize;
        protected override int AtlasTexturePropertyID => InternalShader.PropertyID.PointShadowAtlas;
        protected override int DataBufferPropertyID => InternalShader.PropertyID.PointShadowData;

        public override void SetupResources(ShadowResources resources)
        {
            atlas = collector.shadowedPointLightCount > 0
                ? resources.pointShadowAtlas
                : resources.defaultShadowTexture;
            dataBuffer = resources.pointShadowData;
        }

        protected override void OnBuildRendererLists(int count, ScriptableRenderContext context)
        {
            int atlasSize = GetAtlasSize();
            int tiles = count * 6;
            pointTileData = new ShadowMapTileData(atlasSize, tiles);

            for (int i = 0; i < count; i++)
            {
                BuildRendererListForIndex(i, context);
            }
        }

        protected override void BuildRendererListForIndex(int shadowedPointLightIndex,
            ScriptableRenderContext context)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
            {
                useRenderingLayerMaskTest = true
            };
            // m00 = cot(FOV/2) / Aspect (Aspect = 1, cot(FOV/2) = 1 for point shadow map)
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

        protected override void RenderTilesForIndex(int shadowedPointLightIndex, CommandBuffer cmd)
        {
            var lightShadowData = collector.ShadowMapDataPoints[shadowedPointLightIndex];
            int tileOffset = shadowedPointLightIndex * 6;
            // m00 = cot(FOV/2) / Aspect (Aspect = 1, cot(FOV/2) = 1 for point shadow map)
            float texelSize = 2f / pointTileData.tileSize;
            float filterSize = texelSize * settings.FilterSize;
            float normalBiasFactor = filterSize * 1.4142136f;
            float tileScale = 1.0f / pointTileData.splitCount;
            cmd.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);

            float oneDivideAtlasSize = 1f / GetAtlasSize();

            for (int i = 0; i < 6; i++)
            {
                RenderInfo info = pointRenderInfo[shadowedPointLightIndex * RenderPipelineInfo.MaxTilesPerLight + i];
                // Undo the front face culling effect
                info.view.m11 = -info.view.m11;
                info.view.m12 = -info.view.m12;
                info.view.m13 = -info.view.m13;

                int tileIndex = tileOffset + i;
                Vector2 offset = cmd.SetTileViewport(tileIndex, pointTileData.splitCount, pointTileData.tileSize);

                pointShadowData[tileIndex] = new ShadowTileBufferData(
                    offset, tileScale, oneDivideAtlasSize, normalBiasFactor,
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                cmd.SetViewProjectionMatrices(info.view, info.projection);
                cmd.DrawRendererList(info.handle);
            }
        }

        protected override void OnUploadBufferData(CommandBuffer cmd, int activeLightCount)
        {
            cmd.SetBufferData(dataBuffer, pointShadowData, 0, 0, activeLightCount * 6);
        }
    }
}
