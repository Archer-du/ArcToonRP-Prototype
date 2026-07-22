#ifndef ARCTOON_GEOMETRY_DEBUG_PASS_INCLUDED
#define ARCTOON_GEOMETRY_DEBUG_PASS_INCLUDED

// Shared geometry-debug pass for all Toon shaders.
// Reuses ForwardCorePass's Attributes / Varyings / ForwardCoreVertex so the debug view stays
// structurally aligned with the forward pass as more debug modes are added. Only the fragment
// differs: it selects a visualization via the global _GeometryDebugMode uniform
// (see ArcToon.Utils.CameraDebugger). Surface is built at minimal fidelity: material-detail
// features (normal map, SDF, anisotropic hair highlight, refraction, matcap) are intentionally
// not reconstructed here.
//
// Requires: ForwardCorePass.hlsl (provides Attributes/Varyings/ForwardCoreVertex) plus the
// shader's HLSLINCLUDE providing the surface / toon-lighting interfaces and the RegionID +
// ToonLighting math libraries.

#include "Packages/com.arctoon.render-pipeline/Shaders/ForwardCorePass.hlsl"

#define GEOMETRY_DEBUG_MODE_VERTEX_COLOR_RGB 1
#define GEOMETRY_DEBUG_MODE_VERTEX_COLOR_R   2
#define GEOMETRY_DEBUG_MODE_VERTEX_COLOR_G   3
#define GEOMETRY_DEBUG_MODE_VERTEX_COLOR_B   4
#define GEOMETRY_DEBUG_MODE_VERTEX_COLOR_A   5
#define GEOMETRY_DEBUG_MODE_REGION_ID        6
#define GEOMETRY_DEBUG_MODE_SPECULAR         7
#define GEOMETRY_DEBUG_MODE_DIRECT_BRDF      8
#define GEOMETRY_DEBUG_MODE_INCOMING_LIGHT   9

int _GeometryDebugMode;

float4 GeometryDebugPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GET_INPUT_CONFIG_WITH_REGION(input.positionCS_SS, input.baseUV.xy, input.vertexColor);
    ClipLOD(config.fragment, unity_LODFade.x);

    float4 albedo = GetAlbedo(config);
    #if defined(_CLIPPING)
    clip(albedo.a - GetAlphaClip(config));
    #endif

    // Vertex color modes need no shading context.
    if (_GeometryDebugMode <= GEOMETRY_DEBUG_MODE_VERTEX_COLOR_A)
    {
        float4 vertexColor = input.vertexColor;
        if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_VERTEX_COLOR_R) return float4(vertexColor.rrr, 1.0);
        if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_VERTEX_COLOR_G) return float4(vertexColor.ggg, 1.0);
        if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_VERTEX_COLOR_B) return float4(vertexColor.bbb, 1.0);
        if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_VERTEX_COLOR_A) return float4(vertexColor.aaa, 1.0);
        return float4(vertexColor.rgb, 1.0);
    }

    if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_REGION_ID)
    {
        #if !defined(_REGION_ID_TEXTURE) && !defined(_REGION_ID_VERTEX_COLOR)
        return float4(0, 0, 0, 1);
        #endif
        return float4(GetRegionDebugColor(config.regionIndex), 1.0);
    }

    // Lighting-term modes: build a minimal surface and reuse the shipping lighting functions.
    Surface surface;
    ZERO_INITIALIZE(Surface, surface)
    surface.positionWS = input.positionWS;
    surface.UV = float4(input.baseUV.xy, input.UV1.xy);

    float faceSign = isFrontFace ? 1.0 : -1.0;
    surface.normalWS = normalize(input.normalWS) * faceSign;
    surface.interpolatedNormalWS = surface.normalWS;
    surface.normalVS = normalize(input.normalVS) * faceSign;
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);

    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    surface.metallic = GetMetallic(config);
    surface.roughness = GetRoughness(config);
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    surface.perObjectCasterID = GetPerObjectShadowCasterID();

    BRDF brdf = GetBRDF(surface);
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    RimLightData rimLightData = GetRimLightData(GetRimLightScale(), GetRimLightWidth(), GetRimLightDepthBias());
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);

    // Only the main directional light drives the term visualization.
    Light light = GetDirectionalLight(0, surface, cascadeShadowData, gi);

    float3 color = 0.0;
    if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_SPECULAR)
    {
        color = ToonSpecularStrength(surface, brdf, light) * brdf.specular;
    }
    else if (_GeometryDebugMode == GEOMETRY_DEBUG_MODE_DIRECT_BRDF)
    {
        color = ToonDirectBRDF(surface, brdf, light) +
            ScreenSpaceRimLight(config.fragment, surface, light, rimLightData);
    }
    else // GEOMETRY_DEBUG_MODE_INCOMING_LIGHT
    {
        color = IncomingLight(surface, config.fragment, light);
    }
    return float4(color, 1.0);
}

#endif
