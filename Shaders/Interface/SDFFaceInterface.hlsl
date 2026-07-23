#ifndef ARCTOON_SDF_FACE_INTERFACE_INCLUDED
#define ARCTOON_SDF_FACE_INTERFACE_INCLUDED

// Interface tier: SDF face-lighting map sampling + nose-specular getters.
// Reads the per-material CBUFFER; include after the shader's CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP).

TEXTURE2D(_LightMapSDF); SAMPLER(sampler_LightMapSDF);

float3 GetFaceDirectionOS()
{
    return INPUT_PROP(_FaceVector).xyz;
}

float4 GetFacePositionOS()
{
    return float4(0, 0, 0, 1);
}

float SampleSDFLightMap(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).r;
    #endif
    return 1.0;
}

float SampleSDFLightMapShadowMask(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).a;
    #endif
    return 1.0;
}

float SampleSDFLightMapNoseSpecular1(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP_SPEC)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).g;
    #endif
    return 0.0;
}

float SampleSDFLightMapNoseSpecular2(float2 faceUV)
{
    #if defined(_SDF_LIGHT_MAP_SPEC)
    return SAMPLE_TEXTURE2D(_LightMapSDF, sampler_LightMapSDF, faceUV).b;
    #endif
    return 0.0;
}

float GetNoseSpecularStrength()
{
    return INPUT_PROP(_NoseSpecularStrengthSDF) * 50;
}

float GetNoseSpecularSmooth()
{
    return INPUT_PROP(_NoseSpecularSmoothSDF) * 2;
}

float GetSDFShadowOffset()
{
    return INPUT_PROP(_ShadowOffsetSDF) * 0.25;
}

// Per-region gate for SDF light map influence. ToonFace exposes the toggle in its GUI;
// ToonPupil declares the same slots (defaults to off, but its _SDF_LIGHT_MAP variant is never
// enabled in practice) so the shared SDF path compiles uniformly. The props are declared
// unconditionally in the CBUFFER, so these getters need no keyword guard; the on/off semantics
// live in the SampleSDFLightMap* fallbacks below, and uncalled functions are dead-stripped.
REGION_PROP_DEFINE_GETTER(float, _SDFLightMapRegionEnabled)

bool GetSDFLightMapRegionEnabled(int regionIndex)
{
    return REGION_PROP_GET(float, _SDFLightMapRegionEnabled, regionIndex) > 0.5;
}

// SDF face specular (nose highlight): GF2 two-sample product gated by a view/light clipCenter.
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

// SDF face diffuse sample: the primary lit signal, its view/light-dependent threshold, and the
// shadow mask, extracted once for whichever diffuse attenuation model is active.
struct FaceSDFSample
{
    float attenuation;
    float clipCenter;
    float shadowMask;
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
