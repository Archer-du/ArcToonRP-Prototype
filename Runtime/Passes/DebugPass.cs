using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class DebugPass : RenderPassBase
    {
        public override string Name => "Debug";

        public DebugPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            CameraDebugger.Render(commandBuffer, context);
        }
    }
}