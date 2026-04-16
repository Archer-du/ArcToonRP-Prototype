using System.Collections.Generic;
using ArcToon.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon
{
    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private CameraRenderer cameraRenderer;

        public ArcToonRenderPipelineInstance(RenderPipelineConfig config)
        {
            cameraRenderer = new CameraRenderer(config);
            
            GraphicsSettings.useScriptableRenderPipelineBatching = config.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraRenderer.Render(renderContext, cameras[i]);
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