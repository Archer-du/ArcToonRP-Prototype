using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly struct ShadowMapHandle
    {
        public readonly TextureHandle directionalAtlas;
        public readonly TextureHandle spotAtlas;
        public readonly TextureHandle pointAtlas;
        public readonly TextureHandle perObjectAtlas;


        public readonly BufferHandle cascadeShadowData;
        public readonly BufferHandle directionalShadowMatrices;

        public readonly BufferHandle spotShadowData;

        public readonly BufferHandle pointShadowData;
        
        public readonly BufferHandle perObjectShadowData;

        public ShadowMapHandle(
            TextureHandle directionalAtlas,
            TextureHandle spotAtlas,
            TextureHandle pointAtlas,
            TextureHandle perObjectAtlas,
            BufferHandle cascadeShadowData,
            BufferHandle directionalShadowMatrices,
            BufferHandle spotShadowData,
            BufferHandle pointShadowData,
            BufferHandle perObjectShadowData
            )
        {
            this.directionalAtlas = directionalAtlas;
            this.spotAtlas = spotAtlas;
            this.pointAtlas = pointAtlas;
            this.perObjectAtlas = perObjectAtlas;
            this.cascadeShadowData = cascadeShadowData;
            this.directionalShadowMatrices = directionalShadowMatrices;
            this.spotShadowData = spotShadowData;
            this.pointShadowData = pointShadowData;
            this.perObjectShadowData = perObjectShadowData;
        }
    }
}