using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes
{
    public class DepthPrePass
    {
        static readonly ProfilingSampler sampler = new("Depth Prepass");

        static readonly int depthCopyID = Shader.PropertyToID("_CameraDepthTexture");
        
        private static ShaderTagId[] depthOnlyShaderTagIds =
        {
            new("DepthOnly"),
        };

        private RendererListHandle list;

        private TextureHandle colorAttachment, depthAttachment;
        private TextureHandle depthCopy;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            
            commandBuffer.SetRenderTarget(
                depthCopy,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.DrawRendererList(list);
            
            commandBuffer.SetGlobalTexture(depthCopyID, depthCopy);

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
                sampler.name, out DepthPrePass pass, sampler);
            
            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(depthOnlyShaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                })
            );

            pass.colorAttachment = builder.ReadTexture(handles.colorAttachment);
            pass.depthAttachment = builder.ReadTexture(handles.depthAttachment);

            pass.depthCopy = builder.WriteTexture(handles.depthCopy);

            builder.SetRenderFunc<DepthPrePass>(static (pass, context) => pass.Render(context));
        }
    }
}