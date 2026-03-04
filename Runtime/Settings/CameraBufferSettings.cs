using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Runtime.Settings
{
    [Serializable]
    public class CameraBufferSettings
    {
        [FormerlySerializedAs("allowHDR")] public bool enableHDR = true;

        public bool copyColor, copyColorReflection;

        [Range(0.5f, 2f)] public float renderScale = 1f;
        
        public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }
        
        public BicubicRescalingMode bicubicRescalingMode;

        [Serializable]
        public struct FXAASettings
        {
            public bool enabled;
            
            [Range(0.0312f, 0.0833f)]
            public float fixedThreshold;
            
            [Range(0.063f, 0.333f)]
            public float relativeThreshold;
            
            [Range(0f, 1f)]
            public float subpixelBlending;
            
            public enum Quality { Low, Medium, High }

            public Quality quality;
        }

        public FXAASettings fxaaSettings = new()
        {
            fixedThreshold = 0.0833f,
            relativeThreshold = 0.166f,
            subpixelBlending = 0.75f,
        };
    }
}