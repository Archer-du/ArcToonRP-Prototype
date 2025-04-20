using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class UnsupportedPass
    {
#if UNITY_EDITOR
        static readonly ProfilingSampler sampler = new("Unsupported");
        
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

        static Material errorMaterial;

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults)
        {
#if UNITY_EDITOR
            if (errorMaterial == null)
            {
                errorMaterial = new(Shader.Find("Hidden/InternalErrorShader"));
            }
            
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out UnsupportedPass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(invalidShaderTagIds, cullingResults, camera)
                {
                    overrideMaterial = errorMaterial,
                    renderQueueRange = RenderQueueRange.all
                }
            ));

            builder.SetRenderFunc<UnsupportedPass>(static (pass, context) => pass.Render(context));
#endif
        }
    }
}