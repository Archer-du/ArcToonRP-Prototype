﻿using System.Diagnostics;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class GizmosPass
    {
#if UNITY_EDITOR
        static readonly ProfilingSampler sampler = new("Gizmos");

        CameraAttachmentCopier copier;

        TextureHandle depthAttachment;

        void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;
            ScriptableRenderContext renderContext = context.renderContext;
            copier.CopyCameraTexture(buffer, depthAttachment, BuiltinRenderTextureType.CameraTarget,
                CameraAttachmentCopier.CopyChannel.DepthAttachment);

            renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            renderContext.DrawGizmos(copier.Camera, GizmoSubset.PreImageEffects);
            renderContext.DrawGizmos(copier.Camera, GizmoSubset.PostImageEffects);
        }
#endif
        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph,
            in CameraAttachmentHandles handles,
            CameraAttachmentCopier copier)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                    sampler.name, out GizmosPass pass, sampler);
                
                pass.copier = copier;
                pass.depthAttachment = builder.ReadTexture(handles.depthAttachment);
                
                builder.SetRenderFunc<GizmosPass>(static (pass, context) => pass.Render(context));
            }
#endif
        }
    }
}