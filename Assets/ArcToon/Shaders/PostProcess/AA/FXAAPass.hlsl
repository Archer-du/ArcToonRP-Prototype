#ifndef ARCTOON_FXAA_PASS_INCLUDED
#define ARCTOON_FXAA_PASS_INCLUDED

#include "PostFXStackInput.hlsl"

#if defined(FXAA_QUALITY_LOW)
    #define EXTRA_EDGE_STEPS 3
    #define EDGE_STEP_SIZES 1.5, 2.0, 2.0
    #define LAST_EDGE_STEP_GUESS 8.0
#elif defined(FXAA_QUALITY_MEDIUM)
    #define EXTRA_EDGE_STEPS 8
    #define EDGE_STEP_SIZES 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#else
    #define EXTRA_EDGE_STEPS 10
    #define EDGE_STEP_SIZES 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#endif

float4 _FXAAParams;

static const float edgeStepSizes[EXTRA_EDGE_STEPS] = { EDGE_STEP_SIZES };

struct LumaNeighborhood
{
    float m, n, e, s, w, ne, se, sw, nw;
    float highest, lowest;
    float range;
};

struct FXAAEdge
{
    bool isHorizontal;
    float pixelStep;
    float lumaGradient, otherLuma;
};

float SampleSourceLuminance(float2 screenUV, float uOffset = 0.0, float vOffset = 0.0)
{
    screenUV += float2(uOffset, vOffset) * GetSourceTexelSize().xy;
    #if defined(FXAA_ALPHA_CONTAINS_LUMA)
    return SampleSource(screenUV).a;
    #else
    return SampleSource(screenUV).g;
    #endif
}

bool CanSkipFXAA(LumaNeighborhood luma)
{
    return luma.range < max(_FXAAParams.x, _FXAAParams.y * luma.highest);
}

float GetSubpixelBlendFactor(LumaNeighborhood neighbor)
{
    float factor = neighbor.n + neighbor.e + neighbor.s + neighbor.w;
    factor *= 1.0 / 4;
    factor = abs(factor - neighbor.m);
    factor = factor / neighbor.range;
    factor = smoothstep(0, 1, factor);
    return factor * factor * _FXAAParams.z;
}

float GetEdgeBlendFactor(LumaNeighborhood luma, FXAAEdge edge, float2 uv)
{
    float2 edgeUV = uv;
    float2 uvStep = 0.0;
    if (edge.isHorizontal)
    {
        edgeUV.y += 0.5 * edge.pixelStep;
        uvStep.x = GetSourceTexelSize().x;
    }
    else
    {
        edgeUV.x += 0.5 * edge.pixelStep;
        uvStep.y = GetSourceTexelSize().y;
    }
    float edgeLuma = 0.5 * (luma.m + edge.otherLuma);
    float gradientThreshold = 0.25 * edge.lumaGradient;

    int i;
    float2 uvP = edgeUV + uvStep;
    float lumaDeltaP = SampleSourceLuminance(uvP) - edgeLuma;
    bool atEndP = abs(lumaDeltaP) >= gradientThreshold;
    UNITY_UNROLL
    for (i = 0; i < EXTRA_EDGE_STEPS && !atEndP; i++)
    {
        uvP += uvStep * edgeStepSizes[i];
        lumaDeltaP = SampleSourceLuminance(uvP) - edgeLuma;
        atEndP = abs(lumaDeltaP) >= gradientThreshold;
    }
    if (!atEndP)
    {
        uvP += uvStep * LAST_EDGE_STEP_GUESS;
    }

    float2 uvN = edgeUV - uvStep;
    float lumaDeltaN = SampleSourceLuminance(uvN) - edgeLuma;
    bool atEndN = abs(lumaDeltaN) >= gradientThreshold;
    UNITY_UNROLL
    for (i = 0; i < EXTRA_EDGE_STEPS && !atEndN; i++)
    {
        uvN -= uvStep * edgeStepSizes[i];
        lumaDeltaN = SampleSourceLuminance(uvN) - edgeLuma;
        atEndN = abs(lumaDeltaN) >= gradientThreshold;
    }
    if (!atEndN)
    {
        uvN -= uvStep * LAST_EDGE_STEP_GUESS;
    }
    
    float distanceToEndP, distanceToEndN;
    if (edge.isHorizontal)
    {
        distanceToEndP = uvP.x - uv.x;
        distanceToEndN = uv.x - uvN.x;
    }
    else
    {
        distanceToEndP = uvP.y - uv.y;
        distanceToEndN = uv.y - uvN.y;
    }
    float distanceToNearestEnd;
    bool deltaSign;
    if (distanceToEndP <= distanceToEndN)
    {
        distanceToNearestEnd = distanceToEndP;
        deltaSign = lumaDeltaP >= 0;
    }
    else
    {
        distanceToNearestEnd = distanceToEndN;
        deltaSign = lumaDeltaN >= 0;
    }

    if (deltaSign == (luma.m - edgeLuma >= 0))
    {
        return 0.0;
    }
    else
    {
        return 0.5 - distanceToNearestEnd / (distanceToEndP + distanceToEndN);
    }
}

