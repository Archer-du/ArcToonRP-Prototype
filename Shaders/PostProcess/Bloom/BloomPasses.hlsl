#ifndef ARCTOON_BLOOM_PASSES_INCLUDED
#define ARCTOON_BLOOM_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.arctoon.render-pipeline/Shaders/PostProcess/PostProcessInput.hlsl"

// Uniforms
bool _BloomBicubicUpsampling;
float4 _BloomThreshold;
float _BloomScale;
float _BloomScatter;

// Bloom-specific high-resolution input texture used during the upsample/combine stage.
// During upsample, _SourceTexture holds the low-res (more blurred) level,
// and _BloomHighResTexture holds the higher-res (less blurred) level to be combined.
TEXTURE2D(_BloomHighResTexture);

float4 SampleBloomHighRes(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_BloomHighResTexture, sampler_linear_clamp, screenUV, 0);
}

float3 KneeCurveFilter(float3 color)
{
    float b = Max3(color.r, color.g, color.b);
    float s = b + _BloomThreshold.y;
    s = clamp(s, 0.0, _BloomThreshold.z);
    s = s * s * _BloomThreshold.w;
    float weight = max(s, b - _BloomThreshold.x);
    weight /= max(b, 0.00001);
    return color * weight;
}

float4 BloomHorizontalPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] =
    {
        -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
    };
    float weights[] =
    {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
        0.19459459, 0.12162162, 0.05405405, 0.01621622
    };
    for (int i = 0; i < 9; i++)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += SampleSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] =
    {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] =
    {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    for (int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += SampleSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BloomPrefilterPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 color = KneeCurveFilter(SampleSource(input.screenUV).rgb);
    return float4(color, 1.0);
}

float4 BloomPrefilterFirefliesPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 finalColor = 0.0;
    float weightSum = 0.0;
    float2 offsets[] =
    {
        float2(0.0, 0.0),
        float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
    };
    for (int i = 0; i < 5; i++)
    {
        float3 color =
            SampleSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        color = KneeCurveFilter(color);
        float weight = 1.0 / (Luminance(color) + 1.0);
        finalColor += weight * color;
        weightSum += weight;
    }
    finalColor /= weightSum;
    return float4(finalColor, 1.0);
}

float4 BloomAdditiveCombinePassFragment(Varyings_Default input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = SampleSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = SampleSource(input.screenUV).rgb;
    }
    float3 highRes = SampleBloomHighRes(input.screenUV).rgb;
    return float4(lowRes + highRes, 1.0);
}

float4 BloomAdditiveCombineFinalPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = SampleSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = SampleSource(input.screenUV).rgb;
    }
    float4 highRes = SampleBloomHighRes(input.screenUV);
    return float4(lowRes * _BloomScale + highRes.rgb, highRes.a);
}

float4 BloomScatterCombinePassFragment(Varyings_Default input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = SampleSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = SampleSource(input.screenUV).rgb;
    }
    float3 highRes = SampleBloomHighRes(input.screenUV).rgb;
    return float4(lerp(highRes, lowRes, _BloomScatter), 1.0);
}

float4 BloomScatterCombineFinalPassFragment(Varyings_Default input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = SampleSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = SampleSource(input.screenUV).rgb;
    }
    float4 highRes = SampleBloomHighRes(input.screenUV);
    // lowRes - filtered highRes, energy conservation
    lowRes += highRes.rgb - KneeCurveFilter(highRes.rgb);
    return float4(lerp(highRes.rgb, lowRes, _BloomScatter), highRes.a);
}

#endif
