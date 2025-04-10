#ifndef ARCTOON_LIGHT_INCLUDED
#define ARCTOON_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 8

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

// cpu: PerLightDataDirectional struct
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END


// Structure used to obtain shadow attenuation
struct DirectionalLightShadowData
{
    float strength;
    int tileIndex;
    float slopeScaleBias;
    int shadowMaskChannel;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

DirectionalLightShadowData GetDirectionalLightShadowData(int lightIndex, CascadeShadowData cascadeShadowData)
{
    DirectionalLightShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + cascadeShadowData.offset;
    data.slopeScaleBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

float GetDirectionalRealtimeShadow(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                   Surface surface)
{
    if (directional.strength <= 0) return 1.0;
    float3 normalBias = surface.interpolatedNormal * _CascadeData[cascade.offset].y * directional.slopeScaleBias;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],
                             float4(surface.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    #if defined(_CASCADE_BLEND_SOFT)
    // cascade shadow blend
    normalBias = surface.normal * _CascadeData[cascade.offset + 1].y * directional.slopeScaleBias;
    positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],
                      float4(surface.position + normalBias, 1.0)).xyz;
    shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, cascade.blend);
    #endif
    // farther than max distance but still inside the last culling sphere
    shadow = lerp(1.0, shadow, directional.strength);
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if (strength <= 0) return 1.0;
    float shadow = 1.0;
    if (mask.alwaysMode || mask.distanceMode)
    {
        if (channel >= 0)
        {
            shadow = mask.shadows[channel];
        }
    }
    return lerp(1.0, shadow, strength);
}

float MixBakedAndRealtimeShadow(float bakedShadow, float realtimeShadow, float fade)
{
    return lerp(bakedShadow, realtimeShadow, fade);
}

float GetDirectionalShadowAttenuation(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                      Surface surface, GI gi)
{
    #if !defined(_RECEIVE_SHADOWS)
    return 1.0;
    #endif
    float realtimeShadow = GetDirectionalRealtimeShadow(directional, cascade, surface);
    float attenuation;
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.
        rangeFade;
    if (gi.shadowMask.alwaysMode)
    {
        // TODO:
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel, abs(directional.strength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else if (gi.shadowMask.distanceMode)
    {
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel, abs(directional.strength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else
    {
        attenuation = lerp(1.0, realtimeShadow, fade);
    }
    return attenuation;
}

DirectionalLight GetDirectionalLight(int lightIndex, Surface surface, CascadeShadowData cascadeShadowData, GI gi)
{
    DirectionalLight light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    DirectionalLightShadowData dirShadowData = GetDirectionalLightShadowData(lightIndex, cascadeShadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, cascadeShadowData, surface, gi);
    return light;
}

DirectionalLight GetDirectionalLightDebugCascadeCullingSphere(int lightIndex, Surface surface,
                                                         CascadeShadowData cascadeShadowData)
{
    DirectionalLight light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    light.attenuation = cascadeShadowData.offset * 0.25;
    return light;
}

#endif
