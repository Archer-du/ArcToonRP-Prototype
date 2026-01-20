using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Passes;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public class CameraRenderer
    {
        internal ScriptableRenderContext Context { private set; get; }
        internal Camera RenderCamera { private set; get; }
        internal CameraAdditiveData CameraAdditiveData { private set; get; }
        internal float RenderScale { private set; get; }
        
        internal Vector2Int AttachmentSize { private set; get; }
        internal CullingResults CullingResults { private set; get; }

        internal CameraBufferSettings BufferSettings { private set; get; }
        internal ShadowSettings ShadowSettings { private set; get; }
        internal ForwardPlusSettings ForwardPlusSettings { private set; get; }
        
        
        internal PostFXConfig PostFXConfig { private set; get; }
        
        internal bool useHDR { private set; get; }
        internal bool copyDepth { private set; get; }
        internal bool copyColor { private set; get; }
        
        //TODO:
        internal TransparencyMode mode;
        
        // TODO: Singleton
        internal PerObjectShadowCasterManager PerObjectShadowCasterManager = new();

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
            if (SetupRenderData(context, camera, config))
            {
                ExecuteRenderPass(renderGraph);
            }
        }

        private bool SetupRenderData(ScriptableRenderContext context, Camera camera,
            RenderPipelineConfig config)
        {
            RenderCamera = camera;
            Context = context;

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

            mode = config.transparencyMode;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            if (!GetCullingResults(context, ShadowSettings.maxDistance))
            {
                return false;
            }
            
            RenderScale = CameraAdditiveData.GetRenderScale(BufferSettings.renderScale);
            AttachmentSize = GetCameraBufferSize(RenderCamera, RenderScale);
                        
            useHDR = BufferSettings.enableHDR && RenderCamera.allowHDR;
            if (RenderCamera.cameraType == CameraType.Reflection)
            {
                copyDepth = BufferSettings.copyDepthReflection;
                copyColor = BufferSettings.copyColorReflection;
            }
            else
            {
                copyDepth = BufferSettings.copyDepth && CameraAdditiveData.copyDepth;
                copyColor = BufferSettings.copyColor && CameraAdditiveData.copyColor;
            }

            return true;
        }

        private void ExecuteRenderPass(RenderGraph renderGraph)
        {
            var cameraSampler = new ProfilingSampler(RenderCamera.name);
            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                scriptableRenderContext = Context,
                rendererListCulling = true,
            };

            renderGraph.BeginRecording(renderGraphParameters);
            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                RenderGraphResourceHandle resourceHandle = new();

                RecordRenderPass<LightingPass>("Lighting", 
                    renderGraph, resourceHandle, false);
                RecordRenderPass<SetupPass>("Setup", 
                    renderGraph, resourceHandle, false);
                RecordRenderPass<DepthStencilPrePass>("Prepass", 
                    renderGraph, resourceHandle);
                RecordRenderPass<OpaquePass>("Opaque", 
                    renderGraph, resourceHandle);
                RecordRenderPass<SkyboxPass>("Skybox", 
                    renderGraph, resourceHandle, false);
                // TODO: 
                RecordRenderPass<TransparentPass>("Transparent", 
                    renderGraph, resourceHandle, false);
                RecordRenderPass<UnsupportedPass>("Unsupported", 
                    renderGraph, resourceHandle);
                
                resourceHandle.postFXResult = PostFXPass.Record(this, renderGraph, RenderCamera, resourceHandle.geometryResult, CullingResults, AttachmentSize,
                    CameraAdditiveData, BufferSettings, PostFXConfig, useHDR);
                
                RecordRenderPass<CopyFinalPass>("Final",
                    renderGraph, resourceHandle);
                
                if (CameraDebugger.IsActive && RenderCamera.cameraType <= CameraType.SceneView)
                {
                    RecordRenderPass<DebugPass>("Debug", 
                        renderGraph, resourceHandle);
                }
                if (Handles.ShouldRenderGizmos())
                {
                    RecordRenderPass<GizmosPass>("Gizmos", 
                        renderGraph, resourceHandle);
                }
            }

            renderGraph.EndRecordingAndExecute();
            Context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            Context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        private void RecordRenderPass<TRenderPass>(string passName,
            RenderGraph renderGraph, RenderGraphResourceHandle resourceHandle, bool allowPassCulling = true) 
            where TRenderPass : RenderGraphPassBase, new()
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(passName, out TRenderPass passData);
            passData.Initialize(resourceHandle, this);
            passData.AcquireResource(renderGraph);
            passData.DeclareResourceUsage(builder);

            builder.AllowPassCulling(allowPassCulling);
            builder.SetRenderFunc<TRenderPass>(static (pass, context) =>
            {
                pass.Render(context.cmd, context.renderContext);
                context.renderContext.ExecuteCommandBuffer(context.cmd);
                context.cmd.Clear();
            });
        }

        private Vector2Int GetCameraBufferSize(Camera camera, float renderScale)
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

        private bool GetCullingResults(ScriptableRenderContext context, float maxShadowDistance)
        {
            if (!RenderCamera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                return false;
            }

            scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, RenderCamera.farClipPlane);
            CullingResults = context.Cull(ref scriptableCullingParameters);
            PerObjectShadowCasterManager.Cull(RenderCamera);
            
            return true;
        }
    }
}