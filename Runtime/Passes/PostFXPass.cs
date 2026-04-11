using ArcToon.Runtime.Passes.PostProcessing;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class PostFXPass : RenderPassBase
    {
        public override string Name => "Post FX";

        private readonly BloomPass bloomPass = new();
        private readonly ColorGradingPass colorGradingPass = new();
        private readonly AntiAliasingPass antiAliasingPass = new();

        public override void Execute(CommandBuffer cmd, ScriptableRenderContext context)
        {
            bool hasActivePostFX =
                renderer.PostFXConfig != null && PostFXConfig.AreApplicableTo(Camera);
            if (!hasActivePostFX)
            {
                // No PostFX: point postFXResult to colorAttachment so CopyFinalPass can read it
                resources.PostFX.postFXResult = resources.Camera.colorAttachment;
                return;
            }

            PostFXStack postFXStack = new PostFXStack(renderer.PostFXConfig.PostProcessStackMaterial);

            // Track the current source handle through the chain
            RTHandle currentSource = resources.Camera.colorAttachment;

            // Bloom
            if (bloomPass.Execute(cmd, resources, renderer, renderer.PostFXConfig, postFXStack))
            {
                currentSource = resources.PostFX.bloomResult;
            }

            // Color Grading (always runs when PostFX is active)
            colorGradingPass.Execute(cmd, resources, renderer, renderer.PostFXConfig, postFXStack, currentSource);
            currentSource = resources.PostFX.colorGradingResult;

            // Anti-Aliasing (FXAA)
            if (antiAliasingPass.Execute(cmd, resources, renderer, postFXStack, currentSource))
            {
                currentSource = resources.PostFX.fxaaResult;
            }

            // Set the final PostFX result for CopyFinalPass
            resources.PostFX.postFXResult = currentSource;
        }
    }
}