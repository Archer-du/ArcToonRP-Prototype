using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/ArcToon Render Pipeline")]
    public class ArcToonRenderPipelineAsset : RenderPipelineAsset<ArcToonRenderPipelineInstance>
    {
        [SerializeField] bool enableSRPBatcher;
        [SerializeField] bool enableGPUInstancing;
        [SerializeField] bool allowHDR = true;

        [SerializeField] ShadowSettings shadowSettings;
        [SerializeField] PostFXSettings postFXSettings;

        protected override RenderPipeline CreatePipeline()
        {
            return new ArcToonRenderPipelineInstance(
                new ArcToonRenderPipelineParams
                {
                    enableSRPBatcher = enableSRPBatcher,
                    enableGPUInstancing = enableGPUInstancing,
                    allowHDR = allowHDR,
                },
                shadowSettings, postFXSettings
            );
        }
    }
}