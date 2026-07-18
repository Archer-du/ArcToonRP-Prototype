using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes
{
    public class OpaquePass : RenderPassBase
    {
        public override string Name => "Opaque";

        public OpaquePass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        private static ShaderTagId[] baseShaderTagIds =
        {
            new("ToonForward"),
            new("SRPDefaultUnlit"),
        };

        private RendererList outlineList;
        private RendererList baseList;

        public override void SetupRendererList(ScriptableRenderContext context)
        {
            baseList = context.CreateRendererList(new RendererListDesc(baseShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            
            outlineList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryOutline, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample("Toon Base");
            commandBuffer.DrawRendererList(baseList);
            commandBuffer.EndSample("Toon Base");
            
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }
    }
}