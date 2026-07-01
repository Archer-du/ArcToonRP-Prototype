#ifndef ARCTOON_COMMON_INCLUDED
#define ARCTOON_COMMON_INCLUDED

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#include "Input/UnityInput.hlsl"

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
SAMPLER_CMP(sampler_linear_clamp_compare);

#include "Fragment.hlsl"
#include "ForwardPlus.hlsl"

#define COLOR_BLEND_LERP 0
#define COLOR_BLEND_MULTIPLY 1
#define COLOR_BLEND_ADD 2
#define COLOR_BLEND_OVERLAY 3
#define COLOR_BLEND_SCREEN 4
#define COLOR_BLEND_SOFT_LIGHT 5
#define COLOR_BLEND_HARD_LIGHT 6
#define COLOR_BLEND_COLOR_DODGE 7
#define COLOR_BLEND_COLOR_BURN 8
#define COLOR_BLEND_DARKEN 9
#define COLOR_BLEND_LIGHTEN 10
#define COLOR_BLEND_DIFFERENCE 11
#define COLOR_BLEND_EXCLUSION 12

// -----------------------------------------------------------------------------
// Fullscreen-triangle VS shared by every post-process / blit shader.
//
// Emits a single oversized triangle that conservatively covers the [-1, 1]^2
// clip-space viewport. DrawProcedural is expected to be called with
// MeshTopology.Triangles and a vertexCount of 3.
//
// Vertex layout (clip-space, before the Y-flip branch):
//
//     vertexID = 0 : positionCS = (-1, -1)   screenUV = (0, 0)   bottom-left
//     vertexID = 1 : positionCS = (-1,  3)   screenUV = (0, 2)   top-left   (overshoots)
//     vertexID = 2 : positionCS = ( 3, -1)   screenUV = (2, 0)   bottom-right (overshoots)
//
// Triangle edges: v0->v1 runs along x=-1 (screen's left edge), v0->v2 along
// y=-1 (screen's bottom edge); the hypotenuse v1->v2 sits fully outside the
// [-1, 1]^2 viewport. Only the portion inside [-1, 1]^2 (UV [0, 1]^2) reaches
// the framebuffer; pixels past the viewport are discarded by the rasterizer.
// This "oversized triangle" trick saves one vertex and one interior edge
// versus a two-triangle quad, and avoids the diagonal seam where both
// sub-triangles would otherwise touch the same pixels.
//
// The final `if (_ProjectionParams.x < 0.0)` branch is the project-wide
// runtime Y-flip used when Unity renders with a Y-flipped projection matrix
// (i.e. rendering to a RenderTexture on D3D / Metal / Vulkan). See
// ChatLogs/Document/FullscreenBlit_UVConvention.md for the full rationale and
// why we do NOT combine this with SRP core's GetFullScreenTriangle* helpers.
// -----------------------------------------------------------------------------
struct Varyings_Default
{
    float4 positionCS_SS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings_Default DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varyings_Default output;
    output.positionCS_SS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    output.screenUV = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );
    // Runtime Y-flip: Unity sets _ProjectionParams.x to -1 when it flips the
    // projection matrix's Y axis (render-to-RT on APIs with top-left UV
    // origin). Without this flip the blit output would appear upside-down.
    if (_ProjectionParams.x < 0.0)
    {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    return output;
}

// basic math helpers --------------------------
float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}

float FadedStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

float SigmoidSharp(float x, float center, float sharp)
{
    float s = 1.0 / (1.0 + pow(100000.0, (-3.0 * sharp * (x - center))));
    return s;
};

float SelectChannel(float4 value, int channel)
{
    return channel == 0 ? value.r : channel == 1 ? value.g : channel == 2 ? value.b : value.a;
}

// decoder helpers -------------------------------
float3 DecodeOctahedral(float2 uv)
{
    float3 n = float3(uv.x, uv.y, 1 - abs(uv.x) - abs(uv.y));

    if (n.z < 0)
    {
        n.xy = (1 - abs(n.yx)) * sign(n.xy);
    }

    return normalize(n);
}

float3 DecodeNormal(float4 sample, float scale = 1.0)
{
    #if defined(UNITY_NO_DXT5nm)
    return normalize(UnpackNormalRGB(sample, scale));
    #else
    return normalize(UnpackNormalmapRGorAG(sample, scale));
    #endif
}

float4 TransformObjectToWorldTangent(float4 tangentOS)
{
    return float4(TransformObjectToWorldDir(tangentOS.xyz), tangentOS.w);
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS, bool doNormalize = false)
{
    float3x3 tangentToWorld =
        CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld, doNormalize);
}

float GetTexelSizeWorldSpace(float linearDepth)
{
    float size = 2.0 * linearDepth / (_CameraBufferSize.z * GetViewToHClipMatrix()._m00);
    return size;
}

float3 GetObjectCenterWorldPosition()
{
    float3 objectCenterWS = mul(GetObjectToWorldMatrix(), float4(0, 0, 0, 1)).xyz;
    return objectCenterWS;
}

