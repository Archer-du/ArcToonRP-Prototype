using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public readonly struct RenderTextureHelpers
    {
        public enum CopyChannel
        {
            DepthAttachment = 1,
            ColorAttachment = 2,
        }

        public static void CopyTexture(CommandBuffer commandBuffer,
            RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, CopyChannel channel)
        {
            if (RenderPipelineInfo.CopyTextureSupported)
            {
                commandBuffer.CopyTexture(srcHandle, dstHandle);
            }
            else
            {
                BlitTexture(commandBuffer, srcHandle, dstHandle, 
                    ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.CameraCopy), (int)channel);
            }
        }

        public static void BlitTexture(CommandBuffer commandBuffer,
            RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, Material material, int shaderPass)
        {
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.SourceTexture, srcHandle);
            commandBuffer.SetRenderTarget(dstHandle,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.DrawProcedural(
                Matrix4x4.identity, 
                material, shaderPass,
                MeshTopology.Triangles, 3
            );
        }
    }
}