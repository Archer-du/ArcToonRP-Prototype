#ifndef ARCTOON_RAMP_INCLUDED
#define ARCTOON_RAMP_INCLUDED

#define RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL 0.125
#define RAMP_DIRECT_LIGHTING_SPECULAR_CHANNEL 0.375

struct DirectLightAttenData
{
    float offset;
    float smooth;
};

struct FaceData
{
    float3 directionWS;
    float3 positionWS;
    float2 faceUV;
};

DirectLightAttenData GetDirectLightAttenData(float offset, float smooth)
{
    DirectLightAttenData data;
    data.offset = offset;
    data.smooth = smooth;
    return data;
}

FaceData GetFaceData(float2 faceUV)
{
    FaceData data;
    data.directionWS = GetFaceFrontDir();
    data.positionWS = GetFaceCenterPositionWorld();
    data.faceUV = faceUV;
    return data;
}

#endif
