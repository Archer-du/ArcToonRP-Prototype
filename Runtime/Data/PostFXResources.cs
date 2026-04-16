using System;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Shared post-processing resources.
    /// - postFXResult: used by both PostProcessPass (new) and PostFXPass (legacy) as the
    ///   final output pointer consumed by CopyFinalPass.
    /// - All other RTHandles (bloom chain, color LUT, FXAA result) are only used by
    ///   Legacy passes (BloomPass, ColorGradingPass, AntiAliasingPass).
    ///   In the new framework, each Processor privately owns its intermediate RTHandles.
    /// </summary>
    public class PostFXResources : IDisposable
    {
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

        /// <summary>
        /// Allocate or re-allocate PostFX RTHandles if the size has changed.
        /// Called each frame alongside camera resource allocation.
        /// </summary>
        public void Allocate(int width, int height, bool useHDR)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            var hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

            // Bloom
            RenderTextureDescriptor bloomDesc = new(width, height, colorFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref bloomPrefilter, bloomDesc, name: "Bloom Prefilter");
            RenderingUtils.ReAllocateIfNeeded(ref bloomResult, bloomDesc, name: "Bloom Result");
            // Bloom pyramid is allocated on-demand in BloomPass based on actual step count

            // Color Grading result
            RenderTextureDescriptor cgDesc = new(width, height, hdrFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref colorGradingResult, cgDesc, name: "Color Grading");

            // FXAA result
            RenderTextureDescriptor fxaaDesc = new(width, height, colorFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref fxaaResult, fxaaDesc, name: "FXAA Result");
        }

        /// <summary>
        /// Allocate or re-allocate a bloom pyramid level RTHandle.
        /// Called by BloomPass during setup based on actual pyramid dimensions.
        /// </summary>
        public void AllocateBloomPyramidLevel(int index, int width, int height, bool useHDR, string name)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            RenderTextureDescriptor desc = new(width, height, colorFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref bloomPyramid[index], desc, name: name);
        }

        /// <summary>
        /// Allocate or re-allocate the color LUT RTHandle.
        /// Called by ColorGradingPass during setup based on LUT resolution.
        /// </summary>
        public void AllocateColorLUT(int lutWidth, int lutHeight)
        {
            var hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            RenderTextureDescriptor desc = new(lutWidth, lutHeight, hdrFormat, 0);
            RenderingUtils.ReAllocateIfNeeded(ref colorLUT, desc, name: "Color LUT");
        }

        public void Dispose()
        {
            bloomPrefilter?.Release();
            for (int i = 0; i < bloomPyramid.Length; i++)
            {
                bloomPyramid[i]?.Release();
            }
            bloomResult?.Release();
            colorLUT?.Release();
            colorGradingResult?.Release();
            fxaaResult?.Release();
            // Note: postFXResult is not released here because it is an alias
            // pointing to one of the above handles (or to CameraAttachments.colorAttachment).
        }
    }
}
