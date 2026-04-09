using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes.Transparency
{
    public class DepthPeelingPass : RenderPassBase
    {
        public override string Name => "Transparent Depth Peeling";

        private RendererList[] transparencyLists = new RendererList[RenderResources.DepthPeelingLayers];

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            for (int i = 0; i < RenderResources.DepthPeelingLayers; i++)
            {
                transparencyLists[i] = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardDepthPeeling, renderer.CullingResults, Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                });
            }
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueDepthBuffer, resources.depthAttachment);
            for (int i = 0; i < RenderResources.DepthPeelingLayers; i++)
            {
                commandBuffer.SetRenderTarget(resources.dpCompositeArray, resources.dpDualDepthBuffer[i % 2], 0, CubemapFace.Unknown, i);
                commandBuffer.ClearRenderTarget(true, true, Color.clear);
                commandBuffer.SetGlobalInteger(InternalShader.PropertyID.PeelingLayerIndex, i);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DualDepthBufferRef, resources.dpDualDepthBuffer[(i + 1) % 2]);
                commandBuffer.DrawRendererList(transparencyLists[i]);
            }
            RenderTextureHelpers.CopyTexture(commandBuffer, resources.colorAttachment, resources.dpOpaqueColorBuffer, RenderTextureHelpers.BlitMode.Color);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueColorBuffer, resources.dpOpaqueColorBuffer);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DepthPeelingClips, resources.dpCompositeArray);
            commandBuffer.SetRenderTarget(
                resources.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            // TODO: config
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 4);
        }
    }
}