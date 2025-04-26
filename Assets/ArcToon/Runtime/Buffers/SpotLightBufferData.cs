using System.Runtime.InteropServices;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpotLightBufferData
    {
        public const int stride = 4 * 4 * 5;

        public Vector4 color;
        public Vector4 position;
        public Vector4 direction;

        public Vector4 spotAngle;

        // x: shadow strength
        // y: shadow map tile index
        // z: shadow slope scale bias
        // w: shadow mask channel
        public Vector4 shadowData;

        public static SpotLightBufferData GenerateStructuredData(in VisibleLight visibleLight, Light light,
            Vector4 shadowData)
        {
            SpotLightBufferData data;
            data.color = visibleLight.finalColor;
            data.position = visibleLight.localToWorldMatrix.GetColumn(3);
            data.position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            data.direction = -visibleLight.localToWorldMatrix.GetColumn(2);
            data.direction.w = light.renderingLayerMask.ReinterpretAsFloat();
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            data.spotAngle = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            data.shadowData = shadowData;
            return data;
        }
    }
}