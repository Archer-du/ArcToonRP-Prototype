using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        private const string bufferName = "ArcToon Render Camera";

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;

        private Camera camera;
        private CullingResults cullingResults;

        private Lighting lighting;

        private static ShaderTagId[] shaderTagIds =
        {
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };

        public CameraRenderer()
        {
            commandBuffer = new()
            {
                name = bufferName
            };
            lighting = new();
        }

        public void Render(ScriptableRenderContext context, Camera camera, bool enableInstancing,
            ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;

            // 1.editor
            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull(shadowSettings.maxDistance)) return;

            // 2.set up
            lighting.Setup(context, cullingResults, shadowSettings);

            context.SetupCameraProperties(camera);
            var flags = camera.clearFlags;
            commandBuffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

            // 3.render
            DrawVisibleGeometry(enableInstancing);
            DrawUnsupportedGeometry();

            // 4.clean up
            lighting.CleanUp();

            // 5.submit
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
            DrawGizmos();
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
            renderParams.drawSettings.perObjectData = 
                PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume;

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

        private bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref p);
                return true;
            }

            return false;
        }
    }
}