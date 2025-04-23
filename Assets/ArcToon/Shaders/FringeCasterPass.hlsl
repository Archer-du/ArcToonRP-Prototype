#ifndef ARCTOON_FRINGE_CASTER_PASS_INCLUDED
#define ARCTOON_FRINGE_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings FringeCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 mainLightDirectionWS = normalize(GetMainLightDirection());
    float3 mainLightDirectionVS = normalize(TransformWorldToViewDir(mainLightDirectionWS));
    float3 cameraDirectionOS = normalize(TransformWorldToObject(_WorldSpaceCameraPos));
    // float camDirFactor = 1 - smoothstep(0.1, 0.9, cameraDirectionOS.y);
        
    float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
    positionVS.x -= mainLightDirectionVS.x * GetFringeShadowBiasScale().x;
    // positionVS.y -= 0.007 * 10 * camDirFactor;
    positionVS.y -= mainLightDirectionVS.y * GetFringeShadowBiasScale().y;
    output.positionCS_SS = TransformWViewToHClip(positionVS);
    
    return output;
}

float4 FringeCasterPassFragment(Varyings input) : SV_TARGET
{
    return 0.0;
}

#endif