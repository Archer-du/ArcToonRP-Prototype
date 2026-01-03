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
                var lightingHandles = LightingPass.Record(renderGraph, this, camera, CullingResults, AttachmentSize,
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

                DepthStencilPrePass.Record(renderGraph, camera, CullingResults, copyDepth, attachmentHandles);

                OpaquePass.Record(renderGraph, camera, CullingResults, attachmentHandles, lightingHandles);

                SkyboxPass.Record(renderGraph, camera, CullingResults, attachmentHandles);

                TransparentPass.Record(renderGraph, camera, CullingResults, attachmentHandles, lightingHandles);

                UnsupportedPass.Record(renderGraph, camera, CullingResults);

                // post fx
                var texture = PostFXPass.Record(renderGraph, camera, CullingResults, AttachmentSize,
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

        private void RecordRenderPass<T>(RenderGraph renderGraph, string passName) where T : RenderGraphPassDataBase, new()
        {
            // using (RenderGraphBuilder builder = renderGraph.AddRenderPass(
            //     passName, out T passData))
            // {
            //     pass.Setup(cullingResults, camera, attachmentSize, shadowSettings, forwardPlusSettings,
            //         perObjectShadowCasterManager);
            //     pass.spotLightDataHandle = builder.WriteBuffer(
            //         renderGraph.CreateBuffer(new BufferDesc(maxSpotLightCount, SpotLightBufferData.stride)
            //         {
            //             name = "Spot Light Data",
            //             target = GraphicsBuffer.Target.Structured
            //         })
            //     );
            //     pass.pointLightDataHandle = builder.WriteBuffer(
            //         renderGraph.CreateBuffer(new BufferDesc(maxPointLightCount, PointLightBufferData.stride)
            //         {
            //             name = "Point Light Data",
            //             target = GraphicsBuffer.Target.Structured
            //         })
            //     );
            //     pass.directionalLightDataHandle = builder.WriteBuffer(
            //         renderGraph.CreateBuffer(new BufferDesc(maxDirectionalLightCount, DirectionalLightBufferData.stride)
            //         {
            //             name = "Directional Light Data",
            //             target = GraphicsBuffer.Target.Structured
            //         })
            //     );
            //     pass.perObjectShadowCasterDataHandle = builder.WriteBuffer(
            //         renderGraph.CreateBuffer(new BufferDesc(maxPerObjectCasterCount, PerObjectCasterBufferData.stride)
            //         {
            //             name = "Per Object Shadow Caster Data",
            //             target = GraphicsBuffer.Target.Structured
            //         })
            //     );
            //     pass.forwardPlusTileBufferHandle = builder.WriteBuffer(
            //         renderGraph.CreateBuffer(new BufferDesc(pass.TileCount * pass.tileDataSize, 4)
            //         {
            //             name = "Forward+ Tiles",
            //         }));
            //
            //     builder.AllowPassCulling(false);
            //     builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));
            //
            //     ShadowMapHandles shadowMapHandles =
            //         pass.shadowMapRenderer.Record(renderGraph, builder, context);
            //
            //     return new LightingDataHandles(
            //         pass.directionalLightDataHandle, 
            //         pass.spotLightDataHandle,
            //         pass.pointLightDataHandle,
            //         pass.perObjectShadowCasterDataHandle,
            //         pass.forwardPlusTileBufferHandle,
            //         shadowMapHandles);
            // }
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