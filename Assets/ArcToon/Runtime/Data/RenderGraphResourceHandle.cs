using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public class RenderGraphResourceHandle
    {
        public TextureHandle colorAttachment;
        public TextureHandle depthAttachment;
        
        public TextureHandle colorCopy;
        public TextureHandle depthCopy;

        public TextureHandle stencilMask;
        
        public BufferHandle lightDataDirectional;
        public BufferHandle lightDataSpot;
        public BufferHandle lightDataPoint;
        public BufferHandle perObjectShadowCasterData;

        public BufferHandle forwardPlusTileBuffer;

        public ShadowMapHandle shadowMapHandle;
        
        // TODO: ref
        public TextureHandle geometryResult;
        // TODO: ref
        public TextureHandle postFXResult;
    }
}