using ArcToon.Runtime.Utils;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct DirectionalLightBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<DirectionalLightBufferData>();

        public Vector4 color;

        public Vector4 direction;

        // x: shadow strength
        // y: shadowed directional light index
        // z: shadow slope scale bias
        // w: shadow mask channel
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