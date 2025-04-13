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

        private const int maxShadowedDirectionalLightCount = 4;
        private const int maxShadowedSpotLightCount = 16;
        private const int maxShadowedPointLightCount = 2;
        private const int maxCascades = 4;

        private int shadowedDirectionalLightCount;
        private int shadowedSpotLightCount;

        struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[maxShadowedDirectionalLightCount];

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[maxShadowedSpotLightCount];
        
        private ShadowMapDataSpot[] shadowMapDataPoints =
            new ShadowMapDataSpot[maxShadowedPointLightCount];

        private static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        static string[] spotFilterKeywords =
        {
            "_SPOT_PCF3",
            "_SPOT_PCF5",
            "_SPOT_PCF7",
        };

        private static string[] cascadeBlendKeywords =
        {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER"
        };

        private static string[] shadowMaskKeywords =
        {
            "_SHADOW_MASK_ALWAYS",
            "_SHADOW_MASK_DISTANCE"
        };

        private bool useShadowMask;

        Vector4 atlasSizes;
        
        private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        private static Vector4[] cascadeData = new Vector4[maxCascades];
        private static Vector4[] spotShadowTileData = new Vector4[maxShadowedSpotLightCount];
        
        private static Matrix4x4[] dirShadowVPMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
        private static Matrix4x4[] spotShadowMatrices = new Matrix4x4[maxShadowedSpotLightCount];

        private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        private static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int spotShadowAtlasId = Shader.PropertyToID("_SpotShadowAtlas");
        private static int dirShadowVPMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int spotShadowVPMatricesId = Shader.PropertyToID("_SpotShadowMatrices");
        
        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        
        private static int spotShadowTileDataId = Shader.PropertyToID("_SpotShadowTiles");

        public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, CullingResults cullingResults,
            ShadowSettings settings
        )
        {
            this.context = context;
            this.commandBuffer = commandBuffer;
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedSpotLightCount = 0;

            useShadowMask = false;
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

            SetKeywords(shadowMaskKeywords,
                useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

            commandBuffer.SetGlobalInt(cascadeCountId,
                shadowedDirectionalLightCount > 0 ? settings.directionalCascade.cascadeCount : -1);
            
            float f = 1f - settings.directionalCascade.edgeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeId,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
            commandBuffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
        }

        void RenderDirectionalShadowMap()
        {
            int atlasSize = (int)settings.directionalCascade.atlasSize;
            atlasSizes.x = atlasSize;
            atlasSizes.y = 1f / atlasSize;

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

            // for shadow receiver lighting calculation
            commandBuffer.SetGlobalInt(cascadeCountId,
                shadowedDirectionalLightCount > 0 ? settings.directionalCascade.cascadeCount : -1);
            commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            commandBuffer.SetGlobalMatrixArray(dirShadowVPMatricesId, dirShadowVPMatrices);

            SetKeywords(
                directionalFilterKeywords, (int)settings.directionalCascade.filterMode - 1
            );
            SetKeywords(
                cascadeBlendKeywords, (int)settings.directionalCascade.blendMode - 1
            );
        }

        void RenderSpotShadowMap()
        {
            int atlasSize = (int)settings.pointSpot.atlasSize;
            atlasSizes.z = atlasSize;
            atlasSizes.w = 1f / atlasSize;

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

            commandBuffer.SetGlobalMatrixArray(spotShadowVPMatricesId, spotShadowMatrices);
            commandBuffer.SetGlobalVectorArray(spotShadowTileDataId, spotShadowTileData);

            SetKeywords(
                spotFilterKeywords, (int)settings.pointSpot.filterMode - 1
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
            float filterSize = texelSize * ((float)settings.pointSpot.filterMode + 1f);
            float bias = lightShadowData.normalBias * filterSize * 1.4142136f;
            Vector2 offset = SetTileViewport(shadowedSpotLightIndex, split, tileSize);
            float tileScale = 1f / split;
            SetTileData(shadowedSpotLightIndex, offset, tileScale, bias);
            
            spotShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

            commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
            commandBuffer.DrawRendererList(context.CreateShadowRendererList(ref shadowSettings));
            commandBuffer.SetGlobalDepthBias(0f, 0);
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

        void SetTileData(int shadowedSpotLightIndex, Vector2 offset, float scale, float bias)
        {
            float border = atlasSizes.w * 0.5f;
            Vector4 data;
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            data.z = scale - border - border;
            data.w = bias;
            spotShadowTileData[shadowedSpotLightIndex] = data;
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

        public void CleanUp()
        {
            commandBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
            if (shadowedSpotLightCount > 0)
            {
                commandBuffer.ReleaseTemporaryRT(spotShadowAtlasId);
            }
        }
    }
}