using ArcToon.Data;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Passes
{
    public class TransparentPass : RenderPassBase
    {
        public override string Name => "Transparent";

        public TransparentPass(RenderResources resources, CameraRenderer renderer) : base(resources, renderer) { }

        #region Ordered
        private RendererList frontFaceList;
        private RendererList backFaceList;
        #endregion

        #region Weighted Average
        private RendererList geometryList;
        
        private RTHandle accumulateRGBA;
        private RTHandle revealage;
        private RTHandle backgroundColor;
        #endregion

        #region Depth Peeling
        public const int DepthPeelingLayers = 4;
        
        private RendererList[] transparencyLists = new RendererList[DepthPeelingLayers];
        
        private RTHandle opaqueColorBuffer;
        private RTHandle compositeArray;
        private RTHandle[] dualDepthBuffer = new RTHandle[2];
        #endregion
        
        RendererList outlineList;

        public override void SetupFrameData(CommandBuffer cmd)
        {
            var colorFormat = SystemInfo.GetGraphicsFormat(
                renderer.useHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            int width = AttachmentSize.x;
            int height = AttachmentSize.y;

            // Weighted Average
            RenderingUtils.ReAllocateIfNeeded(
                ref accumulateRGBA, 
                new RenderTextureDescriptor(width, height, colorFormat, 0), 
                name: "Weighted Average Accumulate RGBA"
            );
            RenderingUtils.ReAllocateIfNeeded(
                ref revealage, 
                new RenderTextureDescriptor(width, height, GraphicsFormat.R16_UNorm, 0), 
                name: "Weighted Average Revealage"
            );
            RenderingUtils.ReAllocateIfNeeded(
                ref backgroundColor, 
                new RenderTextureDescriptor(width, height, colorFormat, 0), 
                name: "Weighted Average Background Color"
            );

            // Depth Peeling
            RenderingUtils.ReAllocateIfNeeded(
                ref compositeArray, 
                new RenderTextureDescriptor(width, height, colorFormat, 0)
                {
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = 6,
                }, 
                name: "Depth Peeling Composite Array"
            );
            var dpDepthDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.None, GraphicsFormat.D32_SFloat_S8_UInt);
            RenderingUtils.ReAllocateIfNeeded(ref dualDepthBuffer[0], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 0");
            RenderingUtils.ReAllocateIfNeeded(ref dualDepthBuffer[1], dpDepthDesc, name: "Depth Peeling Dual Depth Buffer 1");
            RenderingUtils.ReAllocateIfNeeded(
                ref opaqueColorBuffer, 
                new RenderTextureDescriptor(width, height, colorFormat, 0), 
                name: "Depth Peeling Opaque Color Buffer"
            );
        }

        public override void SetupRendererList(ScriptableRenderContext context)
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
            
            for (int i = 0; i < DepthPeelingLayers; i++)
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
                    new RenderTargetIdentifier[]{ accumulateRGBA, revealage, }, 
                    resources.Camera.depthAttachment);
                commandBuffer.ClearRenderTarget(RTClearFlags.Color, 
                    new[]{ Color.clear, Color.white, });
                commandBuffer.DrawRendererList(geometryList);
                
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateRGBA, accumulateRGBA);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.AccumulateComplexity, revealage);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.BackGroundColor, backgroundColor);
                BlitUtils.CopyTexture(commandBuffer, resources.Camera.colorAttachment, backgroundColor, BlitUtils.BlitMode.Color);
                
                commandBuffer.SetRenderTarget(
                    resources.Camera.colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    resources.Camera.depthAttachment,
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
                // TODO: config
                commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.Blitter), 3);
            }

            // Depth peeling
            {
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueDepthBuffer, resources.Camera.depthAttachment);
                for (int i = 0; i < DepthPeelingLayers; i++)
                {
                    commandBuffer.SetRenderTarget(compositeArray, dualDepthBuffer[i % 2], 0, CubemapFace.Unknown, i);
                    commandBuffer.ClearRenderTarget(true, true, Color.clear);
                    commandBuffer.SetGlobalInteger(InternalShader.PropertyID.PeelingLayerIndex, i);
                    commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DualDepthBufferRef, dualDepthBuffer[(i + 1) % 2]);
                    commandBuffer.DrawRendererList(transparencyLists[i]);
                }
                BlitUtils.CopyTexture(commandBuffer, resources.Camera.colorAttachment, opaqueColorBuffer, BlitUtils.BlitMode.Color);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.OpaqueColorBuffer, opaqueColorBuffer);
                commandBuffer.SetGlobalTexture(InternalShader.PropertyID.DepthPeelingClips, compositeArray);
                commandBuffer.SetRenderTarget(
                    resources.Camera.colorAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    resources.Camera.depthAttachment,
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

        public override void Dispose()
        {
            accumulateRGBA?.Release();
            revealage?.Release();
            backgroundColor?.Release();
            opaqueColorBuffer?.Release();
            compositeArray?.Release();
            dualDepthBuffer[0]?.Release();
            dualDepthBuffer[1]?.Release();
        }
    }
}