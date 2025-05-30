﻿#ifndef ARCTOON_CAMERA_COPY_INCLUDED
#define ARCTOON_CAMERA_COPY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_SourceTexture);
float4 _SourceTexture_TexelSize;

float _FinalSrcBlend;
float _FinalDstBlend;

bool _CopyBicubic;

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS_SS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    output.screenUV = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );
    if (_ProjectionParams.x < 0.0)
    {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    return output;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
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

float CopyDepthPassFragment(Varyings input) : SV_DEPTH
{
    return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture, sampler_point_clamp, input.screenUV, 0);
}
#endif
