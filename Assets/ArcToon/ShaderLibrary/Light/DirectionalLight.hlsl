#ifndef ARCTOON_DIRECTIONAL_LIGHT_INCLUDED
#define ARCTOON_DIRECTIONAL_LIGHT_INCLUDED

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomDirectionalLight)
    int _DirectionalLightCount;
CBUFFER_END

struct DirectionalLightBufferData
{
    float4 color;
    float4 direction;
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow slope scale bias
    // w: shadow mask channel
    float4 shadowData;
};

StructuredBuffer<DirectionalLightBufferData> _DirectionalLightData;

struct DirectionalLightShadowData
{
    float shadowStrength;
    int tileIndex;
    float normalBiasScale;
    int shadowMaskChannel;
};

DirectionalLightShadowData DecodeDirectionalLightShadowData(DirectionalLightBufferData bufferData,
                                                            CascadeShadowData cascadeShadowData)
{
    DirectionalLightShadowData data;
    data.shadowStrength = bufferData.shadowData.x;
    data.tileIndex = bufferData.shadowData.y + cascadeShadowData.offset;
    data.normalBiasScale = bufferData.shadowData.z;
    data.shadowMaskChannel = bufferData.shadowData.w;
    return data;
}

float GetDirectionalRealtimeShadow(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                   Surface surface)
{
    if (directional.shadowStrength <= 0) return 1.0;
    float3 normalBias = surface.interpolatedNormalWS * _ShadowCascadeData[cascade.offset].data.y * directional.normalBiasScale;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],
                             float4(surface.positionWS + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    #if defined(_CASCADE_BLEND_SOFT)
    // cascade shadow blend
    normalBias = surface.normalWS * _ShadowCascadeData[cascade.offset].data.y * directional.normalBiasScale;
    positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],
                      float4(surface.position + normalBias, 1.0)).xyz;
    shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, cascade.softBlend);
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
    DirectionalLightShadowData dirShadowData = DecodeDirectionalLightShadowData(bufferData, cascade);
    light.shadowAttenuation = GetDirectionalShadowAttenuation(dirShadowData, cascade, surface, gi);
    light.distanceAttenuation = 1.0;
    return light;
}


Light GetDirectionalLightDebugCascadeCullingSphere(int lightIndex, Surface surface, CascadeShadowData cascade, GI gi)
{
    DirectionalLightBufferData bufferData = _DirectionalLightData[lightIndex];
    Light light;
    light.color = bufferData.color.rgb;
    light.directionWS = bufferData.direction.xyz;
    light.shadowAttenuation = cascade.offset * 0.25;
    light.distanceAttenuation = 1.0;
    return light;
}

#endif
