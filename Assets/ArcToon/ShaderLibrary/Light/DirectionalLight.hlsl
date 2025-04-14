#ifndef ARCTOON_DIRECTIONAL_LIGHT_INCLUDED
#define ARCTOON_DIRECTIONAL_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 8

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

// cpu: PerLightDataDirectional struct
CBUFFER_START(_CustomDirectionalLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    // per light shadow data:
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow normal bias scale
    // w: shadow mask channel
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

// Structure used to obtain shadow attenuation
struct DirectionalLightShadowData
{
    float shadowStrength;
    int tileIndex;
    float normalBiasScale;
    int shadowMaskChannel;
};

DirectionalLightShadowData GetDirectionalLightShadowData(int lightIndex, CascadeShadowData cascadeShadowData)
{
    DirectionalLightShadowData data;
    data.shadowStrength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + cascadeShadowData.offset;
    data.normalBiasScale = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

float GetDirectionalRealtimeShadow(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                   Surface surface)
{
    if (directional.shadowStrength <= 0) return 1.0;
    float3 normalBias = surface.interpolatedNormal * _CascadeData[cascade.offset].y * directional.normalBiasScale;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],
                             float4(surface.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    #if defined(_CASCADE_BLEND_SOFT)
    // cascade shadow blend
    normalBias = surface.normal * _CascadeData[cascade.offset + 1].y * directional.normalBiasScale;
    positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],
                      float4(surface.position + normalBias, 1.0)).xyz;
    shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, cascade.blend);
    #endif
    shadow = lerp(1.0, shadow, directional.shadowStrength);
    return shadow;
}

float GetDirectionalShadowAttenuation(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                      Surface surface, GI gi)
{
    // #if !defined(_RECEIVE_SHADOWS)
    // return 1.0;
    // #endif
    float realtimeShadow = GetDirectionalRealtimeShadow(directional, cascade, surface);
    float attenuation;
    // farther than max distance but still inside the last culling sphere
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.rangeFade;
    if (gi.shadowMask.alwaysMode)
    {
        // TODO:
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel, abs(directional.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else if (gi.shadowMask.distanceMode)
    {
        float bakedShadow = GetBakedShadow(gi.shadowMask, directional.shadowMaskChannel, abs(directional.shadowStrength));
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
    Light light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    DirectionalLightShadowData dirShadowData = GetDirectionalLightShadowData(lightIndex, cascade);
    light.shadowAttenuation = GetDirectionalShadowAttenuation(dirShadowData, cascade, surface, gi);
    light.distanceAttenuation = 1.0;
    return light;
}





Light GetDirectionalLightDebugCascadeCullingSphere(int lightIndex, Surface surface,
                                                         CascadeShadowData cascade)
{
    Light light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    light.shadowAttenuation = cascade.offset * 0.25;
    light.distanceAttenuation = 1.0;
    return light;
}

#endif
