#ifndef ARCTOON_OUTLINE_INTERFACE_INCLUDED
#define ARCTOON_OUTLINE_INTERFACE_INCLUDED

// Interface tier: geometry outline getters. Reads the per-material CBUFFER.
// _OutlineColor is a region-level parameter (see RegionID.hlsl); the getter is generated
// here so it reads the shader's own _OutlineColor0.._OutlineColor7 declarations.
// Requires: SurfaceSampling.hlsl (INPUT_PROP), RegionID.hlsl (REGION_PROP_* macros).

REGION_PROP_DEFINE_GETTER(float4, _OutlineColor)

float GetOutlineScale()
{
    return INPUT_PROP(_OutlineScale);
}

float3 GetOutlineColor(int regionIndex)
{
    return REGION_PROP_GET(float4, _OutlineColor, regionIndex).rgb;
}

#endif
