using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime.Passes
{
    public class TransparentPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Transparent");

        TransparencyMode transparencyMode => renderer.transparencyMode;
        
        #region Ordered
        RendererListHandle frontFaceList;
        RendererListHandle backFaceList;
        #endregion

        #region Weighted Average
        private TextureHandle backgroundColor;
        
        private TextureHandle accumulateRGBA;
        private TextureHandle revealage;
        
        private RendererListHandle geometryList;
        #endregion

        #region Depth Peeling
        // TODO: config
        public const int depthLayer = 4;

        private TextureHandle opaqueColorBuffer;
        private TextureHandle compositeArray;
        private TextureHandle[] dualDepthBuffer = new TextureHandle[2];
        
        private RendererListHandle[] transparencyLists = new RendererListHandle[depthLayer];
        #endregion
        
        RendererListHandle outlineList;

        public override bool AllowCulling() => false;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.BeginSample($"Toon Transparent ({transparencyMode})");
            if (transparencyMode == TransparencyMode.Ordered)
            {
                commandBuffer.DrawRendererList(backFaceList);
                commandBuffer.DrawRendererList(frontFaceList);
            }
            else if (transparencyMode == TransparencyMode.WeightedAverage)
            {
                commandBuffer.SetRenderTarget(
                    new RenderTargetIdentifier[]{ accumulateRGBA, revealage, }, 
                    resourceHandle.depthAttachment);
                commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                    new[]{ Color.clear, Color.white, });
                commandBuffer.DrawRendererList(geometryList);
                
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, accumulateRGBA);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, revealage);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, backgroundColor);
                RenderTextureHelpers.CopyTexture(commandBuffer, resourceHandle.colorAttachment, backgroundColor, RenderTextureHelpers.BlitMode.Color);
                
                commandBuffer.SetRenderTarget(
                    resourceHandle.colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    resourceHandle.depthAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
                // TODO: config
                commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
            }
            else if (transparencyMode == TransparencyMode.DepthPeeling)
            {
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueDepthBuffer, resourceHandle.depthAttachment);
                for (int i = 0; i < depthLayer; i++)
                {
                    commandBuffer.SetRenderTarget(compositeArray, dualDepthBuffer[i % 2], 0, CubemapFace.Unknown, i);
                    commandBuffer.ClearRenderTarget(true, true, Color.clear);
                    commandBuffer.SetGlobalInteger(InternalShader.PropertyID.PeelingLayerIndex, i);
                    commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DualDepthBufferRef, dualDepthBuffer[(i + 1) % 2]);
                    commandBuffer.DrawRendererList(transparencyLists[i]);
                }
                RenderTextureHelpers.CopyTexture(commandBuffer, resourceHandle.colorAttachment, opaqueColorBuffer, RenderTextureHelpers.BlitMode.Color);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueColorBuffer, opaqueColorBuffer);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DepthPeelingClips, compositeArray);
                commandBuffer.SetRenderTarget(
                    resourceHandle.colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    resourceHandle.depthAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                // TODO: config
                commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 4);
            }
            commandBuffer.EndSample($"Toon Transparent ({transparencyMode})");
            
            commandBuffer.BeginSample("Toon Outline");
            commandBuffer.DrawRendererList(outlineList);
            commandBuffer.EndSample("Toon Outline");
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            outlineList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.GeometryOutline, renderer.CullingResults, Camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.transparent,
            });
                        
            if (transparencyMode == TransparencyMode.Ordered)
            {
                backFaceList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentBackFace, renderer.CullingResults, Camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                });
                frontFaceList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardTransparentFrontFace, renderer.CullingResults, Camera)
                {
                    sortingCriteria = SortingCriteria.CommonTransparent,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                });
            }
            else if (transparencyMode == TransparencyMode.WeightedAverage)
            {
                geometryList = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardWeightedAverage, renderer.CullingResults, Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.transparent,
                    rendererConfiguration = PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                                            PerObjectData.LightProbeProxyVolume |
                                            PerObjectData.OcclusionProbeProxyVolume |
                                            PerObjectData.ReflectionProbes,
                });
                accumulateRGBA = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
                {
                    name = "Weighted Average Accumulate RGBA",
                    colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                });
                revealage = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
                {
                    name = "Weighted Average Revealage",
                    format = GraphicsFormat.R16_UNorm,
                });
                backgroundColor = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
                {
                    name = "Weighted Average Background Color",
                    colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                });
            }
            else if (transparencyMode == TransparencyMode.DepthPeeling)
            {
                compositeArray = renderGraph.CreateTexture(new TextureDesc(AttachmentSize.x, AttachmentSize.y)
                {
                    name = "Depth Peeling Composite Array",
                    colorFormat = SystemInfo.GetGraphicsFormat(renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR),
                    dimension = TextureDimension.Tex2DArray,
                    slices = 6,
                });
                dualDepthBuffer[0] = renderGraph.CreateTexture(new TextureDesc(resourceHandle.depthAttachment.GetDescriptor(renderGraph))
                {
                    name = "Depth Peeling Dual Depth Buffer 0",
                });
                dualDepthBuffer[1] = renderGraph.CreateTexture(new TextureDesc(resourceHandle.depthAttachment.GetDescriptor(renderGraph))
                {
                    name = "Depth Peeling Dual Depth Buffer 1",
                });
                opaqueColorBuffer = renderGraph.CreateTexture(new TextureDesc(resourceHandle.colorAttachment.GetDescriptor(renderGraph))
                {
                    name = "Depth Peeling Opaque Color Buffer",
                });

                for (int i = 0; i < depthLayer; i++)
                {
                    transparencyLists[i] = renderGraph.CreateRendererList(new RendererListDesc(InternalShader.TagId.ToonForwardDepthPeeling, renderer.CullingResults, Camera)
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
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.UseRendererList(outlineList);
            
            if (transparencyMode == TransparencyMode.Ordered)
            {
                builder.UseRendererList(backFaceList);
                builder.UseRendererList(frontFaceList);
            }
            else if (transparencyMode == TransparencyMode.WeightedAverage)
            {
                builder.UseRendererList(geometryList);
                
                builder.ReadWriteTexture(revealage);
                builder.ReadWriteTexture(accumulateRGBA);
                builder.ReadWriteTexture(backgroundColor);
            }
            else if (transparencyMode == TransparencyMode.DepthPeeling)
            {
                for (int i = 0; i < depthLayer; i++)
                {
                    builder.UseRendererList(transparencyLists[i]);
                }

                builder.ReadWriteTexture(compositeArray);
                builder.ReadWriteTexture(dualDepthBuffer[0]);
                builder.ReadWriteTexture(dualDepthBuffer[1]);
                builder.ReadWriteTexture(opaqueColorBuffer);
            }
            
            builder.ReadWriteTexture(resourceHandle.colorAttachment);
            builder.ReadWriteTexture(resourceHandle.depthAttachment);
            
            if (resourceHandle.colorCopy.IsValid())
            {
                builder.ReadTexture(resourceHandle.colorCopy);
            }
            if (resourceHandle.preDepthStencil.IsValid())
            {
                builder.ReadTexture(resourceHandle.preDepthStencil);
            }
            builder.ReadTexture(resourceHandle.stencilMask);
            
            builder.ReadTexture(resourceHandle.shadowMapHandle.directionalAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.spotAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.pointAtlas);
            builder.ReadTexture(resourceHandle.shadowMapHandle.perObjectAtlas);

            builder.ReadBuffer(resourceHandle.lightDataDirectional);
            builder.ReadBuffer(resourceHandle.lightDataSpot);
            builder.ReadBuffer(resourceHandle.lightDataPoint);
            builder.ReadBuffer(resourceHandle.perObjectShadowCasterData);
            builder.ReadBuffer(resourceHandle.forwardPlusTileBuffer);
            
            builder.ReadBuffer(resourceHandle.shadowMapHandle.cascadeShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.directionalShadowMatrices);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.spotShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.pointShadowData);
            builder.ReadBuffer(resourceHandle.shadowMapHandle.perObjectShadowData);
        }
    }
}