using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Utils
{
    /// <summary>
    /// Shared RTHandle allocation utilities used by all resource sub-classes.
    /// Extracted from the old monolithic RenderResources to avoid code duplication.
    /// </summary>
    public static class RenderingUtils
    {
        /// <summary>
        /// Re-allocate fixed-size RTHandle if it is not allocated or doesn't match the descriptor.
        /// Simplified version of URP's RenderingUtils.ReAllocateHandleIfNeeded,
        /// without RTHandle pool recycling (not needed for custom SRP).
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="descriptor">RenderTextureDescriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done.</returns>
        public static bool ReAllocateIfNeeded(
            ref RTHandle handle,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            if (NeedsReAlloc(handle, descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name))
            {
                handle?.Release();
                handle = RTHandles.Alloc(descriptor.width, descriptor.height,
                    CreateAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if an RTHandle needs re-allocation based on the given descriptor.
        /// Mirrors URP's RTHandleNeedsReAlloc logic for fixed-size textures.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsReAlloc(
            RTHandle handle,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            int anisoLevel,
            float mipMapBias,
            string name)
        {
            if (handle == null || handle.rt == null)
                return true;

            var rt = handle.rt;
            var rtDesc = rt.descriptor;
            var rtFormat = rtDesc.depthStencilFormat != GraphicsFormat.None
                ? rtDesc.depthStencilFormat
                : rtDesc.graphicsFormat;
            var requestedFormat = descriptor.depthStencilFormat != GraphicsFormat.None
                ? descriptor.depthStencilFormat
                : descriptor.graphicsFormat;

            if (rt.width != descriptor.width || rt.height != descriptor.height)
                return true;

            return
                rtFormat != requestedFormat ||
                rtDesc.dimension != descriptor.dimension ||
                rtDesc.volumeDepth != descriptor.volumeDepth ||
                rtDesc.enableRandomWrite != descriptor.enableRandomWrite ||
                rtDesc.useMipMap != descriptor.useMipMap ||
                rtDesc.autoGenerateMips != descriptor.autoGenerateMips ||
                (rtDesc.shadowSamplingMode != ShadowSamplingMode.None) !=
                    (descriptor.shadowSamplingMode != ShadowSamplingMode.None) ||
                rtDesc.msaaSamples != descriptor.msaaSamples ||
                rtDesc.bindMS != descriptor.bindMS ||
                rtDesc.useDynamicScale != descriptor.useDynamicScale ||
                rtDesc.memoryless != descriptor.memoryless ||
                rt.filterMode != filterMode ||
                rt.wrapMode != wrapMode ||
                rt.anisoLevel != anisoLevel ||
                Mathf.Abs(rt.mipMapBias - mipMapBias) > Mathf.Epsilon ||
                handle.name != name;
        }

        /// <summary>
        /// Create RTHandleAllocInfo from a RenderTextureDescriptor.
        /// Mirrors URP's CreateRTHandleAllocInfo logic.
        /// NOTE: RTHandleAllocInfo in the current SRP version does not have an isShadowMap field,
        /// and RTHandles.Alloc(int, int, RTHandleAllocInfo) hardcodes isShadowMap = false internally.
        /// Shadow maps must be allocated via RTHandles.Alloc overloads that accept isShadowMap parameter directly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RTHandleAllocInfo CreateAllocInfo(
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            int anisoLevel,
            float mipMapBias,
            string name)
        {
            var actualFormat = descriptor.graphicsFormat != GraphicsFormat.None
                ? descriptor.graphicsFormat
                : descriptor.depthStencilFormat;

            RTHandleAllocInfo allocInfo = new RTHandleAllocInfo();
            allocInfo.slices = descriptor.volumeDepth;
            allocInfo.format = actualFormat;
            allocInfo.filterMode = filterMode;
            allocInfo.wrapModeU = wrapMode;
            allocInfo.wrapModeV = wrapMode;
            allocInfo.wrapModeW = wrapMode;
            allocInfo.dimension = descriptor.dimension;
            allocInfo.enableRandomWrite = descriptor.enableRandomWrite;
            allocInfo.useMipMap = descriptor.useMipMap;
            allocInfo.autoGenerateMips = descriptor.autoGenerateMips;
            allocInfo.anisoLevel = anisoLevel;
            allocInfo.mipMapBias = mipMapBias;
            allocInfo.msaaSamples = (MSAASamples)descriptor.msaaSamples;
            allocInfo.bindTextureMS = descriptor.bindMS;
            allocInfo.useDynamicScale = descriptor.useDynamicScale;
            allocInfo.memoryless = descriptor.memoryless;
            allocInfo.vrUsage = descriptor.vrUsage;
            allocInfo.name = name;

            return allocInfo;
        }
    }
}
