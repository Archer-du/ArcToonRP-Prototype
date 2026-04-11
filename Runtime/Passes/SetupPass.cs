using ArcToon.Data;
using ArcToon.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class SetupPass : RenderPassBase
    {
        public override string Name => "Setup";

        CameraClearFlags clearFlags;

        public override void Setup(RenderResources resources, CameraRenderer renderer)
        {
            base.Setup(resources, renderer);
            clearFlags = renderer.RenderCamera.clearFlags;
            if (clearFlags > CameraClearFlags.Color)
            {
                clearFlags = CameraClearFlags.Color;
            }
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            context.SetupCameraProperties(Camera);

            commandBuffer.SetRenderTarget(
                resources.Camera.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.Camera.depthAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );

            commandBuffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth,
                clearFlags <= CameraClearFlags.Color,
                clearFlags == CameraClearFlags.Color ? Camera.backgroundColor.linear : Color.clear);

            commandBuffer.SetGlobalVector(InternalShader.PropertyID.CameraBufferSize, new Vector4(
                1f / renderer.AttachmentSize.x, 1f / renderer.AttachmentSize.y,
                renderer.AttachmentSize.x, renderer.AttachmentSize.y
            ));
        }
    }
}