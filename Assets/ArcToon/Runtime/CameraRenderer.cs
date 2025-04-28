using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Passes;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Passes.PostProcess;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        private Camera camera;
        private PerObjectShadowCasterManager perObjectShadowCasterManager = new();

        static CameraSettings defaultCameraSettings = new();

        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

        private Material cameraCopyMaterial;

        public CameraRenderer(Shader cameraCopyShader, Shader cameraDebuggerShader)
        {
            cameraCopyMaterial = CoreUtils.CreateEngineMaterial(cameraCopyShader);
            CameraDebugger.Initialize(cameraDebuggerShader);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(cameraCopyMaterial);
            CameraDebugger.Cleanup();
        }


        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera,
            RenderPipelineSettings settings)
        {
            this.camera = camera;
            CameraBufferSettings bufferSettings = settings.cameraBufferSettings;
            PostFXSettings postFXSettings = settings.enablePostProcessing ? settings.globalPostFXSettings : null;
            ShadowSettings shadowSettings = settings.globalShadowSettings;
            ForwardPlusSettings forwardPlusSettings = settings.forwardPlusSettings;
            var additiveCameraData = camera.GetComponent<ArcToonAdditiveCameraData>();
            CameraSettings cameraSettings = additiveCameraData ? additiveCameraData.Settings : defaultCameraSettings;
            postFXSettings = cameraSettings.overridePostFX ? cameraSettings.postFXSettings : postFXSettings;
            var cameraSampler =
                additiveCameraData ? additiveCameraData.Sampler : ProfilingSampler.Get(camera.cameraType);
            bool useHDR = bufferSettings.allowHDR && camera.allowHDR;

            // render scale
            var bufferSize = GetCameraBufferSize(cameraSettings, bufferSettings);

            // prepare scene data
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
            // camera texture
            bool copyColorTexture, copyDepthTexture;
            if (camera.cameraType == CameraType.Reflection)
            {
                copyDepthTexture = bufferSettings.copyDepthReflection;
                copyColorTexture = bufferSettings.copyColorReflection;
            }
            else
            {
                copyDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
                copyColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
            }

            // cull
            if (!GetCullingResults(context, out var cullingResults, shadowSettings.maxDistance))
            {
                return;
            }

            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                scriptableRenderContext = context,
                rendererListCulling = true,
            };
            CameraAttachmentCopier copier = new(cameraCopyMaterial, camera);

            renderGraph.BeginRecording(renderGraphParameters);
            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                var lightingHandles = LightingPass.Record(renderGraph, cullingResults, bufferSize,
                    shadowSettings,
                    forwardPlusSettings,
                    context);

                var attachmentHandles = SetupPass.Record(renderGraph, camera, bufferSize,
                    copyColorTexture, copyDepthTexture, useHDR);

                DepthStencilPrePass.Record(renderGraph, camera, cullingResults, copyDepthTexture, attachmentHandles);

                OpaquePass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                SkyboxPass.Record(renderGraph, camera, cullingResults, attachmentHandles);

                TransparentPass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                UnsupportedPass.Record(renderGraph, camera, cullingResults);

                // post fx
                var texture = PostFXPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraSettings, bufferSettings, postFXSettings, useHDR,
                    attachmentHandles.colorAttachment);

                var bicubicRescalingMode = bufferSettings.bicubicRescalingMode;
                bool bicubicSampling =
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                    bufferSize.x < camera.pixelWidth;
                CopyFinalPass.Record(renderGraph, cameraSettings.finalBlendMode, bicubicSampling, texture, copier);

                DebugPass.Record(renderGraph, camera, lightingHandles);

                GizmosPass.Record(renderGraph, attachmentHandles, copier);
            }

            renderGraph.EndRecordingAndExecute();
            // submit
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        private Vector2Int GetCameraBufferSize(CameraSettings cameraSettings, CameraBufferSettings bufferSettings)
        {
            float renderScale = cameraSettings.GetRenderScale(bufferSettings.renderScale);
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                useScaledRendering = false;
            }
#endif
            Vector2Int bufferSize = default;
            if (useScaledRendering)
            {
                bufferSize.x = (int)(camera.pixelWidth * renderScale);
                bufferSize.y = (int)(camera.pixelHeight * renderScale);
            }
            else
            {
                bufferSize.x = camera.pixelWidth;
                bufferSize.y = camera.pixelHeight;
            }

            return bufferSize;
        }

        private bool GetCullingResults(ScriptableRenderContext context, out CullingResults cullingResults,
            float maxShadowDistance)
        {
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                cullingResults = default;
                return false;
            }

            scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref scriptableCullingParameters);
            perObjectShadowCasterManager.Cull(camera);
            
            return true;
        }
    }
}