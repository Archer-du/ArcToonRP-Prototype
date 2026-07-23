#ifndef ARCTOON_TOON_LIGHTING_INCLUDED
#define ARCTOON_TOON_LIGHTING_INCLUDED

// Library tier: CBUFFER-free lighting math. The CBUFFER-derived toon assembly
// (ToonSpecularStrength / GF2FaceSpecularStrength / toon IncomingLight / ToonDirectBRDF /
// GetLighting) lives in Shaders/Assembly/ToonLightingAssembly.hlsl.
// Global constant buffers (light data, camera textures) are not per-material CBUFFER and
// are allowed here.

#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/SigmoidRamp.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/LinearPartition.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Light/DirectionalLight.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Light/SpotLight.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Light/PointLight.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/GI.hlsl"

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

float MinimalCookTorranceSpecularTerm(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
    float nh2 = Square(saturate(dot(surface.normalWS, h)));
    float lh2 = Square(saturate(dot(light.directionWS, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = surface.fresnelStrength *
        Pow4(1.0 - saturate(dot(surface.normalWS, surface.viewDirectionWS)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return (diffuse * brdf.diffuse + reflection) * surface.occlusion;
}

float3 PhysicDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return MinimalCookTorranceSpecularTerm(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.directionWS) *
        light.shadowAttenuation * light.distanceAttenuation);
    return lightAttenuation * light.color * surface.occlusion;
}

void AccumulatePunctualLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi,
                                CascadeShadowData cascadeShadowData,
                                inout float3 color)
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
            color += IncomingLight(surface, light) * PhysicDirectBRDF(surface, brdf, light);
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
            color += IncomingLight(surface, light) * PhysicDirectBRDF(surface, brdf, light);
        }
    }
}
#endif
