#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../BRDF.hlsl"
#include "../GI.hlsl"
#include "DirectionalLight.hlsl"

float3 IncomingLight(Surface surface, DirectionalLight light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, DirectionalLight light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = gi.diffuse * brdf.diffuse;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        DirectionalLight light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    return color;
}

float3 GetLightingRealtime(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = 0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        DirectionalLight light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    return color;
}

float3 GetLightingIndirect(Surface surface, BRDF brdf, GI gi)
{
    return gi.diffuse * brdf.diffuse;
}

float3 GetLightingDebug(Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = 0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        DirectionalLight light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
        // color += brdf.specular;
        // color += brdf.diffuse;
        // color += SpecularStrength(surface, brdf, light);
    }
    return gi.diffuse;
    return color;
}


#endif
