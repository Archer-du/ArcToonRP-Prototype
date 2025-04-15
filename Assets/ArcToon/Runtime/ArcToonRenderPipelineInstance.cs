using System.Collections.Generic;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public struct ArcToonRenderPipelineParams
    {
        public bool enableSRPBatcher;
        public bool enableGPUInstancing;
    }

    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private CameraRenderer cameraRenderer = new();

        private ArcToonRenderPipelineParams renderParams;

        private ShadowSettings shadowSettings;
        private PostFXSettings postFXSettings;
        
        public ArcToonRenderPipelineInstance(ArcToonRenderPipelineParams pipelineParams, 
            ShadowSettings shadowSettings, PostFXSettings postFXSettings)
        {
            renderParams = pipelineParams;
            this.shadowSettings = shadowSettings;
            this.postFXSettings = postFXSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = pipelineParams.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
            
            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraRenderer.Render(renderContext, cameras[i], 
                    renderParams.enableGPUInstancing, 
                    shadowSettings, postFXSettings);
            }
        }

        public static void ConsumeCommandBuffer(ScriptableRenderContext renderContext, CommandBuffer commandBuffer)
        {
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
    }
}