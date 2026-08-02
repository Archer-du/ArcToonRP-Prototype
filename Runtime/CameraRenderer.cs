using System.Collections.Generic;
using ArcToon.Behavior;
using ArcToon.Config;
using ArcToon.Data;
using ArcToon.Passes;
using ArcToon.Passes.Lighting;
using ArcToon.Passes.PostProcessing;
using ArcToon.Passes.PostProcessing.Processors;
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

        // Sub-pixel camera jitter for TAA. Only advanced on frames where TAA is active,
        // so the projection is left untouched when the effect is disabled.
        private readonly TemporalAAData temporalAAData = new();
        internal bool TemporalAAActive { private set; get; }
        internal TemporalAAData TemporalAAData => temporalAAData;
        
        #region Built-in Pass Instances

        private readonly LightingPass lightingPass;
        private readonly SetupPass setupPass;
        private readonly DepthStencilPrePass depthStencilPrePass;
        
        private readonly OpaquePass opaquePass;
        private readonly SkyboxPass skyboxPass;
        private readonly TransparentPass transparentPass;
        
        private readonly UnsupportedPass unsupportedPass;
        private readonly PostProcessPass postProcessPass;
        
        private readonly GeometryDebugPass geometryDebugPass;
        private readonly ScreenDebugPass screenDebugPass;
        private readonly GizmosPass gizmosPass;
        
        private readonly BackBufferPass backBufferPass;

        #endregion

        private readonly List<RenderPassBase> activePassQueue = new();

        internal CameraBufferSettings BufferSettings => PipelineConfig.cameraBufferSettings;
        internal ShadowSettings ShadowSettings => PipelineConfig.shadowSettings;
        internal ForwardPlusSettings ForwardPlusSettings => PipelineConfig.forwardPlusSettings;

        public CameraRenderer(RenderPipelineConfig config)
        {
            PipelineConfig = config;
            Resources = new RenderResources();
            
            lightingPass = new LightingPass(Resources, this);
            setupPass = new SetupPass(Resources, this);
            depthStencilPrePass = new DepthStencilPrePass(Resources, this);
            opaquePass = new OpaquePass(Resources, this);
            skyboxPass = new SkyboxPass(Resources, this);
            transparentPass = new TransparentPass(Resources, this);
            unsupportedPass = new UnsupportedPass(Resources, this);
            postProcessPass = new PostProcessPass(Resources, this);
            geometryDebugPass = new GeometryDebugPass(Resources, this);
            screenDebugPass = new ScreenDebugPass(Resources, this);
            gizmosPass = new GizmosPass(Resources, this);
            backBufferPass = new BackBufferPass(Resources, this);
            
            DebuggerSingleton.Initialize();
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
            geometryDebugPass.Dispose();
            screenDebugPass.Dispose();
            gizmosPass.Dispose();
            backBufferPass.Dispose();
            
            DebuggerSingleton.Cleanup();
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

            PostProcessConfig = PipelineConfig.globalPostProcessConfig;
            if (CameraAdditiveData.overridePostProcessConfig != null)
            {
                PostProcessConfig = CameraAdditiveData.overridePostProcessConfig;
            }
            
            RenderScale = CameraAdditiveData.GetRenderScale(BufferSettings.renderScale);
            AttachmentSize = RenderCamera.GetAttachmentSize(RenderScale);
            useHDR = BufferSettings.enableHDR && RenderCamera.allowHDR;

            // Resolve TAA state and advance the sub-pixel jitter before culling, so the geometry
            // passes render with the jittered projection SetupPass installs this frame.
            TemporalAAActive = IsTemporalAAActive();
            if (TemporalAAActive)
            {
                temporalAAData.Update(RenderCamera, AttachmentSize);
            }

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

            if (DebuggerSingleton.IsGeometryDebugActive)
            {
                // Geometry debug replaces the geometry + post-process chain.
                // Lighting / Setup / Prepass are kept: lighting-term modes read shadow and depth data.
                EnqueuePass(geometryDebugPass);
            }
            else
            {
                // Geometry
                EnqueuePass(opaquePass);
                EnqueuePass(skyboxPass);
                EnqueuePass(transparentPass);
                EnqueuePass(unsupportedPass);
            }

            // Post Processing
            EnqueuePass(postProcessPass);
            // Back Buffer
            EnqueuePass(backBufferPass);

            // Editor
            if (DebuggerSingleton.IsScreenDebugActive && RenderCamera.cameraType <= CameraType.SceneView)
            {
                EnqueuePass(screenDebugPass);
            }
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                EnqueuePass(gizmosPass);
            }
#endif
        }

        /// <summary>
        /// Execute all enqueued passes following URP-aligned lifecycle:
        /// Configuration Phase: SetupFrameData → SetupRendererList (all passes)
        /// Execution Phase: Execute (each pass in order)
        /// Cleanup Phase: CleanupFrameData (all passes)
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

        /// <summary>
        /// Whether TAA should jitter and resolve this frame. Mirrors the post-process chain's
        /// gating (config present, applicable to this camera, not overridden by geometry debug),
        /// plus an enabled TemporalAAVolumeConfig entry.
        /// </summary>
        private bool IsTemporalAAActive()
        {
            if (PostProcessConfig == null ||
                !PostProcessConfig.AreApplicableTo(RenderCamera) ||
                DebuggerSingleton.IsGeometryDebugActive)
            {
                return false;
            }

            var config = PostProcessConfig.GetVolumeConfig<TemporalAAVolumeConfig>();
            return config is { enabled: true };
        }
    }
}