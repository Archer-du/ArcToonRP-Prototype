using System.Collections.Generic;
using ArcToon.Config;
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
    /// </summary>
    public class PostProcessPass : RenderPassBase
    {
        public override string Name => "Post Process";

        private readonly List<PostProcessor> processors = new();
        private readonly List<PostProcessor> activeProcessors = new();
        private readonly List<ProfilingSampler> profilingSamplers = new();

        private RTHandle tempRTA;
        private RTHandle tempRTB;

        private const string TempRTAName = "_PostProcess_TempA";
        private const string TempRTBName = "_PostProcess_TempB";

        public PostProcessPass()
        {
            // Register built-in processors
            RegisterProcessor(new BloomProcessor());
            RegisterProcessor(new ColorGradingProcessor());
            RegisterProcessor(new FXAAProcessor());
        }

        /// <summary>
        /// Register a processor into the chain. Maintains sorted order by Order.
        /// </summary>
        public void RegisterProcessor(PostProcessor processor)
        {
            processors.Add(processor);
            profilingSamplers.Add(new ProfilingSampler(processor.Name));
            // Keep sorted by Order
            SortProcessors();
        }

        private void SortProcessors()
        {
            // Sort both lists together by processor Order
            for (int i = 0; i < processors.Count - 1; i++)
            {
                for (int j = i + 1; j < processors.Count; j++)
                {
                    if (processors[j].Order < processors[i].Order)
                    {
                        (processors[i], processors[j]) = (processors[j], processors[i]);
                        (profilingSamplers[i], profilingSamplers[j]) = (profilingSamplers[j], profilingSamplers[i]);
                    }
                }
            }
        }

        public override void Execute(CommandBuffer cmd, ScriptableRenderContext context)
        {
            var config = renderer.PostProcessConfig;
            if (config == null || !PostProcessConfig.AreApplicableTo(Camera))
            {
                // No PostFX: point postFXResult to colorAttachment so CopyFinalPass can read it
                resources.PostFX.postFXResult = resources.Camera.colorAttachment;
                return;
            }

            // Collect active processors for this frame
            activeProcessors.Clear();
            for (int i = 0; i < processors.Count; i++)
            {
                if (processors[i].IsActive(config, renderer))
                {
                    processors[i].Setup(config, renderer);
                    activeProcessors.Add(processors[i]);
                }
            }

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
                // Single effect optimization: source → TempA directly, no extra blit
                int procIndex = processors.IndexOf(activeProcessors[0]);
                using (new ProfilingScope(cmd, profilingSamplers[procIndex]))
                {
                    activeProcessors[0].Render(cmd, resources.Camera.colorAttachment, tempRTA);
                }
            }
            else
            {
                // Multiple effects: blit source → TempA, then ping-pong through chain
                RenderingUtils.ReAllocateIfNeeded(ref tempRTB, desc, name: TempRTBName);
                PostFXUtility.Blit(cmd, resources.Camera.colorAttachment, tempRTA);

                foreach (var processor in activeProcessors)
                {
                    int procIndex = processors.IndexOf(processor);
                    using (new ProfilingScope(cmd, profilingSamplers[procIndex]))
                    {
                        processor.Render(cmd, tempRTA, tempRTB);
                    }
                    CoreUtils.Swap(ref tempRTA, ref tempRTB);
                }
            }

            // After the loop (or single effect), TempA holds the final result
            resources.PostFX.postFXResult = tempRTA;
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

        public override void Dispose()
        {
            tempRTA?.Release();
            tempRTB?.Release();
            foreach (var p in processors)
            {
                p.Dispose();
            }
        }
    }
}
