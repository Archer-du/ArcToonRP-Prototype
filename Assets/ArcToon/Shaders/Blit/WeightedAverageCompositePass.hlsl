#ifndef ARCTOON_WEIGHTED_AVERAGE_INCLUDED
#define ARCTOON_WEIGHTED_AVERAGE_INCLUDED

TEXTURE2D(_AccumulateRGBA);
TEXTURE2D(_AccumulateComplexity);
TEXTURE2D(_BackGroundColor);

float4 WeightedAverageCompositePassFragment(Varyings_Default input) : SV_TARGET
{
    float3 backgroundColor = SAMPLE_TEXTURE2D(_BackGroundColor, sampler_point_clamp, input.screenUV);
    float4 accumulateRGBA = SAMPLE_TEXTURE2D(_AccumulateRGBA, sampler_point_clamp, input.screenUV);
    float revealage = SAMPLE_TEXTURE2D(_AccumulateComplexity, sampler_point_clamp, input.screenUV);

    float3 overlayColor = float3(accumulateRGBA.rgb / max(accumulateRGBA.a, 1e-5));
    float overlayAlpha = 1 - revealage;
    return float4(overlayAlpha * overlayColor + (1 - overlayAlpha) * backgroundColor, 1.0);
}

#endif
