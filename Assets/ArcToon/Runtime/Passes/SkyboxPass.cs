﻿using ArcToon.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class SkyboxPass
    {
        static readonly ProfilingSampler sampler = new("Skybox");

        RendererListHandle list;

        void Render(RenderGraphContext context)
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, 
            in CameraAttachmentHandles handles)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out SkyboxPass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateSkyboxRendererList(camera));
            builder.ReadWriteTexture(handles.colorAttachment);
            builder.ReadTexture(handles.depthAttachment);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SkyboxPass>(static (pass, context) => pass.Render(context));
        }
    }
}