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

#endif
