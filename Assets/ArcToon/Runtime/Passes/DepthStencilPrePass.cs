using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public class DepthStencilPrePass
    {
        static readonly ProfilingSampler sampler = new("Prepass");
        private RenderGraphResourceData resourceData;

        private static ShaderTagId[] depthPrePassShaderTagIds =
        {
            InternalShader.TagId.DepthOnly,
            InternalShader.TagId.StencilOnly,
            InternalShader.TagId.DepthStencil,
        };
        private static ShaderTagId[] stencilMaskShaderTagIds =
        {
            InternalShader.TagId.FringeShadowReceiver,
            InternalShader.TagId.EyeLashesReceiver
        };

        private RendererListHandle opaqueDepthPrepassList;
        private RendererListHandle transparentDepthPrepassList;
        private RendererListHandle stencilMaskList;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            
            commandBuffer.SetRenderTarget(
                resourceData.depthCopy,
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
                resourceData.stencilMask,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resourceData.depthCopy,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            
            commandBuffer.BeginSample("Stencil Mask");
            commandBuffer.DrawRendererList(stencilMaskList);
            commandBuffer.EndSample("Stencil Mask");

            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.CameraDepthTexture, resourceData.depthCopy);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.StencilMaskTexture, resourceData.stencilMask);

            // reset
            commandBuffer.SetRenderTarget(
                resourceData.colorAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                resourceData.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );

            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(CameraRenderer renderer, RenderGraph renderGraph, Camera camera,
            RenderGraphResourceData resourceData,
            CullingResults cullingResults,
            bool preCopyDepth)
        {
            if (!preCopyDepth) return;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out DepthStencilPrePass pass, sampler);

            // TODO: force initialize
            pass.resourceData = resourceData;
            
            pass.opaqueDepthPrepassList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(depthPrePassShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );
            pass.transparentDepthPrepassList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(depthPrePassShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                })
            );
            pass.stencilMaskList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(stencilMaskShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );

            builder.ReadTexture(resourceData.colorAttachment);
            builder.ReadTexture(resourceData.depthAttachment);

            builder.ReadWriteTexture(resourceData.depthCopy);
            builder.WriteTexture(resourceData.stencilMask);

            builder.SetRenderFunc<DepthStencilPrePass>(static (pass, context) => pass.Render(context));
        }
    }
}