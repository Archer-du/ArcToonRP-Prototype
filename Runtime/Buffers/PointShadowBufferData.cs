using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct PointShadowBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<PointShadowBufferData>();

        // x: tile border start x
        // y: tile border start y
        // z: tile border length
        // w: shadow normal bias scale
        public Vector4 atlasData;

        public Matrix4x4 shadowMatrix;

        public PointShadowBufferData(Vector2 offset, float scale, float normalBiasScale, float oneDivideAtlasSize,
            Matrix4x4 matrix)
        {
            float border = oneDivideAtlasSize * 0.5f;
            atlasData.x = offset.x * scale + border;
            atlasData.y = offset.y * scale + border;
            atlasData.z = scale - border - border;
            atlasData.w = normalBiasScale;
            shadowMatrix = matrix;
        }
    }
}