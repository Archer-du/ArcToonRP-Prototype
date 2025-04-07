#ifndef ARCTOON_LIGHT_INCLUDED
#define ARCTOON_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 8

#include "../Shadow.hlsl"

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct DirectionalLight
{
    float3 color;
    float3 direction;
    float attenuation;
};

struct DirectionalLightShadowData
{
    float strength;
    int tileIndex;
    float slopeScaleBias;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

DirectionalLightShadowData GetDirectionalLightShadowData(int lightIndex, CascadeShadowData cascadeShadowData)
{
    DirectionalLightShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + cascadeShadowData.cascadeOffset;
    data.slopeScaleBias = _DirectionalLightShadowData[lightIndex].z;
    return data;
}

float GetDirectionalShadowAttenuation(DirectionalLightShadowData directional, CascadeShadowData cascade,
                                      Surface surface)
{
    float3 normalBias = surface.normal * _CascadeData[cascade.cascadeOffset].y * directional.slopeScaleBias;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],
                             float4(surface.position + normalBias, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    // farther than max distance but still inside the last culling sphere
    float attenuation = lerp(1.0, shadow, directional.strength *
                             FadedStrength(surface.linearDepth, _ShadowDistanceFade.x, _ShadowDistanceFade.y) *
                             cascade.cascadeFade
    );
    // stop sampling if end up beyond the last cascade
    if (cascade.cascadeOffset >= _CascadeCount) attenuation = 1.0f;
    return attenuation;
}

DirectionalLight GetDirectionalLight(int lightIndex, Surface surface, CascadeShadowData cascadeShadowData)
{
    DirectionalLight light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    DirectionalLightShadowData dirShadowData = GetDirectionalLightShadowData(lightIndex, cascadeShadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, cascadeShadowData, surface);
    return light;
}

DirectionalLight GetDirectionalLightDebug(int lightIndex, Surface surface, CascadeShadowData cascadeShadowData)
{
    DirectionalLight light;
    light.color = _DirectionalLightColors[lightIndex].rgb;
    light.direction = _DirectionalLightDirections[lightIndex].xyz;
    light.attenuation = cascadeShadowData.cascadeOffset * 0.25;
    return light;
}

#endif
