using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public class ShadowRenderer
    {
        private const string bufferName = "Shadows";

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;

        private CullingResults cullingResults;

        private ShadowSettings settings;

        private static string[] shadowMaskKeywords =
        {
            "_SHADOW_MASK_ALWAYS",
            "_SHADOW_MASK_DISTANCE"
        };

        private bool useShadowMask;

        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

        // ----------------- directional shadow -----------------
        struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }
        Vector4 directionalAtlasSizes;
        private static int directionalShadowAtlasSizeId = Shader.PropertyToID("_DirectionalShadowAtlasSize");

        private const int maxShadowedDirectionalLightCount = 4;
        private int shadowedDirectionalLightCount;

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[maxShadowedDirectionalLightCount];

        private static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

        private static Matrix4x4[] dirShadowVPMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
        private static int dirShadowVPMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

        private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        private static Vector4[] cascadeData = new Vector4[maxCascades];

        private const int maxCascades = 4;

        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");

        private static string[] cascadeBlendKeywords =
        {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER"
        };


        // ----------------- spot shadow -----------------
        struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }
        Vector4 spotAtlasSizes;
        private static int spotShadowAtlasSizeId = Shader.PropertyToID("_SpotShadowAtlasSize");

        private const int maxShadowedSpotLightCount = 16;
        private int shadowedSpotLightCount;

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[maxShadowedSpotLightCount];

        static string[] spotFilterKeywords =
        {
            "_SPOT_PCF3",
            "_SPOT_PCF5",
            "_SPOT_PCF7",
        };

        private static int spotShadowAtlasId = Shader.PropertyToID("_SpotShadowAtlas");

        private static Matrix4x4[] spotShadowMatrices = new Matrix4x4[maxShadowedSpotLightCount];
        private static int spotShadowVPMatricesId = Shader.PropertyToID("_SpotShadowMatrices");

        private static Vector4[] spotShadowTileData = new Vector4[maxShadowedSpotLightCount];
        private static int spotShadowTileDataId = Shader.PropertyToID("_SpotShadowTiles");

        // ----------------- point shadow -----------------

        struct ShadowMapDataPoint
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }
        Vector4 pointAtlasSizes;
        private static int pointShadowAtlasSizeId = Shader.PropertyToID("_PointShadowAtlasSize");

        private const int maxShadowedPointLightCount = 2;
        private int shadowedPointLightCount;

        private ShadowMapDataPoint[] shadowMapDataPoints =
            new ShadowMapDataPoint[maxShadowedPointLightCount];

        static string[] pointFilterKeywords =
        {
            "_POINT_PCF3",
            "_POINT_PCF5",
            "_POINT_PCF7",
        };

        private static int pointShadowAtlasId = Shader.PropertyToID("_PointShadowAtlas");

        private static Matrix4x4[] pointShadowMatrices = new Matrix4x4[maxShadowedPointLightCount * 6];
        private static int pointShadowVPMatricesId = Shader.PropertyToID("_PointShadowMatrices");
        
        private static Vector4[] pointShadowTileData = new Vector4[maxShadowedPointLightCount * 6];
        private static int pointShadowTileDataId = Shader.PropertyToID("_PointShadowTiles");


        public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, CullingResults cullingResults,
            ShadowSettings settings
        )
        {
            this.context = context;
            this.commandBuffer = commandBuffer;
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedSpotLightCount = shadowedPointLightCount = 0;

            useShadowMask = false;
        }
        
        public void CleanUp()
        {
            commandBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
            if (shadowedSpotLightCount > 0)
            {
                commandBuffer.ReleaseTemporaryRT(spotShadowAtlasId);
            }
            if (shadowedPointLightCount > 0)
            {
                commandBuffer.ReleaseTemporaryRT(pointShadowAtlasId);
            }
        }

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


        public void RenderShadowMap()
        {
            if (shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadowMap();
            }
            else
            {
                // for capability
                commandBuffer.GetTemporaryRT(
                    dirShadowAtlasId, 1, 1,
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
                );
            }

            if (shadowedSpotLightCount > 0)
            {
                RenderSpotShadowMap();
            }
            else
            {
                commandBuffer.SetGlobalTexture(spotShadowAtlasId, dirShadowAtlasId);
            }

            if (shadowedPointLightCount > 0)
            {
                RenderPointShadowMap();
            }
            else
            {
                commandBuffer.SetGlobalTexture(pointShadowAtlasId, dirShadowAtlasId);
            }

            SetKeywords(shadowMaskKeywords,
                useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

            commandBuffer.SetGlobalInt(cascadeCountId,
                shadowedDirectionalLightCount > 0 ? settings.directionalCascade.cascadeCount : -1);

            float f = 1f - settings.directionalCascade.edgeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeId,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
        }

        void RenderDirectionalShadowMap()
        {
            int atlasSize = (int)settings.directionalCascade.atlasSize;
            directionalAtlasSizes.x = directionalAtlasSizes.y = 1f / atlasSize;
            directionalAtlasSizes.z = directionalAtlasSizes.w = atlasSize;

            commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(
                dirShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingId, 1f);
            // TODO: config
            int tiles = shadowedDirectionalLightCount * settings.directionalCascade.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i, split, tileSize);
            }
            
            commandBuffer.SetGlobalVector(directionalShadowAtlasSizeId, directionalAtlasSizes);
            commandBuffer.SetGlobalMatrixArray(dirShadowVPMatricesId, dirShadowVPMatrices);
            commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);

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

            commandBuffer.GetTemporaryRT(spotShadowAtlasId, atlasSize, atlasSize,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(
                spotShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingId, 0f);
            // TODO: config
            int tiles = shadowedSpotLightCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedSpotLightCount; i++)
            {
                RenderSpotShadowSplitTile(i, split, tileSize);
            }
            commandBuffer.SetGlobalVector(spotShadowAtlasSizeId, spotAtlasSizes);
            commandBuffer.SetGlobalMatrixArray(spotShadowVPMatricesId, spotShadowMatrices);
            commandBuffer.SetGlobalVectorArray(spotShadowTileDataId, spotShadowTileData);

            SetKeywords(
                spotFilterKeywords, (int)settings.spotShadow.filterMode - 1
            );
        }

        void RenderPointShadowMap()
        {
            int atlasSize = (int)settings.directionalCascade.atlasSize;
            pointAtlasSizes.x = pointAtlasSizes.y = 1f / atlasSize;
            pointAtlasSizes.z = pointAtlasSizes.w = atlasSize;

            commandBuffer.GetTemporaryRT(pointShadowAtlasId, atlasSize, atlasSize,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(
                pointShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalFloat(shadowPancakingId, 0f);
            // TODO: config
            int tiles = shadowedPointLightCount * 6;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedPointLightCount; i++)
            {
                RenderPointShadowSplitTile(i, split, tileSize);
            }
            commandBuffer.SetGlobalVector(pointShadowAtlasSizeId, pointAtlasSizes);
            commandBuffer.SetGlobalMatrixArray(pointShadowVPMatricesId, pointShadowMatrices);
            commandBuffer.SetGlobalVectorArray(pointShadowTileDataId, pointShadowTileData);

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
                // for performance: the cascades of all directional lights are equivalent
                if (shadowedDirectionalLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center with a surface fragment square radius
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                dirShadowVPMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

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
            
            spotShadowTileData[tileIndex] = GetTileData(offset, tileScale, normalBiasScale, spotAtlasSizes.x);
            spotShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

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
                
                pointShadowTileData[tileIndex] = GetTileData(offset, tileScale, normalBiasScale, pointAtlasSizes.x);
                pointShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

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

        void SetCascadeData(int cascadeIndex, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)settings.directionalCascade.filterMode + 1f);
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[cascadeIndex] = cullingSphere;
            cascadeData[cascadeIndex] = new Vector4(
                1f / cullingSphere.w,
                filterSize * 1.4142136f
            );
        }

        Vector4 GetTileData(Vector2 offset, float scale, float normalBiasScale, float oneDivideAtlasSize)
        {
            float border = oneDivideAtlasSize * 0.5f;
            Vector4 data;
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            data.z = scale - border - border;
            data.w = normalBiasScale;
            return data;
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
        void SetKeywords(string[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enabledIndex)
                {
                    commandBuffer.EnableShaderKeyword(keywords[i]);
                }
                else
                {
                    commandBuffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }

    }
}