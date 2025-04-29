using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly ref struct ShadowMapHandles
    {
        public readonly TextureHandle directionalAtlas;
        public readonly TextureHandle spotAtlas;
        public readonly TextureHandle pointAtlas;
        public readonly TextureHandle perObjectAtlas;


        public readonly BufferHandle cascadeShadowDataHandle;
        public readonly BufferHandle directionalShadowMatricesHandle;

        public readonly BufferHandle spotShadowDataHandle;

        public readonly BufferHandle pointShadowDataHandle;
        
        public readonly BufferHandle perObjectShadowDataHandle;

        public ShadowMapHandles(
            TextureHandle directionalAtlas,
            TextureHandle spotAtlas,
            TextureHandle pointAtlas,
            TextureHandle perObjectAtlas,
            BufferHandle cascadeShadowDataHandle,
            BufferHandle directionalShadowMatricesHandle,
            BufferHandle spotShadowDataHandle,
            BufferHandle pointShadowDataHandle,
            BufferHandle perObjectShadowDataHandle
            )
        {
            this.directionalAtlas = directionalAtlas;
            this.spotAtlas = spotAtlas;
            this.pointAtlas = pointAtlas;
            this.perObjectAtlas = perObjectAtlas;
            this.cascadeShadowDataHandle = cascadeShadowDataHandle;
            this.directionalShadowMatricesHandle = directionalShadowMatricesHandle;
            this.spotShadowDataHandle = spotShadowDataHandle;
            this.pointShadowDataHandle = pointShadowDataHandle;
            this.perObjectShadowDataHandle = perObjectShadowDataHandle;
        }
    }
}