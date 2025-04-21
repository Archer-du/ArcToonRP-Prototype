#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../BRDF.hlsl"
#include "../GI.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normalWS, light.direction)) * light.shadowAttenuation * light.distanceAttenuation * light.color /* occulation */;
}

float3 IncomingLightRamp(Surface surface, Light light)
{
    float attenuationUV = dot(surface.normalWS, light.direction) * light.shadowAttenuation;
    return light.distanceAttenuation * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light);
    #endif
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    ForwardPlusTile tile = GetForwardPlusTile(fragment.screenUV);
    int firstLightIndex = tile.GetFirstLightIndexInTile();

    int spotLightCount = tile.GetSpotLightCount();
    for (int i = 0; i < spotLightCount; i++)
    {
        int spotLightIndex = tile.GetLightIndex(firstLightIndex + i);
        Light light = GetSpotLight(spotLightIndex, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    firstLightIndex += spotLightCount;
    int pointLightCount = tile.GetPointLightCount();
    for (int i = 0; i < pointLightCount; i++)
    {
        int pointLightIndex = tile.GetLightIndex(firstLightIndex + i);
        Light light = GetPointLight(pointLightIndex, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    return color;
}

float3 GetLightingIndirect(Surface surface, BRDF brdf, GI gi)
{
    return IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
}

#endif
