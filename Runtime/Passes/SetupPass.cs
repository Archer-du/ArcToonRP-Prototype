using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SetupPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Setup");
        
        CameraClearFlags clearFlags;

        public override void Initialize(RenderGraphResourceHandle resourceHandle, CameraRenderer renderer)
        {
            base.Initialize(resourceHandle, renderer);
            clearFlags = renderer.RenderCamera.clearFlags;
            if (clearFlags > CameraClearFlags.Color)
            {
                clearFlags = CameraClearFlags.Color;
            }
        }

        public override bool AllowCulling() => false;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            context.SetupCameraProperties(Camera);
            
            commandBuffer.SetRenderTarget(
                resourceHandle.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resourceHandle.depthAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );

            commandBuffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth,
                clearFlags <= CameraClearFlags.Color,
                clearFlags == CameraClearFlags.Color ? Camera.backgroundColor.linear : Color.clear);

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.CameraBufferSize, new Vector4(
                1f / renderer.AttachmentSize.x, 1f / renderer.AttachmentSize.y,
                renderer.AttachmentSize.x, renderer.AttachmentSize.y
            ));
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            resourceHandle.colorAttachment = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Color Attachment Buffer",
                colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
            });
            resourceHandle.depthAttachment = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Depth Attachment Buffer",
                depthBufferBits = DepthBits.Depth32,
            });
            
            resourceHandle.preDepthStencil = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Depth Copy",
                depthBufferBits = DepthBits.Depth32,
            });
            resourceHandle.stencilMask = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
            {
                name = "Stencil Mask",
                colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.WriteTexture(resourceHandle.colorAttachment);
            builder.WriteTexture(resourceHandle.depthAttachment);
        }
    }
}