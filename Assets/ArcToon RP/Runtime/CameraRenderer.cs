using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon_RP.Runtime
{
    public partial class CameraRenderer 
    {
        private ScriptableRenderContext context;

        private Camera camera;

        private const string bufferName = "ArcToon Render Camera";
        
        CullingResults cullingResults;
        
        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        
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
            
            commandBuffer.BeginSample(sampleName);
            ConsumeCommandBuffer();

            DrawVisibleGeometry(enableInstancing);
            DrawUnsupportedGeometry();
            DrawGizmos();
            
            // submit
            commandBuffer.EndSample(sampleName);
            ConsumeCommandBuffer();
            context.Submit();
        }

        private void DrawVisibleGeometry(bool enableInstancing) 
        {
            // render opaque
            RendererListDesc desc = new(unlitShaderTagId, cullingResults, camera)
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

        private void ConsumeCommandBuffer()
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
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