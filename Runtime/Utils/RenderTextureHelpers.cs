﻿using ArcToon.System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Utils
{
    public readonly struct RenderTextureHelpers
    {
        public enum BlitMode
        {
            Depth = 1,
            Color = 2,
        }

        public static void CopyTexture(CommandBuffer commandBuffer,
            RenderTargetIdentifier srcHandle, RenderTargetIdentifier dstHandle, BlitMode mode)
        {
            if (RenderPipelineInfo.CopyTextureSupported)
            {
                commandBuffer.CopyTexture(srcHandle, dstHandle);
            }
            else
            {
                Debug.LogWarning("copying texture without DMA copying texture supported.");
                BlitTexture(commandBuffer, srcHandle, dstHandle, 
                    ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), (int)mode);
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
        
        public static void BlitTexture(CommandBuffer commandBuffer, RenderTargetIdentifier dstHandle, Material material, int shaderPass)
        {
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