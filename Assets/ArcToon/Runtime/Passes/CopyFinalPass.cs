using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class CopyFinalPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Copy Final");

        TextureHandle source;
        TextureHandle backBuffer;

        CameraAdditiveData.FinalBlendMode finalBlendMode;

        bool bicubicSampling;

        public override void Initialize(RenderGraphResourceHandle resourceHandle, CameraRenderer renderer)
        {
            base.Initialize(resourceHandle, renderer);
            var bicubicRescalingMode = renderer.BufferSettings.bicubicRescalingMode;
            bicubicSampling =
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescalingMode == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                AttachmentSize.x < Camera.pixelWidth;
            finalBlendMode = renderer.CameraAdditiveData.finalBlendMode;
        }

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.FinalSrcBlend, (float)finalBlendMode.source);
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.FinalDstBlend, (float)finalBlendMode.destination);

            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.CopyBicubic, bicubicSampling ? 1f : 0f);
            
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.SourceTexture, resourceHandle.postFXResult);
            commandBuffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode.destination == BlendMode.Zero && Camera.rect == RenderPipelineInfo.FullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            commandBuffer.SetViewport(Camera.pixelRect);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, 
                ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.CameraCopy), 0,
                MeshTopology.Triangles, 3
            );
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            backBuffer = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.ReadTexture(resourceHandle.postFXResult);
            builder.WriteTexture(backBuffer);
        }
    }
}