#ifndef ARCTOON_SPOT_LIGHT_INCLUDED
#define ARCTOON_SPOT_LIGHT_INCLUDED

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomSpotLight)
    int _SpotLightCount;
CBUFFER_END

struct SpotLightBufferData
{
    float4 color;
    float4 position;
    float4 direction;
    float4 spotAngle;
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow slope scale bias
    // w: shadow mask channel
    float4 shadowData;
};

struct SpotShadowData
{
    float shadowStrength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
};

StructuredBuffer<SpotLightBufferData> _SpotLightData;

int GetSpotLightCount()
{
    return _SpotLightCount;
}

SpotShadowData DecodeSpotLightShadowData(SpotLightBufferData bufferData)
{
    SpotShadowData data;
    data.shadowStrength = bufferData.shadowData.x;
    data.tileIndex = bufferData.shadowData.y;
    data.shadowMaskChannel = bufferData.shadowData.w;
    data.lightPositionWS = bufferData.position.xyz;
    data.spotDirectionWS = bufferData.direction.xyz;
    return data;
}

float GetSpotRealtimeShadow(SpotShadowData spotShadow, CascadeShadowData cascade,
                                   Surface surface)
{
    if (spotShadow.shadowStrength <= 0) return 1.0;
    int tileIndex = spotShadow.tileIndex;
    SpotShadowBufferData shadowData = _SpotShadowData[tileIndex];
    float3 surfaceToLight = spotShadow.lightPositionWS - surface.positionWS;
    float distanceToLightPlane = dot(surfaceToLight, spotShadow.spotDirectionWS);
    float3 normalBias = surface.interpolatedNormalWS * (distanceToLightPlane * shadowData.tileData.w);
    float4 positionSTS = mul(shadowData.shadowMatrix,
        float4(surface.positionWS + normalBias, 1.0));
    float shadow = FilterSpotShadow(positionSTS.xyz / positionSTS.w,
        shadowData.tileData.xyz);
    shadow = lerp(1.0, shadow, spotShadow.shadowStrength);
    return shadow;
}

float GetSpotShadowAttenuation(SpotShadowData pointSpot, CascadeShadowData cascade,
                                    Surface surface, GI gi)
{
    #if !defined(_RECEIVE_SHADOWS)
    return 1.0;
    #endif
    float realtimeShadow = GetSpotRealtimeShadow(pointSpot, cascade, surface);
    float attenuation;
    // TODO:
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.rangeFade;
    if (gi.shadowMask.alwaysMode)
    {
        // TODO:
        float bakedShadow = GetBakedShadow(gi.shadowMask, pointSpot.shadowMaskChannel, abs(pointSpot.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else if (gi.shadowMask.distanceMode)
    {
        float bakedShadow = GetBakedShadow(gi.shadowMask, pointSpot.shadowMaskChannel, abs(pointSpot.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else
    {
        attenuation = lerp(1.0, realtimeShadow, fade);
    }
    return attenuation;
}

Light GetSpotLight(int index, Surface surface, CascadeShadowData cascade, GI gi)
{
    SpotLightBufferData bufferData = _SpotLightData[index];
    Light light;
    light.isMainLight = false;
    light.color = bufferData.color.rgb;
    float3 position = bufferData.position.xyz;
    float3 raydirection = position - surface.positionWS;
    light.directionWS = normalize(raydirection);
    float distanceSqr = max(dot(raydirection, raydirection), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * bufferData.position.w)));
    float4 spotAngles = bufferData.spotAngle;
    float3 spotDirection = bufferData.direction.xyz;
    float spotAttenuation = Square(saturate(dot(spotDirection, light.directionWS) *
        spotAngles.x + spotAngles.y));
    light.distanceAttenuation = spotAttenuation * rangeAttenuation / distanceSqr;
    
    SpotShadowData spotShadowData = DecodeSpotLightShadowData(bufferData);
    light.shadowAttenuation = GetSpotShadowAttenuation(spotShadowData, cascade, surface, gi);
    return light;
}

#endif
