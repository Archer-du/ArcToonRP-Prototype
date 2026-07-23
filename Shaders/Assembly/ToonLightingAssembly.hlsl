#ifndef ARCTOON_TOON_LIGHTING_ASSEMBLY_INCLUDED
#define ARCTOON_TOON_LIGHTING_ASSEMBLY_INCLUDED

// Assembly tier: toon lighting composition. Consumes the per-feature Interface getters (specular,
// rim, SDF face) and the diffuse attenuation model, and assembles them into the final lit color.
// This is NOT an Interface file: it depends on other interfaces being included first, so it lives
// in Assembly/ rather than Interface/. MUST be included AFTER the shader's CBUFFER and after
// SpecularInterface.hlsl / RimLightInterface.hlsl / SDFFaceInterface.hlsl (whichever the shader's
// keywords activate). Requires: SurfaceSampling.hlsl (INPUT_PROP), ToonLighting.hlsl (math),
// SigmoidRamp.hlsl, Common.hlsl. The diffuse attenuation model is selected by
// _ATTEN_LINEAR_PARTITION and pulled in below from its own model file.

float3 ToonSpecularStrength(Surface surface, BRDF brdf, Light light)
{
    // TODO: config
    #if defined(_SDF_LIGHT_MAP)
    if (GetSDFLightMapRegionEnabled(surface.regionIndex))
    {
        return GF2FaceSpecularStrength(surface, light);
    }
    #endif

    float3 specularStrength;
    #if defined(_OVERRIDE_HIGHLIGHT)
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
        #if defined(_TANGENT_SHIFT_MAP)
        float2 hairUV = surface.GetUV(INPUT_PROP(_TangentShiftMapUV));
        float shiftScale = SampleTangentShiftNoise(hairUV) + GetTangentShiftOffset();
        float3 bitangentWS = SafeNormalize(surface.bitangentWS + shiftScale * surface.normalWS);
        float dotTH = dot(bitangentWS, h);
        // avoid sqrt crashes caused by floating point precision
        float cosTH = saturate(dotTH);
        float sinTH = sqrt(saturate(1.0 - cosTH * cosTH));
        float dirAttenuation = smoothstep(-1.0, 0.0, dotTH);
        specularStrength = dirAttenuation * pow(sinTH, GetSpecGloss()) * GetSpecScale();
        #else
        float dotNH = saturate(dot(surface.normalWS, h));
        specularStrength = GetSpecScale() * pow(dotNH, GetSpecGloss());
        #endif
    #else
    specularStrength = MinimalCookTorranceSpecularTerm(surface, brdf, light);
    #endif

    #if defined(_SPEC_MASK)
    float2 specUV = surface.GetUV(INPUT_PROP(_SpecularMaskUV));
    float slide = GetParallaxSensitivity();
    float offset = GetParallaxOffset();
        #if defined(_SPEC_PARALLAX)
        // TODO: use local space viewDirection.y
        float parallaxOffsetV = - surface.viewDirectionWS.y * slide + offset;
        specUV.y += parallaxOffsetV;
        #endif
    float3 specMask = SampleParallaxSpecularMask(float2(specUV.x, specUV.y));
    specularStrength *= specMask;
    #endif

    return specularStrength;
}

float3 ToonDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return ToonSpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float FoldFringeShadow(float shadow, Fragment fragment, Light light)
{
    #if defined(_RECEIVE_FRINGE_SHADOWS)
    if (light.isMainLight)
    {
        shadow = min(shadow, 1 - fragment.stencilMask.STENCIL_MASK_CHANNEL_FRINGE_SHADOW);
        shadow = min(shadow, 1 - fragment.stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES);
    }
    #endif
    return shadow;
}

#if defined(_ATTEN_LINEAR_PARTITION)
    #include "Packages/com.arctoon.render-pipeline/Shaders/Assembly/LinearPartitionModel.hlsl"
#else
    #include "Packages/com.arctoon.render-pipeline/Shaders/Assembly/SigmoidRampModel.hlsl"
#endif

float3 IncomingLight(Surface surface, Fragment fragment, Light light)
{
    float3 attenuation;
    #if defined(_SDF_LIGHT_MAP)
    if (GetSDFLightMapRegionEnabled(surface.regionIndex))
    {
        FaceSDFSample face = SampleFaceSDF(surface, light);
        float shadow = FoldFringeShadow(face.shadowMask, fragment, light);
        attenuation = ShadeDiffuseFace(surface, face, shadow);
    }
    else
    #endif
    {
        float shadow = FoldFringeShadow(light.shadowAttenuation, fragment, light);
        attenuation = ShadeDiffuseBody(surface, light, shadow);
    }

    return attenuation * light.distanceAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light)
{
    return IncomingLight(surface, fragment, light) *
        (ToonDirectBRDF(surface, brdf, light) + ScreenSpaceRimLight(fragment, surface, light));
}

#endif
