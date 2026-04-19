using ArcToon.Data;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class SkyboxPass : RenderPassBase
    {
        public override string Name => "Skybox";

        public SkyboxPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        RendererList list;

        public override void SetupRendererList(ScriptableRenderContext context)
        {
            list = context.CreateSkyboxRendererList(Camera);
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.DrawRendererList(list);
        }
    }
}