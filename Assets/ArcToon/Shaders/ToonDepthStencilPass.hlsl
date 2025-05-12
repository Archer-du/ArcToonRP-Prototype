#ifndef ARCTOON_TOON_DEPTH_STENCIL_PASS_INCLUDED
#define ARCTOON_TOON_DEPTH_STENCIL_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Light/Lighting.hlsl"

struct AttributesDS
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsDS
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VaryingsDS DefaultDepthStencilPassVertex(AttributesDS input)
{
    VaryingsDS output = (VaryingsDS)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS_SS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half DefaultDepthStencilPassFragment(VaryingsDS input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS_SS);
    #endif
    return input.positionCS_SS.z;
}

VaryingsDS FringeStencilPassPassVertex(AttributesDS input)
{
    VaryingsDS output;
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

#endif