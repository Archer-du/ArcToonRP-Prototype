using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Settings;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        private const string bufferName = "ArcToon Render Camera";

        static CameraSettings defaultCameraSettings = new();

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;

        private Camera camera;
        private CullingResults cullingResults;

        private Lighting lighting;

        private PostFXStack postFXStack;

        private bool useHDR;
        
        private bool useScaledRendering;
        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

        private static ShaderTagId[] shaderTagIds =
        {
            new("SRPDefaultUnlit"),
            new("SimpleLit"),
        };

        bool useIntermediateBuffer;

        Vector2Int bufferSize;
        private static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize");

        private static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        private static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");

        bool useDepthTexture;
        bool useColorTexture;
        private static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        private static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");

        private static int SourceTextureId = Shader.PropertyToID("_SourceTexture");


        private Material cameraCopyMaterial;

        private Texture2D missingCameraTexture;

        private CameraBufferSettings.BicubicRescalingMode bicubicRescalingMode;

        // TODO: move
        public struct FXAARuntimeConfig
        {
            public bool enabled;
            public bool keepAlpha;
            public float fixedThreshold;
            public float relativeThreshold;
            public float subpixelBlending;
            public CameraBufferSettings.FXAASettings.Quality quality;
        }
        public CameraRenderer(Shader cameraCopyShader)
        {
            commandBuffer = new()
            {
                name = bufferName
            };
            lighting = new();
            postFXStack = new();
            cameraCopyMaterial = CoreUtils.CreateEngineMaterial(cameraCopyShader);
            missingCameraTexture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Missing Camera Texture"
            };
            missingCameraTexture.SetPixel(0, 0, Color.white * 0.5f);
            missingCameraTexture.Apply(true, true);
        }

        public void Setup()
        {
        }

        public void Render(ScriptableRenderContext context, Camera camera,
            bool enableInstancing,
            int colorLUTResolution,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings, CameraBufferSettings bufferSettings)
        {
            // access settings
            var customCameraData = camera.GetComponent<ArcToonAdditiveCameraData>();
            CameraSettings cameraSettings =
                customCameraData ? customCameraData.Settings : defaultCameraSettings;
            if (cameraSettings.overridePostFX)
            {
                postFXSettings = cameraSettings.postFXSettings;
            }

            this.context = context;
            this.camera = camera;
            bicubicRescalingMode = bufferSettings.bicubicRescalingMode;
            useHDR = bufferSettings.allowHDR && camera.allowHDR;
            float renderScale = cameraSettings.GetRenderScale(bufferSettings.renderScale);
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

            if (camera.cameraType == CameraType.Reflection)
            {
                useDepthTexture = bufferSettings.copyDepthReflection;
                useColorTexture = bufferSettings.copyColorReflection;
            }
            else
            {
                useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
                useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
            }
            useIntermediateBuffer = useScaledRendering ||
                                    useDepthTexture || useColorTexture;
            // TODO: buffer settings translate
            FXAARuntimeConfig fxaaConfig = new FXAARuntimeConfig
            {
                enabled = bufferSettings.fxaaSettings.enabled && cameraSettings.allowFXAA,
                keepAlpha = cameraSettings.keepAlpha,
                fixedThreshold = bufferSettings.fxaaSettings.fixedThreshold,
                relativeThreshold = bufferSettings.fxaaSettings.relativeThreshold,
                subpixelBlending = bufferSettings.fxaaSettings.subpixelBlending,
                quality = bufferSettings.fxaaSettings.quality,
            };

            // editor only
            PrepareBuffer();
            PrepareForSceneWindow();

            // cull
            if (!Cull(shadowSettings.maxDistance)) return;

            // set up light data & render shadow maps
            lighting.Setup(context, cullingResults, shadowSettings);

            // set up camera data
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
            commandBuffer.SetGlobalVector(bufferSizeId, new Vector4(
                1f / bufferSize.x, 1f / bufferSize.y,
                bufferSize.x, bufferSize.y
            ));
            context.SetupCameraProperties(camera);
            commandBuffer.SetGlobalTexture(cameraDepthTextureId, missingCameraTexture);
            commandBuffer.SetGlobalTexture(cameraColorTextureId, missingCameraTexture);

            // set up post FX stacks
            postFXStack.Setup(
                context, commandBuffer, camera,
                bufferSize, 
                postFXSettings,
                useHDR,
                colorLUTResolution,
                fxaaConfig
            );
            useIntermediateBuffer |= postFXStack.IsActive;

            // set up render target
            SetupRenderTargets();

            // main render loop ------------------------------------
            // render geometry
            DrawVisibleGeometry(enableInstancing);
            DrawUnsupportedGeometry();
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);

            // post processing
            DrawGizmosBeforeFX();
            int finalBufferId = postFXStack.Render(colorAttachmentId);
            DrawGizmosAfterFX();

            if (useIntermediateBuffer)
            {
                CopyFinal(finalBufferId, cameraSettings.finalBlendMode);
            }

            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
            // main render loop end --------------------------------

            // clean up
            Cleanup();
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);

            // submit
            context.Submit();
        }

        void Cleanup()
        {
            lighting.CleanUp();
            postFXStack.CleanUp();
            if (useIntermediateBuffer)
            {
                commandBuffer.ReleaseTemporaryRT(colorAttachmentId);
                commandBuffer.ReleaseTemporaryRT(depthAttachmentId);
            }

            if (useDepthTexture)
            {
                commandBuffer.ReleaseTemporaryRT(cameraDepthTextureId);
            }

            if (useColorTexture)
            {
                commandBuffer.ReleaseTemporaryRT(cameraColorTextureId);
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(cameraCopyMaterial);
        }

        private void SetupRenderTargets()
        {
            if (useIntermediateBuffer)
            {
                commandBuffer.GetTemporaryRT(
                    colorAttachmentId, bufferSize.x, bufferSize.y,
                    0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                commandBuffer.GetTemporaryRT(
                    depthAttachmentId, bufferSize.x, bufferSize.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth
                );
                commandBuffer.SetRenderTarget(
                    colorAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            // else use default

            var flags = GetClearFlags();
            commandBuffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        }

        private void DrawVisibleGeometry(bool enableInstancing)
        {
            // render opaque
            RendererListDesc desc = new(shaderTagIds, cullingResults, camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            };
            var renderParams = RendererListDesc.ConvertToParameters(desc);
            renderParams.drawSettings.enableInstancing = enableInstancing;
            renderParams.drawSettings.perObjectData =
                PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                PerObjectData.LightProbeProxyVolume |
                PerObjectData.OcclusionProbeProxyVolume |
                PerObjectData.ReflectionProbes;

            commandBuffer.DrawRendererList(context.CreateRendererList(ref renderParams));

            // render skybox
            commandBuffer.DrawRendererList(context.CreateSkyboxRendererList(camera));
            if (useColorTexture || useDepthTexture)
            {
                CopyCameraAttachmentBuffer();
            }

            // render transparent
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            renderParams.drawSettings.sortingSettings = sortingSettings;
            renderParams.filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            renderParams.drawSettings.enableInstancing = enableInstancing;

            commandBuffer.DrawRendererList(context.CreateRendererList(ref renderParams));
        }

        private bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref p);
                return true;
            }

            return false;
        }

        static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

        void CopyCameraAttachmentBuffer()
        {
            if (useDepthTexture)
            {
                commandBuffer.GetTemporaryRT(
                    cameraDepthTextureId, bufferSize.x, bufferSize.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth
                );
                if (copyTextureSupported)
                {
                    commandBuffer.CopyTexture(depthAttachmentId, cameraDepthTextureId);
                }
                else
                {
                    CopyCameraTexture(depthAttachmentId, cameraDepthTextureId, CopyChannel.DepthAttachment);
                }
            }

            if (useColorTexture)
            {
                commandBuffer.GetTemporaryRT(
                    cameraColorTextureId, bufferSize.x, bufferSize.y,
                    0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                if (copyTextureSupported)
                {
                    commandBuffer.CopyTexture(colorAttachmentId, cameraColorTextureId);
                }
                else
                {
                    CopyCameraTexture(depthAttachmentId, cameraDepthTextureId, CopyChannel.ColorAttachment);
                }
            }

            // reset
            commandBuffer.SetRenderTarget(
                colorAttachmentId,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
        }

        enum CopyChannel
        {
            DepthAttachment = 1,
            ColorAttachment = 2,
        }

        void CopyCameraTexture(int srcBufferId, RenderTargetIdentifier dstBufferId, CopyChannel channel)
        {
            commandBuffer.SetGlobalTexture(SourceTextureId, srcBufferId);
            commandBuffer.SetRenderTarget(
                dstBufferId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, cameraCopyMaterial, (int)channel,
                MeshTopology.Triangles, 3
            );
        }

        private int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
        private int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
        private int _CopyBicubicId = Shader.PropertyToID("_CopyBicubic");
        static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);

        void CopyFinal(int srcBufferId, CameraSettings.FinalBlendMode finalBlendMode)
        {
            commandBuffer.BeginSample("Copy Final");

            commandBuffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
            commandBuffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
            bool bicubicSampling =
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                bufferSize.x < camera.pixelWidth;
            commandBuffer.SetGlobalFloat(_CopyBicubicId, bicubicSampling ? 1f : 0f);
            commandBuffer.SetGlobalTexture(SourceTextureId, srcBufferId);
            commandBuffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            commandBuffer.SetViewport(camera.pixelRect);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, cameraCopyMaterial, 0,
                MeshTopology.Triangles, 3
            );
            commandBuffer.ReleaseTemporaryRT(srcBufferId);
            commandBuffer.EndSample("Copy Final");
        }

        public CameraClearFlags GetClearFlags()
        {
            var flags = camera.clearFlags;
            if (postFXStack.IsActive && flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }

            return flags;
        }
    }
}