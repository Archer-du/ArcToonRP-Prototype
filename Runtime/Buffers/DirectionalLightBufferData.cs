using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct DirectionalLightBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<DirectionalLightBufferData>();

        public Vector4 color;

        public Vector4 direction;

        /// <summary>
        /// x: shadow strength (float)
        /// y: packed(tileIndex | maskChannel) via asuint/asfloat
        /// z: shadow normal bias scale (light.shadowNormalBias, per-light config)
        /// w: lightSize (per-light PCSS light size, from ArcToonLightData or ShadowSettings fallback)
        /// </summary>
        public Vector4 shadowData;

        public static DirectionalLightBufferData GenerateStructuredData(in VisibleLight visibleLight, Light light,
            Vector4 shadowData)
        {
            DirectionalLightBufferData data;
            data.color = visibleLight.finalColor;
            data.direction = -visibleLight.localToWorldMatrix.GetColumn(2);
            data.direction.w = light.renderingLayerMask;
            data.shadowData = shadowData;
            return data;
        }
    }
}