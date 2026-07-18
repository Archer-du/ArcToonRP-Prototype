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

float3 GetRegionDebugColor(int regionIndex)
{
    return _RegionDebugColors[clamp(regionIndex, 0, REGION_MAX_COUNT - 1)];
}

#endif
