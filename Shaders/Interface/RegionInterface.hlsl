#ifndef ARCTOON_REGION_INTERFACE_INCLUDED
#define ARCTOON_REGION_INTERFACE_INCLUDED

// Interface tier: region-aware InputConfig construction. Reads _RegionCount / _RegionIDChannel
// and (in texture mode) samples _RegionIDMap, so it MUST be included after the shader's CBUFFER.
// The mode divergence is resolved ONCE here; downstream code only consumes config.regionIndex.
// Requires: SurfaceSampling.hlsl (INPUT_PROP), InputConfig.hlsl, RegionID.hlsl.

InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float4 vertexColor)
{
    InputConfig config = GetInputConfig(positionSS, baseUV);
    #if defined(_REGION_ID_TEXTURE)
    float4 idSample = SAMPLE_TEXTURE2D(_RegionIDMap, sampler_point_clamp, baseUV);
    float raw = SelectChannel(idSample, INPUT_PROP(_RegionIDChannel));
    config.regionIndex = ResolveRegionIndex(raw, INPUT_PROP(_RegionCount));
    #elif defined(_REGION_ID_VERTEX_COLOR)
    float raw = SelectChannel(vertexColor, INPUT_PROP(_RegionIDChannel));
    config.regionIndex = ResolveRegionIndex(raw, INPUT_PROP(_RegionCount));
    #endif
    return config;
}

#endif
