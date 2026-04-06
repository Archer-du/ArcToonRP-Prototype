using ArcToon.Runtime.Settings;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct ShadowCascadeBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<ShadowCascadeBufferData>();

        public Vector4 cullingSphere;
        public Vector4 data;

        public ShadowCascadeBufferData(
            Vector4 cullingSphere,
            float tileSize,
            float filterSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float scaledSize = texelSize * filterSize;
            cullingSphere.w -= scaledSize;
            cullingSphere.w *= cullingSphere.w;
            this.cullingSphere = cullingSphere;
            data = new Vector4(1f / cullingSphere.w, scaledSize * 1.4142136f);
        }
    }
}