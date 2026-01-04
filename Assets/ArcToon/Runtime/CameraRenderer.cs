using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
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
        internal Camera RenderCamera { private set; get; }
        
        internal float RenderScale { private set; get; }
        
        internal Vector2Int AttachmentSize { private set; get; }
        
        internal CullingResults CullingResults { private set; get; }

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
            RenderCamera = camera;

            var cameraRenderController = camera.GetComponent<CameraRenderController>();
            if (!cameraRenderController)
            {
                CameraAdditiveData = CameraAdditiveData.DefaultAdditiveData;
            }
            else
            {
                CameraAdditiveData = cameraRenderController.AdditiveData;
            }
            
            BufferSettings = config.cameraBufferSettings;
            ShadowSettings = config.globalShadowSettings;
            ForwardPlusSettings = config.forwardPlusSettings;
            
            PostFXConfig = config.globalPostFXConfig;
            if (CameraAdditiveData.overridePostFXConfig != null)
            {
                PostFXConfig = CameraAdditiveData.overridePostFXConfig;
            }

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            if (!GetCullingResults(context, ShadowSettings.maxDistance))
            {
                return;
            }
            
            RenderScale = CameraAdditiveData.GetRenderScale(BufferSettings.renderScale);
            AttachmentSize = GetCameraBufferSize(RenderScale);

            var cameraSampler = new ProfilingSampler(RenderCamera.name);
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
                RenderGraphResourceData resourceData = new();
                
                var lightingHandles = LightingPass.Record(this, renderGraph, camera, CullingResults,
                    AttachmentSize,
                    ShadowSettings,
                    ForwardPlusSettings, context, perObjectShadowCasterManager);

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
                SetupPass.Record(this, renderGraph, camera, resourceData, AttachmentSize, copyColor, copyDepth, useHDR);

                DepthStencilPrePass.Record(this, renderGraph, camera, resourceData, CullingResults, copyDepth);

                OpaquePass.Record(this, renderGraph, camera, resourceData, CullingResults, lightingHandles);

                SkyboxPass.Record(this, renderGraph, camera, resourceData, CullingResults);

                TransparentPass.Record(this, renderGraph, camera, resourceData, CullingResults, lightingHandles);

                UnsupportedPass.Record(renderGraph, camera, CullingResults);
                
                var postFXResult = PostFXPass.Record(this, renderGraph, camera, resourceData, CullingResults, AttachmentSize,
                    CameraAdditiveData, BufferSettings, PostFXConfig, useHDR);

                CameraAttachmentCopier copier = new(camera);
                var bicubicRescalingMode = BufferSettings.bicubicRescalingMode;
                bool bicubicSampling =
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                    bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                    AttachmentSize.x < camera.pixelWidth;
                CopyFinalPass.Record(renderGraph, resourceData, postFXResult, CameraAdditiveData.finalBlendMode, bicubicSampling, copier);

                DebugPass.Record(renderGraph, camera, lightingHandles);

                GizmosPass.Record(renderGraph, resourceData, copier);
            }

            renderGraph.EndRecordingAndExecute();
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        private void RecordRenderPass<TRenderPass>(RenderGraph renderGraph, RenderGraphResourceData resourceData, string passName, 
            bool allowPassCulling = true) 
            where TRenderPass : RenderGraphPassBase, new()
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(passName, out TRenderPass passData);
            passData.Initialize(resourceData, this);
            passData.AcquireResource(renderGraph);
            passData.DeclareResourceUsage(builder);

            builder.AllowPassCulling(allowPassCulling);
            builder.SetRenderFunc<TRenderPass>(static (pass, context) => pass.Render(context));
        }

        private Vector2Int GetCameraBufferSize(float renderScale)
        {
            renderScale = Mathf.Clamp(renderScale, CameraAdditiveData.renderScaleMin, CameraAdditiveData.renderScaleMax);
            bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
#if UNITY_EDITOR
            if (RenderCamera.cameraType == CameraType.SceneView)
            {
                useScaledRendering = false;
            }
#endif
            Vector2Int bufferSize = default;
            if (useScaledRendering)
            {
                bufferSize.x = (int)(RenderCamera.pixelWidth * renderScale);
                bufferSize.y = (int)(RenderCamera.pixelHeight * renderScale);
            }
            else
            {
                bufferSize.x = RenderCamera.pixelWidth;
                bufferSize.y = RenderCamera.pixelHeight;
            }

            return bufferSize;
        }

        private bool GetCullingResults(ScriptableRenderContext context, float maxShadowDistance)
        {
            if (!RenderCamera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                return false;
            }

            scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, RenderCamera.farClipPlane);
            CullingResults = context.Cull(ref scriptableCullingParameters);
            perObjectShadowCasterManager.Cull(RenderCamera);
            
            return true;
        }
    }
}