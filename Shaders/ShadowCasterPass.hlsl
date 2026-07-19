#ifndef ARCTOON_SHADOW_CASTER_PASS_INCLUDED
#define ARCTOON_SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

bool _ShadowPancaking;

struct AttributesSC
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSC
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float4 vertexColor : VAR_COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VaryingsSC ShadowCasterPassVertex(AttributesSC input)
{
    VaryingsSC output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.vertexColor = input.color;

    if (_ShadowPancaking)
    {
        #if UNITY_REVERSED_Z
        output.positionCS_SS.z =
            min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
        #else
        output.positionCS.z =
            max(output.positionCS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
        #endif
    }

    return output;
}

void ShadowCasterPassFragment(VaryingsSC input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GET_INPUT_CONFIG_WITH_REGION(input.positionCS_SS, input.baseUV, input.vertexColor);
    float4 base = GetAlbedo(config);
    #if defined(_CLIPPING)
    clip(base.a - GetAlphaClip(config));
    #endif
    #if defined(_SHADOWS_DITHER)
    float dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    clip(base.a - dither);
    #endif
}

#endif
