using ArcToon.Data;
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

        private RendererList[] transparencyLists = new RendererList[TransparencyResources.DepthPeelingLayers];

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            for (int i = 0; i < TransparencyResources.DepthPeelingLayers; i++)
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
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueDepthBuffer, resources.Camera.depthAttachment);
            for (int i = 0; i < TransparencyResources.DepthPeelingLayers; i++)
            {
                commandBuffer.SetRenderTarget(resources.Transparency.dpCompositeArray, resources.Transparency.dpDualDepthBuffer[i % 2], 0, CubemapFace.Unknown, i);
                commandBuffer.ClearRenderTarget(true, true, Color.clear);
                commandBuffer.SetGlobalInteger(InternalShader.PropertyID.PeelingLayerIndex, i);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DualDepthBufferRef, resources.Transparency.dpDualDepthBuffer[(i + 1) % 2]);
                commandBuffer.DrawRendererList(transparencyLists[i]);
            }
            RenderTextureHelpers.CopyTexture(commandBuffer, resources.Camera.colorAttachment, resources.Transparency.dpOpaqueColorBuffer, RenderTextureHelpers.BlitMode.Color);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueColorBuffer, resources.Transparency.dpOpaqueColorBuffer);
            commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DepthPeelingClips, resources.Transparency.dpCompositeArray);
            commandBuffer.SetRenderTarget(
                resources.Camera.colorAttachment,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                resources.Camera.depthAttachment,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
            );
            // TODO: config
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 4);
        }
    }
}