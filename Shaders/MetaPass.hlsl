#ifndef ARCTOON_META_PASS_INCLUDED
#define ARCTOON_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct AttributesMT
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
    float4 color : COLOR;
};

struct VaryingsMT
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float4 vertexColor : VAR_COLOR;
};

VaryingsMT MetaPassVertex(AttributesMT input)
{
    VaryingsMT output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS_SS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.vertexColor = input.color;
    return output;
}

float4 MetaPassFragment(VaryingsMT input) : SV_TARGET
{
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV, input.vertexColor);
    float4 base = GetAlbedo(config);
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(config);
    surface.roughness = GetRoughness(config);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(
            PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue
        );
    }
    else if (unity_MetaFragmentControl.y)
    {
        meta = float4(GetEmission(config), 1.0);
    }
    return meta;
}

#endif
