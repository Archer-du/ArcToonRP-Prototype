using System;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Behavior
{
    [Serializable]
    public class CameraAdditiveData
    {
        public static CameraAdditiveData DefaultAdditiveData = new();
        
        public bool maskLights;

        public RenderingLayerMask renderingLayerMask = -1;

        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;
        
        public enum RenderScaleMode
        {
            Inherit,
            Multiply,
            Override
        }

        public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;
        
        [Range(0.1f, 2f)] public float renderScale = 1f;

        public bool copyDepth = true;
        public bool copyColor = true;

        public PostFXConfig overridePostFXConfig;

        [Serializable]
        public struct FinalBlendMode
        {
            public BlendMode source, destination;
        }
        
        public FinalBlendMode finalBlendMode = new()
        {
            source = BlendMode.One,
            destination = BlendMode.Zero
        };

        // FXAA
        public bool allowFXAA;
        
        public bool keepAlpha;

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