using ArcToon.Buffers;
using ArcToon.Data;
using ArcToon.Settings;
using ArcToon.System;
using ArcToon.Utils;
using ArcToon.Utils.Extensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Passes.Lighting
{
    /// <summary>
    /// Template method base class for rendering a single shadow atlas.
    /// Each subclass manages one atlas RTHandle + one structured buffer of ShadowTileBufferData.
    /// Lifecycle: Initialize → SetupResources → BuildRendererLists → RenderAtlas.
    /// </summary>
    public abstract class ShadowAtlasRenderer
    {
        // ─── Shared internal types ───

        protected struct RenderInfo
        {
            public RendererList handle;
            public Matrix4x4 view, projection;
            public float width, height;
        }

        protected struct ShadowMapTileData
        {
            public int atlasSize;
            public int tileCount;
            public int splitCount;
            public int tileSize;

            public ShadowMapTileData(int atlasSize, int tileCount)
            {
                this.atlasSize = atlasSize;
                this.tileCount = tileCount;
                splitCount = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4;
                tileSize = atlasSize / splitCount;
            }
        }

        // ─── Shared state (set by Initialize) ───

        protected CullingResults cullingResults;
        protected ShadowSettings settings;
        protected Camera camera;
        protected PerLightDataCollector collector;

        // Shared culling data (owned by ShadowMapRenderer orchestrator, passed by reference)
        protected NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;
        protected NativeArray<ShadowSplitData> shadowSplitDataPerLight;

        // ─── Per-atlas resources (set by SetupResources) ───

        protected RTHandle atlas;
        protected GraphicsBuffer dataBuffer;

        // ─── Abstract: subclass identity & configuration ───

        protected abstract string SampleName { get; }
        protected abstract int GetAtlasSize();
        protected abstract int GetActiveLightOrCasterCount();
        protected abstract bool UsePancaking { get; }
        protected abstract int AtlasSizePropertyID { get; }
        protected abstract int AtlasTexturePropertyID { get; }
        protected abstract int DataBufferPropertyID { get; }

        // ─── Abstract: per-light operations ───

        protected abstract void BuildRendererListForIndex(int index, ScriptableRenderContext context);
        protected abstract void RenderTilesForIndex(int index, CommandBuffer cmd);

        // ─── Template methods ───

        /// <summary>
        /// Store shared references. Called once per frame by the orchestrator.
        /// </summary>
        public void Initialize(CullingResults cullingResults, Camera camera,
            ShadowSettings settings, PerLightDataCollector collector,
            NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight,
            NativeArray<ShadowSplitData> shadowSplitDataPerLight)
        {
            this.cullingResults = cullingResults;
            this.camera = camera;
            this.settings = settings;
            this.collector = collector;
            this.cullingInfoPerLight = cullingInfoPerLight;
            this.shadowSplitDataPerLight = shadowSplitDataPerLight;
        }

        /// <summary>
        /// Bind RTHandle and GraphicsBuffer references from ShadowResources.
        /// Subclasses select real atlas or default shadow texture based on active light count.
        /// </summary>
        public abstract void SetupResources(ShadowResources resources);

        /// <summary>
        /// Build phase: compute tile layout, then delegate per-light RendererList creation to subclass.
        /// </summary>
        public void BuildRendererLists(ScriptableRenderContext context)
        {
            int count = GetActiveLightOrCasterCount();
            if (count <= 0) return;

            OnBuildRendererLists(count, context);
        }

        /// <summary>
        /// Subclasses must set up tileLayout and call BuildRendererListForIndex for each light.
        /// Default implementation handles the common case.
        /// </summary>
        protected virtual void OnBuildRendererLists(int count, ScriptableRenderContext context)
        {
            for (int i = 0; i < count; i++)
            {
                BuildRendererListForIndex(i, context);
            }
        }

        /// <summary>
        /// Render phase: set render target, iterate tiles, upload buffer data.
        /// </summary>
        public void RenderAtlas(CommandBuffer cmd)
        {
            int count = GetActiveLightOrCasterCount();
            if (count <= 0) return;

            int atlasSize = GetAtlasSize();
            Vector4 atlasSizes = new Vector4(1f / atlasSize, 1f / atlasSize, atlasSize, atlasSize);

            cmd.BeginSample(SampleName);
            cmd.SetRenderTarget(atlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(true, false, Color.clear);
            cmd.SetGlobalFloat(InternalShader.PropertyID.ShadowPancaking,
                UsePancaking ? 1f : 0f);

            for (int i = 0; i < count; i++)
            {
                RenderTilesForIndex(i, cmd);
            }

            cmd.SetGlobalVector(AtlasSizePropertyID, atlasSizes);
            OnUploadBufferData(cmd, count);
            OnPostRender(cmd);
            cmd.EndSample(SampleName);
        }

        /// <summary>
        /// Upload tile buffer data to GPU. Subclasses override if they have non-standard tile counts.
        /// </summary>
        protected abstract void OnUploadBufferData(CommandBuffer cmd, int activeLightCount);

        /// <summary>
        /// Virtual hook called after tile rendering and buffer upload, before EndSample.
        /// Override for extra work (e.g., cascade data upload, keyword setting).
        /// </summary>
        protected virtual void OnPostRender(CommandBuffer cmd) { }

        /// <summary>
        /// Bind this atlas's texture and buffer as global shader resources.
        /// Called by the orchestrator after all atlases are rendered.
        /// Subclasses may override to bind additional resources (e.g., cascade data).
        /// </summary>
        public virtual void SetGlobalBindings(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(AtlasTexturePropertyID, atlas);
            cmd.SetGlobalBuffer(DataBufferPropertyID, dataBuffer);
        }

        // ─── Shared utility ───

        protected static Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }
    }
}
