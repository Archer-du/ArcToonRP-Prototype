// a variant of the Minimalist CookTorrance BRDF
#ifndef ARCTOON_BRDF_INCLUDED
#define ARCTOON_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

#include "Surface.hlsl"
#include "Light/LightType.hlsl"

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
    float perceptualRoughness;
    float fresnel;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float reflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * reflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    // TODO: energy conservation
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    brdf.perceptualRoughness = RoughnessToPerceptualRoughness(surface.roughness);
    brdf.roughness = surface.roughness;
    float perceptualSmoothness = RoughnessToPerceptualSmoothness(surface.roughness);
    brdf.fresnel = saturate(perceptualSmoothness + 1.0 - reflectivity);
    return brdf;
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

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    #ifdef _SPEC_MAP
    float specularStrengthMask = surface.specularStrength;
    return specularStrengthMask + SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
    #else
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
    #endif
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = surface.fresnelStrength *
        Pow4(1.0 - saturate(dot(surface.normalWS, surface.viewDirectionWS)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return (diffuse * brdf.diffuse + reflection) * surface.occlusion;
}

#endif
