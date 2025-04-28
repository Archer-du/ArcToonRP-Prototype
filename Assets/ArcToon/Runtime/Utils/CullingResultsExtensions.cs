using ArcToon.Runtime.Passes.Lighting;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public static class CullingResultsExtensions
    {
        // public static unsafe bool ComputePerObjectShadowMatricesAndCullingPrimitives(
        //     this CullingResults cullingResults,
        //     int perObjectShadowCasterIndex,
        //     int visibleLightIndex,
        //     int shadowResolution,
        //     out Matrix4x4 viewMatrix,
        //     out Matrix4x4 projMatrix)
        // {
        //     VisibleLight directionalLight = cullingResults.visibleLights[visibleLightIndex];
        //     float4* frustumCorners = stackalloc float4[CullUtilities.FrustumCornerCount];
        //     CullUtilities.SetFrustumEightCorners(frustumCorners, camera);
        //     PerObjectShadowCullingParams param = new PerObjectShadowCullingParams()
        //     {
        //         
        //     };
        //     return false;
        // }
    }
}