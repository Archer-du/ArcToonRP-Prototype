using UnityEngine;

namespace ArcToon.Runtime.Settings
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Post FX Settings")]
    public class PostFXSettings : ScriptableObject
    {
        [SerializeField] Shader postProcessShader;

        [System.NonSerialized] Material postProcessMaterial;

        [System.Serializable]
        public struct BloomSettings
        {
            [Range(0f, 16f)] public int maxIterations;

            [Min(1f)] public int downscaleLimit;

            [Min(0f)] public float threshold;

            [Range(0f, 1f)] public float thresholdKnee;

            [Min(0f)] public float intensity;

            public bool bicubicUpsampling;
        }

        [SerializeField] BloomSettings bloom;

        public BloomSettings Bloom => bloom;

        public Material PostProcessMaterial
        {
            get
            {
                if (postProcessMaterial == null && postProcessShader != null)
                {
                    postProcessMaterial = new Material(postProcessShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }

                return postProcessMaterial;
            }
        }
    }
}