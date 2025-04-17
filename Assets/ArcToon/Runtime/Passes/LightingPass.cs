using ArcToon.Runtime.Settings;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class LightingPass
    {
        Lighting lighting;

        CullingResults cullingResults;

        ShadowSettings shadowSettings;

        void Render(RenderGraphContext context) => lighting.Setup(
            context.renderContext, cullingResults, shadowSettings);

        public static void Record(
            RenderGraph renderGraph, Lighting lighting,
            CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass("Lighting", out LightingPass pass);
            pass.lighting = lighting;
            pass.cullingResults = cullingResults;
            pass.shadowSettings = shadowSettings;
            builder.SetRenderFunc<LightingPass>((pass, context) => pass.Render(context));
        }
    }
}