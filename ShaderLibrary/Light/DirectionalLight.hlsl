#ifndef ARCTOON_DIRECTIONAL_LIGHT_INCLUDED
#define ARCTOON_DIRECTIONAL_LIGHT_INCLUDED

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomDirectionalLight)
    int _DirectionalLightCount;
    int _PerObjectShadowCasterCount;
CBUFFER_END

#include "Packages/com.arctoon.render-pipeline/Runtime/Buffers/DirectionalLightBufferData.cs.hlsl"

StructuredBuffer<DirectionalLightBufferData> _DirectionalLightData;

struct DirectionalLightShadowData
{
    float shadowStrength;
    int tileIndex;
    int shadowedLightIndex;
    float normalBiasScale;
    int shadowMaskChannel;
    float lightSize;
};

#include "Packages/com.arctoon.render-pipeline/Runtime/Buffers/PerObjectCasterBufferData.cs.hlsl"

bool CheckPerObjectCasterID(PerObjectCasterBufferData bufferData, float casterID)
{
    return abs(bufferData.perObjectData.y - casterID) <= 0.01;
}

StructuredBuffer<PerObjectCasterBufferData> _PerObjectShadowCasterData;

DirectionalLightShadowData DecodeDirectionalLightShadowData(float4 shadowData,
                                                            CascadeShadowData cascadeShadowData)
{
    DirectionalLightShadowData data;
    data.shadowStrength = shadowData.x;
    int shadowedLightIndex;
    UnpackTileIndexAndMaskChannel(shadowData.y, shadowedLightIndex, data.shadowMaskChannel);
    data.shadowedLightIndex = shadowedLightIndex;
    data.tileIndex = shadowedLightIndex * _CascadeCount + cascadeShadowData.offset;
    data.normalBiasScale = shadowData.z;
    data.lightSize = shadowData.w;
    return data;
}

float GetDirectionalRealtimeShadow(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                   Surface surface)
{
    if (directional.shadowStrength <= 0) return 1.0;
    if (surface.perObjectCasterID > 0)
    {
        for (int i = 0; i < _PerObjectShadowCasterCount; i++)
        {
            PerObjectCasterBufferData bufferData = _PerObjectShadowCasterData[i];
            if (CheckPerObjectCasterID(bufferData, surface.perObjectCasterID))
            {
                int tileIndex = bufferData.perObjectData.z + directional.shadowedLightIndex;
                PerObjectShadowBufferData shadowData = _PerObjectShadowData[tileIndex];
                float3 normalBias = surface.interpolatedNormalWS * shadowData.normalBias.x * directional.normalBiasScale;
                float3 positionSTS = mul(shadowData.shadowMatrix,
                                         float4(surface.positionWS + normalBias, 1.0)).xyz;
                float shadow = FilterPerObjectShadow(positionSTS, directional.lightSize);
                shadow = lerp(1.0, shadow, directional.shadowStrength);
                return shadow;
            }
        }
    }
    float3 normalBias = surface.interpolatedNormalWS * _ShadowCascadeData[cascade.offset].data.y * directional.normalBiasScale;
    float3 positionSTS = mul(_DirectionalShadowData[directional.tileIndex].shadowMatrix,
                             float4(surface.positionWS + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS, directional.lightSize);
    #if defined(_CASCADE_BLEND_SOFT)
    // cascade shadow blend
    normalBias = surface.normalWS * _ShadowCascadeData[cascade.offset].data.y * directional.normalBiasScale;
    positionSTS = mul(_DirectionalShadowData[directional.tileIndex + 1].shadowMatrix,
                      float4(surface.positionWS + normalBias, 1.0)).xyz;
    shadow = lerp(FilterDirectionalShadow(positionSTS, directional.lightSize), shadow, cascade.softBlend);
    #endif
    shadow = lerp(1.0, shadow, directional.shadowStrength);
    return shadow;
}

float GetDirectionalShadowAttenuation(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                      Surface surface, GI gi)
{
    #if !defined(_RECEIVE_SHADOWS)
    return 1.0;
    #endif
    float realtimeShadow = GetDirectionalRealtimeShadow(directional, cascade, surface);
    float attenuation;
    // farther than max distance but still inside the last culling sphere
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.rangeFade;
    if (gi.shadowMask.alwaysMode)
    {
        // TODO:
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel,
                                           abs(directional.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else if (gi.shadowMask.distanceMode)
    {
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel,
                                           abs(directional.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else
    {
        attenuation = lerp(1.0, realtimeShadow, fade);
    }
    return attenuation;
}

Light GetDirectionalLight(int lightIndex, Surface surface, CascadeShadowData cascade, GI gi)
{
    DirectionalLightBufferData bufferData = _DirectionalLightData[lightIndex];
    Light light;
    light.isMainLight = lightIndex == 0;
    light.color = bufferData.color.rgb;
    light.directionWS = bufferData.direction.xyz;
    light.renderingLayerMask = bufferData.direction.w;
    DirectionalLightShadowData dirShadowData = DecodeDirectionalLightShadowData(bufferData.shadowData, cascade);
    light.shadowAttenuation = GetDirectionalShadowAttenuation(dirShadowData, cascade, surface, gi);
    light.distanceAttenuation = 1.0;
    return light;
}

// Light GetDirectionalLightDebugCascadeCullingSphere(int lightIndex, Surface surface, CascadeShadowData cascade, GI gi)
// {
//     DirectionalLightBufferData bufferData = _DirectionalLightData[lightIndex];
//     Light light;
//     light.color = bufferData.color.rgb;
//     light.directionWS = bufferData.direction.xyz;
//     light.shadowAttenuation = cascade.offset * 0.25;
//     light.distanceAttenuation = 1.0;
//     return light;
// }

#endif
