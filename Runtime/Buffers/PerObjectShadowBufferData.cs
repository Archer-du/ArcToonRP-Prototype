using System.Runtime.InteropServices;
using UnityEngine;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerObjectShadowBufferData
    {
        public const int stride = 4 * 4 + 4 * 16;

        public Vector4 normalBias;

        public Matrix4x4 shadowMatrix;

        public PerObjectShadowBufferData(float normalBias, float filterSize, Matrix4x4 matrix)
        {
            this.normalBias = Vector4.zero;
            this.normalBias.x = normalBias * (filterSize * 1.4142136f);
            shadowMatrix = matrix;
        }
    }
}