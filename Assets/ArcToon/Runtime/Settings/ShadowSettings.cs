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
            Hard,
            Soft,
            Dither
        }

        [System.Serializable]
        public struct DirectionalCascade
        {
            public MapSize atlasSize;

            public FilterMode filterMode;

            public CascadeBlendMode blendMode;

            [Range(1, 4)] public int cascadeCount;

            [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

            [Range(0.001f, 1f)] public float edgeFade;

            public Vector3 CascadeRatios =>
                new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
        }

        public DirectionalCascade directionalCascade = new()
        {
            atlasSize = MapSize._1024,
            filterMode = FilterMode.PCF2x2,
            cascadeCount = 4,
            cascadeRatio1 = 0.1f,
            cascadeRatio2 = 0.25f,
            cascadeRatio3 = 0.5f,
            edgeFade = 0.1f,
            blendMode = CascadeBlendMode.Hard
        };

        [System.Serializable]
        public struct PointSpot
        {
            public MapSize atlasSize;

            public FilterMode filterMode;
        }

        public PointSpot pointSpot = new()
        {
            atlasSize = MapSize._1024,
            filterMode = FilterMode.PCF2x2
        };


        [Min(0.001f)] public float maxDistance = 100f;

        [Range(0.001f, 1f)] public float distanceFade = 0.1f;
    }
}