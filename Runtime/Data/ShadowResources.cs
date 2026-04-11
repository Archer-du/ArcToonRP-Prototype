using System;
using ArcToon.Buffers;
using ArcToon.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Data
{
    /// <summary>
    /// Shadow atlas RTHandles and shadow data GraphicsBuffers.
    /// Used by: ShadowMapRenderer (exclusively).
    /// </summary>
    public class ShadowResources : IDisposable
    {
        #region Shadow Atlases

        public RTHandle directionalShadowAtlas;
        public RTHandle spotShadowAtlas;
        public RTHandle pointShadowAtlas;
        public RTHandle perObjectShadowAtlas;

        /// <summary>
        /// 1x1 default shadow texture used when no shadow casters are present.
        /// Replaces renderGraph.defaultResources.defaultShadowTexture.
        /// </summary>
        public RTHandle defaultShadowTexture;

        #endregion

        #region Shadow Data Buffers

        public GraphicsBuffer cascadeShadowData;
        public GraphicsBuffer directionalShadowData;
        public GraphicsBuffer spotShadowData;
        public GraphicsBuffer pointShadowData;
        public GraphicsBuffer perObjectShadowData;

        #endregion

        public ShadowResources()
        {
            AllocateDefaultShadowTexture();
        }

        /// <summary>
        /// Allocate shadow atlas RTHandles and structured buffers based on shadow settings.
        /// Called once at pipeline creation or when settings change.
        /// </summary>
        public void Allocate(ShadowSettings settings)
        {
            // Shadow Atlases
            ReAllocateShadowAtlas(ref directionalShadowAtlas,
                (int)settings.directionalCascadeShadow.atlasSize, "Directional Shadow Atlas");
            ReAllocateShadowAtlas(ref spotShadowAtlas,
                (int)settings.spotShadow.atlasSize, "Spot Shadow Atlas");
            ReAllocateShadowAtlas(ref pointShadowAtlas,
                (int)settings.pointShadow.atlasSize, "Point Shadow Atlas");
            ReAllocateShadowAtlas(ref perObjectShadowAtlas,
                (int)settings.perObjectShadow.atlasSize, "Per Object Shadow Atlas");

            // Shadow Data Buffers
            ReAllocateStructuredBuffer(ref cascadeShadowData,
                RenderPipelineInfo.MaxCascades, ShadowCascadeBufferData.stride);
            ReAllocateStructuredBuffer(ref directionalShadowData,
                RenderPipelineInfo.MaxShadowedDirectionalLightCount * RenderPipelineInfo.MaxCascades,
                ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref spotShadowData,
                RenderPipelineInfo.MaxShadowedSpotLightCount, ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref pointShadowData,
                RenderPipelineInfo.MaxShadowedPointLightCount * 6, ShadowTileBufferData.stride);
            ReAllocateStructuredBuffer(ref perObjectShadowData,
                RenderPipelineInfo.MaxPerObjectShadowCasterCount *
                RenderPipelineInfo.MaxShadowedDirectionalLightCount,
                ShadowTileBufferData.stride);
        }

        public void Dispose()
        {
            directionalShadowAtlas?.Release();
            spotShadowAtlas?.Release();
            pointShadowAtlas?.Release();
            perObjectShadowAtlas?.Release();
            defaultShadowTexture?.Release();

            cascadeShadowData?.Release();
            directionalShadowData?.Release();
            spotShadowData?.Release();
            pointShadowData?.Release();
            perObjectShadowData?.Release();
        }

        private void AllocateDefaultShadowTexture()
        {
            defaultShadowTexture = RTHandles.Alloc(
                1, 1,
                depthBufferBits: DepthBits.Depth16,
                isShadowMap: true,
                name: "Default Shadow Texture"
            );
        }

        private static void ReAllocateShadowAtlas(ref RTHandle handle, int size, string name)
        {
            if (handle != null && handle.rt != null &&
                handle.rt.width == size && handle.rt.height == size)
                return;

            handle?.Release();
            handle = RTHandles.Alloc(
                size, size,
                depthBufferBits: DepthBits.Depth32,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                isShadowMap: true,
                name: name
            );
        }

        private static void ReAllocateStructuredBuffer(ref GraphicsBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count >= count && buffer.stride == stride)
                return;

            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }
    }
}
