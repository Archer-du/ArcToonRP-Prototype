#ifndef ARCTOON_REGION_ID_INCLUDED
#define ARCTOON_REGION_ID_INCLUDED

// Region ID system: provides a per-fragment region index for parameter array selection.
//
// Keywords:
//   _REGION_ID_TEXTURE      - sample from a dedicated ID texture
//   _REGION_ID_VERTEX_COLOR - read from vertex color
//   (neither)               - region index is always 0
//
// Architecture: the mode divergence is resolved ONCE at InputConfig construction.
// All downstream code consumes only `int regionIndex`.

#define REGION_MAX_COUNT 8

// --- Region property macros ---
// REGION_PROP_DECLARE(type, name): declares name##0 ~ name##7 in instancing buffer.
// REGION_PROP_GET(type, name, regionIndex): returns the value at regionIndex.

#define REGION_PROP_DECLARE(type, name) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##0) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##1) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##2) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##3) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##4) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##5) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##6) \
    UNITY_DEFINE_INSTANCED_PROP(type, name##7)

#define REGION_PROP_GET(type, name, regionIndex) _RegionPropGet_##type##_##name(regionIndex)

// Pack the 8 UNITY_ACCESS_INSTANCED_PROP entries into a local array indexed by regionIndex.
// Written as a single expression so the shader compiler can constant-fold when regionIndex is known,
// and turn the lookup into a small select tree otherwise.
#define REGION_PROP_DEFINE_GETTER(type, name) \
    type _RegionPropGet_##type##_##name(int regionIndex) \
    { \
        const type values[REGION_MAX_COUNT] = { \
            INPUT_PROP(name##0), \
            INPUT_PROP(name##1), \
            INPUT_PROP(name##2), \
            INPUT_PROP(name##3), \
            INPUT_PROP(name##4), \
            INPUT_PROP(name##5), \
            INPUT_PROP(name##6), \
            INPUT_PROP(name##7), \
        }; \
        return values[clamp(regionIndex, 0, REGION_MAX_COUNT - 1)]; \
    }

// --- Debug colors ---
static const float3 _RegionDebugColors[REGION_MAX_COUNT] =
{
    float3(1.0, 0.2, 0.2),   // 0: red
    float3(0.2, 1.0, 0.2),   // 1: green
    float3(0.2, 0.4, 1.0),   // 2: blue
    float3(1.0, 1.0, 0.2),   // 3: yellow
    float3(1.0, 0.2, 1.0),   // 4: magenta
    float3(0.2, 1.0, 1.0),   // 5: cyan
    float3(1.0, 0.6, 0.2),   // 6: orange
    float3(0.6, 0.2, 1.0),   // 7: purple
};

#if defined(_REGION_ID_TEXTURE)
// Sampler comes from the shared static sampler declared in Common.hlsl (sampler_point_clamp).
// A region ID map is a classification tag per texel, never filtered.
TEXTURE2D(_RegionIDMap);
#endif

int ResolveRegionIndex(float raw, int regionCount)
{
    int index = (int)round(raw * (regionCount - 1));
    return clamp(index, 0, regionCount - 1);
}

// The single InputConfig constructor. CBUFFER-free: regionCount / channel arrive as parameters,
// and the ID map is a per-material texture (not part of the instancing CBUFFER, like _BaseMap in
// SurfaceSampling.hlsl), so this stays in the Library tier. Call sites use the
// GET_INPUT_CONFIG_WITH_REGION(...) macro below, which fills the two params from the per-material
// CBUFFER at the (post-CBUFFER) call site. Requires Fragment.hlsl (via Common.hlsl) to provide the
// InputConfig struct and GetFragment before this file.
InputConfig MakeInputConfigWithRegion(float4 positionSS, float2 baseUV, float4 vertexColor,
                                      int regionCount, int channel)
{
    InputConfig config;
    config.fragment = GetFragment(positionSS);
    config.baseUV = baseUV;
    config.regionIndex = 0;
    #if defined(_REGION_ID_TEXTURE)
    float4 idSample = SAMPLE_TEXTURE2D(_RegionIDMap, sampler_point_clamp, baseUV);
    config.regionIndex = ResolveRegionIndex(SelectChannel(idSample, channel), regionCount);
    #elif defined(_REGION_ID_VERTEX_COLOR)
    config.regionIndex = ResolveRegionIndex(SelectChannel(vertexColor, channel), regionCount);
    #endif
    return config;
}

// Single-line region-aware construction used by pass fragment/vertex code:
//   InputConfig config = GET_INPUT_CONFIG_WITH_REGION(positionSS, baseUV, vertexColor);
// The _RegionCount / _RegionIDChannel reads materialize here, at the call site (after the shader's
// CBUFFER), while the mechanism above stays CBUFFER-free in the Library tier.
#define GET_INPUT_CONFIG_WITH_REGION(positionSS, baseUV, vertexColor) \
    MakeInputConfigWithRegion(positionSS, baseUV, vertexColor, INPUT_PROP(_RegionCount), INPUT_PROP(_RegionIDChannel))

float3 GetRegionDebugColor(int regionIndex)
{
    return _RegionDebugColors[clamp(regionIndex, 0, REGION_MAX_COUNT - 1)];
}

#endif
