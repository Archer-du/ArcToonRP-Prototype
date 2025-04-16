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
        
        [Serializable]
        public struct FXAASettings
        {
            public bool enabled;
        }

        public FXAASettings fxaaSettings;
    }
}