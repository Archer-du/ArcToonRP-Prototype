#ifndef ARCTOON_OTHER_LIGHT_INCLUDED
#define ARCTOON_OTHER_LIGHT_INCLUDED

#define MAX_SPOT_LIGHT_COUNT 64

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomOtherLight)
    int _SpotLightCount;
    float4 _SpotLightColors[MAX_SPOT_LIGHT_COUNT];
    float4 _SpotLightPositions[MAX_SPOT_LIGHT_COUNT];
    float4 _SpotLightDirections[MAX_SPOT_LIGHT_COUNT];
    float4 _SpotLightSpotAngles[MAX_SPOT_LIGHT_COUNT];
    // per light shadow data:
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow slope scale bias
    // w: shadow mask channel
    float4 _SpotLightShadowData[MAX_SPOT_LIGHT_COUNT];
CBUFFER_END

int GetSpotLightCount()
{
    return _SpotLightCount;
}

struct SpotShadowData
{
    float shadowStrength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
};

SpotShadowData GetSpotLightShadowData(int lightIndex)
{
    SpotShadowData data;
    data.shadowStrength = _SpotLightShadowData[lightIndex].x;
    data.tileIndex = _SpotLightShadowData[lightIndex].y;
    data.shadowMaskChannel = _SpotLightShadowData[lightIndex].w;
    data.lightPositionWS = _SpotLightPositions[lightIndex].xyz;
    data.spotDirectionWS = _SpotLightDirections[lightIndex].xyz;
    return data;
}

float GetSpotRealtimeShadow(SpotShadowData spot, CascadeShadowData cascade,
                                   Surface surface)
{
    if (spot.shadowStrength <= 0) return 1.0;
    float3 surfaceToLight = spot.lightPositionWS - surface.position;
    float distanceToLightPlane = dot(surfaceToLight, spot.spotDirectionWS);
    float3 normalBias = surface.interpolatedNormal * (distanceToLightPlane * _SpotShadowTiles[spot.tileIndex].w);
    float4 positionSTS = mul(_SpotShadowMatrices[spot.tileIndex],
        float4(surface.position + normalBias, 1.0));
    float shadow = FilterPointSpotShadow(positionSTS.xyz / positionSTS.w,
        _SpotShadowTiles[spot.tileIndex].xyz);
    shadow = lerp(1.0, shadow, spot.shadowStrength);
    return shadow;
}

float GetSpotShadowAttenuation(SpotShadowData pointSpot, CascadeShadowData cascade,
                                    Surface surface, GI gi)
{
    // #if !defined(_RECEIVE_SHADOWS)
    // return 1.0;
    // #endif
    float realtimeShadow = GetSpotRealtimeShadow(pointSpot, cascade, surface);
    float attenuation;
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.
        rangeFade;
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
    Light light;
    light.color = _SpotLightColors[index].rgb;
    float3 position = _SpotLightPositions[index].xyz;
    float3 raydirection = position - surface.position;
    light.direction = normalize(raydirection);
    float distanceSqr = max(dot(raydirection, raydirection), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _SpotLightPositions[index].w)));
    float4 spotAngles = _SpotLightSpotAngles[index];
    float3 spotDirection = _SpotLightDirections[index].xyz;
    float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) *
        spotAngles.x + spotAngles.y));
    light.distanceAttenuation = spotAttenuation * rangeAttenuation / distanceSqr;
    
    SpotShadowData otherShadowData = GetSpotLightShadowData(index);
    light.shadowAttenuation = GetSpotShadowAttenuation(otherShadowData, cascade, surface, gi);
    return light;
}

#endif
