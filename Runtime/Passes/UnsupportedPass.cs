using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes
{
    public class UnsupportedPass : RenderPassBase
    {
        public override string Name => "Unsupported";

        public UnsupportedPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        private static ShaderTagId[] invalidShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        RendererList list;

        public override void SetupRendererList(ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            list = context.CreateRendererList(new RendererListDesc(invalidShaderTagIds, renderer.CullingResults, Camera)
            {
                overrideMaterial = ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.InternalError),
                renderQueueRange = RenderQueueRange.all
            });
#endif
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            commandBuffer.DrawRendererList(list);
#endif
        }
    }
}