using System.Collections.Generic;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public partial class ArcToonRenderPipelineInstance : RenderPipeline
    {
        private readonly RenderPipelineConfig config;
        
        private readonly RenderGraph renderGraph;
        
        private CameraRenderer cameraRenderer;

        public ArcToonRenderPipelineInstance(RenderPipelineConfig config)
        {
            this.config = config;
            renderGraph = new RenderGraph("Arc Toon Render Graph");
            cameraRenderer = new CameraRenderer();
            
            GraphicsSettings.useScriptableRenderPipelineBatching = config.useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameraRenderer.Render(renderGraph, renderContext, cameras[i], config);
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