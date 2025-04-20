using ArcToon.Runtime.Data;
using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Passes.PostProcess;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class PostFXPass
    {
        static readonly ProfilingSampler sampler = new("Post FX");

        public static TextureHandle Record(RenderGraph renderGraph, Camera camera,
            CullingResults cullingResults, Vector2Int bufferSize,
            CameraSettings cameraSettings,
            CameraBufferSettings bufferSettings,
            PostFXSettings postFXSettings,
            bool useHDR,
            in TextureHandle srcHandle)
        {
            bool hasActivePostFX =
                postFXSettings != null && PostFXSettings.AreApplicableTo(camera);
            if (!hasActivePostFX) return srcHandle;
            
            using (new RenderGraphProfilingScope(renderGraph, sampler))
            {
                PostFXStack postFXStack = new PostFXStack(postFXSettings.PostProcessStackMaterial);
                TextureHandle handle = srcHandle;
                handle = BloomPass.Record(renderGraph, camera, cullingResults, bufferSize, 
                    cameraSettings, bufferSettings, postFXSettings, useHDR,
                    handle, postFXStack);
                handle = ColorGradingPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraSettings, bufferSettings, postFXSettings, useHDR,
                    handle, postFXStack);
                handle = AntiAliasingPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraSettings, bufferSettings, postFXSettings, useHDR,
                    handle, postFXStack);
                return handle;
            }
        }
    }
}