using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [FormerlySerializedAs("globalConfig")] [FormerlySerializedAs("settings")] [SerializeField]
        private RenderPipelineConfig config;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(config);
        }
    }
}