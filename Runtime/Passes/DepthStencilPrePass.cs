using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class DepthStencilPrePass : RenderPassBase
    {
        public override string Name => "Prepass";

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

        private RendererList opaqueDepthPrepassList;
        private RendererList transparentDepthPrepassList;
        private RendererList stencilMaskList;

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            opaqueDepthPrepassList = context.CreateRendererList(new RendererListDesc(DepthPrePassShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            });
            transparentDepthPrepassList = context.CreateRendererList(new RendererListDesc(DepthPrePassShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
            });
            stencilMaskList = context.CreateRendererList(new RendererListDesc(StencilMaskShaderTagIds, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
            });
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetRenderTarget(
                resources.preDepthStencil,
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
                resources.stencilMask,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.preDepthStencil,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            
            commandBuffer.BeginSample("Stencil Mask");
            commandBuffer.DrawRendererList(stencilMaskList);
            commandBuffer.EndSample("Stencil Mask");

            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.CameraDepthTexture, resources.preDepthStencil);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.StencilMaskTexture, resources.stencilMask);

            commandBuffer.SetRenderTarget(
                resources.colorAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                resources.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
        }
    }
}