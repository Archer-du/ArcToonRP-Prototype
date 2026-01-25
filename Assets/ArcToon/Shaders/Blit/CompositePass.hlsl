#ifndef ARCTOON_COMPOSITE_INCLUDED
#define ARCTOON_COMPOSITE_INCLUDED

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

TEXTURE2D(_OpaqueColorBuffer);
TEXTURE2D_ARRAY(_DepthPeelingClips);

int _PeelingLayerIndex;

float4 DepthPeelingCompositePassFragment(Varyings_Default input) : SV_TARGET
{
    float4 accumulateColor = SAMPLE_TEXTURE2D(_OpaqueColorBuffer, sampler_point_clamp, input.screenUV);
    for (int k = 0; k < _PeelingLayerIndex + 1; k++)
    {
        // from back to front
        float4 frontColor = SAMPLE_TEXTURE2D_ARRAY(_DepthPeelingClips, sampler_point_clamp, input.screenUV, _PeelingLayerIndex - k);
        accumulateColor = accumulateColor * (1 - frontColor.a) + frontColor * frontColor.a;
        accumulateColor.a = 1;
    }
    return accumulateColor;
}

#endif
