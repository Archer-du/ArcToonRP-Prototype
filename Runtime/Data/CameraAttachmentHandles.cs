using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly ref struct CameraAttachmentHandles
    {
        public readonly TextureHandle
            colorAttachment,
            depthAttachment,
            depthStencilBuffer,
            stencilMask;

        public CameraAttachmentHandles(
            TextureHandle colorAttachment,
            TextureHandle depthAttachment,
            TextureHandle depthStencilBuffer,
            TextureHandle stencilMask)
        {
            this.colorAttachment = colorAttachment;
            this.depthAttachment = depthAttachment;
            this.depthStencilBuffer = depthStencilBuffer;
            this.stencilMask = stencilMask;
        }
    }
}