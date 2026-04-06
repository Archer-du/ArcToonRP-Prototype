using ArcToon.Runtime.Utils;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct PointLightBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<PointLightBufferData>();

        public Vector4 color;
        public Vector4 position;

        public Vector4 direction;

        // x: shadow strength
        // y: shadow map tile index
        // z: shadow slope scale bias
        // w: shadow mask channel
        public Vector4 shadowData;

        public static PointLightBufferData GenerateStructuredData(in VisibleLight visibleLight, Light light,
            Vector4 shadowData)
        {
            PointLightBufferData data;
            data.color = visibleLight.finalColor;
            data.position = visibleLight.localToWorldMatrix.GetColumn(3);
            data.position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            data.direction = Vector4.zero;
            data.direction.w = light.renderingLayerMask;
            data.shadowData = shadowData;
            return data;
        }
    }
}