using System.Runtime.InteropServices;
using ArcToon.Runtime.Settings;
using UnityEngine;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    struct ShadowCascadeBufferData
    {
        public const int stride = 4 * 4 * 2;

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