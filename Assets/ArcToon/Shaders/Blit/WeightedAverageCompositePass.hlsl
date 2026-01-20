#ifndef ARCTOON_WEIGHTED_AVERAGE_INCLUDED
#define ARCTOON_WEIGHTED_AVERAGE_INCLUDED

TEXTURE2D(_AccumulateRGBA);
TEXTURE2D(_AccumulateComplexity);
TEXTURE2D(_BackGroundColor);

float4 WeightedAverageCompositePassFragment(Varyings_Default input) : SV_TARGET
{
    float4 accumulateRGBA = SAMPLE_TEXTURE2D(_AccumulateRGBA, sampler_point_clamp, input.screenUV);
    float accumulateComplexity = SAMPLE_TEXTURE2D(_AccumulateComplexity, sampler_point_clamp, input.screenUV);
    float3 averageColor = accumulateRGBA.a == 0 ? 0 : accumulateRGBA.rgb / accumulateRGBA.a;
    float averageAlpha = accumulateComplexity == 0 ? 0 : accumulateRGBA.a / accumulateComplexity;
    float3 backgroundColor = SAMPLE_TEXTURE2D(_BackGroundColor, sampler_point_clamp, input.screenUV);
    float overlayAlpha = pow(max(0.0001, 1 - averageAlpha), accumulateComplexity);
    return float4((averageColor * (1 - overlayAlpha)) + backgroundColor * overlayAlpha, 1.0);
}

#endif
