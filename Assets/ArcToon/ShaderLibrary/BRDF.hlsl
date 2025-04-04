﻿// a variant of the Minimalist CookTorrance BRDF
#ifndef ARCTOON_BRDF_INCLUDED
#define ARCTOON_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

#include "Surface.hlsl"
#include "Light/DirectionalLight.hlsl"

struct BRDF {
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity (float metallic) {
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false) {
    BRDF brdf;
    float reflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * reflectivity;
    if (applyAlphaToDiffuse) {
        brdf.diffuse *= surface.alpha;
    }
    // TODO: energy conservation
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

// reference: com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl
float SpecularStrength(Surface surface, BRDF brdf, DirectionalLight light) {
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, DirectionalLight light) {
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif