using System;
using UnityEngine;

namespace ArcToon.Passes.PostProcessing.Processors
{
    /// <summary>
    /// Volume config for the Color Grading post-processing effect.
    /// Fields are kept flat for serialization and Inspector compatibility
    /// (see ChatLogs/Bugfix/PostProcessConfigEditor_NestedStruct_Limitation_2026-04-19.md);
    /// [Header] attributes are used purely for visual grouping.
    /// </summary>
    [Serializable]
    public class ColorGradingVolumeConfig : PostProcessVolumeConfig
    {
        public override int Order => 200;

        [Header("Color Adjustments")]
        public float postExposure;

        [Range(-100f, 100f)] public float contrast = 5f;

        [ColorUsage(false, true)] public Color colorFilter = Color.white;

        [Range(-180f, 180f)] public float hueShift;

        [Range(-100f, 100f)] public float saturation = 20f;

        [Header("White Balance")]
        [Range(-100f, 100f)] public float temperature = -5f;
        [Range(-100f, 100f)] public float tint;

        [Header("Split Toning")]
        [ColorUsage(false)] public Color splitToningShadows = Color.gray;
        [ColorUsage(false)] public Color splitToningHighlights = Color.gray;
        [Range(-100f, 100f)] public float splitToningBalance;

        [Header("Channel Mixer")]
        public Vector3 channelMixerRed = Vector3.right;
        public Vector3 channelMixerGreen = Vector3.up;
        public Vector3 channelMixerBlue = Vector3.forward;

        [Header("Shadows Midtones Highlights")]
        [ColorUsage(false, true)] public Color smhShadows = Color.white;
        [ColorUsage(false, true)] public Color smhMidtones = Color.white;
        [ColorUsage(false, true)] public Color smhHighlights = Color.white;
        [Range(0f, 2f)] public float smhShadowsStart;
        [Range(0f, 2f)] public float smhShadowsEnd = 0.3f;
        [Range(0f, 2f)] public float smhHighlightsStart = 0.55f;
        [Range(0f, 2f)] public float smhHighlightsEnd = 1f;

        public enum ToneMappingMode
        {
            None,
            Reinhard,
            Neutral,
            ACES,
        }

        public enum ColorLUTResolution
        {
            _16 = 16,
            _32 = 32,
            _64 = 64
        }

        [Header("Tone Mapping")]
        public ToneMappingMode toneMapping = ToneMappingMode.Neutral;

        public ColorLUTResolution colorLUTResolution = ColorLUTResolution._64;

        protected override PostProcessor CreateProcessor() => new ColorGradingProcessor(this);
    }
}
