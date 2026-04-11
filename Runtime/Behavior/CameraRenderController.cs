using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Behavior
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("ArcToon/Camera Render Controller")]
    public class CameraRenderController : MonoBehaviour
    {
        [FormerlySerializedAs("data")] [FormerlySerializedAs("settings")] [SerializeField] CameraAdditiveData additiveData;

        ProfilingSampler sampler;

        public CameraAdditiveData AdditiveData => additiveData ??= new CameraAdditiveData();
        public ProfilingSampler Sampler => sampler ??= new(GetComponent<Camera>().name);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnEnable() => sampler = null;
#endif
    }
}