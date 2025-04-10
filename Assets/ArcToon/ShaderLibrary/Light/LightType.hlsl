#ifndef ARCTOON_LIGHT_TYPE_INCLUDED
#define ARCTOON_LIGHT_TYPE_INCLUDED

struct DirectionalLight
{
    float3 color;
    float3 direction;
    float attenuation;
};

#endif