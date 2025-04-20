using System.Collections.Generic;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        readonly RenderGraph renderGraph = new("Arc Toon Render Graph");
        
        readonly RenderPipelineSettings settings;
        
        private CameraRenderer cameraRenderer;

        public ArcToonRenderPipelineInstance(RenderPipelineSettings settings)
        {
            this.settings = settings;
            cameraRenderer = new CameraRenderer(settings.cameraCopyShader, settings.cameraDebugShader);
            
            GraphicsSettings.useScriptableRenderPipelineBatching = settings.useSRPBatcher;
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
                cameraRenderer.Render(renderGraph, renderContext, cameras[i], settings);
            }
            renderGraph.EndFrame();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeForEditor();
            cameraRenderer.Dispose();
            renderGraph.Cleanup();
        }
    }
}