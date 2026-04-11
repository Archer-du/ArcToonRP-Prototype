using System.Collections.Generic;
using ArcToon.Runtime;
using ArcToon.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon
{
    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private readonly RenderPipelineConfig config;
        
        private CameraRenderer cameraRenderer;

        public ArcToonRenderPipelineInstance(RenderPipelineConfig config)
        {
            this.config = config;
            cameraRenderer = new CameraRenderer();
            
            GraphicsSettings.useScriptableRenderPipelineBatching = config.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraRenderer.Render(renderContext, cameras[i], config);
            }
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeForEditor();
            cameraRenderer.Dispose();
        }
    }
}