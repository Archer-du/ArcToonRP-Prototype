#ifndef ARCTOON_SIMPLELIT_PASS_INCLUDED
#define ARCTOON_SIMPLELIT_PASS_INCLUDED

#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

Varyings SimplelitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 SimplelitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 color = GetColor(input.baseUV);

    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(input.baseUV));
    #endif

    Surface surface;
    surface.position = input.positionWS;
    surface.color = color.rgb;
    surface.alpha = color.a;
    surface.normal = normalize(input.normalWS);
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.metallic = GetMetallic(input.baseUV);
    surface.smoothness = GetSmoothness(input.baseUV);
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

    #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
    #else
    BRDF brdf = GetBRDF(surface);
    #endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);
    float3 finalColor = GetLighting(surface, brdf, gi);
    finalColor += GetEmission(input.baseUV);
    
    return float4(finalColor.rgb, surface.alpha);
}

#endif
