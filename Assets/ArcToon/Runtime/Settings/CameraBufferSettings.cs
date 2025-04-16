using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [Serializable]
    public struct CameraBufferSettings
    {
        public bool allowHDR;

        public bool copyDepth, copyDepthReflection;
        public bool copyColor, copyColorReflection;

        [Range(0.1f, 2f)] public float renderScale;
        
        public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }
        
        public BicubicRescalingMode bicubicRescalingMode;

        [Serializable]
        public struct FXAASettings
        {
            public bool enabled;
        }

        public FXAASettings fxaaSettings;
    }
}