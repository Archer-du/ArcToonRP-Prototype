using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly struct CameraAttachmentHandles
    {
        public readonly TextureHandle
            colorAttachment,
            depthAttachment,
            colorCopy,
            depthStencilBuffer,
            stencilMask;

        public CameraAttachmentHandles(
            TextureHandle colorAttachment,
            TextureHandle depthAttachment,
            TextureHandle colorCopy,
            TextureHandle depthStencilBuffer,
            TextureHandle stencilMask)
        {
            this.colorAttachment = colorAttachment;
            this.depthAttachment = depthAttachment;
            this.colorCopy = colorCopy;
            this.depthStencilBuffer = depthStencilBuffer;
            this.stencilMask = stencilMask;
        }
    }
}