float GenerateSphereDistanceMaskByUV(float2 UV, float radiusSquare, float sharp)
{
    UV = mad(UV, 2, -1);
    float distanceSquare = dot(UV, UV);
    float sphereMask = 1 - SigmoidSharp(distanceSquare, radiusSquare, sharp);
    return sphereMask;
}

float3 GenerateSphereNormalByUV(float2 UV, float radiusSquare = 1.0, float2 scale = float2(1.0, 1.0))
{
    UV = mad(UV, 2, -1);
    UV *= scale;
    float distanceSquare = dot(UV, UV);
    float z = sqrt(max(0, radiusSquare - distanceSquare));
    float3 sphereNormalOS = normalize(float3(UV.x, UV.y, z));
    return sphereNormalOS;
}

float GetHalfLambertFactor(float3 normal, float3 lightDir)
{
    float NdotL = dot(normal, lightDir);
    return NdotL * 0.5 + 0.5;
}

// void poissonDiskSamples(const in float2 randomSeed, int sampleNum, out real disk)
// {
//     float angle = rand_2to1(randomSeed) * PI2;
//     float invSampleNum = 1.0 / float(sampleNum);
//     float radius = invSampleNum;
//     float angleStep = 3.883222077450933; // (sqrt(5)-1)/2 *2PI
//     float radiusStep = radius;
//
//     for (int i = 0; i < sampleNum; i++)
//     {
//         disk[i] = float2(cos(angle), sin(angle)) * pow(radius, 0.75);
//         radius += radiusStep;
//         angle += angleStep;
//     }
// }

// feature helpers --------------------------

void ClipFragmentDepthTest(float depth, float bufferDepth)
{
    #if UNITY_REVERSED_Z
    clip(depth - bufferDepth);
    #else
    clip(bufferDepth - depth);
    #endif
}

void ClipLOD(Fragment fragment, float fade)
{
    #if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(fragment.positionSS.xy, 0);;
    clip((fade < 0 ? fade + 1 : fade) - dither);
    #endif
}

float3 BlendColor(float3 color1, float3 color2, float alpha, int blendMode)
{
    alpha = saturate(alpha);
    float3 blendedColor = color1;
    switch (blendMode)
    {
        case COLOR_BLEND_LERP:
            blendedColor = lerp(color1, color2, alpha);
            break;
        case COLOR_BLEND_MULTIPLY:
            blendedColor = lerp(color1, color1 * color2, alpha);
            break;
        case COLOR_BLEND_ADD:
            blendedColor = lerp(color1, color1 + color2, alpha);
            break;
        case COLOR_BLEND_OVERLAY:
            {
                float3 overlay = lerp(
                    2.0 * color1 * color2,
                    1.0 - 2.0 * (1.0 - color1) * (1.0 - color2),
                    step(0.5, color1)
                );
                blendedColor = lerp(color1, overlay, alpha);
            }
            break;
        case COLOR_BLEND_SCREEN:
            {
                float3 screen = 1.0 - (1.0 - color1) * (1.0 - color2);
                blendedColor = lerp(color1, screen, alpha);
            }
            break;
        case COLOR_BLEND_SOFT_LIGHT:
            {
                float3 softLight = lerp(
                    2.0 * color1 * color2 + color1 * color1 * (1.0 - 2.0 * color2),
                    sqrt(color1) * (2.0 * color2 - 1.0) + 2.0 * color1 * (1.0 - color2),
                    step(0.5, color2)
                );
                blendedColor = lerp(color1, softLight, alpha);
            }
            break;
        case COLOR_BLEND_HARD_LIGHT:
            {
                float3 hardLight = lerp(
                    2.0 * color1 * color2,
                    1.0 - 2.0 * (1.0 - color1) * (1.0 - color2),
                    step(0.5, color2)
                );
                blendedColor = lerp(color1, hardLight, alpha);
            }
            break;
        case COLOR_BLEND_COLOR_DODGE:
            {
                float3 colorDodge = color1 / (1.0001 - color2);
                blendedColor = lerp(color1, colorDodge, alpha);
            }
            break;
        case COLOR_BLEND_COLOR_BURN:
            {
                float3 colorBurn = 1.0 - (1.0 - color1) / (color2 + 0.0001);
                blendedColor = lerp(color1, colorBurn, alpha);
            }
            break;
        case COLOR_BLEND_DARKEN:
            blendedColor = lerp(color1, min(color1, color2), alpha);
            break;
        case COLOR_BLEND_LIGHTEN:
            blendedColor = lerp(color1, max(color1, color2), alpha);
            break;
        case COLOR_BLEND_DIFFERENCE:
            blendedColor = lerp(color1, abs(color1 - color2), alpha);
            break;
        case COLOR_BLEND_EXCLUSION:
            blendedColor = lerp(color1, color1 + color2 - 2.0 * color1 * color2, alpha);
            break;
        default:
            break;
    }
    return saturate(blendedColor);
}

#endif
