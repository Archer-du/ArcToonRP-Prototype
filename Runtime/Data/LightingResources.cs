using System;
using ArcToon.Buffers;
using ArcToon.Settings;
using ArcToon.System;
using UnityEngine;

namespace ArcToon.Data
{
    /// <summary>
    /// Lighting data GraphicsBuffers and Forward+ tile buffer.
    /// Used by: LightingPass (exclusively).
    /// </summary>
    public class LightingResources : IDisposable
    {
        public GraphicsBuffer directionalLightData;
        public GraphicsBuffer spotLightData;
        public GraphicsBuffer pointLightData;
        public GraphicsBuffer perObjectShadowCasterData;

        public GraphicsBuffer forwardPlusTileBuffer;

        /// <summary>
        /// Allocate lighting structured buffers.
        /// Called once at pipeline creation.
        /// </summary>
        public void Allocate()
        {
            ReAllocateStructuredBuffer(ref directionalLightData,
                RenderPipelineInfo.MaxDirectionalLightCount, DirectionalLightBufferData.stride);
            ReAllocateStructuredBuffer(ref spotLightData,
                RenderPipelineInfo.MaxSpotLightCount, SpotLightBufferData.stride);
            ReAllocateStructuredBuffer(ref pointLightData,
                RenderPipelineInfo.MaxPointLightCount, PointLightBufferData.stride);
            ReAllocateStructuredBuffer(ref perObjectShadowCasterData,
                RenderPipelineInfo.MaxPerObjectCasterCount, PerObjectCasterBufferData.stride);
        }

        /// <summary>
        /// Allocate forward+ tile buffer with the given tile count and data size.
        /// Called when camera resolution or tile settings change.
        /// </summary>
        public void AllocateForwardPlusTileBuffer(int tileCount, int tileDataSize)
        {
            int requiredCount = tileCount * tileDataSize;
            if (forwardPlusTileBuffer != null && forwardPlusTileBuffer.count >= requiredCount)
                return;

            forwardPlusTileBuffer?.Release();
            forwardPlusTileBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, requiredCount, 4);
        }

        public void Dispose()
        {
            directionalLightData?.Release();
            spotLightData?.Release();
            pointLightData?.Release();
            perObjectShadowCasterData?.Release();
            forwardPlusTileBuffer?.Release();
        }

        private static void ReAllocateStructuredBuffer(ref GraphicsBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count >= count && buffer.stride == stride)
                return;

            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }
    }
}
