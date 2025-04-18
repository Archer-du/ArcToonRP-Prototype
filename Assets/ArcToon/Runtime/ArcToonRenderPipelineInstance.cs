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
        
        private CameraRenderer cameraRenderer;

        private ShadowSettings globalShadowSettings;
        private PostFXSettings globalPostFXSettings;
        private CameraBufferSettings cameraBufferSettings;

        public ArcToonRenderPipelineInstance(ShadowSettings globalShadowSettings, PostFXSettings globalPostFXSettings,
            CameraBufferSettings cameraBufferSettings,
            Shader cameraCopyShader, bool enableSRPBatcher)
        {
            cameraRenderer = new CameraRenderer(cameraCopyShader);
            this.globalShadowSettings = globalShadowSettings;
            this.globalPostFXSettings = globalPostFXSettings;
            this.cameraBufferSettings = cameraBufferSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = enableSRPBatcher;
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
                cameraRenderer.Render(renderGraph, renderContext, cameras[i],
                    globalShadowSettings, globalPostFXSettings, cameraBufferSettings);
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