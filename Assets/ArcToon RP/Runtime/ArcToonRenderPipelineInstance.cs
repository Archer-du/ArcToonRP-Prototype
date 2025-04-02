using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon_RP.Runtime
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
    }
}