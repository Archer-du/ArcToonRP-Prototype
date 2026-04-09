using System;
using System.Runtime.CompilerServices;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Data
{
    /// <summary>
    /// Persistent render resources managed by RTHandle system and GraphicsBuffer.
    /// Replaces the old RenderGraphResourceHandle — resources are allocated once,
    /// resized on demand, and released on dispose.
    /// </summary>
    public class RenderResources : IDisposable
    {
        #region Camera Attachments

        public RTHandle colorAttachment;
        public RTHandle depthAttachment;
        public RTHandle preDepthStencil;
        public RTHandle stencilMask;

        #endregion

        #region Shadow Atlases

        public RTHandle directionalShadowAtlas;
        public RTHandle spotShadowAtlas;
        public RTHandle pointShadowAtlas;
        public RTHandle perObjectShadowAtlas;

        /// <summary>
        /// 1x1 default shadow texture used when no shadow casters are present.
        /// Replaces renderGraph.defaultResources.defaultShadowTexture.
        /// </summary>
        public RTHandle defaultShadowTexture;

        #endregion

        #region Shadow Data Buffers

        public GraphicsBuffer cascadeShadowData;
        public GraphicsBuffer directionalShadowData;
        public GraphicsBuffer spotShadowData;
        public GraphicsBuffer pointShadowData;
        public GraphicsBuffer perObjectShadowData;

        #endregion

        #region Lighting Data Buffers

        public GraphicsBuffer directionalLightData;
        public GraphicsBuffer spotLightData;
        public GraphicsBuffer pointLightData;
        public GraphicsBuffer perObjectShadowCasterData;

        #endregion

        #region Forward+ Buffer

        public GraphicsBuffer forwardPlusTileBuffer;

        #endregion

        #region PostFX

        // Bloom
        public const int MaxBloomPyramidLevels = 16;
        public RTHandle bloomPrefilter;
        public RTHandle[] bloomPyramid = new RTHandle[2 * MaxBloomPyramidLevels];
        public RTHandle bloomResult;

        // Color Grading
        public RTHandle colorLUT;
        public RTHandle colorGradingResult;

        // Anti-Aliasing
        public RTHandle fxaaResult;

        /// <summary>
        /// Final post-processing output, points to the last active PostFX result.
        /// Used by CopyFinalPass.
        /// </summary>
        public RTHandle postFXResult;

        #endregion

        #region Transparency

        // Weighted Average
        public RTHandle waAccumulateRGBA;
        public RTHandle waRevealage;
        public RTHandle waBackgroundColor;

        // Depth Peeling
        public const int DepthPeelingLayers = 4;
        public RTHandle dpOpaqueColorBuffer;
        public RTHandle dpCompositeArray;
        public RTHandle[] dpDualDepthBuffer = new RTHandle[2];

        #endregion

        private bool disposed;

        public RenderResources()
        {
            AllocateDefaultShadowTexture();
        }

        /// <summary>
        /// Allocate or re-allocate camera attachment RTHandles if the size has changed.
        /// Called each frame before rendering.
        /// </summary>
        public void AllocateCameraResources(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            RenderTextureDescriptor colorDesc = new(width, height, colorFormat, 0);
            // Use D32_SFloat_S8_UInt to match RenderGraph's DepthBits.Depth32 behavior.
            // GraphicsFormatUtility.GetDepthStencilFormat(32) returns D32_SFloat_S8_UInt on most platforms.
            RenderTextureDescriptor depthStencilDesc = new(width, height, GraphicsFormat.None, GraphicsFormat.D32_SFloat_S8_UInt);
            RenderTextureDescriptor stencilMaskDesc = new(width, height, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR), 0);

            ReAllocateHandleIfNeeded(ref colorAttachment, colorDesc, name: "Color Attachment Buffer");
            ReAllocateHandleIfNeeded(ref depthAttachment, depthStencilDesc, name: "Depth Attachment Buffer");
            ReAllocateHandleIfNeeded(ref preDepthStencil, depthStencilDesc, name: "Depth Copy");
            ReAllocateHandleIfNeeded(ref stencilMask, stencilMaskDesc, name: "Stencil Mask");
        }

        /// <summary>
        /// Allocate shadow atlas RTHandles and structured buffers based on shadow settings.
        /// Called once at pipeline creation or when settings change.
        /// </summary>
        public void AllocateShadowResources(ShadowSettings settings)
        {
            // Shadow Atlases
            ReAllocateShadowAtlas(ref directionalShadowAtlas,
                (int)settings.directionalCascadeShadow.atlasSize, "Directional Shadow Atlas");
            ReAllocateShadowAtlas(ref spotShadowAtlas,
                (int)settings.spotShadow.atlasSize, "Spot Shadow Atlas");
            ReAllocateShadowAtlas(ref pointShadowAtlas,
                (int)settings.pointShadow.atlasSize, "Point Shadow Atlas");
            ReAllocateShadowAtlas(ref perObjectShadowAtlas,
                (int)settings.perObjectShadow.atlasSize, "Per Object Shadow Atlas");

            // Shadow Data Buffers
            ReAllocateStructuredBuffer(ref cascadeShadowData,
                RenderPipelineInfo.MaxCascades, ShadowCascadeBufferData.stride);
            ReAllocateStructuredBuffer(ref directionalShadowData,
                RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades,
                ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref spotShadowData,
                RenderPipelineInfo.MaxShadowedSpotLightCount, ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref pointShadowData,
                RenderPipelineInfo.MaxShadowedPointLightCount * 6, ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref perObjectShadowData,
                RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                RenderPipelineInfo.MaxShadowedDirectionalLightCount,
                ShadowTileBufferData.stride);
        }

        /// <summary>
        /// Allocate lighting structured buffers.
        /// Called once at pipeline creation.
        /// </summary>
        public void AllocateLightingResources()
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

        /// <summary>
        /// Allocate or re-allocate transparency rendering RTHandles if the size has changed.
        /// Called each frame alongside AllocateCameraResources.
        /// </summary>
        public void AllocateTransparencyResources(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            // Weighted Average
            RenderTextureDescriptor waAccumDesc = new(width, height, colorFormat, 0);
            RenderTextureDescriptor waRevealDesc = new(width, height, GraphicsFormat.R16_UNorm, 0);
            RenderTextureDescriptor waBgDesc = new(width, height, colorFormat, 0);

            ReAllocateHandleIfNeeded(ref waAccumulateRGBA, waAccumDesc, name: "Weighted Average Accumulate RGBA");
            ReAllocateHandleIfNeeded(ref waRevealage, waRevealDesc, name: "Weighted Average Revealage");
            ReAllocateHandleIfNeeded(ref waBackgroundColor, waBgDesc, name: "Weighted Average Background Color");

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

            ReAllocateHandleIfNeeded(ref dpCompositeArray, dpCompositeDesc, name: "Depth Peeling Composite Array");
            ReAllocateHandleIfNeeded(ref dpDualDepthBuffer[0], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 0");
            ReAllocateHandleIfNeeded(ref dpDualDepthBuffer[1], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 1");
            ReAllocateHandleIfNeeded(ref dpOpaqueColorBuffer, dpColorDesc, name: "Depth Peeling Opaque Color Buffer");
        }

        /// <summary>
        /// Allocate or re-allocate PostFX RTHandles if the size has changed.
        /// Called each frame alongside AllocateCameraResources.
        /// </summary>
        public void AllocatePostFXResources(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            var hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

            // Bloom
            RenderTextureDescriptor bloomDesc = new(width, height, colorFormat, 0);
            ReAllocateHandleIfNeeded(ref bloomPrefilter, bloomDesc, name: "Bloom Prefilter");
            ReAllocateHandleIfNeeded(ref bloomResult, bloomDesc, name: "Bloom Result");
            // Bloom pyramid is allocated on-demand in BloomPass based on actual step count

            // Color Grading result
            RenderTextureDescriptor cgDesc = new(width, height, hdrFormat, 0);
            ReAllocateHandleIfNeeded(ref colorGradingResult, cgDesc, name: "Color Grading");

            // FXAA result
            RenderTextureDescriptor fxaaDesc = new(width, height, colorFormat, 0);
            ReAllocateHandleIfNeeded(ref fxaaResult, fxaaDesc, name: "FXAA Result");
        }

        /// <summary>
        /// Allocate or re-allocate a bloom pyramid level RTHandle.
        /// Called by BloomPass during setup based on actual pyramid dimensions.
        /// </summary>
        public void AllocateBloomPyramidLevel(int index, int width, int height, bool useHDR, string name)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            RenderTextureDescriptor desc = new(width, height, colorFormat, 0);
            ReAllocateHandleIfNeeded(ref bloomPyramid[index], desc, name: name);
        }

        /// <summary>
        /// Allocate or re-allocate the color LUT RTHandle.
        /// Called by ColorGradingPass during setup based on LUT resolution.
        /// </summary>
        public void AllocateColorLUT(int lutWidth, int lutHeight)
        {
            var hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            RenderTextureDescriptor desc = new(lutWidth, lutHeight, hdrFormat, 0);
            ReAllocateHandleIfNeeded(ref colorLUT, desc, name: "Color LUT");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            // Camera Attachments
            colorAttachment?.Release();
            depthAttachment?.Release();
            preDepthStencil?.Release();
            stencilMask?.Release();

            // Shadow Atlases
            directionalShadowAtlas?.Release();
            spotShadowAtlas?.Release();
            pointShadowAtlas?.Release();
            perObjectShadowAtlas?.Release();
            defaultShadowTexture?.Release();

            // Shadow Data Buffers
            cascadeShadowData?.Release();
            directionalShadowData?.Release();
            spotShadowData?.Release();
            pointShadowData?.Release();
            perObjectShadowData?.Release();

            // Lighting Data Buffers
            directionalLightData?.Release();
            spotLightData?.Release();
            pointLightData?.Release();
            perObjectShadowCasterData?.Release();

            // Forward+ Buffer
            forwardPlusTileBuffer?.Release();

            // PostFX
            bloomPrefilter?.Release();
            for (int i = 0; i < bloomPyramid.Length; i++)
            {
                bloomPyramid[i]?.Release();
            }
            bloomResult?.Release();
            colorLUT?.Release();
            colorGradingResult?.Release();
            fxaaResult?.Release();
            postFXResult?.Release();

            // Transparency
            waAccumulateRGBA?.Release();
            waRevealage?.Release();
            waBackgroundColor?.Release();
            dpOpaqueColorBuffer?.Release();
            dpCompositeArray?.Release();
            dpDualDepthBuffer[0]?.Release();
            dpDualDepthBuffer[1]?.Release();
        }

        #region Private Helpers

        private void AllocateDefaultShadowTexture()
        {
            defaultShadowTexture = RTHandles.Alloc(
                1, 1,
                depthBufferBits: DepthBits.Depth16,
                isShadowMap: true,
                name: "Default Shadow Texture"
            );
        }

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
        internal static bool ReAllocateHandleIfNeeded(
            ref RTHandle handle,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            if (RTHandleNeedsReAlloc(handle, descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name))
            {
                handle?.Release();
                handle = RTHandles.Alloc(descriptor.width, descriptor.height,
                    CreateRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if an RTHandle needs re-allocation based on the given descriptor.
        /// Mirrors URP's RTHandleNeedsReAlloc logic for fixed-size textures.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool RTHandleNeedsReAlloc(
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
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RTHandleAllocInfo CreateRTHandleAllocInfo(
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

        private static void ReAllocateShadowAtlas(ref RTHandle handle, int size, string name)
        {
            if (handle != null && handle.rt != null &&
                handle.rt.width == size && handle.rt.height == size)
                return;

            handle?.Release();
            handle = RTHandles.Alloc(
                size, size,
                depthBufferBits: DepthBits.Depth32,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                isShadowMap: true,
                name: name
            );
        }

        private static void ReAllocateStructuredBuffer(ref GraphicsBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count >= count && buffer.stride == stride)
                return;

            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }

        #endregion
    }
}
