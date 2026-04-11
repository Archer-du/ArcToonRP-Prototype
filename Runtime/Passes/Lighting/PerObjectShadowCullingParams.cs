using Unity.Mathematics;

namespace ArcToon.Passes.Lighting
{
    public unsafe struct PerObjectShadowCullingParams
    {
        public float4* frustumCorners;
        public float4x4 cameraLocalToWorldMatrix;
        public float4x4 lightLocalToWorldMatrix;
        
        public float3 AABBMin;
        public float3 AABBMax;
        public float3 CasterUpVector;
    }
}