using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Post FX Settings")]
    public class PostFXSettings : ScriptableObject
    {
        [System.Serializable]
        public struct BloomSettings
        {
            public enum Mode
            {
                Additive,
                Scattering
            }

            public Mode mode;

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
        

        [System.Serializable]
        public struct ToneMappingSettings
        {
            public enum Mode
            {
                None = -1, 
                Reinhard,
                Neutral,
                ACES,
            }

            public Mode mode;
        }

        [SerializeField] ToneMappingSettings toneMapping;
        public ToneMappingSettings ToneMapping => toneMapping;

        
        [SerializeField] Shader postProcessStackShader;

        [System.NonSerialized] Material postProcessStackMaterial;
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