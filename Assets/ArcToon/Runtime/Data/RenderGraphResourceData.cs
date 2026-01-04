using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public class RenderGraphResourceData
    {
        public TextureHandle colorAttachment;
        public TextureHandle depthAttachment;
        
        public TextureHandle colorCopy;
        public TextureHandle depthCopy;

        public TextureHandle stencilMask;
    }
}