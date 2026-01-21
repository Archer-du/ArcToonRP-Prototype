#ifndef ARCTOON_CAMERA_COPY_INCLUDED
#define ARCTOON_CAMERA_COPY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_SourceTexture);
float4 _SourceTexture_TexelSize;

float _FinalSrcBlend;
float _FinalDstBlend;

bool _CopyBicubic;

float4 CopyFinalPassFragment(Varyings_Default input) : SV_TARGET
{
    if (_CopyBicubic)
    {
        return SampleTexture2DBicubic(
            TEXTURE2D_ARGS(_SourceTexture, sampler_linear_clamp), input.screenUV,
            _SourceTexture_TexelSize.zwxy, 1.0, 0.0
        );
    }
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV, 0);
}

float CopyColorPassFragment(Varyings_Default input) : SV_TARGET
{
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_point_clamp, input.screenUV, 0);
}

float CopyDepthPassFragment(Varyings_Default input) : SV_DEPTH
{
    return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture, sampler_point_clamp, input.screenUV, 0);
}
#endif
