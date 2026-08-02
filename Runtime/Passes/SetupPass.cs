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

        public SetupPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        public override void SetupFrameData(CommandBuffer cmd)
        {
            clearFlags = renderer.RenderCamera.clearFlags;
            if (clearFlags > CameraClearFlags.Color)
            {
                clearFlags = CameraClearFlags.Color;
            }
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            context.SetupCameraProperties(Camera);

            // Install the sub-pixel jittered projection so every geometry pass this frame (depth
            // prepass, opaque, transparent) renders with a matching offset. The view matrix is
            // unchanged; only the projection carries the jitter. Left untouched when TAA is off.
            if (renderer.TemporalAAActive)
            {
                commandBuffer.SetViewProjectionMatrices(Camera.worldToCameraMatrix, renderer.TemporalAAData.JitteredProjectionMatrix);
            }

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