using UnityEngine;

namespace ArcToon.Runtime.Behavior
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("ArcToon/ArcToon Light Data")]
    public class ArcToonLightData : MonoBehaviour
    {
        [Header("PCSS")]
        [Min(0.001f)]
        [Tooltip("Per-light PCSS light size. Overrides the global ShadowSettings.lightSize when present.")]
        public float lightSize = 1f;
    }
}
