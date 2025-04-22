#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../Ramp.hlsl"
#include "../BRDF.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"
#include "../GI.hlsl"

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.directionWS)) * light.shadowAttenuation;
    return lightAttenuation * light.distanceAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light);
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, brdf, light) * brdf.specular;
    #endif
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

void AccumulatePunctualLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi, CascadeShadowData cascadeShadowData, inout float3 color)
{
    ForwardPlusTile tile = GetForwardPlusTile(fragment.screenUV);
    int firstLightIndex = tile.GetFirstLightIndexInTile();

    int spotLightCount = tile.GetSpotLightCount();
    for (int j = 0; j < spotLightCount; j++)
    {
        int spotLightIndex = tile.GetLightIndex(firstLightIndex + j);
        Light light = GetSpotLight(spotLightIndex, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
    firstLightIndex += spotLightCount;
    int pointLightCount = tile.GetPointLightCount();
    for (int k = 0; k < pointLightCount; k++)
    {
        int pointLightIndex = tile.GetLightIndex(firstLightIndex + k);
        Light light = GetPointLight(pointLightIndex, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }
}

float3 GetLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetLighting(surface, brdf, light);
    }

    AccumulatePunctualLighting(fragment, surface, brdf, gi, cascadeShadowData, color);

    return color;
}

//
// float3 GetLightingIndirect(Surface surface, BRDF brdf, GI gi)
// {
//     return IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
// }

#endif
