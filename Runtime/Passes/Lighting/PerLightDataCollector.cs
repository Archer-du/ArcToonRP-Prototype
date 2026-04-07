using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils.Extensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class PerLightDataCollector
    {
        private CullingResults cullingResults;
        private ShadowSettings settings;

        public int shadowedDirectionalLightCount { get; private set; }
        public int enabledPerObjectShadowCasterCount { get; private set; }
        public int shadowedSpotLightCount { get; private set; }
        public int shadowedPointLightCount { get; private set; }

        public bool useShadowMask { get; private set; }

        /// <summary>
        /// Gets per-light lightSize from ArcToonLightData component, or falls back to global ShadowSettings.lightSize.
        /// </summary>
        private float GetLightSize(Light light)
        {
            var lightData = light.GetComponent<ArcToonLightData>();
            return lightData != null ? lightData.lightSize : 1.0f;
        }

        /// <summary>
        /// Returns the default shadowData Vector4 for lights with no shadow (strength = 0).
        /// </summary>
        private static Vector4 NoShadowData =>
            new Vector4(0f, BitPackingExtensions.Pack2x16(0, 0), 0f, 0f);

        public struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[RenderPipelineInfo.MaxShadowedDirectionalLightCount];

        public ShadowMapDataDirectional[] ShadowMapDataDirectionals => shadowMapDataDirectionals;

        public struct ShadowMapDataPerObjectCaster
        {
            public int visibleCasterIndex;
        }

        private ShadowMapDataPerObjectCaster[] shadowMapDataPerObjectCasters =
            new ShadowMapDataPerObjectCaster[RenderPipelineInfo.MaxPerObjectShadowCasterCount];

        public ShadowMapDataPerObjectCaster[] ShadowMapDataPerObjectCasters => shadowMapDataPerObjectCasters;
        
        public struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[RenderPipelineInfo.MaxShadowedSpotLightCount];

        public ShadowMapDataSpot[] ShadowMapDataSpots => shadowMapDataSpots;

        public struct ShadowMapDataPoint
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataPoint[] shadowMapDataPoints =
            new ShadowMapDataPoint[RenderPipelineInfo.MaxShadowedPointLightCount];

        public ShadowMapDataPoint[] ShadowMapDataPoints => shadowMapDataPoints;

        public void Setup(CullingResults cullingResults, ShadowSettings settings)
        {
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedSpotLightCount = shadowedPointLightCount = 0;
            enabledPerObjectShadowCasterCount = 0;

            useShadowMask = false;
        }

        public Vector4 ReservePerLightShadowDataDirectional(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                int maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                // only baked shadows are used
                if (shadowedDirectionalLightCount >= RenderPipelineInfo.MaxShadowedDirectionalLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    // a trick to only sample baked shadow
                    return new Vector4(
                        -light.shadowStrength,
                        BitPackingExtensions.Pack2x16(0, maskChannel + 1),
                        0f, 0f
                    );
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
                    BitPackingExtensions.Pack2x16(shadowedDirectionalLightIndex, maskChannel + 1),
                    light.shadowNormalBias, GetLightSize(light)
                );
            }

            return NoShadowData;
        }
        
        public Vector4 ReservePerObjectShadowCasterData(PerObjectShadowCaster caster, int visibleCasterIndex)
        {
            // if (shadows != None)
            {
                int enabledCasterIndex = enabledPerObjectShadowCasterCount++;
                shadowMapDataPerObjectCasters[enabledCasterIndex] = new ShadowMapDataPerObjectCaster()
                {
                    visibleCasterIndex = visibleCasterIndex,
                };
                return new Vector4(1f, caster.perObjectShadowCasterID, enabledCasterIndex * shadowedDirectionalLightCount, -1f);
            }
        }
        
        public Vector4 ReservePerLightShadowDataSpot(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                int maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedSpotLightCount >= RenderPipelineInfo.MaxShadowedSpotLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(
                        -light.shadowStrength,
                        BitPackingExtensions.Pack2x16(0, maskChannel + 1),
                        0f, 0f
                    );
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
                    light.shadowStrength,
                    BitPackingExtensions.Pack2x16(shadowedSpotLightIndex, maskChannel + 1),
                    light.shadowNormalBias, GetLightSize(light)
                );
            }

            return NoShadowData;
        }


        public Vector4 ReservePerLightShadowDataPoint(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBaking = light.bakingOutput;
                int maskChannel = -1;
                if (lightBaking is
                    { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (shadowedPointLightCount >= RenderPipelineInfo.MaxShadowedPointLightCount ||
                    !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(
                        -light.shadowStrength,
                        BitPackingExtensions.Pack2x16(0, maskChannel + 1),
                        0f, 0f
                    );
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
                    light.shadowStrength,
                    BitPackingExtensions.Pack2x16(shadowedPointLightIndex * 6, maskChannel + 1),
                    light.shadowNormalBias, GetLightSize(light)
                );
            }

            return NoShadowData;
        }

    }
}