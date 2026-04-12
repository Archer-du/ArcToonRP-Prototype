using ArcToon.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes.Transparency
{
    public class OrderedDualFacePass : RenderPassBase
    {
        public override string Name => "Transparent Dual Face";

        private RendererList frontFaceList;
        private RendererList backFaceList;

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            backFaceList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentBackFace, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            frontFaceList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentFrontFace, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.DrawRendererList(backFaceList);
            commandBuffer.DrawRendererList(frontFaceList);
        }
    }
}