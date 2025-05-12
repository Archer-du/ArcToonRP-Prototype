#ifndef ARCTOON_RAMP_INCLUDED
#define ARCTOON_RAMP_INCLUDED

#define RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL 0.125
#define RAMP_DIRECT_LIGHTING_SPECULAR_CHANNEL 0.375

struct DirectLightAttenData
{
    float offset;
    float smooth;
};

struct DirectLightSpecData
{
    float offset;
    float smooth;
};

struct FaceData
{
    float2 faceUV;
    float3 directionWS;
    float3 positionWS;
};

struct HairSpecData
{
    float2 hairUV;
    float3 bitangentWS;
    float gloss;
    float scale;
};

struct RimLightData
{
    float scale;
    float width;
    float depthBias;
};

DirectLightAttenData GetDirectLightAttenData(float offset, float smooth, float smoothNew)
{
    DirectLightAttenData data;
    data.offset = offset;
    data.smooth = smooth;
    // TODO: optimize
    data.smooth = -0.1 / (smoothNew - 1.001);
    return data;
}

DirectLightSpecData GetDirectLightSpecData(float offset, float smooth)
{
    DirectLightSpecData data;
    data.offset = offset;
    data.smooth = -0.1 / (smooth - 1.001);
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

HairSpecData GetHairSpecData(float2 hairUV, float3 bitangentWS, float gloss, float scale)
{
    HairSpecData data;
    data.hairUV = hairUV;
    data.bitangentWS = SafeNormalize(bitangentWS);
    data.gloss = gloss;
    data.scale = scale;
    return data;
}

RimLightData GetRimLightData(float scale, float width, float depthBias)
{
    RimLightData data;
    data.scale = scale;
    data.width = width;
    data.depthBias = depthBias;
    return data;
}

#endif
