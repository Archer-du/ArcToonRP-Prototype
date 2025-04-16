using System;
using ArcToon.Runtime.Settings;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Overrides
{
    [Serializable]
    public class CameraSettings
    {
        [Serializable]
        public struct FinalBlendMode
        {
            public BlendMode source, destination;
        }

        public bool copyDepth = true;
        public bool copyColor = true;
        
        public bool overridePostFX = false;

        public PostFXSettings postFXSettings = default;
        
        public FinalBlendMode finalBlendMode = new FinalBlendMode
        {
            source = BlendMode.One,
            destination = BlendMode.Zero
        };
        
        public bool allowFXAA = false;
    }
}