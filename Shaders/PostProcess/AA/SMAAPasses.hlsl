#ifndef ARCTOON_POSTPROCESSING_SMAA_BRIDGE_INCLUDED
#define ARCTOON_POSTPROCESSING_SMAA_BRIDGE_INCLUDED

// -----------------------------------------------------------------------------
// SMAA bridge file.
//
// Responsibilities:
//   1) Pull in the project shader library (for Varyings_Default /
//      DefaultPassVertex) and SRP core macros (TEXTURE2D, SAMPLER, ...).
//   2) Select a quality preset from our SMAA_PRESET_* keywords.
//   3) Provide the defines expected by SMAA.hlsl (SMAA_HLSL_4_1, RT_METRICS,
//      sampler names, area/search texture channel selectors, gamma path).
//   4) Declare the textures we feed from C# (_SourceTexture, _SMAAAreaTexture,
//      _SMAASearchTexture, _SMAABlendTexture) and the metrics constant.
//   5) Provide thin vertex/fragment wrappers around the three SMAA entry
//      points (edge detection, blending weight calculation, neighborhood
//      blending). SMAA's built-in VS helpers expect a caller to write the
//      `offsets[]` / `pixcoord` varyings — we do that here.
//
// NOTE on fullscreen triangle: we call the project-wide DefaultPassVertex
// (defined in ShaderLibrary/Common.hlsl) to obtain positionCS / screenUV,
// then augment the output with SMAA's extra interpolants (offsets[3] and
// pixcoord). We intentionally do NOT use SRP core's
// GetFullScreenTriangleVertexPosition / GetFullScreenTriangleTexCoord: those
// apply a compile-time Y-flip based on UNITY_UV_STARTS_AT_TOP which would
// compound with the runtime `_ProjectionParams.x < 0` flip inside
// DefaultPassVertex and invert the image. See
// ChatLogs/Document/FullscreenBlit_UVConvention.md for the full rationale.
//
// This file is a close adaptation of URP's
// `SubpixelMorphologicalAntialiasingBridge.hlsl`, simplified for ArcToonRP's
// naming conventions (_SourceTexture instead of _BlitTexture, no dynamic
// scaling scale-bias, no XR support, no Blitter protocol).
// -----------------------------------------------------------------------------

// Project Common.hlsl transitively pulls in SRP core Common.hlsl and project
// UnityInput.hlsl (which declares _ProjectionParams), so we do not need to
// include them separately here.
#include "../../../ShaderLibrary/Common.hlsl"

// ArcToonRP does not support XR / stereo rendering. The vendored SMAA.hlsl
// algorithm library (kept pristine for easy upstream sync with URP) internally
// references TEXTURE2D_X / SAMPLE_TEXTURE2D_X[_LOD]. Alias those to the plain
// 2D variants here so we can keep the library unmodified without dragging
// TextureXR.hlsl into the build.
#define TEXTURE2D_X                               TEXTURE2D
#define SAMPLE_TEXTURE2D_X(tex, sampler, uv)      SAMPLE_TEXTURE2D(tex, sampler, uv)
#define SAMPLE_TEXTURE2D_X_LOD(tex, sampler, uv, lod) SAMPLE_TEXTURE2D_LOD(tex, sampler, uv, lod)

// Static clamp samplers required by SMAA's macros ( LinearSampler / PointSampler aliases below ).
SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_PointClamp);

// Map our project keywords to SMAA's own preset macros.
#if defined(_SMAA_PRESET_LOW)
    #define SMAA_PRESET_LOW
#elif defined(_SMAA_PRESET_MEDIUM)
    #define SMAA_PRESET_MEDIUM
#else
    #define SMAA_PRESET_HIGH
#endif

#define SMAA_HLSL_4_1

// Lookup texture channel selectors match the way URP's AreaTex.tga / SearchTex.tga
// were authored (RG and A respectively).
#define SMAA_AREATEX_SELECT(s) s.rg
#define SMAA_SEARCHTEX_SELECT(s) s.a

// SMAA.hlsl's ported HLSL4+ macros reference `LinearSampler` / `PointSampler`
// directly. Bind them to the SRP built-in static samplers.
#define LinearSampler sampler_LinearClamp
#define PointSampler  sampler_PointClamp

