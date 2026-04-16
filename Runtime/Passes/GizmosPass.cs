using ArcToon.Utils;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class GizmosPass : RenderPassBase
    {
        public override string Name => "Gizmos";

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            BlitUtils.BlitTexture(commandBuffer, 
                resources.Camera.depthAttachment, 
                BuiltinRenderTextureType.CameraTarget,
                ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 
                (int)BlitUtils.BlitMode.Depth);

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            context.DrawGizmos(Camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(Camera, GizmoSubset.PostImageEffects);
#endif
        }
    }
}