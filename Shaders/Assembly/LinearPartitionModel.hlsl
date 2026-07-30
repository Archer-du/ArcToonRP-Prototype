#ifndef ARCTOON_LINEAR_PARTITION_MODEL_INCLUDED
#define ARCTOON_LINEAR_PARTITION_MODEL_INCLUDED

// Diffuse attenuation model: 7-band linear partition (energy-conserving), per-band tint + per-region
// base colors. Peer of SigmoidRampModel.hlsl; exactly one is included by ToonLightingAssembly.hlsl
// (selected by _ATTEN_LINEAR_PARTITION). Provides ShadeDiffuseBody / ShadeDiffuseFace for the shared
// dispatcher. Requires: ShaderLibrary/LinearPartition.hlsl (CalculateAttenuation, CalculateAlbedo),
// RegionID.hlsl (REGION_PROP_*), SDFFaceInterface.hlsl (FaceSDFSample, when _SDF_LIGHT_MAP).

REGION_PROP_DEFINE_GETTER(float4, _ShadowColor)
REGION_PROP_DEFINE_GETTER(float4, _ShallowColor)

float3 LinearPartitionColor(float litFactor, int regionIndex)
{
    AttenuationData attenuation = CalculateAttenuation(
        INPUT_PROP(_AlbedoSmoothness), litFactor, INPUT_PROP(_DiffuseOffset));
    return CalculateAlbedo(
        REGION_PROP_GET(float4, _ShadowColor, regionIndex).rgb,
        REGION_PROP_GET(float4, _ShallowColor, regionIndex).rgb,
        INPUT_PROP(_ShadowFadeTint).rgb,
        INPUT_PROP(_ShadowTint).rgb,
        INPUT_PROP(_ShallowFadeTint).rgb,
        INPUT_PROP(_ShallowTint).rgb,
        INPUT_PROP(_SSSTint).rgb,
        INPUT_PROP(_FrontTint).rgb,
        attenuation);
}

float3 ShadeDiffuseBody(Surface surface, Light light, float shadow)
{
    float NoL = GetHalfLambertFactor(surface.normalWS, light.directionWS) * 2.0 - 1.0;
    return LinearPartitionColor(min(NoL, shadow * 2.0 - 1.0), surface.regionIndex);
}

#if defined(_SDF_LIGHT_MAP)
float3 ShadeDiffuseFace(Surface surface, FaceSDFSample face, float shadow)
{
    float litFactor = (face.attenuation - face.clipCenter) * 2.0;
    return LinearPartitionColor(min(litFactor, shadow * 2.0 - 1.0), surface.regionIndex);
}
#endif

#endif
