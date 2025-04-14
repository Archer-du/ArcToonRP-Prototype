#ifndef ARCTOON_POINT_LIGHT_INCLUDED
#define ARCTOON_POINT_LIGHT_INCLUDED

#define MAX_POINT_LIGHT_COUNT 16

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomPointLight)
    int _PointLightCount;
    float4 _PointLightColors[MAX_POINT_LIGHT_COUNT];
    float4 _PointLightPositions[MAX_POINT_LIGHT_COUNT];
    float4 _PointLightDirections[MAX_POINT_LIGHT_COUNT];
    // per light shadow data:
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow slope scale bias
    // w: shadow mask channel
    float4 _PointLightShadowData[MAX_POINT_LIGHT_COUNT];
CBUFFER_END

static const float3 pointShadowPlanes[6] =
{
    float3(-1.0, 0.0, 0.0),
    float3(1.0, 0.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, -1.0),
    float3(0.0, 0.0, 1.0)
};

int GetPointLightCount()
{
    return _PointLightCount;
}

struct PointShadowData
{
    float shadowStrength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
};

PointShadowData GetPointLightShadowData(int lightIndex)
{
    PointShadowData data;
    data.shadowStrength = _PointLightShadowData[lightIndex].x;
    data.tileIndex = _PointLightShadowData[lightIndex].y;
    data.shadowMaskChannel = _PointLightShadowData[lightIndex].w;
    data.lightPositionWS = _PointLightPositions[lightIndex].xyz;
    data.spotDirectionWS = _PointLightDirections[lightIndex].xyz;
    return data;
}

float GetPointRealtimeShadow(PointShadowData pointShadow, CascadeShadowData cascade,
                             Surface surface)
{
    if (pointShadow.shadowStrength <= 0) return 1.0;
    int tileIndex = pointShadow.tileIndex;
    float3 surfaceToLight = pointShadow.lightPositionWS - surface.position;
    float faceOffset = CubeMapFaceID(-surfaceToLight);
    tileIndex += faceOffset;
    float3 lightPlane = pointShadowPlanes[faceOffset];
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);

    float3 normalBias = surface.interpolatedNormal * (distanceToLightPlane * _PointShadowTiles[tileIndex].w);
    float4 positionSTS = mul(_PointShadowMatrices[tileIndex],
                             float4(surface.position + normalBias, 1.0));
    float shadow = FilterPointShadow(positionSTS.xyz / positionSTS.w,
                                    _PointShadowTiles[tileIndex].xyz);
    shadow = lerp(1.0, shadow, pointShadow.shadowStrength);
    return shadow;
}

float GetPointShadowAttenuation(PointShadowData pointShadow, CascadeShadowData cascade,
                                Surface surface, GI gi)
{
    // #if !defined(_RECEIVE_SHADOWS)
    // return 1.0;
    // #endif
    float realtimeShadow = GetPointRealtimeShadow(pointShadow, cascade, surface);
    float attenuation;
    // TODO:
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.
        rangeFade;
    if (gi.shadowMask.alwaysMode)
    {
        // TODO:
        float bakedShadow = GetBakedShadow(gi.shadowMask, pointShadow.shadowMaskChannel,
                                           abs(pointShadow.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else if (gi.shadowMask.distanceMode)
    {
        float bakedShadow = GetBakedShadow(gi.shadowMask, pointShadow.shadowMaskChannel,
                                           abs(pointShadow.shadowStrength));
        attenuation = MixBakedAndRealtimeShadow(bakedShadow, realtimeShadow, fade);
    }
    else
    {
        attenuation = lerp(1.0, realtimeShadow, fade);
    }
    return attenuation;
}


Light GetPointLight(int index, Surface surface, CascadeShadowData cascade, GI gi)
{
    Light light;
    light.color = _PointLightColors[index].rgb;
    float3 position = _PointLightPositions[index].xyz;
    float3 raydirection = position - surface.position;
    light.direction = normalize(raydirection);
    float distanceSqr = max(dot(raydirection, raydirection), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _PointLightPositions[index].w)));
    light.distanceAttenuation = rangeAttenuation / distanceSqr;

    PointShadowData pointShadowData = GetPointLightShadowData(index);
    light.shadowAttenuation = GetPointShadowAttenuation(pointShadowData, cascade, surface, gi);
    return light;
}

#endif
