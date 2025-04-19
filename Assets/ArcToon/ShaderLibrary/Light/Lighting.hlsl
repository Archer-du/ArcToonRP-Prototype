#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../BRDF.hlsl"
#include "../GI.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.shadowAttenuation * light.distanceAttenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    for (int i = 0; i < GetSpotLightCount(); i++)
    {
        Light light = GetSpotLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    for (int i = 0; i < GetPointLightCount(); i++)
    {
        Light light = GetPointLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    return color;
}

float3 GetLightingDirect(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = 0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    for (int j = 0; j < GetSpotLightCount(); j++)
    {
        Light light = GetSpotLight(j, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    for (int j = 0; j < GetPointLightCount(); j++)
    {
        Light light = GetPointLight(j, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    return color;
}

float3 GetLightingIndirect(Surface surface, BRDF brdf, GI gi)
{
    return IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
}

float3 GetLightingDebug(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = 0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
        // color += brdf.specular;
        // color += brdf.diffuse;
        // color += SpecularStrength(surface, brdf, light);
    }
    return gi.diffuse;
    return color;
}


#endif
