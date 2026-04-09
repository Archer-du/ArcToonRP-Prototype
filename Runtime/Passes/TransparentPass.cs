using ArcToon.Runtime.Data;
using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class TransparentPass : RenderPassBase
    {
        public override string Name => "Transparent";

        #region Ordered
        RendererList frontFaceList;
        RendererList backFaceList;
        #endregion

        #region Weighted Average
        private RendererList geometryList;
        #endregion

        #region Depth Peeling
        private RendererList[] transparencyLists = new RendererList[RenderResources.DepthPeelingLayers];
        #endregion
        
        RendererList outlineList;

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            outlineList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryOutline, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.transparent,
            });
            
            backFaceList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentBackFace, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            frontFaceList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentFrontFace, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            geometryList = context.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardWeightedAverage, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.transparent,
                rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume |
                                        PerObjectData.ReflectionProbes,
            });
            
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
            commandBuffer.BeginSample("Toon Transparent");

            // Ordered dual face
            commandBuffer.DrawRendererList(backFaceList);
            commandBuffer.DrawRendererList(frontFaceList);

            // Weighted average
            {
                commandBuffer.SetRenderTarget(
                    new RenderTargetIdentifier[]{ resources.waAccumulateRGBA, resources.waRevealage, }, 
                    resources.depthAttachment);
                commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                    new[]{ Color.clear, Color.white, });
                commandBuffer.DrawRendererList(geometryList);
                
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, resources.waAccumulateRGBA);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, resources.waRevealage);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, resources.waBackgroundColor);
                RenderTextureHelpers.CopyTexture(commandBuffer, resources.colorAttachment, resources.waBackgroundColor, RenderTextureHelpers.BlitMode.Color);
                
                commandBuffer.SetRenderTarget(
                    resources.colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    resources.depthAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
                // TODO: config
                commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
            }

            // Depth peeling
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

            commandBuffer.EndSample("Toon Transparent");
            
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }
    }
}