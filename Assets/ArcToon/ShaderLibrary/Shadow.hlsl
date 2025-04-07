#ifndef ARCTOON_SHADOWS_INCLUDED
#define ARCTOON_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

#include "Surface.hlsl"
#include "Common.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
CBUFFER_END


struct CascadeShadowData
{
    int cascadeOffset;
    float cascadeFade;
};

CascadeShadowData GetCascadeShadowData(Surface surface)
{
    CascadeShadowData data;
    int i;
    data.cascadeFade = 1.0;
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            if (i == _CascadeCount - 1)
            {
                data.cascadeFade *= FadedStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            }
            break;
        }
    }
    data.cascadeOffset = i;
    return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(
        _DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS
    );
}

#endif
