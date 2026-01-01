using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Passes;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public class CameraRenderer
    {
        private Camera camera;

        private CameraBufferSettings bufferSettings;
        private ShadowSettings shadowSettings;
        private ForwardPlusSettings forwardPlusSettings;
        
        private CameraAdditiveData cameraAdditiveData;
        
        private PerObjectShadowCasterManager perObjectShadowCasterManager = new();

        public CameraRenderer()
        {
            CameraDebugger.Initialize();
        }

        public void Dispose()
        {
            CameraDebugger.Cleanup();
        }

        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera,
            RenderPipelineSettings settings)
        {
            this.camera = camera;
            
            bufferSettings = settings.cameraBufferSettings;
            shadowSettings = settings.globalShadowSettings;
            forwardPlusSettings = settings.forwardPlusSettings;
            
            var cameraRenderController = camera.GetComponent<CameraRenderController>();
            if (!cameraRenderController)
            {
                Debug.LogError("[ArcToonRP] Camera does not have ArcToonAdditiveCameraData attached.");
                return;
            }
            cameraAdditiveData = cameraRenderController.AdditiveData;
            
            PostFXConfig postFXConfig = settings.globalPostFXConfig;
            if (cameraAdditiveData.overridePostFXConfig != null)
            {
                postFXConfig = cameraAdditiveData.overridePostFXConfig;
            }
            bool useHDR = bufferSettings.enableHDR && camera.allowHDR;

            // render scale
            var bufferSize = GetCameraBufferSize(cameraAdditiveData.GetRenderScale(bufferSettings.renderScale));

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
                copyDepthTexture = bufferSettings.copyDepth && cameraAdditiveData.copyDepth;
                copyColorTexture = bufferSettings.copyColor && cameraAdditiveData.copyColor;
            }

            // cull
            if (!GetCullingResults(context, out var cullingResults, shadowSettings.maxDistance))
            {
                return;
            }

            var cameraSampler = cameraRenderController.Sampler;
            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                scriptableRenderContext = context,
                rendererListCulling = true,
            };
            CameraAttachmentCopier copier = new(ShaderResourceManager.AcquireTransientMaterial(InternalShaderHelpers.Path.CameraCopy), camera);

            renderGraph.BeginRecording(renderGraphParameters);
            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                var lightingHandles = LightingPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    shadowSettings,
                    forwardPlusSettings,
                    context, perObjectShadowCasterManager);

                var attachmentHandles = SetupPass.Record(renderGraph, camera, bufferSize,
                    copyColorTexture, copyDepthTexture, useHDR);

                DepthStencilPrePass.Record(renderGraph, camera, cullingResults, copyDepthTexture, attachmentHandles);

                OpaquePass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                SkyboxPass.Record(renderGraph, camera, cullingResults, attachmentHandles);

                TransparentPass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                UnsupportedPass.Record(renderGraph, camera, cullingResults);

                // post fx
                var texture = PostFXPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraAdditiveData, bufferSettings, postFXConfig, useHDR,
                    attachmentHandles.colorAttachment);

                var bicubicRescalingMode = bufferSettings.bicubicRescalingMode;
                bool bicubicSampling =
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                    bufferSize.x < camera.pixelWidth;
                CopyFinalPass.Record(renderGraph, cameraAdditiveData.finalBlendMode, bicubicSampling, texture, copier);

                DebugPass.Record(renderGraph, camera, lightingHandles);

                GizmosPass.Record(renderGraph, attachmentHandles, copier);
            }

            renderGraph.EndRecordingAndExecute();
            // submit
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        private Vector2Int GetCameraBufferSize(float renderScale)
        {
            renderScale = Mathf.Clamp(renderScale, CameraAdditiveData.renderScaleMin, CameraAdditiveData.renderScaleMax);
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