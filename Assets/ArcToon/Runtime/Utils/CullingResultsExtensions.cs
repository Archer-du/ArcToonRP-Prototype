using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Passes.Lighting;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public static class CullingResultsExtensions
    {
        public static unsafe bool ComputePerObjectShadowMatricesAndCullingPrimitives(
            this CullingResults cullingResults,
            int visibleCasterIndex,
            int visibleLightIndex,
            Camera camera,
            PerObjectShadowCasterManager manager,
            out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix,
            out float width, out float height)
        {
            PerObjectShadowCaster caster = manager.visibleCasters[visibleCasterIndex];
            caster.GetWorldBounds(out Bounds bounds);
            VisibleLight directionalLight = cullingResults.visibleLights[visibleLightIndex];
            float4* frustumCorners = stackalloc float4[CullUtilities.FrustumCornerCount];
            CullUtilities.SetFrustumEightCorners(frustumCorners, camera);
            PerObjectShadowCullingParams param = new PerObjectShadowCullingParams()
            {
                frustumCorners = frustumCorners,
                cameraLocalToWorldMatrix = camera.transform.localToWorldMatrix,
                lightLocalToWorldMatrix = directionalLight.localToWorldMatrix,
                AABBMin = bounds.min,
                AABBMax = bounds.max,
                CasterUpVector = caster.transform.up,
            };
            bool result = CullUtilities.ComputePerObjectShadowMatricesAndCullingPrimitives(param,
                out float4x4 lightViewMatrix, out float4x4 projectionMatrix, out width, out height);
            viewMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref lightViewMatrix);
            projMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref projectionMatrix);
            return result;
        }
    }
}