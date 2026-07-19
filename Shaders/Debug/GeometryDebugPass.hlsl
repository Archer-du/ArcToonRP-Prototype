#ifndef ARCTOON_GEOMETRY_DEBUG_PASS_INCLUDED
#define ARCTOON_GEOMETRY_DEBUG_PASS_INCLUDED

// Shared geometry-debug pass for all Toon shaders.
// Relies on the shader's HLSLINCLUDE providing the surface / region / toon-lighting interfaces
// (SurfaceInterface, RegionInterface, ToonLightingInterface) plus the ToonLighting math library.
// Selected by the global _GeometryDebugMode uniform (see ArcToon.Utils.CameraDebugger).
// Surface is built at minimal fidelity: material-detail features (normal map, SDF, anisotropic
// hair highlight, refraction, matcap) are intentionally not reconstructed here.

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

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    float2 UV1 : TEXCOORD1;
    float4 vertexColor : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL_WS;
    float3 normalVS : VAR_NORMAL_VS;
    float2 baseUV : VAR_BASE_UV;
    float2 UV1 : VAR_UV1;
    float4 vertexColor : VAR_VERTEX_COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

Varyings GeometryDebugPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.normalVS = TransformWorldToViewNormal(output.normalWS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.UV1 = input.UV1;
    output.vertexColor = input.vertexColor;
    return output;
}

float4 GeometryDebugPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV.xy, input.vertexColor);
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
        // return float4(config.regionIndex.xxx, 1.0);
        // return input.vertexColor.g < 0.003 ? float4(1, 0, 0, 1) : float4(0, 0, 0, 1);
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
    DirectLightAttenData attenData = GetDirectLightAttenData(INPUT_PROP(_DirectLightAttenOffset), INPUT_PROP(_DirectLightAttenSmoothNew));
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
        color = IncomingLight(surface, config.fragment, light, attenData);
    }
    return float4(color, 1.0);
}

#endif
