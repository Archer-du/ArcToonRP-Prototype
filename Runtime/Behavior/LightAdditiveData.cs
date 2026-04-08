using UnityEngine;

namespace ArcToon.Runtime.Behavior
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("ArcToon/Light Additive Data")]
    public class LightAdditiveData : MonoBehaviour
    {
        [Header("PCSS")]
        [Range(0.1f, 100f)]
        [Tooltip("Per-light PCSS light size. Overrides the global ShadowSettings.lightSize when present.")]
        public float lightSize = 10f;
    }
}
