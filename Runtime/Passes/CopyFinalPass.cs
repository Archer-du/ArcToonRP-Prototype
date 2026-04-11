using ArcToon.Behavior;
using ArcToon.Data;
using ArcToon.Runtime;
using ArcToon.Settings;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine.Rendering;

namespace ArcToon.Passes
{
    public class CopyFinalPass : RenderPassBase
    {
        public override string Name => "Copy Final";

        CameraAdditiveData.FinalBlendMode finalBlendMode;

        bool bicubicSampling;

        public override void Setup(RenderResources resources, CameraRenderer renderer)
        {
            base.Setup(resources, renderer);
            var bicubicRescalingMode = renderer.BufferSettings.bicubicRescalingMode;
            bicubicSampling =
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                AttachmentSize.x < Camera.pixelWidth;
            finalBlendMode = renderer.CameraAdditiveData.finalBlendMode;
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.FinalSrcBlend, (float)finalBlendMode.source);
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.FinalDstBlend, (float)finalBlendMode.destination);

            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.CopyBicubic, bicubicSampling ? 1f : 0f);
            
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.SourceTexture, resources.PostFX.postFXResult);
            commandBuffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode.destination == BlendMode.Zero && Camera.rect == RenderPipelineInfo.FullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            commandBuffer.SetViewport(Camera.pixelRect);
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 0);
        }
    }
}