using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Post FX Settings")]
    public class PostFXSettings : ScriptableObject
    {
        [Serializable]
        public struct BloomSettings
        {
            public enum Mode
            {
                Additive,
                Scattering
            }

            public Mode mode;

            public bool ignoreRenderScale;
            
            [Range(0.05f, 0.95f)] public float scatter;

            [Range(0f, 16f)] public int maxIterations;

            [Min(1f)] public int downscaleLimit;

            [Min(0f)] public float threshold;

            [Range(0f, 1f)] public float thresholdKnee;

            [Min(0f)] public float intensity;

            public bool fadeFireflies;

            public bool bicubicUpsampling;
        }

        [SerializeField] BloomSettings bloom;
        public BloomSettings Bloom => bloom;


        [Serializable]
        public struct ColorAdjustmentsSettings
        {
            public float postExposure;

            [Range(-100f, 100f)] public float contrast;

            [ColorUsage(false, true)] public Color colorFilter;

            [Range(-180f, 180f)] public float hueShift;

            [Range(-100f, 100f)] public float saturation;
        }

        [SerializeField] ColorAdjustmentsSettings colorAdjustments = new()
        {
            colorFilter = Color.white
        };

        public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;


        [Serializable]
        public struct WhiteBalanceSettings
        {
            [Range(-100f, 100f)] public float temperature, tint;
        }

        [SerializeField] WhiteBalanceSettings whiteBalance;

        public WhiteBalanceSettings WhiteBalance => whiteBalance;


        [Serializable]
        public struct SplitToningSettings
        {
            [ColorUsage(false)] public Color shadows, highlights;

            [Range(-100f, 100f)] public float balance;
        }

        [SerializeField] SplitToningSettings splitToning = new SplitToningSettings
        {
            shadows = Color.gray,
            highlights = Color.gray
        };

        public SplitToningSettings SplitToning => splitToning;


        [Serializable]
        public struct ChannelMixerSettings
        {
            public Vector3 red, green, blue;
        }

        [SerializeField] ChannelMixerSettings channelMixer = new ChannelMixerSettings
        {
            red = Vector3.right,
            green = Vector3.up,
            blue = Vector3.forward
        };

        public ChannelMixerSettings ChannelMixer => channelMixer;

        [Serializable]
        public struct ShadowsMidtonesHighlightsSettings
        {
            [ColorUsage(false, true)] public Color shadows, midtones, highlights;

            [Range(0f, 2f)] public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
        }

        [SerializeField] ShadowsMidtonesHighlightsSettings
            shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
            {
                shadows = Color.white,
                midtones = Color.white,
                highlights = Color.white,
                shadowsEnd = 0.3f,
                highlightsStart = 0.55f,
                highLightsEnd = 1f
            };

        public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights =>
            shadowsMidtonesHighlights;

        [Serializable]
        public struct ToneMappingSettings
        {
            public enum Mode
            {
                None,
                Reinhard,
                Neutral,
                ACES,
            }

            public Mode mode;
        }

        [SerializeField] ToneMappingSettings toneMapping;
        public ToneMappingSettings ToneMapping => toneMapping;


        [SerializeField] Shader postProcessStackShader;

        [NonSerialized] Material postProcessStackMaterial;

        public Material PostProcessStackMaterial
        {
            get
            {
                if (postProcessStackMaterial == null && postProcessStackShader != null)
                {
                    postProcessStackMaterial = new Material(postProcessStackShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }

                return postProcessStackMaterial;
            }
        }
    }
}