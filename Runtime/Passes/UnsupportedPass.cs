using System.Diagnostics;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class UnsupportedPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Unsupported");
        
        RendererListHandle list;

        private static ShaderTagId[] invalidShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            commandBuffer.DrawRendererList(list);
#endif
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
#if UNITY_EDITOR
            list = renderGraph.CreateRendererList(new RendererListDesc(invalidShaderTagIds, renderer.CullingResults, Camera)
            {
                overrideMaterial = ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.InternalError),
                renderQueueRange = RenderQueueRange.all
            });
#endif
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
#if UNITY_EDITOR
            builder.UseRendererList(list);
#endif
        }
    }
}