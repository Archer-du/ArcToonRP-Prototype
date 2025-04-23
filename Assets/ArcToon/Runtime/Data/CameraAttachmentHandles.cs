using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly struct CameraAttachmentHandles
    {
        public readonly TextureHandle
            colorAttachment,
            depthAttachment,
            colorCopy,
            depthStencilCopy,
            stencilMask;

        public CameraAttachmentHandles(
            TextureHandle colorAttachment,
            TextureHandle depthAttachment,
            TextureHandle colorCopy,
            TextureHandle depthStencilCopy,
            TextureHandle stencilMask)
        {
            this.colorAttachment = colorAttachment;
            this.depthAttachment = depthAttachment;
            this.colorCopy = colorCopy;
            this.depthStencilCopy = depthStencilCopy;
            this.stencilMask = stencilMask;
        }
    }
}