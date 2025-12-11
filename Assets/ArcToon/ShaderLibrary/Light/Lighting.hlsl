#ifndef ARCTOON_LIGHTING_INCLUDED
#define ARCTOON_LIGHTING_INCLUDED

#include "../Ramp.hlsl"
#include "../BRDF.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"
#include "../GI.hlsl"

float3 GetMainLightDirection()
{
    if (_DirectionalLightCount <= 0) return 1.0;
    DirectionalLightBufferData bufferData = _DirectionalLightData[0];
    return bufferData.direction.xyz;
}

bool RenderingLayersOverlap(Surface surface, Light light)
{
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

// reference: com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
    float nh2 = Square(saturate(dot(surface.normalWS, h)));
    float lh2 = Square(saturate(dot(light.directionWS, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 ToonDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = surface.fresnelStrength *
        Pow4(1.0 - saturate(dot(surface.normalWS, surface.viewDirectionWS)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return (diffuse * brdf.diffuse + reflection) * surface.occlusion;
}

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.directionWS) *
        light.shadowAttenuation * light.distanceAttenuation);
    return lightAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return ToonDirectBRDF(surface, brdf, light);
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, brdf, light) * brdf.specular;
    #endif
    return IncomingLight(surface, light) * ToonDirectBRDF(surface, brdf, light);
}

void AccumulatePunctualLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi,
                                CascadeShadowData cascadeShadowData, inout float3 color)
{
    ForwardPlusTile tile = GetForwardPlusTile(fragment.screenUV);
    int firstLightIndex = tile.GetFirstLightIndexInTile();

    int spotLightCount = tile.GetSpotLightCount();
    for (int j = 0; j < spotLightCount; j++)
    {
        int spotLightIndex = tile.GetLightIndex(firstLightIndex + j);
        Light light = GetSpotLight(spotLightIndex, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }
    firstLightIndex += spotLightCount;
    int pointLightCount = tile.GetPointLightCount();
    for (int k = 0; k < pointLightCount; k++)
    {
        int pointLightIndex = tile.GetLightIndex(firstLightIndex + k);
        Light light = GetPointLight(pointLightIndex, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }
}

float3 AccumulateRealtimeLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }

    AccumulatePunctualLighting(fragment, surface, brdf, gi, cascadeShadowData, color);

    return color;
}

#endif
