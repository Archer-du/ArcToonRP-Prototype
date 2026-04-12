using ArcToon.Buffers;
using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes.Lighting
{
    /// <summary>
    /// Renders the per-object shadow atlas.
    /// Tiles per caster = shadowedDirectionalLightCount (dynamic).
    /// Uses DrawPerObjectShadowRenderer instead of DrawRendererList.
    /// </summary>
    public class PerObjectShadowAtlasRenderer : ShadowAtlasRenderer
    {
        private static readonly ShadowTileBufferData[] perObjectShadowData =
            new ShadowTileBufferData[RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                                     RenderPipelineInfo.MaxShadowedDirectionalLightCount];

        private readonly RenderInfo[] perObjectRenderInfo =
            new RenderInfo[RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                           RenderPipelineInfo.MaxShadowedDirectionalLightCount];

        private ShadowMapTileData perObjectTileData;

        // ─── Abstract implementation ───

        protected override string SampleName => "Per Object Shadows";
        protected override int GetAtlasSize() => (int)settings.perObjectShadow.atlasSize;
        protected override int GetActiveLightOrCasterCount() => collector.enabledPerObjectShadowCasterCount;

        protected override bool UsePancaking => false;
        protected override int AtlasSizePropertyID => InternalShader.PropertyID.PerObjectAtlasSize;
        protected override int AtlasTexturePropertyID => InternalShader.PropertyID.PerObjectShadowAtlas;
        protected override int DataBufferPropertyID => InternalShader.PropertyID.PerObjectShadowData;

        public override void SetupResources(ShadowResources resources)
        {
            atlas = collector.enabledPerObjectShadowCasterCount > 0
                ? resources.perObjectShadowAtlas
                : resources.defaultShadowTexture;
            dataBuffer = resources.perObjectShadowData;
        }

        protected override void OnBuildRendererLists(int count, ScriptableRenderContext context)
        {
            int atlasSize = GetAtlasSize();
            int tiles = count * collector.shadowedDirectionalLightCount;
            perObjectTileData = new ShadowMapTileData(atlasSize, tiles);

            for (int i = 0; i < count; i++)
            {
                BuildRendererListForIndex(i, context);
            }
        }

        protected override void BuildRendererListForIndex(int enabledPerObjectShadowCasterIndex,
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
                    camera,
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

        protected override void RenderTilesForIndex(int enabledPerObjectShadowCasterIndex, CommandBuffer cmd)
        {
            int tileCount = collector.shadowedDirectionalLightCount;
            int tileOffset = enabledPerObjectShadowCasterIndex * tileCount;
            float tileScale = 1.0f / perObjectTileData.splitCount;
            float oneDivideAtlasSize = 1f / GetAtlasSize();
            // TODO: config
            cmd.SetGlobalDepthBias(0f, 5f);
            for (int i = 0; i < tileCount; i++)
            {
                RenderInfo info =
                    perObjectRenderInfo[enabledPerObjectShadowCasterIndex *
                        RenderPipelineInfo.MaxShadowedDirectionalLightCount + i];
                int tileIndex = tileOffset + i;
                Vector2 offset = cmd.SetTileViewport(tileIndex, perObjectTileData.splitCount,
                    perObjectTileData.tileSize);

                float normalBias = Mathf.Max(info.width / perObjectTileData.tileSize,
                    info.height / perObjectTileData.tileSize);
                float normalBiasFactor = normalBias * (settings.FilterSize * 1.4142136f);
                perObjectShadowData[tileIndex] = new ShadowTileBufferData(
                    offset, tileScale, oneDivideAtlasSize, normalBiasFactor,
                    ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale));

                cmd.SetViewProjectionMatrices(info.view, info.projection);
                cmd.DrawPerObjectShadowRenderer(
                    collector.ShadowMapDataPerObjectCasters[enabledPerObjectShadowCasterIndex]
                        .visibleCasterIndex);
                // cmd.DrawRendererList(info.handle);
            }
        }

        protected override void OnUploadBufferData(CommandBuffer cmd, int activeCasterCount)
        {
            cmd.SetBufferData(dataBuffer, perObjectShadowData,
                0, 0, activeCasterCount * collector.shadowedDirectionalLightCount);
        }
    }
}
