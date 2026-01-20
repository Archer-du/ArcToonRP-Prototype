using System.Diagnostics;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class DebugPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Debug");

        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            CameraDebugger.Render(commandBuffer, context);
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.ReadBuffer(resourceHandle.forwardPlusTileBuffer);
        }
    }
}