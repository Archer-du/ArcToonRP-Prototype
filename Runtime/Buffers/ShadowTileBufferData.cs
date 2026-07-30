using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct ShadowTileBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<ShadowTileBufferData>();

        // xyz: tile bounds in atlas UV space (min xy, length). Shadow sampling is clamped to
        //      this rect so filter kernels cannot bleed into neighbouring tiles.
        // w: per-tile normalBias factor = texelSize * filterSize * sqrt(2).
        //    0 for directional lights, which derive normalBias from cascadeData.data.y instead.
        public Vector4 atlasData;

        public Matrix4x4 shadowMatrix;

        public ShadowTileBufferData(Vector2 tileOffset, float tileScale, float oneDivideAtlasSize,
            float normalBiasFactor, Matrix4x4 matrix)
        {
            float halfTexelSize = oneDivideAtlasSize * 0.5f;
            atlasData.x = tileOffset.x * tileScale + halfTexelSize;
            atlasData.y = tileOffset.y * tileScale + halfTexelSize;
            atlasData.z = tileScale - halfTexelSize - halfTexelSize;
            atlasData.w = normalBiasFactor;
            shadowMatrix = matrix;
        }
    }
}
