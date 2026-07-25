#ifndef ARCTOON_LINEAR_PARTITION_INCLUDED
#define ARCTOON_LINEAR_PARTITION_INCLUDED

// Library tier: CBUFFER-free math for the linear-partition diffuse attenuation model.
// The lit factor (NoL-space, -1..1) is split into 7 adjacent, linearly-blended bands whose
// weights sum to ~1 (energy conserving). Each band is tinted and summed into a diffuse color.
// The CBUFFER-reading assembly (reading per-material tints / per-region base colors and building
// the lit factor from surface/light) lives in Shaders/Assembly/ToonLightingAssembly.hlsl.

// Per-band weights, dark -> lit:
// shadowFade  : shadow edge (most affected by ambient / rim)
// shadow      : core shadow
// shallowFade : penumbra edge (shadow-to-lit transition)
// shallow     : weakly lit
// sss         : subsurface scattering band (skin / wax translucency)
// front       : near-direct transition
// forward     : fully direct highlight core
struct AttenuationData
{
    float shadowFade;
    float shadow;
    float shallowFade;
    float shallow;
    float sss;
    float front;
    float forward;
};

AttenuationData CalculateAttenuation(float albedoSmoothness, float litFactor, float diffuseOffset)
{
    AttenuationData attenuationData;
    float baseAttenuation = (litFactor + diffuseOffset) * 1.5;
    albedoSmoothness = max(0.0001, albedoSmoothness) * 1.5;

    float tempShadowFade = (baseAttenuation + 1.5) / (1.0 - albedoSmoothness);
    attenuationData.shadowFade = saturate(min(1.0, 1.0 - tempShadowFade));

    float tempShadow = (baseAttenuation + 0.5) / albedoSmoothness + 0.5;
    attenuationData.shadow = saturate(min(tempShadowFade, 1.0 - tempShadow));

    float tempShallowFade = baseAttenuation / albedoSmoothness + 0.5;
    attenuationData.shallowFade = saturate(min(tempShadow, 1.0 - tempShallowFade));

    float tempShallow = (baseAttenuation - 0.5) / albedoSmoothness + 0.5;
    attenuationData.shallow = saturate(min(tempShallowFade, 1.0 - tempShallow));

    float tempSSS = (baseAttenuation - 0.5) / albedoSmoothness - 0.5;
    attenuationData.sss = saturate(min(tempShallow, 1.0 - tempSSS));

    float tempFront = (baseAttenuation - 2.0) / albedoSmoothness + 1.5;
    attenuationData.front = saturate(min(tempSSS, 1.0 - tempFront));

    attenuationData.forward = saturate(tempFront);
    return attenuationData;
}

// Blend the 7 bands into a diffuse attenuation color. Shadow-side bands use shadowColor, lit-side
// bands use shallowColor; the forward band is untinted (pure lit). Light color / distance / GI are
// applied by the caller, so this returns only the band-blended tint (no light color, no ambient).
float3 CalculateAlbedo(
    float3 shadowColor,
    float3 shallowColor,
    float3 shadowFadeTint,
    float3 shadowTint,
    float3 shallowFadeTint,
    float3 shallowTint,
    float3 sssTint,
    float3 frontTint,
    AttenuationData attenuation)
{
    float3 shadowFadeColor  = attenuation.shadowFade  * shadowFadeTint  * shadowColor;
    float3 shadowColorPart  = attenuation.shadow      * shadowTint      * shadowColor;
    float3 shallowFadeColor = attenuation.shallowFade * shallowFadeTint * shallowColor;
    float3 shallowColorPart = attenuation.shallow     * shallowTint     * shallowColor;
    float3 sssColor         = attenuation.sss         * sssTint         * shallowColor;
    float3 frontColor       = attenuation.front       * frontTint       * shallowColor;
    float3 forwardColor     = attenuation.forward;

    return shadowFadeColor + shadowColorPart + shallowFadeColor + shallowColorPart
        + sssColor + frontColor + forwardColor;
}

#endif
