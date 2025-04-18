using ArcToon.Runtime.Overrides;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public readonly struct CameraAttachmentCopier
    {
        static readonly int sourceTextureID = Shader.PropertyToID("_SourceTexture");

        static readonly int finalSrcBlendID = Shader.PropertyToID("_FinalSrcBlend");
        static readonly int finalDstBlendID = Shader.PropertyToID("_FinalDstBlend");
        static readonly int _CopyBicubicID = Shader.PropertyToID("_CopyBicubic");
        
        static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f);

        static readonly bool copyTextureSupported =
            SystemInfo.copyTextureSupport > CopyTextureSupport.None;

        public static bool RequiresRenderTargetResetAfterCopy => !copyTextureSupported;

        public Camera Camera => camera;

        readonly Material copyMaterial;

        readonly Camera camera;

        public enum CopyChannel
        {
            DepthAttachment = 1,
            ColorAttachment = 2,
        }

        public CameraAttachmentCopier(Material copyMaterial, Camera camera)
        {
            this.copyMaterial = copyMaterial;
            this.camera = camera;
        }

        public void Copy(CommandBuffer commandBuffer,
            RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, CopyChannel channel)
        {
            if (copyTextureSupported)
            {
                commandBuffer.CopyTexture(srcHandle, dstHandle);
            }
            else
            {
                CopyCameraTexture(commandBuffer, srcHandle, dstHandle, channel);
            }
        }

        public void CopyFinal(CommandBuffer commandBuffer, 
            RenderTargetIdentifier srcHandle, CameraSettings.FinalBlendMode finalBlendMode, bool bicubicSampling)
        {
            commandBuffer.SetGlobalFloat(finalSrcBlendID, (float)finalBlendMode.source);
            commandBuffer.SetGlobalFloat(finalDstBlendID, (float)finalBlendMode.destination);

            commandBuffer.SetGlobalFloat(_CopyBicubicID, bicubicSampling ? 1f : 0f);
            
            commandBuffer.SetGlobalTexture(sourceTextureID, srcHandle);
            commandBuffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            commandBuffer.SetViewport(camera.pixelRect);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, copyMaterial, 0,
                MeshTopology.Triangles, 3
            );
        }

        public void CopyCameraTexture(CommandBuffer commandBuffer,
            RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, CopyChannel channel)
        {
            commandBuffer.SetGlobalTexture(sourceTextureID, srcHandle);
            commandBuffer.SetRenderTarget(dstHandle,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, copyMaterial, (int)channel,
                MeshTopology.Triangles, 3
            );
        }
    }
}