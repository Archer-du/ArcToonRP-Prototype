using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public abstract class RendererPass
    {
        // protected abstract ProfilingSampler GetSampler();
        
        protected virtual void Setup(RenderGraph renderGraph, CullingResults cullingResults)
        {
        }

        protected virtual void BuildRenderGraph(RenderGraph renderGraph, RenderGraphBuilder builder,
            CullingResults cullingResults)
        {
        }

        protected virtual void Render(RenderGraphContext context)
        {
        }
    }
}