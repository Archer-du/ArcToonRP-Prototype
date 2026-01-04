using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public abstract class RenderGraphPassBase
    {
        protected virtual ProfilingSampler Sampler { get; }
        
        protected RenderGraphResourceData resourceData;

        protected CameraRenderer renderer;

        public bool IsValid()
        {
            return resourceData != null && renderer != null;
        }

        public virtual void Initialize(RenderGraphResourceData resourceData, CameraRenderer renderer)
        {
            this.resourceData = resourceData;
            this.renderer = renderer;
        }
        
        public abstract void Render(RenderGraphContext context);

        public abstract void AcquireResource(RenderGraph renderGraph);
        
        public abstract void DeclareResourceUsage(RenderGraphBuilder builder);

    }
}