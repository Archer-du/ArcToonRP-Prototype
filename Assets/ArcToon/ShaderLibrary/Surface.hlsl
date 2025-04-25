#ifndef ARCTOON_SURFACE_INCLUDED
#define ARCTOON_SURFACE_INCLUDED

struct Surface
{
    float3 positionWS;
    float3 normalWS;
    float3 normalVS;
    float3 interpolatedNormalWS;
    float3 viewDirectionWS;
    float3 color;
    float linearDepth;
    float alpha;
    float metallic;
    float roughness;
    float fresnelStrength;
    float specularStrength;
    float occlusion;
    float dither;
};

#endif
