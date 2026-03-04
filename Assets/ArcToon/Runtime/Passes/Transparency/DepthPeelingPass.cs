using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Passes.Transparency
{
    public class DepthPeelingPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Transparent Depth Peeling");
        
        // TODO: config
        public const int depthLayer = 4;

        private TextureHandle opaqueColorBuffer;
        private TextureHandle compositeArray;
        private TextureHandle[] dualDepthBuffer = new TextureHandle[2];
        
        private RendererListHandle[] transparencyLists = new RendererListHandle[depthLayer];
        
        public override bool AllowCulling() => true;

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
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

        public override void AcquireResource(RenderGraph renderGraph)
        {
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
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            for (int i = 0; i < depthLayer; i++)
            {
                builder.UseRendererList(transparencyLists[i]);
            }
            builder.ReadWriteTexture(compositeArray);
            builder.ReadWriteTexture(dualDepthBuffer[0]);
            builder.ReadWriteTexture(dualDepthBuffer[1]);
            builder.ReadWriteTexture(opaqueColorBuffer);
            
            // general
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