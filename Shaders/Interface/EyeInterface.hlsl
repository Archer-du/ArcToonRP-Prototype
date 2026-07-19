#ifndef ARCTOON_EYE_INTERFACE_INCLUDED
#define ARCTOON_EYE_INTERFACE_INCLUDED

// Interface tier: eye parallax refraction + matcap getters (pupil shader).
// Reads the per-material CBUFFER; include after the shader's CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP), Common.hlsl (sphere/tangent helpers).

TEXTURE2D(_MatCap); SAMPLER(sampler_MatCap);

float2 GetParallaxRefractionUV(float2 baseUV, float3 viewDirectionWS, float3x3 tangentToWorld)
{
    float mask = GenerateSphereDistanceMaskByUV(baseUV, INPUT_PROP(_RefractionEdge), INPUT_PROP(_RefractionSmooth));
    float3 viewDirectionTS = TransformWorldToTangentDir(viewDirectionWS, tangentToWorld, true);
    viewDirectionTS *= INPUT_PROP(_AnteriorChamberHeight);
    viewDirectionTS.x *= INPUT_PROP(_ParallaxFlipSignX);
    viewDirectionTS.y *= INPUT_PROP(_ParallaxFlipSignY);
    float2 offsetUV = baseUV - viewDirectionTS.xy;
    return lerp(baseUV, offsetUV, mask);
}

float4 GetMatCap(float2 UV)
{
    return SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, UV);
}

float2 GetMatCapUV(float2 baseUV, float3 normalVS)
{
    float3 matCapNormalVS = normalVS;
    // #if defined(_MATCAP_SPH_NORMAL)
    // TODO: config
    // TODO: use sphere normal?
    float radiusSquare = 1;
    float3 sphereNormalOS = GenerateSphereNormalByUV(baseUV, radiusSquare, float2(1, 0.8));
    float3 sphereNormalVS = TransformWorldToViewNormal(TransformObjectToWorldNormal(sphereNormalOS, true), true);
    matCapNormalVS = sphereNormalVS;
    // #endif

    float2 matCapUV = mad(matCapNormalVS.xy, 0.5, 0.5);
    return matCapUV;
}

#endif
