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

        static readonly int depthStencilID = Shader.PropertyToID("_CameraDepthTexture");
        static readonly int stencilMaskID = Shader.PropertyToID("_StencilMaskTexture");
        
        private static ShaderTagId[] depthPrePassShaderTagIds =
        {
            new("DepthOnly"),
        };
        
        private static ShaderTagId[] stencilPrePassShaderTagIds =
        {
            new("FringeCaster"),
            new("EyeCaster"),
        };
        
        private static ShaderTagId[] stencilShaderTagIds =
        {
            new("FringeReceiver"),
            new("EyeReceiver"),
        };

        private RendererListHandle depthPrepassList;
        private RendererListHandle stencilPrepassList;
        private RendererListHandle stencilMaskList;

        private TextureHandle colorAttachment, depthAttachment;
        
        private TextureHandle depthStencilBuffer;
        private TextureHandle stencilMask;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            
            commandBuffer.SetRenderTarget(
                depthStencilBuffer,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            
            commandBuffer.BeginSample("Depth Prepass");
            commandBuffer.DrawRendererList(depthPrepassList);
            commandBuffer.EndSample("Depth Prepass");
            
            commandBuffer.BeginSample("Stencil Prepass");
            commandBuffer.DrawRendererList(stencilPrepassList);
            commandBuffer.EndSample("Stencil Prepass");
            
            commandBuffer.SetRenderTarget(
                stencilMask,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthStencilBuffer,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            
            commandBuffer.ClearRenderTarget(false, true, Color.clear);
            
            commandBuffer.BeginSample("Stencil Mask");
            commandBuffer.DrawRendererList(stencilMaskList);
            commandBuffer.EndSample("Stencil Mask");
            
            commandBuffer.SetGlobalTexture(depthStencilID, depthStencilBuffer);
            commandBuffer.SetGlobalTexture(stencilMaskID, stencilMask);

            // reset
            commandBuffer.SetRenderTarget(
                colorAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );

            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults,
            bool preCopyDepth,
            in CameraAttachmentHandles handles)
        {
            if (!preCopyDepth) return;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out DepthStencilPrePass pass, sampler);
            
            pass.depthPrepassList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(depthPrePassShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );
            pass.stencilPrepassList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(stencilPrePassShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );
            pass.stencilMaskList = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(stencilShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );

            pass.colorAttachment = builder.ReadTexture(handles.colorAttachment);
            pass.depthAttachment = builder.ReadTexture(handles.depthAttachment);

            pass.depthStencilBuffer = builder.ReadWriteTexture(handles.depthStencilBuffer);
            pass.stencilMask = builder.WriteTexture(handles.stencilMask);

            builder.SetRenderFunc<DepthStencilPrePass>(static (pass, context) => pass.Render(context));
        }
    }
}