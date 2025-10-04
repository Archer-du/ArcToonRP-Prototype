#ifndef ARCTOON_TOON_STENCIL_MASK_PASS_INCLUDED
#define ARCTOON_TOON_STENCIL_MASK_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

struct AttributesSM
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSM
{
    float4 positionCS_SS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VaryingsSM EyeLashesReceiverPassVertex(AttributesSM input)
{
    VaryingsSM output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS_SS = TransformObjectToHClip(input.positionOS);
    return output;
}

float4 EyeLashesReceiverPassFragment(VaryingsSM input) : SV_TARGET
{
    float4 stencilMask = 0;
    stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES = 1.0;
    return stencilMask;
}

VaryingsSM FringeReceiverPassVertex(AttributesSM input)
{
    VaryingsSM output;
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

float4 FringeReceiverPassFragment(VaryingsSM input) : SV_TARGET
{
    float4 stencilMask = 0;
    stencilMask.STENCIL_MASK_CHANNEL_FRINGE_SHADOW = 1.0;
    return stencilMask;
}

#endif