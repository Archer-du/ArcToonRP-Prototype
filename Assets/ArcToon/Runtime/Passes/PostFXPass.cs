using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
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
            CameraAdditiveData cameraAdditiveData,
            CameraBufferSettings bufferSettings,
            PostFXConfig postFXConfig,
            bool useHDR,
            in TextureHandle srcHandle)
        {
            bool hasActivePostFX =
                postFXConfig != null && PostFXConfig.AreApplicableTo(camera);
            if (!hasActivePostFX) return srcHandle;
            
            using (new RenderGraphProfilingScope(renderGraph, sampler))
            {
                PostFXStack postFXStack = new PostFXStack(postFXConfig.PostProcessStackMaterial);
                TextureHandle handle = srcHandle;
                handle = BloomPass.Record(renderGraph, camera, cullingResults, bufferSize, 
                    cameraAdditiveData, bufferSettings, postFXConfig, useHDR,
                    handle, postFXStack);
                handle = ColorGradingPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraAdditiveData, bufferSettings, postFXConfig, useHDR,
                    handle, postFXStack);
                handle = AntiAliasingPass.Record(renderGraph, camera, cullingResults, bufferSize,
                    cameraAdditiveData, bufferSettings, postFXConfig, useHDR,
                    handle, postFXStack);
                return handle;
            }
        }
    }
}