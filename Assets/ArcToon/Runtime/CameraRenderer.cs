using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        private const string bufferName = "ArcToon Render Camera";

        static CameraSettings defaultCameraSettings = new();

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;

        private Camera camera;
        private CullingResults cullingResults;

        private Lighting lighting;

        private PostFXStack postFXStack;

        private bool useHDR;
        private bool useScaledRendering;

        private static ShaderTagId[] shaderTagIds =
        {
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };

        Vector2Int bufferSize;

        public CameraRenderer()
        {
            commandBuffer = new()
            {
                name = bufferName
            };
            lighting = new();
            postFXStack = new();
        }

        public void Render(ScriptableRenderContext context, Camera camera,
            bool enableInstancing,
            bool allowHDR,
            int colorLUTResolution,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings, CameraBufferSettings cameraBufferSettings)
        {
            this.context = context;
            this.camera = camera;
            this.useHDR = allowHDR && camera.allowHDR;
            
            var customCameraData = camera.GetComponent<ArcToonAdditiveCameraData>();
            CameraSettings cameraSettings =
                customCameraData ? customCameraData.Settings : defaultCameraSettings;

            if (cameraSettings.overridePostFX)
            {
                postFXSettings = cameraSettings.postFXSettings;
            }

            // editor
            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull(shadowSettings.maxDistance)) return;

            lighting.Setup(context, cullingResults, shadowSettings);

            context.SetupCameraProperties(camera);
            postFXStack.Setup(context, commandBuffer, camera,
                postFXSettings,
                useHDR,
                colorLUTResolution,
                cameraSettings.finalBlendMode);
            var flags = postFXStack.GetClearFlags();

            commandBuffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

            // render
            DrawVisibleGeometry(enableInstancing);
            DrawUnsupportedGeometry();
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);

            // post processing
            DrawGizmosBeforeFX();
            postFXStack.Render();
            DrawGizmosAfterFX();
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);

            // clean up
            Cleanup();

            // submit
            context.Submit();
        }

        void Cleanup()
        {
            lighting.CleanUp();
            postFXStack.CleanUp();
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
                PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                PerObjectData.LightProbeProxyVolume |
                PerObjectData.OcclusionProbeProxyVolume |
                PerObjectData.ReflectionProbes;

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