using ArcToon.Runtime.Data;
using ArcToon.Runtime.Passes.PostProcess;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class PostFXPass
    {
        static readonly ProfilingSampler sampler = new("Post FX");

        TextureHandle colorAttachment;

        PostFXStack stack;

        public static TextureHandle Record(RenderGraph renderGraph, Camera camera,
            PostFXStack stack,
            int colorLUTResolution,
            in TextureHandle srcHandle, 
            bool usePostFX)
        {
            if (!usePostFX) return srcHandle;
            using (new RenderGraphProfilingScope(renderGraph, sampler))
            {
                TextureHandle handle = srcHandle;
                handle = BloomPass.Record(renderGraph, camera, stack, handle);
                handle = ColorGradingPass.Record(renderGraph, stack, colorLUTResolution, handle);
                handle = AntiAliasingPass.Record(renderGraph, stack, handle);
                return handle;
            }
        }
    }
}