#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../BRDF.hlsl"
#include "DirectionalLight.hlsl"

float3 IncomingLight(Surface surface, DirectionalLight light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, DirectionalLight light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surface, BRDF brdf)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        DirectionalLight light = GetDirectionalLight(i, surface, cascadeShadowData);
        color += GetLighting(surface, brdf, light);
    }
    return color;
}

#endif
