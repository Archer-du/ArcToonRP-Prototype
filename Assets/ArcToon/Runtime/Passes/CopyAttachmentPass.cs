using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public class CopyAttachmentPass
    {
        static readonly ProfilingSampler sampler = new("Copy Attachment");

        static readonly int
            colorCopyID = Shader.PropertyToID("_CameraColorTexture"),
            depthCopyID = Shader.PropertyToID("_CameraDepthTexture");

        bool copyColor, copyDepth;

        CameraAttachmentCopier copier;

        TextureHandle colorAttachment, depthAttachment, colorCopy, depthCopy;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            if (copyColor)
            {
                copier.Copy(commandBuffer, colorAttachment, colorCopy,
                    CameraAttachmentCopier.CopyChannel.ColorAttachment);
                commandBuffer.SetGlobalTexture(colorCopyID, colorCopy);
            }

            if (copyDepth)
            {
                copier.Copy(commandBuffer, depthAttachment, depthCopy,
                    CameraAttachmentCopier.CopyChannel.DepthAttachment);
                commandBuffer.SetGlobalTexture(depthCopyID, depthCopy);
            }

            if (CameraAttachmentCopier.RequiresRenderTargetResetAfterCopy)
            {
                commandBuffer.SetRenderTarget(
                    colorAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    depthAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
            }

            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph,
            bool copyColor,
            bool copyDepth,
            in CameraAttachmentTextureData textureData,
            CameraAttachmentCopier copier)
        {
            if (!copyColor && !copyDepth) return;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out CopyAttachmentPass pass, sampler);

            pass.copyColor = copyColor;
            pass.copyDepth = copyDepth;
            pass.copier = copier;

            pass.colorAttachment = builder.ReadTexture(textureData.colorAttachment);
            pass.depthAttachment = builder.ReadTexture(textureData.depthAttachment);
            if (copyColor)
            {
                pass.colorCopy = builder.WriteTexture(textureData.colorCopy);
            }

            if (copyDepth)
            {
                pass.depthCopy = builder.WriteTexture(textureData.depthCopy);
            }

            builder.SetRenderFunc<CopyAttachmentPass>(static (pass, context) => pass.Render(context));
        }
    }
}