using ArcToon.Runtime.Utils;
using UnityEditor;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class GizmosPass : RenderPassBase
    {
        public override string Name => "Gizmos";

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            RenderTextureHelpers.BlitTexture(commandBuffer, 
                resources.depthAttachment, 
                BuiltinRenderTextureType.CameraTarget,
                ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 
                (int)RenderTextureHelpers.BlitMode.Depth);

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            context.DrawGizmos(Camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(Camera, GizmoSubset.PostImageEffects);
#endif
        }
    }
}