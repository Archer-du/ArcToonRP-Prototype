using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class PerLightDataCollector
    {
        private CullingResults cullingResults;
        private ShadowSettings settings;

        public static readonly int maxShadowedDirectionalLightCount = 4;
        public static readonly int maxPerObjectShadowCasterCount = 16;
        public static readonly int maxShadowedSpotLightCount = 16;
        public static readonly int maxShadowedPointLightCount = 2;

        public int shadowedDirectionalLightCount { get; private set; }
        public int enabledPerObjectShadowCasterCount { get; private set; }
        public int shadowedSpotLightCount { get; private set; }
        public int shadowedPointLightCount { get; private set; }

        public bool useShadowMask { get; private set; }

        public struct ShadowMapDataDirectional
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataDirectional[] shadowMapDataDirectionals =
            new ShadowMapDataDirectional[maxShadowedDirectionalLightCount];

        public ShadowMapDataDirectional[] ShadowMapDataDirectionals => shadowMapDataDirectionals;

        public struct ShadowMapDataPerObjectCaster
        {
            public int visibleCasterIndex;
        }

        private ShadowMapDataPerObjectCaster[] shadowMapDataPerObjectCasters =
            new ShadowMapDataPerObjectCaster[maxPerObjectShadowCasterCount];

        public ShadowMapDataPerObjectCaster[] ShadowMapDataPerObjectCasters => shadowMapDataPerObjectCasters;
        
        public struct ShadowMapDataSpot
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataSpot[] shadowMapDataSpots =
            new ShadowMapDataSpot[maxShadowedSpotLightCount];

        public ShadowMapDataSpot[] ShadowMapDataSpots => shadowMapDataSpots;

        public struct ShadowMapDataPoint
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public float nearPlaneOffset;
        }

        private ShadowMapDataPoint[] shadowMapDataPoints =
            new ShadowMapDataPoint[maxShadowedPointLightCount];

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
                    shadowedDirectionalLightIndex * settings.directionalCascadeShadow.cascadeCount,
                    light.shadowNormalBias, maskChannel
                );
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }
        
        public Vector4 ReservePerObjectShadowCasterData(PerObjectShadowCaster light, int visibleCasterIndex)
        {
            // if (shadows != None)
            {
                int enabledCasterIndex = enabledPerObjectShadowCasterCount++;
                shadowMapDataPerObjectCasters[enabledCasterIndex] = new ShadowMapDataPerObjectCaster()
                {
                    visibleCasterIndex = visibleCasterIndex,
                };
                return new Vector4(0f, 0f, 0f, -1f);
            }
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

    }
}