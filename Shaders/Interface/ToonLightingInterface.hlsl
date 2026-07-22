#ifndef ARCTOON_TOON_LIGHTING_INTERFACE_INCLUDED
#define ARCTOON_TOON_LIGHTING_INTERFACE_INCLUDED

// Interface tier: CBUFFER-derived toon lighting assembly. Split out of the Library math file
// ShaderLibrary/Light/ToonLighting.hlsl. Reads the per-material CBUFFER and calls the surface /
// hair-spec / SDF-face interface getters, so it MUST be included AFTER the shader's CBUFFER and
// after HairSpecInterface.hlsl / SDFFaceInterface.hlsl (whichever the shader's keywords activate).
// Requires: SurfaceSampling.hlsl (INPUT_PROP), ToonLighting.hlsl (math), Ramp.hlsl, Common.hlsl.

TEXTURE2D(_RampSet); SAMPLER(sampler_RampSet);

float3 SampleRampSetChannel(float rampUV, float channel)
{
    return SAMPLE_TEXTURE2D(_RampSet, sampler_RampSet, float2(rampUV, channel)).rgb;
}

float GetRimLightScale()
{
    return INPUT_PROP(_RimScale) * 12.5;
}

float GetRimLightWidth()
{
    return INPUT_PROP(_RimWidth) * 0.07;
}

float GetRimLightDepthBias()
{
    return INPUT_PROP(_RimDepthBias);
}

#if defined(_SDF_LIGHT_MAP)
float3 GF2FaceSpecularStrength(Surface surface, Light light)
{
    if (!light.isMainLight) return 0.0;
    float3 faceDirectionWS = mul((float3x3)GetObjectToWorldMatrix(), GetFaceDirectionOS());
    float3 facePositionWS = mul(GetObjectToWorldMatrix(), GetFacePositionOS()).xyz;
    float3 faceDirHWS = SafeNormalize(float3(faceDirectionWS.x, 0.0, faceDirectionWS.z));
    float3 lightDirHWS = SafeNormalize(float3(light.directionWS.x, 0.0, light.directionWS.z));
    float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - facePositionWS);
    float3 viewDirHWS = SafeNormalize(float3(viewDirWS.x, 0.0, viewDirWS.z));
    float3 halfVecHWS = SafeNormalize(viewDirHWS + lightDirHWS);
    float HdotN = dot(halfVecHWS, faceDirHWS);
    float clipCenter = clamp(-1.7071 * 1.5 * (HdotN - 1.0), 0.001, 0.999);
    float flipSign = cross(halfVecHWS, faceDirHWS).y;
    float2 faceUV = surface.GetUV(INPUT_PROP(_LightMapSDFSourceUV));
    if (flipSign > 0.0f)
    {
        faceUV.x = 1 - faceUV.x;
    }
    float specFactorNoseSDF1 = SampleSDFLightMapNoseSpecular1(faceUV);
    float specFactorNoseSDF2 = SampleSDFLightMapNoseSpecular2(faceUV);
    float specularUV =
        SigmoidSharp(specFactorNoseSDF1, clipCenter, GetNoseSpecularSmooth()) *
        SigmoidSharp(specFactorNoseSDF2, 1 - clipCenter, GetNoseSpecularSmooth());
    float specularStrength = specularUV;
    // TODO: config nose spec attenuation
    if (HdotN < 0.6095) specularStrength = lerp(specularStrength, 0, saturate((0.6095 - HdotN) * 20));
    return specularStrength * GetNoseSpecularStrength();
}
#endif

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

// --- Shared diffuse source: SDF face sample ---
// The expensive face extraction (direction geometry, UV flip, SDF taps) that both models depend on,
// computed once. Every field is consumed downstream, so this carries no model-specific data.
#if defined(_SDF_LIGHT_MAP)
struct FaceSDFSample
{
    float attenuation;  // SDF light-map sample: the primary lit signal on the face
    float clipCenter;   // view/light-dependent lit threshold for that sample
    float shadowMask;   // SDF shadow-mask sample
};

FaceSDFSample SampleFaceSDF(Surface surface, Light light)
{
    float3 faceDirectionWS = mul((float3x3)GetObjectToWorldMatrix(), GetFaceDirectionOS());
    float3 faceDirHWS = SafeNormalize(float3(faceDirectionWS.x, 0.0, faceDirectionWS.z));
    float3 lightDirHWS = SafeNormalize(float3(light.directionWS.x, 0.0, light.directionWS.z));
    float FdotL = dot(faceDirHWS, lightDirHWS);
    float flipSign = cross(faceDirHWS, lightDirHWS).y;
    float2 faceUV = surface.GetUV(INPUT_PROP(_LightMapSDFSourceUV));
    if (flipSign > 0.0f)
    {
        faceUV.x = 1 - faceUV.x;
    }
    FaceSDFSample faceSample;
    faceSample.attenuation = SampleSDFLightMap(faceUV);
    faceSample.clipCenter = - FdotL * 0.5 + 0.5 + GetSDFShadowOffset();
    faceSample.shadowMask = SampleSDFLightMapShadowMask(faceUV);
    return faceSample;
}
#endif

