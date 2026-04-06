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
        
        public float FilterSize
        {
            get
            {
                switch (filterQuality)
                {
                    case FilterQuality.PoissonDisk:
                    case FilterQuality.PCSS:
                        return 2 * poissonFilterRadius + 1;
                    default:
                        return (float)filterQuality;
                }
            }
        }
    }
}