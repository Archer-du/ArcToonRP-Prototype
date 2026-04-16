using System.Collections.Generic;
using ArcToon.Config;
using ArcToon.Data;
using ArcToon.Passes.PostProcessing;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    /// <summary>
    /// Post-processing dispatcher with Ping-Pong double buffering.
    /// Owns TempRTA/TempRTB and manages the chain execution.
    /// Each processor only sees (source, destination) — the ping-pong is transparent.
    ///
    /// Processors are owned by their VolumeConfigs (lazily created).
    /// This pass acquires active processors each frame for rendering.
    /// On Dispose, this pass is responsible for releasing all processor resources.
    /// Execution order follows volumeConfigs order (already sorted by the Editor).
    /// </summary>
    public class PostProcessPass : RenderPassBase
    {
        public override string Name => "Post Process";

        private PostProcessConfig PostProcessConfig => renderer.PostProcessConfig;
        
        private readonly List<PostProcessor> activeProcessors = new();

        private RTHandle tempRTA;
        private RTHandle tempRTB;

        private const string TempRTAName = "_PostProcess_TempA";
        private const string TempRTBName = "_PostProcess_TempB";

        public override void Initialize(RenderResources resources, CameraRenderer renderer)
        {
            base.Initialize(resources, renderer);
            CollectActiveProcessors();
        }

        public override void SetupFrameData(CommandBuffer cmd)
        {
            base.SetupFrameData(cmd);
            foreach (var activeProcessor in activeProcessors)
            {
                activeProcessor.Setup(renderer);
            }
        }

        public override void Execute(CommandBuffer cmd, ScriptableRenderContext context)
        {
            if (activeProcessors.Count == 0)
            {
                resources.PostFX.postFXResult = resources.Camera.colorAttachment;
                return;
            }

            // Allocate ping-pong buffers matching camera attachment format
            var desc = GetPostFXDescriptor();
            RenderingUtils.ReAllocateIfNeeded(ref tempRTA, desc, name: TempRTAName);

            if (activeProcessors.Count == 1)
            {
                // Single effect optimization: source → TempRTA directly, no extra blit
                using (new ProfilingScope(cmd, activeProcessors[0].Sampler))
                {
                    activeProcessors[0].Render(cmd, resources.Camera.colorAttachment, tempRTA);
                }
            }
            else
            {
                // Multiple effects: blit source → TempRTA, then ping-pong through chain
                RenderingUtils.ReAllocateIfNeeded(ref tempRTB, desc, name: TempRTBName);
                BlitUtils.CopyTexture(cmd, resources.Camera.colorAttachment, tempRTA, BlitUtils.BlitMode.Color);

                foreach (var processor in activeProcessors)
                {
                    using (new ProfilingScope(cmd, processor.Sampler))
                    {
                        processor.Render(cmd, tempRTA, tempRTB);
                    }
                    CoreUtils.Swap(ref tempRTA, ref tempRTB);
                }
            }

            // TempRTA always hold the final result after the loop (or single effect)
            resources.PostFX.postFXResult = tempRTA;
        }

        public override void Dispose()
        {
            tempRTA?.Release();
            tempRTB?.Release();

            // Release resources owned by processors (RTHandles like bloom pyramid, color LUT, etc.)
            if (PostProcessConfig != null)
            {
                foreach (var volumeConfig in PostProcessConfig.volumeConfigs)
                {
                    volumeConfig?.Processor.Dispose();
                }
            }
        }

        private void CollectActiveProcessors()
        {
            activeProcessors.Clear();

            if (PostProcessConfig == null || !PostProcessConfig.AreApplicableTo(Camera))
            {
                return;
            }

            foreach (var volumeConfig in PostProcessConfig.volumeConfigs)
            {
                if (volumeConfig == null) continue;
                var processor = volumeConfig.Processor;
                if (processor.IsActive(renderer))
                {
                    activeProcessors.Add(processor);
                }
            }
        }
        
        private RenderTextureDescriptor GetPostFXDescriptor()
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(
                renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            return new RenderTextureDescriptor(
                AttachmentSize.x, AttachmentSize.y, colorFormat, 0)
            {
                msaaSamples = 1
            };
        }
    }
}
