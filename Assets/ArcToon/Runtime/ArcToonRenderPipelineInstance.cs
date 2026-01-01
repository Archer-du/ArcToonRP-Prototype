using System.Collections.Generic;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private readonly RenderPipelineSettings settings;
        
        private readonly RenderGraph renderGraph;
        
        private CameraRenderer cameraRenderer;

        public ArcToonRenderPipelineInstance(RenderPipelineSettings settings)
        {
            this.settings = settings;
            renderGraph = new RenderGraph("Arc Toon Render Graph");
            cameraRenderer = new CameraRenderer(settings.cameraDebugShader);
            
            GraphicsSettings.useScriptableRenderPipelineBatching = settings.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraRenderer.Render(renderGraph, renderContext, cameras[i], settings);
            }
            renderGraph.EndFrame();
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
            renderGraph.Cleanup();
        }
    }
}