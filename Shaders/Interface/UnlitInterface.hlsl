#ifndef ARCTOON_UNLIT_INTERFACE_INCLUDED
#define ARCTOON_UNLIT_INTERFACE_INCLUDED

// Interface tier: minimal unlit getters. Reads the per-material CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP, SampleAlbedo, TransformUVWithST, ResolveFinalAlpha),
// Common.hlsl (Fragment.hlsl provides InputConfig).

float2 TransformBaseUV(float2 rawBaseUV)
{
    return TransformUVWithST(rawBaseUV, INPUT_PROP(_BaseMap_ST));
}

float4 GetAlbedo(InputConfig input)
{
    return SampleAlbedo(input.baseUV, INPUT_PROP(_BaseColor));
}

float GetAlphaClip(InputConfig input)
{
    return INPUT_PROP(_Cutoff);
}

float GetFinalAlpha(float alpha)
{
    return ResolveFinalAlpha(alpha, INPUT_PROP(_ZWrite));
}

#endif
