using System.Runtime.InteropServices;
using UnityEngine;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    struct SpotShadowBufferData
    {
        public const int stride = 4 * 4 + 4 * 16;

        public Vector4 tileData;

        public Matrix4x4 shadowMatrix;

        public SpotShadowBufferData(Vector2 offset, float scale, float normalBiasScale, float oneDivideAtlasSize,
            Matrix4x4 matrix)
        {
            float halfTexelSize = oneDivideAtlasSize * 0.5f;
            tileData.x = offset.x * scale + halfTexelSize;
            tileData.y = offset.y * scale + halfTexelSize;
            tileData.z = scale - halfTexelSize - halfTexelSize;
            tileData.w = normalBiasScale;
            shadowMatrix = matrix;
        }
    }
}