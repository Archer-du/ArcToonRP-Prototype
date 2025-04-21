#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../Ramp.hlsl"
#include "../BRDF.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"
#include "../GI.hlsl"

TEXTURE2D(_RampSet);
SAMPLER(sampler_RampSet);

#define DirectLightAttenSigmoidCenter 0.5
#define DirectLightAttenSigmoidSharp 0.5

float RampSigmoidSharp(float rampUV, float channel)
{
    float sigmoidCenter = 0.0;
    float sigmoidSharp = 0.0;
    if (channel == RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL)
    {
        sigmoidCenter = DirectLightAttenSigmoidCenter;
        sigmoidSharp = DirectLightAttenSigmoidSharp;
    }
    return SigmoidSharp(rampUV, sigmoidCenter, sigmoidSharp);
}

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.direction)) * light.shadowAttenuation;
    return lightAttenuation * light.distanceAttenuation * light.color /* occulation */;
}

float3 IncomingDirectionalLight(Surface surface, Light light)
{
    float3 lightAttenuation = 0.0f;
    #if defined(_RAMP_SET)
    float NdotL = dot(surface.normalWS, light.direction);
    float halfLambertFactor = NdotL * 0.5 + 0.5;
    float attenuationUV = min(RampSigmoidSharp(halfLambertFactor, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL),
        RampSigmoidSharp(light.shadowAttenuation, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL));
    lightAttenuation = SampleRampSetChannel(TEXTURE2D_ARGS(_RampSet, sampler_RampSet),
        attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    // lightAttenuation = attenuationUV;
    return lightAttenuation * light.distanceAttenuation * light.color;
    #else
    return IncomingLight(surface, light);
    #endif
}

float3 GetPunctualLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light);
    #endif
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetDirectionalLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingDirectionalLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light);
    #endif
    return IncomingDirectionalLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        color += GetDirectionalLighting(surface, brdf, light);
    }

    ForwardPlusTile tile = GetForwardPlusTile(fragment.screenUV);
    int firstLightIndex = tile.GetFirstLightIndexInTile();

    int spotLightCount = tile.GetSpotLightCount();
    for (int j = 0; j < spotLightCount; j++)
    {
        int spotLightIndex = tile.GetLightIndex(firstLightIndex + j);
        Light light = GetSpotLight(spotLightIndex, surface, cascadeShadowData, gi);
        color += GetPunctualLighting(surface, brdf, light);
    }
    firstLightIndex += spotLightCount;
    int pointLightCount = tile.GetPointLightCount();
    for (int k = 0; k < pointLightCount; k++)
    {
        int pointLightIndex = tile.GetLightIndex(firstLightIndex + k);
        Light light = GetPointLight(pointLightIndex, surface, cascadeShadowData, gi);
        color += GetPunctualLighting(surface, brdf, light);
    }

    return color;
}

float3 GetLightingIndirect(Surface surface, BRDF brdf, GI gi)
{
    return IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
}

#endif
