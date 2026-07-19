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

#endif
