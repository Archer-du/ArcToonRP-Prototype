using System.Runtime.InteropServices;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime
{
    public class ShadowRenderer
    {
        public TextureHandle directionalAtlas;
        public TextureHandle spotAtlas;
        public TextureHandle pointAtlas;

        public BufferHandle cascadeShadowDataHandle;
        public BufferHandle directionalShadowMatricesHandle;

        public BufferHandle spotShadowDataHandle;

        public BufferHandle pointShadowDataHandle;

        // TODO: adapt to record
        public ShadowMapHandles GetShadowMapHandles(
            RenderGraph renderGraph,
            RenderGraphBuilder builder)
        {
            int atlasSize = (int)settings.directionalCascade.atlasSize;
            var desc = new TextureDesc(atlasSize, atlasSize)
            {
                depthBufferBits = DepthBits.Depth32,
                isShadowMap = true,
                name = "Directional Shadow Atlas"
            };
            directionalAtlas = shadowedDirectionalLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            cascadeShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxCascades, ShadowCascadeBufferData.stride)
                {
                    name = "Shadow Cascades",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            directionalShadowMatricesHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedDirectionalLightCount * maxCascades, 4 * 16)
                {
                    name = "Directional Shadow Matrices",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            atlasSize = (int)settings.spotShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Spot Shadow Atlas";
            spotAtlas = shadowedSpotLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            spotShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedSpotLightCount, SpotShadowBufferData.stride)
                {
                    name = "Spot Shadow Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            atlasSize = (int)settings.pointShadow.atlasSize;
            desc.width = desc.height = atlasSize;
            desc.name = "Point Shadow Atlas";
            pointAtlas = shadowedPointLightCount > 0
                ? builder.WriteTexture(renderGraph.CreateTexture(desc))
                : renderGraph.defaultResources.defaultShadowTexture;

            pointShadowDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxShadowedPointLightCount * 6, PointShadowBufferData.stride)
                {
                    name = "Point Shadow Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            return new ShadowMapHandles(directionalAtlas, spotAtlas, pointAtlas, 
                cascadeShadowDataHandle, directionalShadowMatricesHandle, spotShadowDataHandle, pointShadowDataHandle);
        }

        public CommandBuffer commandBuffer;

        private ScriptableRenderContext context;

        private CullingResults cullingResults;

        private ShadowSettings settings;

        static readonly GlobalKeyword[] shadowMaskKeywords =
        {
            GlobalKeyword.Create("_SHADOW_MASK_ALWAYS"),
            GlobalKeyword.Create("_SHADOW_MASK_DISTANCE"),
        };

        private bool useShadowMask;

        private static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");


        // ----------------- directional shadow -----------------

        private static Matrix4x4[] directionalShadowVPMatrices =
            new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

        private static int directionalShadowVPMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");

        Vector4 directionalAtlasSizes;
        private static int directionalShadowAtlasSizeID = Shader.PropertyToID("_DirectionalShadowAtlasSize");

        private const int maxShadowedDirectionalLightCount = 4;
        private int shadowedDirectionalLightCount;

        static readonly GlobalKeyword[] directionalFilterKeywords =
        {
            GlobalKeyword.Create("_DIRECTIONAL_PCF3"),
            GlobalKeyword.Create("_DIRECTIONAL_PCF5"),
            GlobalKeyword.Create("_DIRECTIONAL_PCF7"),
        };

        private static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");

        private static readonly ShadowCascadeBufferData[] cascadeShadowData =
            new ShadowCascadeBufferData[maxCascades];

        private static int cascadeShadowDataID = Shader.PropertyToID("_ShadowCascadeData");

        private const int maxCascades = 4;

        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");


        static readonly GlobalKeyword[] cascadeBlendKeywords =
        {
            GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
            GlobalKeyword.Create("_CASCADE_BLEND_DITHER"),
        };


        // ----------------- spot shadow -----------------
        private static readonly SpotShadowBufferData[] spotShadowData =
            new SpotShadowBufferData[maxShadowedSpotLightCount];

        private static int spotShadowDataID = Shader.PropertyToID("_SpotShadowData");

        private static int spotShadowAtlasID = Shader.PropertyToID("_SpotShadowAtlas");

        Vector4 spotAtlasSizes;
        private static int spotShadowAtlasSizeID = Shader.PropertyToID("_SpotShadowAtlasSize");

        private const int maxShadowedSpotLightCount = 16;
        private int shadowedSpotLightCount;


        static GlobalKeyword[] spotFilterKeywords =
        {
            GlobalKeyword.Create("_SPOT_PCF3"),
            GlobalKeyword.Create("_SPOT_PCF5"),
            GlobalKeyword.Create("_SPOT_PCF7"),
        };


        // ----------------- point shadow -----------------
        private static readonly PointShadowBufferData[] pointShadowData =
            new PointShadowBufferData[maxShadowedPointLightCount * 6];

        private static int pointShadowDataID = Shader.PropertyToID("_PointShadowData");

        private static int pointShadowAtlasID = Shader.PropertyToID("_PointShadowAtlas");

        Vector4 pointAtlasSizes;
        private static int pointShadowAtlasSizeID = Shader.PropertyToID("_PointShadowAtlasSize");

        private const int maxShadowedPointLightCount = 2;
        private int shadowedPointLightCount;

        static GlobalKeyword[] pointFilterKeywords =
        {
            GlobalKeyword.Create("_POINT_PCF3"),
            GlobalKeyword.Create("_POINT_PCF5"),
            GlobalKeyword.Create("_POINT_PCF7"),
        };


        public void Setup(CullingResults cullingResults,
            ShadowSettings settings
        )
        {
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedSpotLightCount = shadowedPointLightCount = 0;

            useShadowMask = false;
        }

        struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[maxShadowedDirectionalLightCount];

        public Vector4 ReservePerLightShadowDataDirectional(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                // only baked shadows are used
                if (shadowedDirectionalLightCount >= maxShadowedDirectionalLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    // a trick to only sample baked shadow
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedDirectionalLightIndex = shadowedDirectionalLightCount++;
                shadowMapDataDirectionals[shadowedDirectionalLightIndex] =
                    new ShadowMapDataDirectional
                    {
                        visibleLightIndex = visibleLightIndex,
                        // TODO: interpreting light settings differently than their original purpose, use additional data instead
                        slopeScaleBias = light.shadowBias,
                        nearPlaneOffset = light.shadowNearPlane,
                    };
                return new Vector4(
                    light.shadowStrength,
                    shadowedDirectionalLightIndex * settings.directionalCascade.cascadeCount,
                    light.shadowNormalBias, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[maxShadowedSpotLightCount];

        public Vector4 ReservePerLightShadowDataSpot(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedSpotLightCount >= maxShadowedSpotLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedSpotLightIndex = shadowedSpotLightCount++;
                shadowMapDataSpots[shadowedSpotLightIndex] = new ShadowMapDataSpot
                {
                    visibleLightIndex = visibleLightIndex,
                    // TODO: interpreting light settings differently than their original purpose, use additional data instead
                    slopeScaleBias = light.shadowBias,
                    normalBias = light.shadowNormalBias,
                };
                return new Vector4(
                    light.shadowStrength, shadowedSpotLightIndex, 0, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        struct ShadowMapDataPoint
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataPoint[] shadowMapDataPoints =
            new ShadowMapDataPoint[maxShadowedPointLightCount];

        public Vector4 ReservePerLightShadowDataPoint(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                float maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedPointLightCount >= maxShadowedPointLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                int shadowedPointLightIndex = shadowedPointLightCount++;
                shadowMapDataPoints[shadowedPointLightIndex] = new ShadowMapDataPoint
                {
                    visibleLightIndex = visibleLightIndex,
                    // TODO: interpreting light settings differently than their original purpose, use additional data instead
                    slopeScaleBias = light.shadowBias,
                    normalBias = light.shadowNormalBias,
                };
                return new Vector4(
                    light.shadowStrength, shadowedPointLightIndex * 6, 0, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }


        public void RenderShadowMap(RenderGraphContext context)
        {
            commandBuffer = context.cmd;
            this.context = context.renderContext;

            if (shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadowMap();
            }

            if (shadowedSpotLightCount > 0)
            {
                RenderSpotShadowMap();
            }

            if (shadowedPointLightCount > 0)
            {
                RenderPointShadowMap();
            }
            commandBuffer.SetGlobalBuffer(
                cascadeShadowDataID, cascadeShadowDataHandle);
            commandBuffer.SetGlobalBuffer(
                directionalShadowVPMatricesID, directionalShadowMatricesHandle);
            commandBuffer.SetGlobalBuffer(spotShadowDataID, spotShadowDataHandle);
            commandBuffer.SetGlobalBuffer(pointShadowDataID, pointShadowDataHandle);
            
            commandBuffer.SetGlobalTexture(dirShadowAtlasID, directionalAtlas);
            commandBuffer.SetGlobalTexture(spotShadowAtlasID, spotAtlas);
            commandBuffer.SetGlobalTexture(pointShadowAtlasID, pointAtlas);

            SetKeywords(shadowMaskKeywords,
                useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

            commandBuffer.SetGlobalInt(cascadeCountId,
                shadowedDirectionalLightCount > 0 ? settings.directionalCascade.cascadeCount : -1);

            float f = 1f - settings.directionalCascade.edgeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeID,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        }

        void RenderDirectionalShadowMap()
        {
            int atlasSize = (int)settings.directionalCascade.atlasSize;
            directionalAtlasSizes.x = directionalAtlasSizes.y = 1f / atlasSize;
            directionalAtlasSizes.z = directionalAtlasSizes.w = atlasSize;

            commandBuffer.SetRenderTarget(
                directionalAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 1f);
            // TODO: config
            int tiles = shadowedDirectionalLightCount * settings.directionalCascade.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i, split, tileSize);
            }

            commandBuffer.SetGlobalVector(directionalShadowAtlasSizeID, directionalAtlasSizes);
            commandBuffer.SetBufferData(
                cascadeShadowDataHandle, cascadeShadowData,
                0, 0, settings.directionalCascade.cascadeCount);

            commandBuffer.SetBufferData(
                directionalShadowMatricesHandle, directionalShadowVPMatrices,
                0, 0, shadowedDirectionalLightCount * settings.directionalCascade.cascadeCount);

            SetKeywords(
                directionalFilterKeywords, (int)settings.directionalCascade.filterMode - 1
            );
            SetKeywords(
                cascadeBlendKeywords, (int)settings.directionalCascade.blendMode - 1
            );
        }

        void RenderSpotShadowMap()
        {
            int atlasSize = (int)settings.spotShadow.atlasSize;
            spotAtlasSizes.x = spotAtlasSizes.y = 1f / atlasSize;
            spotAtlasSizes.z = spotAtlasSizes.w = atlasSize;

            commandBuffer.SetRenderTarget(
                spotAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 0f);
            // TODO: config
            int tiles = shadowedSpotLightCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedSpotLightCount; i++)
            {
                RenderSpotShadowSplitTile(i, split, tileSize);
            }

            commandBuffer.SetGlobalVector(spotShadowAtlasSizeID, spotAtlasSizes);
            commandBuffer.SetBufferData(spotShadowDataHandle, spotShadowData,
                0, 0, tiles);

            SetKeywords(
                spotFilterKeywords, (int)settings.spotShadow.filterMode - 1
            );
        }

        void RenderPointShadowMap()
        {
            int atlasSize = (int)settings.pointShadow.atlasSize;
            pointAtlasSizes.x = pointAtlasSizes.y = 1f / atlasSize;
            pointAtlasSizes.z = pointAtlasSizes.w = atlasSize;

            commandBuffer.SetRenderTarget(
                pointAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingID, 0f);
            // TODO: config
            int tiles = shadowedPointLightCount * 6;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedPointLightCount; i++)
            {
                RenderPointShadowSplitTile(i, split, tileSize);
            }

            commandBuffer.SetGlobalVector(pointShadowAtlasSizeID, pointAtlasSizes);
            commandBuffer.SetBufferData(pointShadowDataHandle, pointShadowData,
                0, 0, tiles);

            SetKeywords(
                pointFilterKeywords, (int)settings.pointShadow.filterMode - 1
            );
        }

        void RenderDirectionalShadowSplitTile(int shadowedDirectionalLightIndex, int split, int tileSize)
        {
            ShadowMapDataDirectional lightShadowData = shadowMapDataDirectionals[shadowedDirectionalLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex);
            int cascadeCount = settings.directionalCascade.cascadeCount;
            Vector3 ratios = settings.directionalCascade.CascadeRatios;
            int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
            float cullingFactor = Mathf.Max(0f, 1 - settings.directionalCascade.edgeFade);
            float tileScale = 1.0f / split;
            for (int i = 0; i < cascadeCount; i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex,
                    i, cascadeCount, ratios, tileSize,
                    lightShadowData.nearPlaneOffset,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData
                );
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                int tileIndex = tileOffset + i;
                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
                // set cascade data
                if (shadowedDirectionalLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center with a surface fragment square radius
                    cascadeShadowData[i] = new ShadowCascadeBufferData(
                        splitData.cullingSphere,
                        tileSize, settings.directionalCascade.filterMode);
                }

                directionalShadowVPMatrices[tileIndex] =
                    ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
                commandBuffer.DrawRendererList(context.CreateShadowRendererList(ref shadowSettings));
                commandBuffer.SetGlobalDepthBias(0f, 0);
            }
        }

        void RenderSpotShadowSplitTile(int shadowedSpotLightIndex, int split, int tileSize)
        {
            ShadowMapDataSpot lightShadowData = shadowMapDataSpots[shadowedSpotLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex);
            int tileIndex = shadowedSpotLightIndex;
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
                lightShadowData.visibleLightIndex,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            shadowSettings.splitData = splitData;

            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1 in case of shadow map)
            float texelSize = 2f / (tileSize * projectionMatrix.m00);
            float filterSize = texelSize * ((float)settings.spotShadow.filterMode + 1f);
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            float tileScale = 1f / split;

            spotShadowData[tileIndex] = new SpotShadowBufferData(
                offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale));

            commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            commandBuffer.DrawRendererList(context.CreateShadowRendererList(ref shadowSettings));
            commandBuffer.SetGlobalDepthBias(0f, 0);
        }

        void RenderPointShadowSplitTile(int shadowedPointLightIndex, int split, int tileSize)
        {
            ShadowMapDataPoint lightShadowData = shadowMapDataPoints[shadowedPointLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex);
            int tileOffset = shadowedPointLightIndex * 6;
            // m00 = \frac{cot\frac{FOV}{2}}{Aspect} (Aspect = 1, cot\frac{FOV}{2} = 1 in case of point shadow map)
            float texelSize = 2f / tileSize;
            float filterSize = texelSize * ((float)settings.pointShadow.filterMode + 1f);
            float normalBiasScale = lightShadowData.normalBias * filterSize * 1.4142136f;
            float tileScale = 1.0f / split;
            float fovBias = Mathf.Atan(1f + normalBiasScale + filterSize) * Mathf.Rad2Deg * 2f - 90f;
            for (int i = 0; i < 6; i++)
            {
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex, (CubemapFace)i, fovBias,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData
                );
                // Undo the front face culling effect
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                shadowSettings.splitData = splitData;

                int tileIndex = tileOffset + i;
                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);

                pointShadowData[tileIndex] = new PointShadowBufferData(
                    offset, tileScale, normalBiasScale, spotAtlasSizes.x,
                    ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale));

                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
                commandBuffer.DrawRendererList(context.CreateShadowRendererList(ref shadowSettings));
                commandBuffer.SetGlobalDepthBias(0f, 0);
            }
        }

        Vector2 SetTileViewport(int tileIndex, int split, float tileSize)
        {
            var offset = new Vector2(tileIndex % split, tileIndex / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
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

        // TODO: extract
        void SetKeywords(GlobalKeyword[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                commandBuffer.SetKeyword(keywords[i], i == enabledIndex);
            }
        }
    }
}