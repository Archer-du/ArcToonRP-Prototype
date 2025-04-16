using ArcToon.Runtime.Settings;
using UnityEngine;

namespace ArcToon.Runtime.Overrides
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public class ArcToonAdditiveCameraData : MonoBehaviour
    {
        [SerializeField] CameraSettings settings;

        public CameraSettings Settings => settings ??= new CameraSettings();
    }
}