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
        internal Camera CurrentCamera { private set; get; }
        
        internal float RenderScale { private set; get; }
        
        internal Vector2Int AttachmentSize { private set; get; }

        internal CameraBufferSettings BufferSettings { private set; get; }
        internal ShadowSettings ShadowSettings { private set; get; }
        internal ForwardPlusSettings ForwardPlusSettings { private set; get; }
        
        internal CameraAdditiveData CameraAdditiveData { private set; get; }
        
        internal PostFXConfig PostFXConfig { private set; get; }
        
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
            RenderPipelineConfig config)
        {
            CurrentCamera = camera;
            
            BufferSettings = config.cameraBufferSettings;
            ShadowSettings = config.globalShadowSettings;
            ForwardPlusSettings = config.forwardPlusSettings;
            
            var cameraRenderController = camera.GetComponent<CameraRenderController>();
            if (!cameraRenderController)
            {
                Debug.LogError("[ArcToonRP] Camera does not have ArcToonAdditiveCameraData attached.");
                return;
            }
            CameraAdditiveData = cameraRenderController.AdditiveData;
            
            PostFXConfig = config.globalPostFXConfig;
            if (CameraAdditiveData.overridePostFXConfig != null)
            {
                PostFXConfig = CameraAdditiveData.overridePostFXConfig;
            }

            // prepare scene data
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            // cull
            if (!GetCullingResults(context, out var cullingResults, ShadowSettings.maxDistance))
            {
                return;
            }
            
            RenderScale = CameraAdditiveData.GetRenderScale(BufferSettings.renderScale);
            AttachmentSize = GetCameraBufferSize(RenderScale);

            var cameraSampler = cameraRenderController.Sampler;
            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                scriptableRenderContext = context,
                rendererListCulling = true,
            };

            renderGraph.BeginRecording(renderGraphParameters);
            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                var lightingHandles = LightingPass.Record(renderGraph, this, camera, cullingResults, AttachmentSize,
                    ShadowSettings,
                    ForwardPlusSettings,
                    context, perObjectShadowCasterManager);

                bool useHDR = BufferSettings.enableHDR && camera.allowHDR;
                bool copyColor, copyDepth;
                if (camera.cameraType == CameraType.Reflection)
                {
                    copyDepth = BufferSettings.copyDepthReflection;
                    copyColor = BufferSettings.copyColorReflection;
                }
                else
                {
                    copyDepth = BufferSettings.copyDepth && CameraAdditiveData.copyDepth;
                    copyColor = BufferSettings.copyColor && CameraAdditiveData.copyColor;
                }
                var attachmentHandles = SetupPass.Record(renderGraph, camera, AttachmentSize,
                    copyColor, copyDepth, useHDR);

                DepthStencilPrePass.Record(renderGraph, camera, cullingResults, copyDepth, attachmentHandles);

                OpaquePass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                SkyboxPass.Record(renderGraph, camera, cullingResults, attachmentHandles);

                TransparentPass.Record(renderGraph, camera, cullingResults, attachmentHandles, lightingHandles);

                UnsupportedPass.Record(renderGraph, camera, cullingResults);

                // post fx
                var texture = PostFXPass.Record(renderGraph, camera, cullingResults, AttachmentSize,
                    CameraAdditiveData, BufferSettings, PostFXConfig, useHDR,
                    attachmentHandles.colorAttachment);

                CameraAttachmentCopier copier = new(camera);
                var bicubicRescalingMode = BufferSettings.bicubicRescalingMode;
                bool bicubicSampling =
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                    AttachmentSize.x < camera.pixelWidth;
                CopyFinalPass.Record(renderGraph, CameraAdditiveData.finalBlendMode, bicubicSampling, texture, copier);

                DebugPass.Record(renderGraph, camera, lightingHandles);

                GizmosPass.Record(renderGraph, attachmentHandles, copier);
            }

            renderGraph.EndRecordingAndExecute();
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        private Vector2Int GetCameraBufferSize(float renderScale)
        {
            renderScale = Mathf.Clamp(renderScale, CameraAdditiveData.renderScaleMin, CameraAdditiveData.renderScaleMax);
            bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
#if UNITY_EDITOR
            if (CurrentCamera.cameraType == CameraType.SceneView)
            {
                useScaledRendering = false;
            }
#endif
            Vector2Int bufferSize = default;
            if (useScaledRendering)
            {
                bufferSize.x = (int)(CurrentCamera.pixelWidth * renderScale);
                bufferSize.y = (int)(CurrentCamera.pixelHeight * renderScale);
            }
            else
            {
                bufferSize.x = CurrentCamera.pixelWidth;
                bufferSize.y = CurrentCamera.pixelHeight;
            }

            return bufferSize;
        }

        private bool GetCullingResults(ScriptableRenderContext context, out CullingResults cullingResults,
            float maxShadowDistance)
        {
            if (!CurrentCamera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                cullingResults = default;
                return false;
            }

            scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, CurrentCamera.farClipPlane);
            cullingResults = context.Cull(ref scriptableCullingParameters);
            perObjectShadowCasterManager.Cull(CurrentCamera);
            
            return true;
        }
    }
}