using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace ArcToon.Runtime.Jobs
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    public struct ForwardPlusTileBoundJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<float4> spotLightBounds;
        [ReadOnly]
        public NativeArray<float4> pointLightBounds;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> tileData;

        public int spotLightCount;
        public int pointLightCount;

        public float2 tileScreenUVSize;

        public int maxLightCountPerTile;

        public int tilesPerRow;

        public int tileDataSize;

        public void Execute(int tileIndex)
        {
            int y = tileIndex / tilesPerRow;
            int x = tileIndex - y * tilesPerRow;
            var bounds = float4(x, y, x + 1, y + 1) * tileScreenUVSize.xyxy;

            int headerIndex = tileIndex * tileDataSize;
            int tailIndex = tileIndex * tileDataSize + tileDataSize - 1;
            int dataIndex = headerIndex;
            int lightCountPerTile = 0;

            for (int spotLightIndex = 0; spotLightIndex < spotLightCount; spotLightIndex++)
            {
                float4 b = spotLightBounds[spotLightIndex];
                // if tile inside light bound
                if (all(float4(b.xy, bounds.xy) <= float4(bounds.zw, b.zw)))
                {
                    tileData[++dataIndex] = spotLightIndex;
                    if (++lightCountPerTile >= maxLightCountPerTile)
                    {
                        break;
                    }
                }
            }

            int spotLightCountPerTile = lightCountPerTile;
            // spot light count
            tileData[tailIndex] = spotLightCountPerTile;
            
            for (int pointLightIndex = 0; pointLightIndex < pointLightCount; pointLightIndex++)
            {
                float4 b = pointLightBounds[pointLightIndex];
                // if tile inside light bound
                if (all(float4(b.xy, bounds.xy) <= float4(bounds.zw, b.zw)))
                {
                    tileData[++dataIndex] = pointLightIndex;
                    if (++lightCountPerTile >= maxLightCountPerTile)
                    {
                        break;
                    }
                }
            }
            
            // point light count
            tileData[headerIndex] = lightCountPerTile - spotLightCountPerTile;
        }
    }
}