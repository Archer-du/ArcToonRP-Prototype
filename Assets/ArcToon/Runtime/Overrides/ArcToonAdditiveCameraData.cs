using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Overrides
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public class ArcToonAdditiveCameraData : MonoBehaviour
    {
        [SerializeField] CameraSettings settings;

        ProfilingSampler sampler;

        public CameraSettings Settings => settings ??= new CameraSettings();
        public ProfilingSampler Sampler => sampler ??= new(GetComponent<Camera>().name);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnEnable() => sampler = null;
#endif
    }
}