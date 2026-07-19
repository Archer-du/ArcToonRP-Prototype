#ifndef ARCTOON_SURFACE_SAMPLING_INCLUDED
#define ARCTOON_SURFACE_SAMPLING_INCLUDED

#include "../Common.hlsl"

// Per-material instanced property accessor. CBUFFER-free at definition; it expands to a
// UnityPerMaterial read only where invoked, which is always the Interface tier (after the
// shader's own CBUFFER). Library code must never invoke it.
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

// Shared surface texture used by every ArcToon shader family. Each family declares its own
// _BaseMap_ST / _BaseColor / _Cutoff inside its own CBUFFER and passes them to the helpers below.
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// UV transform: rawUV * st.xy + st.zw. Caller passes _BaseMap_ST from its own CBUFFER.
float2 TransformUVWithST(float2 rawUV, float4 st)
{
    return rawUV * st.xy + st.zw;
}

// Sample the shared _BaseMap and modulate with a caller-provided tint.
float4 SampleAlbedo(float2 uv, float4 tint)
{
    float4 sampled = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    return sampled * tint;
}

// Applied at fragment output so that the ZWrite-disabled transparent path still writes
// a fully-opaque pixel where the shader wants "final pixel is opaque regardless of alpha".
float ResolveFinalAlpha(float alpha, float zwrite)
{
    return zwrite ? 1.0 : alpha;
}

#define STENCIL_MASK_CHANNEL_FRINGE_SHADOW g
#define STENCIL_MASK_CHANNEL_EYE_LASHES b

#endif
