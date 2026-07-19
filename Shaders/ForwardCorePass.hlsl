#ifndef ARCTOON_FORWARD_CORE_PASS_INCLUDED
#define ARCTOON_FORWARD_CORE_PASS_INCLUDED

// Shared vertex/fragment skeleton for the forward-lit pass of the core Toon shaders.
// The skeleton is fixed. Per-shader-type variations that map to a shader keyword
// (_NORMAL_MAP / _TANGENT_SHIFT_MAP / _EYE_REFRACTION / _MATCAP / _FRINGE_TRANSPARENT) are
// compiled in place via #if guards rather than indirected through hooks. This file includes no
// lighting Impl on purpose, so the Impl stays selectable by include order.

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
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
    float4 tangentWS : VAR_TANGENT;
    float2 baseUV : VAR_BASE_UV;
    float2 UV1 : VAR_UV1;
    float4 vertexColor : VAR_VERTEX_COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

Varyings ForwardCoreVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.normalVS = TransformWorldToViewNormal(output.normalWS);
    output.tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.UV1 = input.UV1;
    output.vertexColor = input.vertexColor;
    return output;
}

float3 ToonComputeLighting(Surface surface, InputConfig config, BRDF brdf, GI gi)
{
    DirectLightAttenData attenData = GetDirectLightAttenData(INPUT_PROP(_DirectLightAttenOffset), INPUT_PROP(_DirectLightAttenSmoothNew));
    RimLightData rimLightData = GetRimLightData(GetRimLightScale(), GetRimLightWidth(), GetRimLightDepthBias());
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);

    float3 finalColor = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            finalColor += GetLighting(surface, config.fragment, brdf, light, attenData, rimLightData);
        }
    }
    AccumulatePunctualLighting(config.fragment, surface, brdf, gi, cascadeShadowData, finalColor);
    return finalColor;
}

float4 ForwardCoreFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GET_INPUT_CONFIG_WITH_REGION(input.positionCS_SS, input.baseUV.xy, input.vertexColor);
    ClipLOD(config.fragment, unity_LODFade.x);

    Surface surface;
    ZERO_INITIALIZE(Surface, surface)
    surface.regionIndex = config.regionIndex;
    surface.positionWS = input.positionWS;
    surface.UV = float4(input.baseUV.xy, input.UV1.xy);

    float faceSign = isFrontFace ? 1.0 : -1.0;
    #if defined(_NORMAL_MAP) || defined(_EYE_REFRACTION)
    float3x3 tangentToWorld = CreateTangentToWorld(input.normalWS, input.tangentWS.xyz, input.tangentWS.w);
    #endif
    #if defined(_NORMAL_MAP)
    surface.normalWS = normalize(mul(GetNormalTS(config), tangentToWorld)) * faceSign;
    surface.interpolatedNormalWS = normalize(input.normalWS) * faceSign;
    #else
    surface.normalWS = normalize(input.normalWS) * faceSign;
    surface.interpolatedNormalWS = surface.normalWS * faceSign;
    #endif
    #if defined(_TANGENT_SHIFT_MAP)
        #if defined(_NORMAL_MAP)
        surface.bitangentWS = tangentToWorld[2];
        #else
        float bitangentSign = input.tangentWS.w * GetOddNegativeScale();
        surface.bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * bitangentSign;
        #endif
    #endif
    surface.normalVS = normalize(input.normalVS) * faceSign;
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);

    surface.metallic = GetMetallic(config);
    surface.roughness = GetRoughness(config);
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    surface.perObjectCasterID = GetPerObjectShadowCasterID();

    #if defined(_EYE_REFRACTION)
    float4 albedo = GetAlbedo(GetParallaxRefractionUV(config.baseUV, surface.viewDirectionWS, tangentToWorld));
    #else
    float4 albedo = GetAlbedo(config);
    #endif
    #if defined(_CLIPPING)
    clip(albedo.a - GetAlphaClip(config));
    #endif
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;

    #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
    #else
    BRDF brdf = GetBRDF(surface);
    #endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);

    float3 finalColor = ToonComputeLighting(surface, config, brdf, gi);
    finalColor += GetEmission(config);

    #if defined(_MATCAP)
    float3 matCapColor = GetMatCap(GetMatCapUV(config.baseUV, surface.normalVS));
    finalColor = BlendColor(finalColor, matCapColor, INPUT_PROP(_MatCapStrength), INPUT_PROP(_MatCapBlendMode));
    #endif

    float outputAlpha = surface.alpha;
    #if defined(_FRINGE_TRANSPARENT)
    // where eyelashes are stencil-masked, blend alpha toward the fringe transparent value so the
    // fringe reads as translucent over the eyes.
    outputAlpha = lerp(surface.alpha, GetFringeTransparentScale(),
        config.fragment.stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES);
    #endif

    return float4(finalColor, outputAlpha);
}

#endif
