using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class ShadowSettings
    {
        public enum MapSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192
        }

        public enum FilterMode
        {
            PCF2x2,
            PCF3x3,
            PCF5x5,
            PCF7x7
        }

        public enum CascadeBlendMode
        {
            Dither,
            Soft,
        }

        [System.Serializable]
        public struct DirectionalCascadeShadow
        {
            public MapSize atlasSize;

            public CascadeBlendMode blendMode;

            [Range(1, 4)] public int cascadeCount;

            [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

            [Range(0.001f, 1f)] public float edgeFade;

            public Vector3 CascadeRatios =>
                new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
        }

        [FormerlySerializedAs("directionalCascade")] public DirectionalCascadeShadow directionalCascadeShadow = new()
        {
            atlasSize = MapSize._4096,
            cascadeCount = 4,
            cascadeRatio1 = 0.4f,
            cascadeRatio2 = 0.55f,
            cascadeRatio3 = 0.8f,
            edgeFade = 0.1f,
            blendMode = CascadeBlendMode.Dither
        };

        [System.Serializable]
        public struct PerObjectShadow
        {
            public MapSize atlasSize;
        }
        public PerObjectShadow perObjectShadow = new()
        {
            atlasSize = MapSize._4096
        };

        [System.Serializable]
        public struct SpotShadow
        {
            public MapSize atlasSize;
        }

        public SpotShadow spotShadow = new()
        {
            atlasSize = MapSize._4096,
        };

        [System.Serializable]
        public struct PointShadow
        {
            public MapSize atlasSize;
        }

        public PointShadow pointShadow = new()
        {
            atlasSize = MapSize._4096,
        };

        public enum FilterQuality
        {
            PCF2x2 = 2,
            PCF3x3 = 3,
            PCF5x5 = 5,
            PCF7x7 = 7,
            PoissonDisk,
            PCSS
        }

        public FilterQuality filterQuality = FilterQuality.PCF7x7;

        [Range(0.01f, 10f)] public float poissonFilterRadius = 4f;

        [Min(0.001f)] public float maxDistance = 100f;

        [Range(0.001f, 1f)] public float distanceFade = 0.1f;
        
        /// <summary>
        /// Maximum sampling footprint diameter in texels for the current filter mode.
        /// Used for two purposes:
        /// 1. CullingSphere shrinking: cullingSphere.w -= texelSize * FilterSize,
        ///    ensuring cascade-edge pixels don't sample across tile boundaries.
        /// 2. Normal bias scaling: normalBiasFactor = texelSize * FilterSize * sqrt(2),
        ///    larger filter radius requires proportionally larger normal bias to avoid shadow acne.
        /// </summary>
        public float FilterSize
        {
            get
            {
                switch (filterQuality)
                {
                    case FilterQuality.PoissonDisk:
                        // Poisson disk max offset = poissonDisk[i] * filterRadius * texelSize,
                        // where |poissonDisk[i]| <= 1.0, so max offset = ±filterRadius texels.
                        // The +1 accounts for hardware 2x2 PCF (SAMPLE_TEXTURE2D_SHADOW) which
                        // bilinearly interpolates a 2x2 neighborhood, extending ±0.5 texel beyond
                        // each sample point. Total footprint diameter = 2 * filterRadius + 1.
                        return 2 * poissonFilterRadius + 1;
                    case FilterQuality.PCSS:
                        // TODO:
                        // PCSS PCF stage uses a dynamic radius (penumbraWidth * poissonFilterRadius)
                        // that cannot be predicted on CPU side. Use a conservative fixed estimate.
                        // This mainly affects CullingSphere shrinking for directional lights;
                        // punctual lights use tile-bounds clamping which is independent of this value.
                        return 10;
                    default:
                        // PCF NxN Tent: the enum value equals the kernel width (e.g. PCF7x7 = 7).
                        // Tent filter sample offsets span ±(N-1)/2 texels, but each tap uses
                        // hardware 2x2 PCF (SAMPLE_TEXTURE2D_SHADOW), which extends the effective
                        // footprint to exactly N texels in diameter.
                        return (float)filterQuality;
                }
            }
        }
    }
}