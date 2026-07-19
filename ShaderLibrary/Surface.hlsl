#ifndef ARCTOON_SURFACE_INCLUDED
#define ARCTOON_SURFACE_INCLUDED

struct Surface
{
    float3 positionWS;
    float3 normalWS;
    float3 interpolatedNormalWS;
    float3 bitangentWS;
    float3 normalVS;
    float3 viewDirectionWS;
    float3 color;
    float4 UV;
    float linearDepth;
    float alpha;
    float metallic;
    float roughness;
    float fresnelStrength;
    float occlusion;
    float dither;
    uint renderingLayerMask;
    float perObjectCasterID;
    int regionIndex;

    float3 GetSphereNormalWS()
    {
        float3 sphereNormalWS = normalize(positionWS - GetObjectCenterWorldPosition());
        return sphereNormalWS;
    }

    float2 GetUV(int uvSet)
    {
        return uvSet == 0 ? UV.xy : UV.zw;
    }

    float3 GetGISampleNormalWS()
    {
        float3 SampleGINormalWS = normalWS;
        #if defined(_SDF_LIGHT_MAP)
        SampleGINormalWS = GetSphereNormalWS();
        #endif
        return SampleGINormalWS;
    }
};

#endif
