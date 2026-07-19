#ifndef ARCTOON_INPUT_CONFIG_INCLUDED
#define ARCTOON_INPUT_CONFIG_INCLUDED

// Per-fragment shared context. CBUFFER-free Library.
// The region-aware overload that reads _RegionCount / _RegionIDChannel lives in the
// Interface tier (Shaders/Interface/RegionInterface.hlsl), because Library must never
// read a per-material CBUFFER. Consumers only ever read `int regionIndex`.

struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    int regionIndex;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV)
{
    InputConfig config;
    config.fragment = GetFragment(positionSS);
    config.baseUV = baseUV;
    config.regionIndex = 0;
    return config;
}

#endif