// Color edge detection requires linear color.
#if UNITY_COLORSPACE_GAMMA
    #define GAMMA_FOR_EDGE_DETECTION (1.0)
#else
    #define GAMMA_FOR_EDGE_DETECTION (1.0 / 2.2)
#endif

// ----- Textures bound from the C# side ---------------------------------------
TEXTURE2D(_SourceTexture);        // current ping-pong source, color in pass 0/2, edges in pass 1
TEXTURE2D(_SMAAAreaTexture);      // precomputed lookup LUT
TEXTURE2D(_SMAASearchTexture);    // precomputed lookup LUT
TEXTURE2D(_SMAABlendTexture);     // blending weights, written in pass 1, sampled in pass 2

// Render-target metrics: (1/w, 1/h, w, h). Set per-frame from SMAAProcessor.
float4 _SMAAMetrics;

#define SMAA_RT_METRICS _SMAAMetrics

// NOTE: SMAA.hlsl must be included AFTER all SMAA_* defines and sampler aliases
// are in place. Do not move this include.
#include "SubpixelMorphologicalAntialiasing.hlsl"

// -----------------------------------------------------------------------------
// Pass 1: Edge Detection
// -----------------------------------------------------------------------------
struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct VaryingsEdge
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    float4 offsets[3] : TEXCOORD1;
};

VaryingsEdge VertEdge(Attributes input)
{
    VaryingsEdge output;
    // Use the project's shared fullscreen-triangle VS for positionCS / uv
    // (including the runtime Y-flip). See FullscreenBlit_UVConvention.md.
    Varyings_Default base = DefaultPassVertex(input.vertexID);
    output.positionCS = base.positionCS_SS;
    output.texcoord   = base.screenUV;
    // Fill the 3 offset slots expected by the SMAA reference VS.
    SMAAEdgeDetectionVS(output.texcoord, output.offsets);
    return output;
}

float4 FragEdge(VaryingsEdge input) : SV_Target
{
    return float4(SMAAColorEdgeDetectionPS(input.texcoord, input.offsets, _SourceTexture), 0.0, 0.0);
}

// -----------------------------------------------------------------------------
// Pass 2: Blending Weight Calculation
// -----------------------------------------------------------------------------
struct VaryingsBlend
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    float2 pixcoord   : TEXCOORD1;
    float4 offsets[3] : TEXCOORD2;
};

VaryingsBlend VertBlend(Attributes input)
{
    VaryingsBlend output;
    Varyings_Default base = DefaultPassVertex(input.vertexID);
    output.positionCS = base.positionCS_SS;
    output.texcoord   = base.screenUV;
    SMAABlendingWeightCalculationVS(output.texcoord, output.pixcoord, output.offsets);
    return output;
}

float4 FragBlend(VaryingsBlend input) : SV_Target
{
    // subsampleIndices = 0 for SMAA 1x (no temporal/spatial supersampling).
    return SMAABlendingWeightCalculationPS(
        input.texcoord, input.pixcoord, input.offsets,
        _SourceTexture,          // edges texture, bound via BlitUtils as _SourceTexture in pass 2
        _SMAAAreaTexture,
        _SMAASearchTexture,
        float4(0.0, 0.0, 0.0, 0.0));
}

// -----------------------------------------------------------------------------
// Pass 3: Neighborhood Blending
// -----------------------------------------------------------------------------
struct VaryingsNeighbor
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    float4 offset     : TEXCOORD1;
};

VaryingsNeighbor VertNeighbor(Attributes input)
{
    VaryingsNeighbor output;
    Varyings_Default base = DefaultPassVertex(input.vertexID);
    output.positionCS = base.positionCS_SS;
    output.texcoord   = base.screenUV;
    SMAANeighborhoodBlendingVS(output.texcoord, output.offset);
    return output;
}

float4 FragNeighbor(VaryingsNeighbor input) : SV_Target
{
    return SMAANeighborhoodBlendingPS(input.texcoord, input.offset, _SourceTexture, _SMAABlendTexture);
}

#endif // ARCTOON_POSTPROCESSING_SMAA_BRIDGE_INCLUDED
