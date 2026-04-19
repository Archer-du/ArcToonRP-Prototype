using System;
using UnityEngine;

namespace ArcToon.Settings
{
    [Serializable]
    public class CameraBufferSettings
    {
        public bool enableHDR = true;

        [Range(0.5f, 2f)] public float renderScale = 1.5f;
        
        public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }
        
        public BicubicRescalingMode bicubicRescalingMode = BicubicRescalingMode.UpOnly;
    }
}