#ifndef ARCTOON_SIMPLELIT_PASS_INCLUDED
#define ARCTOON_SIMPLELIT_PASS_INCLUDED

#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    #if defined(_NORMAL_MAP)
    float4 tangentWS : VAR_TANGENT;
    #endif
    float2 baseUV : VAR_BASE_UV;
    #if defined(_DETAIL_MAP)
    float2 detailUV : VAR_DETAIL_UV;
    #endif
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
    #if defined(_NORMAL_MAP)
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    #endif
    output.baseUV = TransformBaseUV(input.baseUV);
    #if defined(_DETAIL_MAP)
    output.detailUV = TransformDetailUV(input.baseUV);
    #endif
    return output;
}

float4 SimplelitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.positionCS.xy, unity_LODFade.x);
    
    InputConfig config = GetInputConfig(input.baseUV);
    #if defined(_MASK_MAP)
    config.useMODSMask = true;
    #endif
    #if defined(_DETAIL_MAP)
    config.detailUV = input.detailUV;
    config.useDetail = true;
    #endif
    
    float4 color = GetColor(config);
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif

    Surface surface;
    surface.position = input.positionWS;
    surface.color = color.rgb;
    surface.alpha = color.a;
    
    #if defined(_NORMAL_MAP)
    surface.normal = normalize(NormalTangentToWorld(GetNormalTS(config),
        input.normalWS, input.tangentWS));
    surface.interpolatedNormal = normalize(input.normalWS);
    #else
    surface.normal = normalize(input.normalWS);
    surface.interpolatedNormal = surface.normal;
    #endif

    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.occlusion = GetOcclusion(config);
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

    #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
    #else
    BRDF brdf = GetBRDF(surface);
    #endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 finalColor = GetLighting(surface, brdf, gi);
    finalColor += GetEmission(config);
    
    return float4(finalColor.rgb, surface.alpha);
}

#endif
