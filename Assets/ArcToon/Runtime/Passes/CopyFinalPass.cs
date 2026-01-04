using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
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

        CameraAdditiveData.FinalBlendMode finalBlendMode;

        bool bicubicSampling;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            copier.CopyFinal(commandBuffer, source, finalBlendMode, bicubicSampling);
            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static void Record(RenderGraph renderGraph,
            RenderGraphResourceData resourceData, TextureHandle postFXResult,
            CameraAdditiveData.FinalBlendMode finalBlendMode, bool bicubicSampling,
            CameraAttachmentCopier copier)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out CopyFinalPass pass, sampler);
            
            pass.finalBlendMode = finalBlendMode;
            pass.bicubicSampling = bicubicSampling;
            pass.copier = copier;
            
            pass.source = builder.ReadTexture(postFXResult);
            pass.result = builder.WriteTexture(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
            
            builder.SetRenderFunc<CopyFinalPass>(static (pass, context) => pass.Render(context));
        }
    }
}