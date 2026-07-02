using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class ScreenDebugPass : RenderPassBase
    {
        public override string Name => "Screen Debug";

        public ScreenDebugPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            DebuggerSingleton.RenderScreenDebug(commandBuffer, context);
        }
    }
}