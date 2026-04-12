using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Buffers
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct ShadowTileBufferData
    {
        public static readonly int stride = UnsafeUtility.SizeOf<ShadowTileBufferData>();

        // x: tile border start x (0 for directional)
        // y: tile border start y (0 for directional)
        // z: tile border length (0 for directional)
        // w: normalBias per-tile factor = texelSize * filterSize * sqrt(2)
        //    (0 for directional — directional uses cascadeData.data.y instead)
        public Vector4 atlasData;

        public Matrix4x4 shadowMatrix;

        /// <summary>
        /// Constructor for directional light tiles (no tile bounds, no per-tile normalBias factor).
        /// </summary>
        public ShadowTileBufferData(Matrix4x4 matrix)
        {
            atlasData = Vector4.zero;
            shadowMatrix = matrix;
        }

        /// <summary>
        /// Constructor for punctual light tiles (spot / point, with tile bounds and per-tile normalBias factor).
        /// </summary>
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
