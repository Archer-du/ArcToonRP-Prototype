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

        public static TextureHandle Record(CameraRenderer renderer, RenderGraph renderGraph, Camera camera,
            RenderGraphResourceData resourceData,
            CullingResults cullingResults, Vector2Int bufferSize,
            CameraAdditiveData cameraAdditiveData,
            CameraBufferSettings bufferSettings,
            PostFXConfig postFXConfig,
            bool useHDR)
        {
            bool hasActivePostFX =
                postFXConfig != null && PostFXConfig.AreApplicableTo(camera);
            if (!hasActivePostFX) return resourceData.colorAttachment;
            
            using (new RenderGraphProfilingScope(renderGraph, sampler))
            {
                PostFXStack postFXStack = new PostFXStack(postFXConfig.PostProcessStackMaterial);
                TextureHandle handle = resourceData.colorAttachment;
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