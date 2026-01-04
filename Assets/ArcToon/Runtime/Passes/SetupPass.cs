using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SetupPass
    {
        static readonly ProfilingSampler sampler = new("Setup");
        private RenderGraphResourceData resourceData;

        Vector2Int attachmentSize;

        Camera camera;

        CameraClearFlags clearFlags;

        void Render(RenderGraphContext context)
        {
            context.renderContext.SetupCameraProperties(camera);
            CommandBuffer commandBuffer = context.cmd;
            commandBuffer.SetRenderTarget(
                resourceData.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resourceData.depthAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );

            commandBuffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth,
                clearFlags <= CameraClearFlags.Color,
                clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.CameraBufferSize, new Vector4(
                1f / attachmentSize.x, 1f / attachmentSize.y,
                attachmentSize.x, attachmentSize.y
            ));
            
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(CameraRenderer renderer, RenderGraph renderGraph, Camera camera,
            RenderGraphResourceData resourceData,
            Vector2Int attachmentSize,
            bool copyColor,
            bool copyDepth,
            bool useHDR)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out SetupPass pass, sampler);

            pass.resourceData = resourceData;
            pass.attachmentSize = attachmentSize;
            pass.camera = camera;
            pass.clearFlags = camera.clearFlags;
            if (pass.clearFlags > CameraClearFlags.Color)
            {
                pass.clearFlags = CameraClearFlags.Color;
            }

            resourceData.colorAttachment = renderGraph.CreateTexture(new TextureDesc(attachmentSize.x, attachmentSize.y)
            {
                name = "Color Attachment Buffer",
                colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });
            resourceData.depthAttachment = renderGraph.CreateTexture(new TextureDesc(attachmentSize.x, attachmentSize.y)
            {
                name = "Depth Attachment Buffer",
                depthBufferBits = DepthBits.Depth32,
            });
            if (copyColor)
            {
                resourceData.colorCopy = renderGraph.CreateTexture(new TextureDesc(attachmentSize.x, attachmentSize.y)
                {
                    name = "Color Copy",
                    colorFormat = SystemInfo.GetGraphicsFormat(useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                });
            }
            if (copyDepth)
            {
                resourceData.depthCopy = renderGraph.CreateTexture(new TextureDesc(attachmentSize.x, attachmentSize.y)
                {
                    name = "Depth Copy",
                    depthBufferBits = DepthBits.Depth32,
                });
            }
            
            resourceData.stencilMask = renderGraph.CreateTexture(new TextureDesc(attachmentSize.x, attachmentSize.y)
            {
                name = "Stencil Mask",
                colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
            });
            
            builder.WriteTexture(resourceData.colorAttachment);
            builder.WriteTexture(resourceData.depthAttachment);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SetupPass>(static (pass, context) => pass.Render(context));
        }
    }
}