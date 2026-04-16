using System.Collections.Generic;
using ArcToon.Behavior;
using ArcToon.Config;
using ArcToon.Data;
using ArcToon.Passes;
using ArcToon.Passes.Legacy;
using ArcToon.Passes.Lighting;
using ArcToon.Passes.PostProcessing;
using ArcToon.Settings;
using ArcToon.System;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon
{
    public class CameraRenderer
    {
        internal RenderPipelineConfig PipelineConfig { private set; get; }
        internal RenderResources Resources { get; private set; }
        
        internal ScriptableRenderContext Context { private set; get; }
        internal Camera RenderCamera { private set; get; }
        internal float RenderScale { private set; get; }
        internal Vector2Int AttachmentSize { private set; get; }
        internal CullingResults CullingResults { private set; get; }
        internal bool useHDR { private set; get; }
        
        internal CameraAdditiveData CameraAdditiveData { private set; get; }
        
        internal PostProcessConfig PostProcessConfig { private set; get; }
        
        #region Built-in Pass Instances

        private readonly LightingPass lightingPass;
        private readonly SetupPass setupPass;
        private readonly DepthStencilPrePass depthStencilPrePass;
        
        private readonly OpaquePass opaquePass;
        private readonly SkyboxPass skyboxPass;
        private readonly TransparentPass transparentPass;
        
        private readonly UnsupportedPass unsupportedPass;
        private readonly PostProcessPass postProcessPass;
        private readonly DebugPass debugPass;
        private readonly GizmosPass gizmosPass;
        private readonly CopyFinalPass copyFinalPass;

        #endregion

        private readonly List<RenderPassBase> activePassQueue = new();

        internal CameraBufferSettings BufferSettings => PipelineConfig.cameraBufferSettings;
        internal ShadowSettings ShadowSettings => PipelineConfig.shadowSettings;
        internal ForwardPlusSettings ForwardPlusSettings => PipelineConfig.forwardPlusSettings;
        
        #region Legacy
        
        internal PostFXConfig PostFXConfig { private set; get; }
        private readonly PostFXPass postFXPass;
        
        #endregion
        
        public CameraRenderer(RenderPipelineConfig config)
        {
            PipelineConfig = config;
            Resources = new RenderResources();
            
            lightingPass = new LightingPass();
            setupPass = new SetupPass();
            depthStencilPrePass = new DepthStencilPrePass();
            opaquePass = new OpaquePass();
            skyboxPass = new SkyboxPass();
            transparentPass = new TransparentPass();
            unsupportedPass = new UnsupportedPass();
            postProcessPass = new PostProcessPass();
            debugPass = new DebugPass();
            gizmosPass = new GizmosPass();
            copyFinalPass = new CopyFinalPass();
            postFXPass = new PostFXPass();
            
            CameraDebugger.Initialize();
        }

        public void Dispose()
        {
            Resources.Dispose();
            
            // Dispose all pass instances
            lightingPass.Dispose();
            setupPass.Dispose();
            depthStencilPrePass.Dispose();
            opaquePass.Dispose();
            skyboxPass.Dispose();
            transparentPass.Dispose();
            unsupportedPass.Dispose();
            postProcessPass.Dispose();
            debugPass.Dispose();
            gizmosPass.Dispose();
            copyFinalPass.Dispose();
            postFXPass.Dispose();
            
            CameraDebugger.Cleanup();
        }

        private void EnqueuePass(RenderPassBase pass)
        {
            activePassQueue.Add(pass);
        }

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            if (SetupRenderData(context, camera))
            {
                EnqueuePasses();
                ExecutePassQueue();
            }
        }

        private bool SetupRenderData(ScriptableRenderContext context, Camera camera)
        {
            RenderCamera = camera;
            Context = context;

            var cameraRenderController = camera.GetComponent<CameraRenderController>();
            CameraAdditiveData = !cameraRenderController ? CameraAdditiveData.DefaultAdditiveData : cameraRenderController.AdditiveData;
            
            PostFXConfig = PipelineConfig.globalPostFXConfig;
            if (CameraAdditiveData.overridePostFXConfig != null)
            {
                PostFXConfig = CameraAdditiveData.overridePostFXConfig;
            }
            
            PostProcessConfig = PipelineConfig.globalPostProcessConfig;
            if (CameraAdditiveData.overridePostProcessConfig != null)
            {
                PostProcessConfig = CameraAdditiveData.overridePostProcessConfig;
            }
            
            RenderScale = CameraAdditiveData.GetRenderScale(BufferSettings.renderScale);
            AttachmentSize = RenderCamera.GetAttachmentSize(RenderScale);
            useHDR = BufferSettings.enableHDR && RenderCamera.allowHDR;

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

            // TODO: refactor
            // Allocate / resize persistent resources
            Resources.AllocateCameraResources(AttachmentSize.x, AttachmentSize.y, useHDR);
            Resources.AllocateShadowResources(ShadowSettings);
            Resources.AllocateLightingResources();
            Resources.AllocatePostFXResources(AttachmentSize.x, AttachmentSize.y, useHDR);

            return true;
        }

        /// <summary>
        /// Build the active pass queue for this frame.
        /// Passes are enqueued in execution order.
        /// </summary>
        private void EnqueuePasses()
        {
            activePassQueue.Clear();

            // Lighting
            EnqueuePass(lightingPass);

            // Setup
            EnqueuePass(setupPass);
            EnqueuePass(depthStencilPrePass);

            // Geometry
            EnqueuePass(opaquePass);
            EnqueuePass(skyboxPass);
            EnqueuePass(transparentPass);
            EnqueuePass(unsupportedPass);

            // Post Processing
            EnqueuePass(postProcessPass);
            // EnqueuePass(postFXPass);

            // Back Buffer
            EnqueuePass(copyFinalPass);

            // Editor
            if (CameraDebugger.IsActive && RenderCamera.cameraType <= CameraType.SceneView)
            {
                EnqueuePass(debugPass);
            }
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                EnqueuePass(gizmosPass);
            }
#endif
            
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].Initialize(Resources, this);
            }
        }

        /// <summary>
        /// Execute all enqueued passes following URP-aligned lifecycle:
        /// Configuration Phase: Initialize → SetupResource → SetupRendererList (all passes)
        /// Execution Phase: Execute (each pass in order)
        /// Cleanup Phase: CleanupResource (all passes)
        /// </summary>
        private void ExecutePassQueue()
        {
            // ─── Configuration Phase ───
            var setupCmd = CommandBufferPool.Get();
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].SetupFrameData(setupCmd);
            }
            Context.ExecuteCommandBuffer(setupCmd);
            setupCmd.Clear();
            CommandBufferPool.Release(setupCmd);

            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].SetupRendererList(Context);
            }

            // ─── Execution Phase ───
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                var cmd = CommandBufferPool.Get(activePassQueue[i].Name);
                activePassQueue[i].Execute(cmd, Context);
                Context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            // ─── Cleanup Phase ───
            var cleanupCmd = CommandBufferPool.Get();
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].CleanupFrameData(cleanupCmd);
            }
            Context.ExecuteCommandBuffer(cleanupCmd);
            cleanupCmd.Clear();
            CommandBufferPool.Release(cleanupCmd);

            Context.Submit();
        }

        private bool GetCullingResults(ScriptableRenderContext context, float maxShadowDistance)
        {
            if (!RenderCamera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                return false;
            }

            scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, RenderCamera.farClipPlane);
            CullingResults = context.Cull(ref scriptableCullingParameters);
            // TODO: move？
            PerObjectShadowCasterManager.Instance.Cull(RenderCamera);
            
            return true;
        }
    }
}