#ifndef ARCTOON_POINT_LIGHT_INCLUDED
#define ARCTOON_POINT_LIGHT_INCLUDED

#include "../Shadow.hlsl"
#include "../GI.hlsl"
#include "LightType.hlsl"

CBUFFER_START(_CustomPointLight)
    int _PointLightCount;
CBUFFER_END

struct PointLightBufferData
{
    float4 color;
    float4 position;
    float4 direction;
    // x: shadow strength
    // y: shadow map tile index
    // z: shadow slope scale bias
    // w: shadow mask channel
    float4 shadowData;
};

struct PointShadowData
{
    float shadowStrength;
    int tileIndex;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
};

StructuredBuffer<PointLightBufferData> _PointLightData;

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

PointShadowData DecodePointLightShadowData(PointLightBufferData bufferData)
{
    PointShadowData data;
    data.shadowStrength = bufferData.shadowData.x;
    data.tileIndex = bufferData.shadowData.y;
    data.shadowMaskChannel = bufferData.shadowData.w;
    data.lightPositionWS = bufferData.position.xyz;
    data.spotDirectionWS = bufferData.direction.xyz;
    return data;
}

float GetPointRealtimeShadow(PointShadowData pointShadow, CascadeShadowData cascade,
                             Surface surface)
{
    if (pointShadow.shadowStrength <= 0) return 1.0;
    int tileIndex = pointShadow.tileIndex;
    float3 surfaceToLight = pointShadow.lightPositionWS - surface.positionWS;
    float faceOffset = CubeMapFaceID(-surfaceToLight);
    tileIndex += faceOffset;
    PointShadowBufferData shadowData = _PointShadowData[tileIndex];
    float3 lightPlane = pointShadowPlanes[faceOffset];
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);

    float3 normalBias = surface.interpolatedNormalWS * (distanceToLightPlane * shadowData.tileData.w);
    float4 positionSTS = mul(shadowData.shadowMatrix,
                             float4(surface.positionWS + normalBias, 1.0));
    float shadow = FilterPointShadow(positionSTS.xyz / positionSTS.w,
                                    shadowData.tileData.xyz);
    shadow = lerp(1.0, shadow, pointShadow.shadowStrength);
    return shadow;
}

float GetPointShadowAttenuation(PointShadowData pointShadow, CascadeShadowData cascade,
                                Surface surface, GI gi)
{
    #if !defined(_RECEIVE_SHADOWS)
    return 1.0;
    #endif
    float realtimeShadow = GetPointRealtimeShadow(pointShadow, cascade, surface);
    float attenuation;
    // TODO:
    float fade = FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) * cascade.rangeFade;
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
    PointLightBufferData bufferData = _PointLightData[index];
    Light light;
    light.isMainLight = false;
    light.color = bufferData.color.rgb;
    float3 position = bufferData.position.xyz;
    float3 raydirection = position - surface.positionWS;
    light.directionWS = normalize(raydirection);
    light.renderingLayerMask = bufferData.direction.w;
    float distanceSqr = max(dot(raydirection, raydirection), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * bufferData.position.w)));
    light.distanceAttenuation = rangeAttenuation / distanceSqr;

    PointShadowData pointShadowData = DecodePointLightShadowData(bufferData);
    light.shadowAttenuation = GetPointShadowAttenuation(pointShadowData, cascade, surface, gi);
    return light;
}

#endif
