using System.Runtime.InteropServices;
using UnityEngine;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    struct PointShadowBufferData
    {
        public const int stride = 4 * 4 + 4 * 16;

        public Vector4 tileData;

        public Matrix4x4 shadowMatrix;

        public PointShadowBufferData(Vector2 offset, float scale, float normalBiasScale, float oneDivideAtlasSize,
            Matrix4x4 matrix)
        {
            float border = oneDivideAtlasSize * 0.5f;
            tileData.x = offset.x * scale + border;
            tileData.y = offset.y * scale + border;
            tileData.z = scale - border - border;
            tileData.w = normalBiasScale;
            shadowMatrix = matrix;
        }
    }
}