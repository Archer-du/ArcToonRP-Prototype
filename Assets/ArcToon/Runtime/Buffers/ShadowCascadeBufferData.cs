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
            ShadowSettings.FilterMode filterMode)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)filterMode + 1f);
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            this.cullingSphere = cullingSphere;
            data = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
        }
    }
}