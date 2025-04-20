using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [SerializeField]
        RenderPipelineSettings settings;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(settings);
        }
    }
}