bool IsHorizontalEdge(LumaNeighborhood luma)
{
    float horizontal =
        2.0 * abs(luma.n + luma.s - 2.0 * luma.m) +
        abs(luma.ne + luma.se - 2.0 * luma.e) +
        abs(luma.nw + luma.sw - 2.0 * luma.w);
    float vertical =
        2.0 * abs(luma.e + luma.w - 2.0 * luma.m) +
        abs(luma.ne + luma.nw - 2.0 * luma.n) +
        abs(luma.se + luma.sw - 2.0 * luma.s);
    return horizontal >= vertical;
}

FXAAEdge GetFXAAEdge(LumaNeighborhood luma)
{
    FXAAEdge edge;
    edge.isHorizontal = IsHorizontalEdge(luma);
    float lumaP, lumaN;
    if (edge.isHorizontal)
    {
        edge.pixelStep = GetSourceTexelSize().y;
        lumaP = luma.n;
        lumaN = luma.s;
    }
    else
    {
        edge.pixelStep = GetSourceTexelSize().x;
        lumaP = luma.e;
        lumaN = luma.w;
    }
    float gradientP = abs(lumaP - luma.m);
    float gradientN = abs(lumaN - luma.m);

    if (gradientP < gradientN)
    {
        edge.pixelStep = -edge.pixelStep;
        edge.lumaGradient = gradientN;
        edge.otherLuma = lumaN;
    }
    else
    {
        edge.lumaGradient = gradientP;
        edge.otherLuma = lumaP;
    }
    return edge;
}

LumaNeighborhood GetLumaNeighborhood(float2 screenUV)
{
    LumaNeighborhood neighbor;
    neighbor.m = SampleSourceLuminance(screenUV);
    neighbor.n = SampleSourceLuminance(screenUV, 0.0, 1.0);
    neighbor.e = SampleSourceLuminance(screenUV, 1.0, 0.0);
    neighbor.s = SampleSourceLuminance(screenUV, 0.0, -1.0);
    neighbor.w = SampleSourceLuminance(screenUV, -1.0, 0.0);
    neighbor.ne = SampleSourceLuminance(screenUV, 1.0, 1.0);
    neighbor.se = SampleSourceLuminance(screenUV, 1.0, -1.0);
    neighbor.sw = SampleSourceLuminance(screenUV, -1.0, -1.0);
    neighbor.nw = SampleSourceLuminance(screenUV, -1.0, 1.0);
    neighbor.highest = max(max(max(max(neighbor.m, neighbor.n), neighbor.e), neighbor.s), neighbor.w);
    neighbor.lowest = min(min(min(min(neighbor.m, neighbor.n), neighbor.e), neighbor.s), neighbor.w);
    neighbor.range = neighbor.highest - neighbor.lowest;
    return neighbor;
}

float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    LumaNeighborhood luma = GetLumaNeighborhood(input.screenUV);

    if (CanSkipFXAA(luma))
    {
        return SampleSource(input.screenUV);
    }

    FXAAEdge edge = GetFXAAEdge(luma);

    float blendFactor = max(
        GetSubpixelBlendFactor(luma), GetEdgeBlendFactor(luma, edge, input.screenUV)
    );
    float2 blendUV = input.screenUV;
    if (edge.isHorizontal)
    {
        blendUV.y += blendFactor * edge.pixelStep;
    }
    else
    {
        blendUV.x += blendFactor * edge.pixelStep;
    }
    return SampleSource(blendUV);
}

#endif
