#ifndef ARCTOON_SURFACE_INCLUDED
#define ARCTOON_SURFACE_INCLUDED

struct Surface
{
    float3 position;
    float3 normal;
    float3 interpolatedNormal;
    float3 viewDirection;
    float3 color;
    float linearDepth;
    float alpha;
    float metallic;
    float smoothness;
    float fresnelStrength;
    float occlusion;
    float dither;
};

#endif
