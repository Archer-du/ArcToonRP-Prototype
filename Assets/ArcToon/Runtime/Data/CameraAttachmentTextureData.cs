using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly struct CameraAttachmentTextureData
    {
        public readonly TextureHandle
            colorAttachment,
            depthAttachment,
            colorCopy,
            depthCopy;

        public CameraAttachmentTextureData(
            TextureHandle colorAttachment,
            TextureHandle depthAttachment,
            TextureHandle colorCopy,
            TextureHandle depthCopy)
        {
            this.colorAttachment = colorAttachment;
            this.depthAttachment = depthAttachment;
            this.colorCopy = colorCopy;
            this.depthCopy = depthCopy;
        }
    }
}