using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SetupPass
    {
        static readonly ProfilingSampler sampler = new("Setup");

        bool useIntermediateAttachments;

        TextureHandle colorAttachment, depthAttachment;

        Vector2Int attachmentSize;

        Camera camera;

        CameraClearFlags clearFlags;

        static readonly int attachmentSizeID = Shader.PropertyToID("_CameraBufferSize");

        void Render(RenderGraphContext context)
        {
            // set up render target
            context.renderContext.SetupCameraProperties(camera);
            CommandBuffer commandBuffer = context.cmd;
            if (useIntermediateAttachments)
            {
                commandBuffer.SetRenderTarget(
                    colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }

            commandBuffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth,
                clearFlags <= CameraClearFlags.Color,
                clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

            commandBuffer.SetGlobalVector(attachmentSizeID, new Vector4(
                1f / attachmentSize.x, 1f / attachmentSize.y,
                attachmentSize.x, attachmentSize.y
            ));
            
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static CameraAttachmentTextureData Record(
            RenderGraph renderGraph, Camera camera,
            bool useIntermediateAttachments,
            bool copyColor,
            bool copyDepth,
            bool useHDR,
            Vector2Int attachmentSize)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out SetupPass pass, sampler);

            pass.useIntermediateAttachments = useIntermediateAttachments;
            pass.attachmentSize = attachmentSize;
            pass.camera = camera;
            pass.clearFlags = camera.clearFlags;
            TextureHandle colorAttachment, depthAttachment;
            TextureHandle colorCopy = default, depthCopy = default;
            if (useIntermediateAttachments)
            {
                if (pass.clearFlags > CameraClearFlags.Color)
                {
                    pass.clearFlags = CameraClearFlags.Color;
                }

                var desc = new TextureDesc(attachmentSize.x, attachmentSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                    name = "Color Attachment Buffer",
                };
                colorAttachment = pass.colorAttachment = builder.WriteTexture(renderGraph.CreateTexture(desc));
                if (copyColor)
                {
                    desc.name = "Color Copy";
                    colorCopy = renderGraph.CreateTexture(desc);
                }
                desc.depthBufferBits = DepthBits.Depth32;
                desc.name = "Depth Attachment Buffer";
                depthAttachment = pass.depthAttachment = builder.WriteTexture(renderGraph.CreateTexture(desc));
                if (copyDepth)
                {
                    desc.name = "Depth Copy";
                    depthCopy = renderGraph.CreateTexture(desc);
                }
            }
            else
            {
                colorAttachment = depthAttachment = pass.colorAttachment = pass.depthAttachment =
                    builder.WriteTexture(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
            }

            // disable pass culling
            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SetupPass>(static (pass, context) => pass.Render(context));
            
            return new CameraAttachmentTextureData(
                colorAttachment, depthAttachment, colorCopy, depthCopy);
        }
    }
}