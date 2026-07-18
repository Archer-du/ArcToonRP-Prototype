#ifndef ARCTOON_TOON_UNLIT_INPUT_INCLUDED
#define ARCTOON_TOON_UNLIT_INPUT_INCLUDED

#include "ToonSurfaceInput.hlsl"
#include "../ShaderLibrary/RegionID.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)

    // Required by the shared InputConfig.hlsl GetInputConfig overload when
    // _REGION_ID_TEXTURE / _REGION_ID_VERTEX_COLOR keywords are active.
    UNITY_DEFINE_INSTANCED_PROP(int, _RegionCount)
    UNITY_DEFINE_INSTANCED_PROP(int, _RegionIDChannel)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

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
