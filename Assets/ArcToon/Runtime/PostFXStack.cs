using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using static ArcToon.Runtime.Settings.PostFXSettings;

namespace ArcToon.Runtime
{
    public partial class PostFXStack
    {
        private CommandBuffer commandBuffer;

        ScriptableRenderContext context;

        Camera camera;

        PostFXSettings settings;
        CameraBufferSettings.FXAASettings fxaaSettings;

        private bool useHDR;
        private int colorLUTResolution;

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

            ColorGradingOnly,
            ColorGradingReinhard,
            ColorGradingNeutral,
            ColorGradingACES,
            ColorGradingApply,

            Copy,
            CopyFinal
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
            PostFXSettings settings,
            bool useHDR,
            int colorLUTResolution,
            CameraBufferSettings.FXAASettings fxaaSettings)
        {
            this.context = context;
            this.commandBuffer = commandBuffer;
            this.camera = camera;
            this.settings = settings;
            this.useHDR = useHDR;
            this.colorLUTResolution = colorLUTResolution;
            this.fxaaSettings = fxaaSettings;

            ApplySceneViewState();
        }

        public void CleanUp()
        {
            if (!IsActive) return;
        }

        public int Render(int rawFrameBufferId)
        {
            if (!IsActive)
            {
                return rawFrameBufferId;
            }
            commandBuffer.BeginSample("Post Processing");

            int RTID = rawFrameBufferId;
            RTID = DoBloom(RTID);
            RTID = DoColorGradingToneMapping(RTID);

            commandBuffer.EndSample("Post Processing");
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);

            return RTID;
        }

        void Draw(RenderTargetIdentifier srcRT, RenderTargetIdentifier dstRT, Pass pass)
        {
            commandBuffer.SetGlobalTexture(fxSourceId, srcRT);
            commandBuffer.SetRenderTarget(
                dstRT,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, settings.PostProcessStackMaterial, (int)pass,
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

        int DoBloom(int srcBufferId)
        {
            BloomSettings bloomSettings = settings.Bloom;

            // skip
            if (bloomSettings.maxIterations == 0 ||
                camera.pixelHeight < bloomSettings.downscaleLimit * 4 ||
                camera.pixelWidth < bloomSettings.downscaleLimit * 4 ||
                bloomSettings.intensity <= 0f)
            {
                return srcBufferId;
            }

            commandBuffer.BeginSample("Bloom");
            RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            // knee curve prefilter
            commandBuffer.SetGlobalVector(bloomThresholdId, GetKneeCurveData(bloomSettings));
            int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
            commandBuffer.GetTemporaryRT(
                bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
            );
            Draw(srcBufferId, bloomPrefilterId,
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
            if (bloomSettings.mode == BloomSettings.Mode.Additive)
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

            commandBuffer.SetGlobalTexture(fxSource2Id, srcBufferId);
            commandBuffer.SetGlobalFloat(bloomScaleId, finalScale);
            int resultBufferId = Shader.PropertyToID("_BloomResultBuffer");
            commandBuffer.GetTemporaryRT(
                resultBufferId, camera.pixelWidth, camera.pixelHeight, 0,
                FilterMode.Bilinear, format
            );
            Draw(srcId, resultBufferId, finalPass);
            commandBuffer.ReleaseTemporaryRT(srcId);

            commandBuffer.ReleaseTemporaryRT(srcBufferId);
            commandBuffer.EndSample("Bloom");

            return resultBufferId;
        }

        public Vector4 GetKneeCurveData(BloomSettings bloomSettings)
        {
            Vector4 thresholdData;
            thresholdData.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            thresholdData.y = thresholdData.x * bloomSettings.thresholdKnee;
            thresholdData.z = 2f * thresholdData.y;
            thresholdData.w = 1f / (4 * thresholdData.y + 0.00001f);
            thresholdData.y -= thresholdData.x;
            return thresholdData;
        }


        // ToneMapping ------------------------------
        private int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
        private int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
        private int colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");

        int DoColorGradingToneMapping(int srcBufferId)
        {
            commandBuffer.BeginSample("Tone Mapping");

            ConfigureColorAdjustments();
            ConfigureWhiteBalance();
            ConfigureSplitToning();
            ConfigureChannelMixer();
            ConfigureShadowsMidtonesHighlights();

            RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            // render LUT
            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            commandBuffer.GetTemporaryRT(
                colorGradingLUTId, lutWidth, lutHeight, 0,
                FilterMode.Bilinear, RenderTextureFormat.DefaultHDR
            );
            var mode = settings.ToneMapping.mode;
            Pass pass = Pass.ColorGradingOnly + (int)mode;
            commandBuffer.SetGlobalVector(colorGradingLUTParametersId,
                new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f))
            );
            commandBuffer.SetGlobalFloat(
                colorGradingLUTInLogCId, useHDR && pass != Pass.ColorGradingOnly ? 1f : 0f
            );
            Draw(srcBufferId, colorGradingLUTId, pass);

            // apply LUT
            int resultBufferId = Shader.PropertyToID("_ToneMappingResultBuffer");
            commandBuffer.GetTemporaryRT(
                resultBufferId, camera.pixelWidth, camera.pixelHeight, 0,
                FilterMode.Bilinear, format
            );
            commandBuffer.SetGlobalVector(colorGradingLUTParametersId,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            Draw(srcBufferId, resultBufferId, Pass.ColorGradingApply);
            commandBuffer.ReleaseTemporaryRT(colorGradingLUTId);

            commandBuffer.ReleaseTemporaryRT(srcBufferId);
            commandBuffer.EndSample("Tone Mapping");

            return resultBufferId;
        }

        private int colorAdjustmentDataId = Shader.PropertyToID("_ColorAdjustmentData");
        private int colorFilterId = Shader.PropertyToID("_ColorFilter");

        void ConfigureColorAdjustments()
        {
            ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
            commandBuffer.SetGlobalVector(colorAdjustmentDataId, new Vector4(
                Mathf.Pow(2f, colorAdjustments.postExposure),
                colorAdjustments.contrast * 0.01f + 1f,
                colorAdjustments.hueShift * (1f / 360f),
                colorAdjustments.saturation * 0.01f + 1f
            ));
            commandBuffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
        }

        private int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");

        void ConfigureWhiteBalance()
        {
            WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
            commandBuffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
                whiteBalance.temperature, whiteBalance.tint
            ));
        }

        private int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
        private int splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");

        void ConfigureSplitToning()
        {
            SplitToningSettings splitToning = settings.SplitToning;
            Color splitColor = splitToning.shadows;
            splitColor.a = splitToning.balance * 0.01f;
            commandBuffer.SetGlobalColor(splitToningShadowsId, splitColor);
            commandBuffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
        }

        private int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
        private int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
        private int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");

        void ConfigureChannelMixer()
        {
            ChannelMixerSettings channelMixer = settings.ChannelMixer;
            commandBuffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
            commandBuffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
            commandBuffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
        }

        private int smhShadowsId = Shader.PropertyToID("_SMHShadows");
        private int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
        private int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
        private int smhRangeId = Shader.PropertyToID("_SMHRange");

        void ConfigureShadowsMidtonesHighlights()
        {
            ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
            commandBuffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
            commandBuffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
            commandBuffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
            commandBuffer.SetGlobalVector(smhRangeId, new Vector4(
                smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
            ));
        }
    }
}