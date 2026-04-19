#ifndef ARCTOON_POST_PROCESS_INPUT_INCLUDED
#define ARCTOON_POST_PROCESS_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

#include "../../ShaderLibrary/Input/UnityInput.hlsl"

// Primary source texture bound by BlitUtils.BlitTexture (matches InternalShader.PropertyID.SourceTexture on the C# side).
TEXTURE2D(_SourceTexture);
float4 _SourceTexture_TexelSize;

float4 GetSourceTexelSize()
{
    return _SourceTexture_TexelSize;
}

float4 SampleSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, screenUV, 0);
}

float4 SampleSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(
        TEXTURE2D_ARGS(_SourceTexture, sampler_linear_clamp), screenUV,
        _SourceTexture_TexelSize.zwxy, 1.0, 0.0
    );
}

#endif
