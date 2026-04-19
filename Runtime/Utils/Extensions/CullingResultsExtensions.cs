using ArcToon.Behavior;
using ArcToon.Passes.Lighting;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Utils.Extensions
{
    public static class CullingResultsExtensions
    {
        public static unsafe bool ComputePerObjectShadowMatricesAndCullingPrimitives(
            this CullingResults cullingResults,
            int visibleCasterIndex,
            int visibleLightIndex,
            Camera camera,
            out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix,
            out float width, out float height)
        {
            var manager = PerObjectShadowCasterManager.Instance;
            PerObjectShadowCaster caster = manager.visibleCasters[visibleCasterIndex];
            caster.GetWorldBounds(out Bounds bounds);
            VisibleLight directionalLight = cullingResults.visibleLights[visibleLightIndex];
            float4* frustumCorners = stackalloc float4[CullUtils.FrustumCornerCount];
            CullUtils.SetFrustumEightCorners(frustumCorners, camera);
            PerObjectShadowCullingParams param = new PerObjectShadowCullingParams()
            {
                frustumCorners = frustumCorners,
                cameraLocalToWorldMatrix = camera.transform.localToWorldMatrix,
                lightLocalToWorldMatrix = directionalLight.localToWorldMatrix,
                AABBMin = bounds.min,
                AABBMax = bounds.max,
                CasterUpVector = caster.transform.up,
            };
            bool result = CullUtils.ComputePerObjectShadowMatricesAndCullingPrimitives(param,
                out float4x4 lightViewMatrix, out float4x4 projectionMatrix, out width, out height);
            viewMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref lightViewMatrix);
            projMatrix = UnsafeUtility.As<float4x4, Matrix4x4>(ref projectionMatrix);
            return result;
        }
    }
}