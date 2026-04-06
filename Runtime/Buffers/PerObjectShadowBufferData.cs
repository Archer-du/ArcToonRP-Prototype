using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct PerObjectShadowBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<PerObjectShadowBufferData>();

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