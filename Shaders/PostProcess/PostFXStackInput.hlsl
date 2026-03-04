#ifndef ARCTOON_POST_FX_INPUT_INCLUDED
#define ARCTOON_POST_FX_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

#include "../../ShaderLibrary/Input/UnityInput.hlsl"

TEXTURE2D(_PostFXSource);
float4 _PostFXSource_TexelSize;

TEXTURE2D(_PostFXSource2);
float4 _PostFXSource2_TexelSize;

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 SampleSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

// TODO: extract
float4 SampleSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(
        TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV,
        _PostFXSource_TexelSize.zwxy, 1.0, 0.0
    );
}


#endif