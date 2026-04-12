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

        // TODO: deprecated
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
            enabled = true,
            fixedThreshold = 0.0338f,
            relativeThreshold = 0.084f,
            subpixelBlending = 1f,
            quality = FXAASettings.Quality.High,
        };
    }
}