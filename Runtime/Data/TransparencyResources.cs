using System;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Transparency rendering RTHandles: weighted average and depth peeling buffers.
    /// Used by: TransparentPass (exclusively).
    /// </summary>
    public class TransparencyResources : IDisposable
    {
        // Weighted Average
        public RTHandle waAccumulateRGBA;
        public RTHandle waRevealage;
        public RTHandle waBackgroundColor;

        // Depth Peeling
        public const int DepthPeelingLayers = 4;
        public RTHandle dpOpaqueColorBuffer;
        public RTHandle dpCompositeArray;
        public RTHandle[] dpDualDepthBuffer = new RTHandle[2];

        /// <summary>
        /// Allocate or re-allocate transparency rendering RTHandles if the size has changed.
        /// Called each frame alongside camera resource allocation.
        /// </summary>
        public void Allocate(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            // Weighted Average
            RenderTextureDescriptor waAccumDesc = new(width, height, colorFormat, 0);
            RenderTextureDescriptor waRevealDesc = new(width, height, GraphicsFormat.R16_UNorm, 0);
            RenderTextureDescriptor waBgDesc = new(width, height, colorFormat, 0);

            RenderingUtils.ReAllocateIfNeeded(ref waAccumulateRGBA, waAccumDesc, name: "Weighted Average Accumulate RGBA");
            RenderingUtils.ReAllocateIfNeeded(ref waRevealage, waRevealDesc, name: "Weighted Average Revealage");
            RenderingUtils.ReAllocateIfNeeded(ref waBackgroundColor, waBgDesc, name: "Weighted Average Background Color");

            // Depth Peeling
            RenderTextureDescriptor dpCompositeDesc = new(width, height, colorFormat, 0)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = 6,
            };
            // Match depthAttachment format (D32_SFloat_S8_UInt) as old RenderGraph code
            // used depthAttachment.GetDescriptor() to create these buffers.
            RenderTextureDescriptor dpDepthDesc = new(width, height, GraphicsFormat.None, GraphicsFormat.D32_SFloat_S8_UInt);
            RenderTextureDescriptor dpColorDesc = new(width, height, colorFormat, 0);

            RenderingUtils.ReAllocateIfNeeded(ref dpCompositeArray, dpCompositeDesc, name: "Depth Peeling Composite Array");
            RenderingUtils.ReAllocateIfNeeded(ref dpDualDepthBuffer[0], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 0");
            RenderingUtils.ReAllocateIfNeeded(ref dpDualDepthBuffer[1], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 1");
            RenderingUtils.ReAllocateIfNeeded(ref dpOpaqueColorBuffer, dpColorDesc, name: "Depth Peeling Opaque Color Buffer");
        }

        public void Dispose()
        {
            waAccumulateRGBA?.Release();
            waRevealage?.Release();
            waBackgroundColor?.Release();
            dpOpaqueColorBuffer?.Release();
            dpCompositeArray?.Release();
            dpDualDepthBuffer[0]?.Release();
            dpDualDepthBuffer[1]?.Release();
        }
    }
}
