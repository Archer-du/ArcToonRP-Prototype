using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        public bool enableSRPBatcher;
        public bool enableGPUInstancing;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(
                new ArcToonRenderPipelineParams
                {
                    enableSRPBatcher = enableSRPBatcher,
                    enableGPUInstancing = enableGPUInstancing
                }
            );
        }
    }
}
