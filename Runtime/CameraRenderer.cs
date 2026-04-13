using System.Collections.Generic;
using ArcToon.Behavior;
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
        internal ScriptableRenderContext Context { private set; get; }
        internal Camera RenderCamera { private set; get; }
        internal CameraAdditiveData CameraAdditiveData { private set; get; }
        internal float RenderScale { private set; get; }
        internal Vector2Int AttachmentSize { private set; get; }
        internal CullingResults CullingResults { private set; get; }
        internal bool useHDR { private set; get; }
        
        internal CameraBufferSettings BufferSettings { private set; get; }
        internal ShadowSettings ShadowSettings { private set; get; }
        internal ForwardPlusSettings ForwardPlusSettings { private set; get; }
        internal PostProcessConfig PostProcessConfig { private set; get; }
        
        internal RenderResources Resources { get; private set; }

        #region Built-in Pass Instances

        private readonly LightingPass lightingPass = new();
        private readonly SetupPass setupPass = new();
        private readonly DepthStencilPrePass depthStencilPrePass = new();
        
        private readonly OpaquePass opaquePass = new();
        private readonly SkyboxPass skyboxPass = new();
        private readonly TransparentPass transparentPass = new();
        
        private readonly UnsupportedPass unsupportedPass = new();
        private readonly PostProcessPass postProcessPass = new();
        private readonly DebugPass debugPass = new();
        private readonly GizmosPass gizmosPass = new();
        private readonly CopyFinalPass copyFinalPass = new();

        #endregion

        #region Pass Queue

        private readonly List<RenderPassBase> activePassQueue = new();

        private void EnqueuePass(RenderPassBase pass)
        {
            activePassQueue.Add(pass);
        }

        #endregion

        #region Legacy
        
        internal PostFXConfig PostFXConfig { private set; get; }
        private readonly PostFXPass postFXPass = new();
        
        #endregion
        
        public CameraRenderer()
        {
            Resources = new RenderResources();
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

        public void Render(ScriptableRenderContext context, Camera camera,
            RenderPipelineConfig config)
        {
            if (SetupRenderData(context, camera, config))
            {
                EnqueuePasses();
                ExecutePassQueue();
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
            ShadowSettings = config.shadowSettings;
            ForwardPlusSettings = config.forwardPlusSettings;
            
            PostFXConfig = config.globalPostFXConfig;
            if (CameraAdditiveData.overridePostFXConfig != null)
            {
                PostFXConfig = CameraAdditiveData.overridePostFXConfig;
            }
            
            PostProcessConfig = config.globalPostProcessConfig;
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
            Resources.AllocateTransparencyResources(AttachmentSize.x, AttachmentSize.y, useHDR);
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

            // Lighting & Shadow
            EnqueuePass(lightingPass);

            // Camera Setup & Prepass
            EnqueuePass(setupPass);
            EnqueuePass(depthStencilPrePass);

            // Opaque Geometry
            EnqueuePass(opaquePass);

            // Skybox
            EnqueuePass(skyboxPass);

            // Transparent Geometry
            EnqueuePass(transparentPass);

            // Unsupported Shaders
            EnqueuePass(unsupportedPass);

            // Post Processing
            EnqueuePass(postProcessPass);
            // EnqueuePass(postFXPass);

            // Final Blit to Back Buffer
            EnqueuePass(copyFinalPass);

            // Conditional: Debug overlay
            if (CameraDebugger.IsActive && RenderCamera.cameraType <= CameraType.SceneView)
            {
                EnqueuePass(debugPass);
            }

            // Conditional: Editor Gizmos
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                EnqueuePass(gizmosPass);
            }
#endif
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
            // All passes complete configuration before any execution begins.
            // This means SetupResource cannot depend on another pass's Execute result.
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].Initialize(Resources, this);
            }

            var setupCmd = CommandBufferPool.Get("Setup Resources");
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].SetupResource(setupCmd);
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
            var cleanupCmd = CommandBufferPool.Get("Cleanup Resources");
            for (int i = 0; i < activePassQueue.Count; i++)
            {
                activePassQueue[i].CleanupResource(cleanupCmd);
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