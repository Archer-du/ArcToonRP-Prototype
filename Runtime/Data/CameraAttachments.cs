using System;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Camera attachment RTHandles: color, depth, pre-depth-stencil copy, and stencil mask.
    /// Used by: SetupPass, DepthStencilPrePass, TransparentPass, GizmosPass.
    /// </summary>
    public class CameraAttachments : IDisposable
    {
        public RTHandle colorAttachment;
        public RTHandle depthAttachment;
        public RTHandle preDepthStencil;
        public RTHandle stencilMask;

        /// <summary>
        /// Allocate or re-allocate camera attachment RTHandles if the size has changed.
        /// Called each frame before rendering.
        /// </summary>
        public void Allocate(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            RenderTextureDescriptor colorDesc = new(width, height, colorFormat, 0);
            // Use D32_SFloat_S8_UInt to match RenderGraph's DepthBits.Depth32 behavior.
            // GraphicsFormatUtility.GetDepthStencilFormat(32) returns D32_SFloat_S8_UInt on most platforms.
            RenderTextureDescriptor depthStencilDesc = new(width, height, GraphicsFormat.None, GraphicsFormat.D32_SFloat_S8_UInt);
            RenderTextureDescriptor stencilMaskDesc = new(width, height, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR), 0);

            RenderingUtils.ReAllocateIfNeeded(ref colorAttachment, colorDesc, name: "Color Attachment Buffer");
            RenderingUtils.ReAllocateIfNeeded(ref depthAttachment, depthStencilDesc, name: "Depth Attachment Buffer");
            RenderingUtils.ReAllocateIfNeeded(ref preDepthStencil, depthStencilDesc, name: "Depth Copy");
            RenderingUtils.ReAllocateIfNeeded(ref stencilMask, stencilMaskDesc, name: "Stencil Mask");
        }

        public void Dispose()
        {
            colorAttachment?.Release();
            depthAttachment?.Release();
            preDepthStencil?.Release();
            stencilMask?.Release();
        }
    }
}