// --- Shared fringe-shadow handling (both models) ---
float FoldFringeShadow(float shadow, Fragment fragment, Light light)
{
    #if defined(_RECEIVE_FRINGE_SHADOWS)
    // Fringe shadow, and eyelashes covered by fringe (whose shadows the fringe caster clips
    // incorrectly), are both occluders folded into the shadow signal by min.
    if (light.isMainLight)
    {
        shadow = min(shadow, 1 - fragment.stencilMask.STENCIL_MASK_CHANNEL_FRINGE_SHADOW);
        shadow = min(shadow, 1 - fragment.stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES);
    }
    #endif
    return shadow;
}

// --- Diffuse attenuation model ---
// Exactly one model compiles (selected by _ATTEN_LINEAR_PARTITION). Each provides the same body/face
// shading interface (ShadeDiffuseBody / ShadeDiffuseFace) that the shared dispatcher below calls, so
// each model's data stays local to it and nothing leaks across the seam. `shadow` arrives already
// fringe-folded, in 0..1.
#if defined(_ATTEN_LINEAR_PARTITION)
REGION_PROP_DEFINE_GETTER(float4, _ShadowColor)
REGION_PROP_DEFINE_GETTER(float4, _ShallowColor)

float3 LinearPartitionColor(float litFactor, int regionIndex)
{
    AttenuationData attenuation = CalculateAttenuation(
        INPUT_PROP(_AlbedoSmoothness), litFactor, INPUT_PROP(_DirectLightAttenOffset));
    return CalculateAlbedo(
        REGION_PROP_GET(float4, _ShadowColor, regionIndex).rgb,
        REGION_PROP_GET(float4, _ShallowColor, regionIndex).rgb,
        INPUT_PROP(_ShadowFadeTint).rgb,
        INPUT_PROP(_ShadowTint).rgb,
        INPUT_PROP(_ShallowFadeTint).rgb,
        INPUT_PROP(_ShallowTint).rgb,
        INPUT_PROP(_SSSTint).rgb,
        INPUT_PROP(_FrontTint).rgb,
        attenuation);
}

float3 ShadeDiffuseBody(Surface surface, Light light, float shadow)
{
    float NoL = GetHalfLambertFactor(surface.normalWS, light.directionWS) * 2.0 - 1.0;
    return LinearPartitionColor(min(NoL, shadow * 2.0 - 1.0), surface.regionIndex);
}

#if defined(_SDF_LIGHT_MAP)
float3 ShadeDiffuseFace(Surface surface, FaceSDFSample face, float shadow)
{
    float litFactor = (face.attenuation - face.clipCenter) * 2.0;
    return LinearPartitionColor(min(litFactor, shadow * 2.0 - 1.0), surface.regionIndex);
}
#endif

#else // Sigmoid/Ramp

float3 SigmoidRampColor(float attenuationUV)
{
    attenuationUV = clamp(attenuationUV, 0.001, 0.999);
    #if defined(_RAMP_SET)
    return SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    #else
    return attenuationUV;
    #endif
}

float3 ShadeDiffuseBody(Surface surface, Light light, float shadow)
{
    float halfLambert = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    float offset = INPUT_PROP(_DirectLightAttenOffset);
    float smooth = RemapSigmoidSmooth(INPUT_PROP(_DirectLightAttenSmoothNew));
    return SigmoidRampColor(SigmoidAttenuation(halfLambert, offset, shadow, offset, smooth));
}

#if defined(_SDF_LIGHT_MAP)
float3 ShadeDiffuseFace(Surface surface, FaceSDFSample face, float shadow)
{
    float offset = INPUT_PROP(_DirectLightAttenOffset);
    float smooth = RemapSigmoidSmooth(INPUT_PROP(_DirectLightAttenSmoothNew));
    return SigmoidRampColor(SigmoidAttenuation(face.attenuation, face.clipCenter, shadow, offset, smooth));
}
#endif

#endif // _ATTEN_LINEAR_PARTITION

// --- Shared dispatch: resolve body/face once, delegate to the active model ---
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

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light, RimLightData rimLightData)
{
    return IncomingLight(surface, fragment, light) *
        (ToonDirectBRDF(surface, brdf, light) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
}

#endif
