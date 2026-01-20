using System.Diagnostics;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class GizmosPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Gizmos");
        
        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            RenderTextureHelpers.BlitTexture(commandBuffer, 
                resourceHandle.depthAttachment, 
                BuiltinRenderTextureType.CameraTarget,
                ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 
                (int)RenderTextureHelpers.CopyMode.DepthAttachment);

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            context.DrawGizmos(Camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(Camera, GizmoSubset.PostImageEffects);
#endif
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        { 
#if UNITY_EDITOR
            builder.ReadTexture(resourceHandle.depthAttachment);
#endif
        }
    }
}