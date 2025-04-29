using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PerObjectCasterBufferData
    {
        public const int stride = 4 * 4 * 1;

        public Vector4 perObjectData;

        public static PerObjectCasterBufferData GenerateStructuredData(Vector4 perObjectData)
        {
            PerObjectCasterBufferData data;
            data.perObjectData = perObjectData;
            return data;
        }
    }
}