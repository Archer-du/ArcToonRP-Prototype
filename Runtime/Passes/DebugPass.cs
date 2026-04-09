using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class DebugPass : RenderPassBase
    {
        public override string Name => "Debug";

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            CameraDebugger.Render(commandBuffer, context);
        }
    }
}