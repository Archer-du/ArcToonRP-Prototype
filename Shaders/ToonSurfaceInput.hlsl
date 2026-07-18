#ifndef ARCTOON_TOON_SURFACE_INPUT_INCLUDED
#define ARCTOON_TOON_SURFACE_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Input/InputConfig.hlsl"

// Shared surface texture used by every ArcToon shader family.
// Each family's private *Input.hlsl declares its own _BaseMap_ST / _BaseColor / _Cutoff
// inside its own UnityPerMaterial CBUFFER and calls the parameterized helpers below.
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

#endif
