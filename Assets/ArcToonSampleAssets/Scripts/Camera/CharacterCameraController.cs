using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Serialization;

namespace ArcToon.Scripts.Camera
{
    [System.Serializable]
    public class CinemachineCameraData
    {
        public CinemachineCamera camera;
        
        [Range(1f, 179f)]
        public float verticalFOV = 25f;
        
        [Min(0.01f)]
        public float nearClipPlane = 0.3f;
        
        [Min(0.1f)]
        public float farClipPlane = 200f;
        
        [Min(0.1f)]
        public float radius = 30f;

        public Transform target;
        
        public float targetHeight = 5f;
        
        public void ApplyParameters()
        {
            if (camera == null) return;
            
            camera.Lens.FieldOfView = verticalFOV;
            camera.Lens.NearClipPlane = nearClipPlane;
            camera.Lens.FarClipPlane = farClipPlane;
            
            var orbitalFollow = camera.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                orbitalFollow.Radius = radius;
            }

            if (target != null)
            {
                target.localPosition = new Vector3(0f, targetHeight, 0f);
            }
        }
    }

    public enum CinemachineCameraMode
    {
        FullBody,
        UpperBody,
        LowerBody,
        TrunkFocus,
        FaceFocus,
        CowboyShot,
    }
    
    public class CharacterCameraController : MonoBehaviour
    {
        public CinemachineCameraMode currentActiveMode = CinemachineCameraMode.FullBody;
        
        public CinemachineCameraData fullBodyCameraData;
        public CinemachineCameraData upperBodyCameraData;
        public CinemachineCameraData lowerBodyCameraData;
        public CinemachineCameraData trunkFocusCameraData;
        public CinemachineCameraData faceFocusCameraData;
        public CinemachineCameraData cowboyShotCameraData;

        private List<CinemachineCamera> virtualCameras;
        private CinemachineCamera currentActiveCamera;
        private float lastOperateTime;
        private bool isFreeLooking = false;
        
        private const float OperateInterval = 0.1f;
        private const int LowPriority = 10;
        private const int HighPriority = 100;

        private void Update()
        {
            if (Time.time - lastOperateTime < OperateInterval) return;

            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    currentActiveMode = (CinemachineCameraMode)i;
                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                isFreeLooking = !isFreeLooking;
            }

            RefreshActiveMode(currentActiveMode);
            RefreshFreeLook(currentActiveCamera, isFreeLooking);
        }

        private void OnValidate()
        {
            fullBodyCameraData.ApplyParameters();
            upperBodyCameraData.ApplyParameters();
            lowerBodyCameraData.ApplyParameters();
            cowboyShotCameraData.ApplyParameters();
            faceFocusCameraData.ApplyParameters();
            trunkFocusCameraData.ApplyParameters();

            virtualCameras ??= new List<CinemachineCamera>
            {
                fullBodyCameraData.camera,
                upperBodyCameraData.camera,
                lowerBodyCameraData.camera,
                trunkFocusCameraData.camera,
                faceFocusCameraData.camera,
                cowboyShotCameraData.camera,
            };
            
            RefreshActiveMode(currentActiveMode);
        }

        private void RefreshActiveMode(CinemachineCameraMode mode)
        {
            int index = (int)mode;
            if (index < 0 || index >= virtualCameras.Count) return;
            var targetCamera = virtualCameras[index];
            if (targetCamera == null || targetCamera == currentActiveCamera) return;

            foreach (var virtualCamera in virtualCameras)
            {
                if (virtualCamera != null)
                {
                    virtualCamera.Priority = LowPriority;
                }
            }
            targetCamera.Priority = HighPriority;
            ResetCameraAxis(targetCamera);

            currentActiveCamera = targetCamera;
            lastOperateTime = Time.time;
            // Debug.Log("Switch To " + mode);
        }

        private void RefreshFreeLook(CinemachineCamera targetCamera, bool isActive)
        {
            if (targetCamera == null) return;
            
            targetCamera.GetComponent<CinemachineInputAxisController>().enabled = isActive;
            Cursor.visible = !isActive;
            if (isActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                ResetCameraAxis(targetCamera);
            }
        }

        private void ResetCameraAxis(CinemachineCamera targetCamera)
        {
            var orbitalFollow = targetCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                orbitalFollow.HorizontalAxis.Value = orbitalFollow.HorizontalAxis.Center;
                orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.Center;
            }
        }
    }
}