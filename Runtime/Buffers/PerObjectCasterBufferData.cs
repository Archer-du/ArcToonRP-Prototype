using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct PerObjectCasterBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<PerObjectCasterBufferData>();

        public Vector4 perObjectData;

        public static PerObjectCasterBufferData GenerateStructuredData(Vector4 perObjectData)
        {
            PerObjectCasterBufferData data;
            data.perObjectData = perObjectData;
            return data;
        }
    }
}