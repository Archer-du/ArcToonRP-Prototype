using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public partial class PostFXStack
    {
        private CommandBuffer commandBuffer;

        ScriptableRenderContext context;

        Camera camera;

        PostFXSettings settings;

        private bool useHDR;

        public bool IsActive
        {
            get
            {
                if (camera.cameraType <= CameraType.SceneView)
                {
                    return settings != null;
                }

                return false;
            }
        }

        private int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
        private int fxSourceId = Shader.PropertyToID("_PostFXSource");
        private int fxSource2Id = Shader.PropertyToID("_PostFXSource2");

        enum Pass
        {
            BloomPrefilter,
            BloomPrefilterFireflies,
            BloomHorizontal,
            BloomVertical,
            BloomAdditive,
            BloomAdditiveFinal,
            BloomScatter,
            BloomScatterFinal,
            Copy
        }

        public PostFXStack()
        {
            bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
            for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }

        public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            PostFXSettings settings, bool useHDR)
        {
            this.context = context;
            this.commandBuffer = commandBuffer;
            this.camera = camera;
            this.settings = settings;
            this.useHDR = useHDR;

            ApplySceneViewState();

            if (IsActive)
            {
                commandBuffer.GetTemporaryRT(
                    frameBufferId, camera.pixelWidth, camera.pixelHeight,
                    32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                commandBuffer.SetRenderTarget(
                    frameBufferId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
        }

        public void CleanUp()
        {
            if (IsActive)
            {
                commandBuffer.ReleaseTemporaryRT(frameBufferId);
                ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
            }
        }

        public void Render()
        {
            if (!IsActive) return;

            commandBuffer.BeginSample("Post Processing");

            // Draw(frameBufferId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            DoBloom(frameBufferId);

            commandBuffer.EndSample("Post Processing");
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
        }

        void Draw(RenderTargetIdentifier srcRT, RenderTargetIdentifier dstRT, Pass pass)
        {
            commandBuffer.SetGlobalTexture(fxSourceId, srcRT);
            commandBuffer.SetRenderTarget(
                dstRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, settings.PostProcessMaterial, (int)pass,
                MeshTopology.Triangles, 3
            );
        }

        // Bloom ------------------------------
        const int maxBloomPyramidLevels = 16;

        private int bloomPyramidId;
        private int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        private int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        private int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
        private int bloomScaleId = Shader.PropertyToID("_BloomScale");
        private int bloomScatterId = Shader.PropertyToID("_BloomScatter");

        void DoBloom(int srcframeBufferId)
        {
            PostFXSettings.BloomSettings bloomSettings = settings.Bloom;

            commandBuffer.BeginSample("Bloom");

            // skip
            if (bloomSettings.maxIterations == 0 ||
                camera.pixelHeight < bloomSettings.downscaleLimit * 4 ||
                camera.pixelWidth < bloomSettings.downscaleLimit * 4 ||
                bloomSettings.intensity <= 0f)
            {
                Draw(srcframeBufferId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
                commandBuffer.EndSample("Bloom");
                return;
            }

            RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            // knee curve prefilter
            commandBuffer.SetGlobalVector(bloomThresholdId, GetKneeCurveData(bloomSettings));
            int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
            commandBuffer.GetTemporaryRT(
                bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
            );
            Draw(srcframeBufferId, bloomPrefilterId,
                bloomSettings.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
            width /= 2;
            height /= 2;

            // down sample
            int srcId = bloomPrefilterId;
            int tmpId = bloomPyramidId;
            int dstId = bloomPyramidId + 1;
            int i;
            for (i = 0; i < bloomSettings.maxIterations; i++)
            {
                if (height < bloomSettings.downscaleLimit ||
                    width < bloomSettings.downscaleLimit) break;

                commandBuffer.GetTemporaryRT(
                    tmpId, width, height, 0, FilterMode.Bilinear, format
                );
                commandBuffer.GetTemporaryRT(
                    dstId, width, height, 0, FilterMode.Bilinear, format
                );
                Draw(srcId, tmpId, Pass.BloomHorizontal);
                Draw(tmpId, dstId, Pass.BloomVertical);
                srcId = dstId;
                dstId += 2;
                tmpId += 2;
                width /= 2;
                height /= 2;
            }

            // up sample
            commandBuffer.ReleaseTemporaryRT(bloomPrefilterId);
            commandBuffer.ReleaseTemporaryRT(srcId - 1);
            commandBuffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloomSettings.bicubicUpsampling ? 1f : 0f);
            tmpId -= 4;
            Pass combinePass, finalPass;
            float finalScale;
            if (bloomSettings.mode == PostFXSettings.BloomSettings.Mode.Additive)
            {
                combinePass = Pass.BloomAdditive;
                finalPass = Pass.BloomAdditiveFinal;
                finalScale = bloomSettings.intensity;
            }
            else
            {
                combinePass = Pass.BloomScatter;
                finalPass = Pass.BloomScatterFinal;
                commandBuffer.SetGlobalFloat(bloomScatterId, bloomSettings.scatter);
                finalScale = bloomSettings.scatter;
            }
            for (i -= 1; i > 0; i--)
            {
                commandBuffer.SetGlobalTexture(fxSource2Id, tmpId + 1);
                Draw(srcId, tmpId, combinePass);
                commandBuffer.ReleaseTemporaryRT(srcId);
                commandBuffer.ReleaseTemporaryRT(tmpId + 1);
                srcId = tmpId;
                tmpId -= 2;
            }

            commandBuffer.SetGlobalTexture(fxSource2Id, srcframeBufferId);
            commandBuffer.SetGlobalFloat(bloomScaleId, finalScale);
            Draw(srcId, BuiltinRenderTextureType.CameraTarget, finalPass);
            commandBuffer.ReleaseTemporaryRT(srcId);

            commandBuffer.EndSample("Bloom");
        }

        public Vector4 GetKneeCurveData(PostFXSettings.BloomSettings bloomSettings)
        {
            Vector4 thresholdData;
            thresholdData.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            thresholdData.y = thresholdData.x * bloomSettings.thresholdKnee;
            thresholdData.z = 2f * thresholdData.y;
            thresholdData.w = 1f / (4 * thresholdData.y + 0.00001f);
            thresholdData.y -= thresholdData.x;
            return thresholdData;
        }

        public CameraClearFlags GetClearFlags()
        {
            var flags = camera.clearFlags;
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }

            return flags;
        }
    }
}