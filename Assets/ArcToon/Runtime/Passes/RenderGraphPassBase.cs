using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public abstract class RenderGraphPassBase
    {
        public abstract ProfilingSampler Sampler { get; }
        
        // TODO: readonly ref
        protected RenderGraphResourceHandle resourceHandle;

        protected CameraRenderer renderer;
        
        protected Camera Camera => renderer.RenderCamera;
        protected Vector2Int AttachmentSize => renderer.AttachmentSize;

        public virtual bool IsValid()
        {
            return resourceHandle != null && renderer != null;
        }

        public virtual void Initialize(RenderGraphResourceHandle resourceHandle, CameraRenderer renderer)
        {
            this.resourceHandle = resourceHandle;
            this.renderer = renderer;
        }
        
        public abstract void Render(CommandBuffer commandBuffer, ScriptableRenderContext context);

        public abstract void AcquireResource(RenderGraph renderGraph);
        
        public abstract void DeclareResourceUsage(RenderGraphBuilder builder);
    }
}