using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Passes;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        static CameraSettings defaultCameraSettings = new();

        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

        private PostFXStack postFXStack;

        private Material cameraCopyMaterial;

        public CameraRenderer(Shader cameraCopyShader)
        {
            postFXStack = new();
            cameraCopyMaterial = CoreUtils.CreateEngineMaterial(cameraCopyShader);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(cameraCopyMaterial);
        }

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

        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera,
            ShadowSettings shadowSettings,
            PostFXSettings postFXSettings,
            CameraBufferSettings bufferSettings
        )
        {
            // settings
            var additiveCameraData = camera.GetComponent<ArcToonAdditiveCameraData>();
            var cameraSettings = additiveCameraData ? additiveCameraData.Settings : defaultCameraSettings;
            postFXSettings = cameraSettings.overridePostFX ? cameraSettings.postFXSettings : postFXSettings;
            // hdr
            bool useHDR = bufferSettings.allowHDR && camera.allowHDR;

            // render scale
            float renderScale = cameraSettings.GetRenderScale(bufferSettings.renderScale);
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
            var bicubicRescalingMode = bufferSettings.bicubicRescalingMode;
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
            bool bicubicSampling =
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                bufferSize.x < camera.pixelWidth;

            // camera texture
            bool useColorTexture, useDepthTexture;
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

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                useScaledRendering = false;
            }
#endif
            // post fx
            bool hasActivePostFX =
                postFXSettings != null && PostFXSettings.AreApplicableTo(camera);
            bool useIntermediateBuffer = useScaledRendering || useDepthTexture || useColorTexture || hasActivePostFX;
            
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
            postFXStack.Setup(bufferSize, postFXSettings, useHDR, fxaaConfig);

            // cull
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                return;
            }

            scriptableCullingParameters.shadowDistance =
                Mathf.Min(shadowSettings.maxDistance, camera.farClipPlane);
            CullingResults cullingResults = context.Cull(ref scriptableCullingParameters);


            var cameraSampler =
                additiveCameraData ? additiveCameraData.Sampler : ProfilingSampler.Get(camera.cameraType);
            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                scriptableRenderContext = context,
                rendererListCulling = true,
            };
            CameraAttachmentCopier copier = new(cameraCopyMaterial, camera);
            renderGraph.BeginRecording(renderGraphParameters);
            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                var shadowData = LightingPass.Record(renderGraph, cullingResults, shadowSettings);

                var textureData = SetupPass.Record(renderGraph, camera,
                    useIntermediateBuffer, useColorTexture, useDepthTexture, useHDR, bufferSize);

                OpaquePass.Record(renderGraph, camera, cullingResults, textureData, shadowData);

                SkyboxPass.Record(renderGraph, camera, cullingResults, textureData);

                CopyAttachmentPass.Record(renderGraph, useColorTexture, useDepthTexture, textureData, copier);

                TransparentPass.Record(renderGraph, camera, cullingResults, textureData, shadowData);

                UnsupportedPass.Record(renderGraph, camera, cullingResults);

                var texture = PostFXPass.Record(renderGraph, camera, postFXStack,
                    postFXSettings ? (int)postFXSettings.ToneMapping.colorLUTResolution : 0, textureData.colorAttachment, hasActivePostFX);

                CopyFinalPass.Record(renderGraph, cameraSettings.finalBlendMode, bicubicSampling, texture, copier);
                
                GizmosPass.Record(renderGraph, useIntermediateBuffer, textureData, copier);
            }
            renderGraph.EndRecordingAndExecute();
            // submit
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }
    }
}