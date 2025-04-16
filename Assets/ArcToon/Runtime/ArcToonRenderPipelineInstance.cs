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
        public bool allowHDR;
        public int colorLUTResolution;
    }

    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private CameraRenderer cameraRenderer = new();

        private ArcToonRenderPipelineParams renderParams;

        private ShadowSettings globalShadowSettings;
        private PostFXSettings globalPostFXSettings;
        private CameraBufferSettings cameraBufferSettings;

        public ArcToonRenderPipelineInstance(ArcToonRenderPipelineParams pipelineParams, 
            ShadowSettings globalShadowSettings, PostFXSettings globalPostFXSettings, CameraBufferSettings cameraBufferSettings)
        {
            renderParams = pipelineParams;
            this.globalShadowSettings = globalShadowSettings;
            this.globalPostFXSettings = globalPostFXSettings;
            this.cameraBufferSettings = cameraBufferSettings;
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
                    renderParams.allowHDR, 
                    renderParams.colorLUTResolution,
                    globalShadowSettings, globalPostFXSettings, cameraBufferSettings);
            }
        }

        public static void ConsumeCommandBuffer(ScriptableRenderContext renderContext, CommandBuffer commandBuffer)
        {
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
    }
}