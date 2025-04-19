using System.Diagnostics;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class DebugPass
    {
        static readonly ProfilingSampler sampler = new("Debug");

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Record(
            RenderGraph renderGraph,
            Camera camera,
            in LightDataHandles lightData)
        {
            if (CameraDebugger.IsActive &&
                camera.cameraType <= CameraType.SceneView)
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                    sampler.name, out DebugPass pass, sampler);
                
                builder.ReadBuffer(lightData.forwardPlusTileBufferHandle);
                
                builder.SetRenderFunc<DebugPass>(
                    static (pass, context) => CameraDebugger.Render(context));
            }
        }
    }
}