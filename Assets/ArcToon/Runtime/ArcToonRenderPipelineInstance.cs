using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public struct ArcToonRenderPipelineParams
    {
        public bool enableSRPBatcher;
        public bool enableGPUInstancing;
    }
    public class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private CameraRenderer renderer = new();

        private ArcToonRenderPipelineParams renderParams;
        public ArcToonRenderPipelineInstance(ArcToonRenderPipelineParams pipelineParams)
        {
            renderParams = pipelineParams;
            GraphicsSettings.useScriptableRenderPipelineBatching = pipelineParams.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
        }
        
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }
        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++) 
            {
                renderer.Render(renderContext, cameras[i], renderParams.enableGPUInstancing);
            }
        }

        public static void ConsumeCommandBuffer(ScriptableRenderContext renderContext, CommandBuffer commandBuffer)
        {
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
    }
}