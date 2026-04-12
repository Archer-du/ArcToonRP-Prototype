using ArcToon.Behavior;
using ArcToon.Data;
using ArcToon.Passes;
using ArcToon.Passes.Lighting;
using ArcToon.Passes.PostProcessing;
using ArcToon.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon
{
    public enum RenderPhase
    {
        Lighting,
        Setup,
        Opaque,
        Skybox,
        Transparent,
        Unsupported,
        PostProcessing,
        BackBuffer,
    }
    
    public class CameraRenderer
    {
        internal ScriptableRenderContext Context { private set; get; }
        internal Camera RenderCamera { private set; get; }
        internal CameraAdditiveData CameraAdditiveData { private set; get; }
        internal float RenderScale { private set; get; }
        internal Vector2Int AttachmentSize { private set; get; }
        internal CullingResults CullingResults { private set; get; }
        internal bool useHDR { private set; get; }
        internal RenderPhase RenderPhase { private set; get; }
        
        internal CameraBufferSettings BufferSettings { private set; get; }
        internal ShadowSettings ShadowSettings { private set; get; }
        internal ForwardPlusSettings ForwardPlusSettings { private set; get; }
        internal PostFXConfig PostFXConfig { private set; get; }
        
        // New post-processing config (coexists with old PostFXConfig during migration)
        internal PostProcessConfig PostProcessConfig { private set; get; }
        
        internal RenderResources Resources { get; private set; }

        #region Pass Instances

        private readonly LightingPass lightingPass = new();
        private readonly SetupPass setupPass = new();
        private readonly DepthStencilPrePass depthStencilPrePass = new();
        private readonly OpaquePass opaquePass = new();
        private readonly GeometryOutlinePass opaqueOutlinePass = new();
        private readonly SkyboxPass skyboxPass = new();
        private readonly TransparentPass transparentPass = new();
        private readonly GeometryOutlinePass transparentOutlinePass = new();
        private readonly UnsupportedPass unsupportedPass = new();
        private readonly PostFXPass postFXPass = new();
        private readonly PostProcessPass postProcessPass = new();
        private readonly DebugPass debugPass = new();
        private readonly GizmosPass gizmosPass = new();
        private readonly CopyFinalPass copyFinalPass = new();

        #endregion

        public CameraRenderer()
        {
            Resources = new RenderResources();
            CameraDebugger.Initialize();
        }

        public void Dispose()
        {
            Resources.Dispose();
            postProcessPass.Dispose();
            CameraDebugger.Cleanup();
        }

        public void Render(ScriptableRenderContext context, Camera camera,
            RenderPipelineConfig config)
        {
            if (SetupRenderData(context, camera, config))
            {
                ExecuteRenderPass();
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
            
            PostProcessConfig = config.globalPostProcessConfig;

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

            // Allocate / resize persistent resources
            Resources.AllocateCameraResources(AttachmentSize.x, AttachmentSize.y, useHDR);
            Resources.AllocateShadowResources(ShadowSettings);
            Resources.AllocateLightingResources();
            Resources.AllocateTransparencyResources(AttachmentSize.x, AttachmentSize.y, useHDR);
            Resources.AllocatePostFXResources(AttachmentSize.x, AttachmentSize.y, useHDR);

            return true;
        }

        private void ExecuteRenderPass()
        {
            // Phase: Lighting
            RenderPhase = RenderPhase.Lighting;
            ExecutePass(lightingPass);

            // Phase: Setup
            RenderPhase = RenderPhase.Setup;
            ExecutePass(setupPass);
            ExecutePass(depthStencilPrePass);

            // Phase: Opaque
            RenderPhase = RenderPhase.Opaque;
            ExecutePass(opaquePass);
            ExecutePass(opaqueOutlinePass);

            // Phase: Skybox
            RenderPhase = RenderPhase.Skybox;
            ExecutePass(skyboxPass);

            // Phase: Transparent
            RenderPhase = RenderPhase.Transparent;
            ExecutePass(transparentPass);

            // Phase: Unsupported
            RenderPhase = RenderPhase.Unsupported;
            ExecutePass(unsupportedPass);

            // Phase: PostProcessing
            RenderPhase = RenderPhase.PostProcessing;
            if (true)
            {
                ExecutePass(postProcessPass);
            }
            else
            {
                ExecutePass(postFXPass);
            }

            // Phase: BackBuffer
            RenderPhase = RenderPhase.BackBuffer;
            ExecutePass(copyFinalPass);

            if (CameraDebugger.IsActive && RenderCamera.cameraType <= CameraType.SceneView)
            {
                ExecutePass(debugPass);
            }
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                ExecutePass(gizmosPass);
            }
#endif

            Context.Submit();
        }

        private void ExecutePass(RenderPassBase pass)
        {
            pass.Setup(Resources, this);
            pass.PrepareRendererLists(Context);

            // Each pass gets its own named CommandBuffer from the pool.
            // The cmd name automatically creates profiling events on
            // ExecuteCommandBuffer, avoiding BeginSample/EndSample mismatch
            // when passes flush the buffer internally.
            var cmd = CommandBufferPool.Get(pass.Name);
            pass.Execute(cmd, Context);
            Context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
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
            PerObjectShadowCasterManager.Instance.Cull(RenderCamera);
            
            return true;
        }
    }
}