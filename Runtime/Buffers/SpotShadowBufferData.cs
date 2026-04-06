using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct SpotShadowBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<SpotShadowBufferData>();
        
        // x: tile border start x
        // y: tile border start y
        // z: tile border length
        // w: shadow normal bias scale
        public Vector4 atlasData;

        public Matrix4x4 shadowMatrix;

        public SpotShadowBufferData(Vector2 offset, float scale, float normalBiasScale, float oneDivideAtlasSize,
            Matrix4x4 matrix)
        {
            float halfTexelSize = oneDivideAtlasSize * 0.5f;
            atlasData.x = offset.x * scale + halfTexelSize;
            atlasData.y = offset.y * scale + halfTexelSize;
            atlasData.z = scale - halfTexelSize - halfTexelSize;
            atlasData.w = normalBiasScale;
            shadowMatrix = matrix;
        }
    }
}