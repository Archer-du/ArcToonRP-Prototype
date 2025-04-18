using ArcToon.Runtime.Data;
using ArcToon.Runtime.Overrides;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class CopyFinalPass
    {
        static readonly ProfilingSampler sampler = new("Copy Final");

        CameraAttachmentCopier copier;

        TextureHandle source;
        TextureHandle result;

        CameraSettings.FinalBlendMode finalBlendMode;

        bool bicubicSampling;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            copier.CopyFinal(commandBuffer, source, finalBlendMode, bicubicSampling);
            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static void Record(RenderGraph renderGraph, 
            CameraSettings.FinalBlendMode finalBlendMode, bool bicubicSampling,
            in TextureHandle srcHandle,
            CameraAttachmentCopier copier)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out CopyFinalPass pass, sampler);
            
            pass.finalBlendMode = finalBlendMode;
            pass.bicubicSampling = bicubicSampling;
            pass.copier = copier;
            pass.source = builder.ReadTexture(srcHandle);
            pass.result = builder.WriteTexture(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
            
            builder.SetRenderFunc<CopyFinalPass>(static (pass, context) => pass.Render(context));
        }
    }
}