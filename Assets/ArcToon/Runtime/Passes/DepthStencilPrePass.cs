using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public class DepthStencilPrePass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Prepass");

        private static ShaderTagId[] DepthPrePassShaderTagIds =
        {
            InternalShader.TagId.DepthOnly,
            InternalShader.TagId.StencilOnly,
            InternalShader.TagId.DepthStencil,
        };
        private static ShaderTagId[] StencilMaskShaderTagIds =
        {
            InternalShader.TagId.FringeShadowReceiver,
            InternalShader.TagId.EyeLashesReceiver
        };

        private RendererListHandle opaqueDepthPrepassList;
        private RendererListHandle transparentDepthPrepassList;
        private RendererListHandle stencilMaskList;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetRenderTarget(
                resourceHandle.depthCopy,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            
            commandBuffer.BeginSample("Opaque Depth Stencil");
            commandBuffer.DrawRendererList(opaqueDepthPrepassList);
            commandBuffer.EndSample("Opaque Depth Stencil");
            
            commandBuffer.BeginSample("Transparent Depth Stencil");
            commandBuffer.DrawRendererList(transparentDepthPrepassList);
            commandBuffer.EndSample("Transparent Depth Stencil");

            commandBuffer.SetRenderTarget(
                resourceHandle.stencilMask,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resourceHandle.depthCopy,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            
            commandBuffer.BeginSample("Stencil Mask");
            commandBuffer.DrawRendererList(stencilMaskList);
            commandBuffer.EndSample("Stencil Mask");

            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.CameraDepthTexture, resourceHandle.depthCopy);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.StencilMaskTexture, resourceHandle.stencilMask);

            commandBuffer.SetRenderTarget(
                resourceHandle.colorAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                resourceHandle.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            opaqueDepthPrepassList = renderGraph.CreateRendererList(new RendererListDesc(DepthPrePassShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            });
            transparentDepthPrepassList = renderGraph.CreateRendererList(new RendererListDesc(DepthPrePassShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
            });
            stencilMaskList = renderGraph.CreateRendererList(new RendererListDesc(StencilMaskShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(opaqueDepthPrepassList);
            builder.UseRendererList(transparentDepthPrepassList);
            builder.UseRendererList(stencilMaskList);

            builder.ReadTexture(resourceHandle.colorAttachment);
            builder.ReadTexture(resourceHandle.depthAttachment);

            builder.ReadWriteTexture(resourceHandle.depthCopy);
            builder.WriteTexture(resourceHandle.stencilMask);
        }
    }
}