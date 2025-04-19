using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly ref struct LightDataHandles
    {
        public readonly BufferHandle directionalLightDataHandle;
        public readonly BufferHandle spotLightDataHandle;
        public readonly BufferHandle pointLightDataHandle;

        public readonly ShadowMapHandles shadowMapHandles;

        public LightDataHandles(BufferHandle directionalLightDataHandle, BufferHandle spotLightDataHandle,
            BufferHandle pointLightDataHandle, ShadowMapHandles shadowMapHandles)
        {
            this.directionalLightDataHandle = directionalLightDataHandle;
            this.spotLightDataHandle = spotLightDataHandle;
            this.pointLightDataHandle = pointLightDataHandle;
            this.shadowMapHandles = shadowMapHandles;
        }
    }
}