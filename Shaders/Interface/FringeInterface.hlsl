#ifndef ARCTOON_FRINGE_INTERFACE_INCLUDED
#define ARCTOON_FRINGE_INTERFACE_INCLUDED

// Interface tier: fringe (hair-over-eye) transparency + shadow-bias getters.
// Reads the per-material CBUFFER; include after the shader's CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP).

float GetFringeTransparentScale()
{
    return INPUT_PROP(_FringeTransparentScale);
}

float2 GetFringeShadowBiasScale()
{
    float2 data;
    data.x = INPUT_PROP(_FringeShadowBiasScaleX) * 0.2;
    data.y = INPUT_PROP(_FringeShadowBiasScaleY) * 0.2;
    return data;
}

#endif
