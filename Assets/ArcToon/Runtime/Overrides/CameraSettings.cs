using System;
using ArcToon.Runtime.Settings;
using UnityEngine;
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

        public enum RenderScaleMode
        {
            Inherit,
            Multiply,
            Override
        }
        public bool maskLights = false;

        public RenderingLayerMask renderingLayerMask = -1;

        public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;

        [Range(0.1f, 2f)] public float renderScale = 1f;

        public bool copyDepth = true;
        public bool copyColor = true;

        public bool overridePostFX = false;

        public PostFXSettings postFXSettings = default;

        public FinalBlendMode finalBlendMode = new FinalBlendMode
        {
            source = BlendMode.One,
            destination = BlendMode.Zero
        };

        // FXAA
        public bool allowFXAA = false;
        
        public bool keepAlpha = false;

        public float GetRenderScale(float globalRenderScale)
        {
            float scale = renderScaleMode switch
            {
                RenderScaleMode.Inherit => globalRenderScale,
                RenderScaleMode.Override => renderScale,
                RenderScaleMode.Multiply => globalRenderScale * renderScale,
                _ => 1
            };
            return scale;
        }
    }
}