using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer 
    {
        private ScriptableRenderContext context;

        private Camera camera;

        private const string bufferName = "ArcToon Render Camera";
        
        private CullingResults cullingResults;
        
        private Lighting lighting = new();
        
        private static ShaderTagId[] shaderTagIds = 
        {
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };
        
        private CommandBuffer commandBuffer = new()
        {
            name = bufferName
        };
        
        public void Render(ScriptableRenderContext context, Camera camera, bool enableInstancing) 
        {
            this.context = context;
            this.camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull()) return;
            
            // set up
            context.SetupCameraProperties(camera);
            var flags = camera.clearFlags;
            commandBuffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ?
                    camera.backgroundColor.linear : Color.clear);
            lighting.Setup(context, cullingResults);
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
            
            commandBuffer.BeginSample(sampleName);

            DrawVisibleGeometry(enableInstancing);
            DrawUnsupportedGeometry();
            DrawGizmos();
            
            commandBuffer.EndSample(sampleName);
            
            // submit
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
            context.Submit();
        }

        private void DrawVisibleGeometry(bool enableInstancing) 
        {
            // render opaque
            RendererListDesc desc = new(shaderTagIds, cullingResults, camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            };
            var renderParams = RendererListDesc.ConvertToParameters(desc);
            renderParams.drawSettings.enableInstancing = enableInstancing;
            
            commandBuffer.DrawRendererList(context.CreateRendererList(ref renderParams));
            
            // render skybox
            commandBuffer.DrawRendererList(context.CreateSkyboxRendererList(camera));
            
            // render transparent
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            renderParams.drawSettings.sortingSettings = sortingSettings;
            renderParams.filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            renderParams.drawSettings.enableInstancing = enableInstancing;

            commandBuffer.DrawRendererList(context.CreateRendererList(ref renderParams));
        }

        private bool Cull() 
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) 
            {
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }
    }
}