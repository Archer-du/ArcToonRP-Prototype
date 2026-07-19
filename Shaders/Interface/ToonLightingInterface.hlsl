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
    #if defined(_RAMP_SET)
    return SAMPLE_TEXTURE2D(_RampSet, sampler_RampSet, float2(rampUV, channel)).rgb;
    #endif
    return 1.0;
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
    return GF2FaceSpecularStrength(surface, light);
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

float3 IncomingLight(Surface surface, Fragment fragment, Light light, DirectLightAttenData attenData)
{
    #if defined(_SDF_LIGHT_MAP)
    float3 faceDirectionWS = mul((float3x3)GetObjectToWorldMatrix(), GetFaceDirectionOS());
    float3 faceDirHWS = SafeNormalize(float3(faceDirectionWS.x, 0.0, faceDirectionWS.z));
    float3 lightDirHWS = SafeNormalize(float3(light.directionWS.x, 0.0, light.directionWS.z));
    float FdotL = dot(faceDirHWS, lightDirHWS);
    float clipCenter = - FdotL * 0.5 + 0.5 + GetSDFShadowOffset();
    float flipSign = cross(faceDirHWS, lightDirHWS).y;
    float2 faceUV = surface.GetUV(INPUT_PROP(_LightMapSDFSourceUV));
    if (flipSign > 0.0f)
    {
        faceUV.x = 1 - faceUV.x;
    }
    float attenFactorSDF = SampleSDFLightMap(faceUV);
    // TODO: shadow mask channel
    float shadowMaskFactorSDF = SampleSDFLightMapShadowMask(faceUV);
    float attenuationUV = min(
        SigmoidSharp(attenFactorSDF, clipCenter, attenData.smooth),
        SigmoidSharp(shadowMaskFactorSDF, attenData.offset, attenData.smooth)
    );
    #else
    float halfLambertFactor = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    float attenuationUV = min(
        SigmoidSharp(halfLambertFactor, attenData.offset, attenData.smooth),
        SigmoidSharp(light.shadowAttenuation, attenData.offset, attenData.smooth)
    );
    #endif

    #if defined(_RECEIVE_FRINGE_SHADOWS)
    if (light.isMainLight)
    {
        attenuationUV = min(
            attenuationUV,
            SigmoidSharp(1 - fragment.stencilMask.STENCIL_MASK_CHANNEL_FRINGE_SHADOW,
                attenData.offset, attenData.smooth)
        );
        // attenuation compensation for transparent fringe —— eyelashes covered by fringe may show incorrect shadows due to the fringe shadow caster clipping.
        attenuationUV = lerp(attenuationUV, 0, fragment.stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES);
    }
    #endif

    #if defined(_RAMP_SET)
    float3 lightAttenuation = SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    #else
    float lightAttenuation = attenuationUV;
    #endif

    return lightAttenuation * light.distanceAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light,
                   DirectLightAttenData attenData, RimLightData rimLightData)
{
    return IncomingLight(surface, fragment, light, attenData) *
        (ToonDirectBRDF(surface, brdf, light) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
}

#endif
