using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly ref struct LightingDataHandles
    {
        public readonly BufferHandle directionalLightDataHandle;
        public readonly BufferHandle spotLightDataHandle;
        public readonly BufferHandle pointLightDataHandle;
        public readonly BufferHandle perObjectShadowCasterDataHandle;


        public readonly BufferHandle forwardPlusTileBufferHandle;

        public readonly ShadowMapHandles shadowMapHandles;

        public LightingDataHandles(
            BufferHandle directionalLightDataHandle, 
            BufferHandle spotLightDataHandle, 
            BufferHandle pointLightDataHandle, 
            BufferHandle perObjectShadowCasterDataHandle,
            BufferHandle forwardPlusTileBufferHandle,
            ShadowMapHandles shadowMapHandles)
        {
            this.directionalLightDataHandle = directionalLightDataHandle;
            this.spotLightDataHandle = spotLightDataHandle;
            this.pointLightDataHandle = pointLightDataHandle;
            this.perObjectShadowCasterDataHandle = perObjectShadowCasterDataHandle;
            this.forwardPlusTileBufferHandle = forwardPlusTileBufferHandle;
            this.shadowMapHandles = shadowMapHandles;
        }
    }
}