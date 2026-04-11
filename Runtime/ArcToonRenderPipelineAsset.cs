using ArcToon.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [SerializeField]
        private RenderPipelineConfig config;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(config);
        }
    }
}