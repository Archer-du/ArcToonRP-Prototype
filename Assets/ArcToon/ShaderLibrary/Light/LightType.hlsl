#ifndef ARCTOON_LIGHT_TYPE_INCLUDED
#define ARCTOON_LIGHT_TYPE_INCLUDED

struct Light
{
    float3 color;
    float3 directionWS;
    float shadowAttenuation;
    float distanceAttenuation;
    bool isMainLight;
};

#endif