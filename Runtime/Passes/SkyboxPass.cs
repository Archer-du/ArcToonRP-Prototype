using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class SkyboxPass : RenderPassBase
    {
        public override string Name => "Skybox";

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