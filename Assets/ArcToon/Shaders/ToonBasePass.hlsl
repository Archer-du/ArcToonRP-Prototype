#ifndef ARCTOON_TOON_BASE_PASS_INCLUDED
#define ARCTOON_TOON_BASE_PASS_INCLUDED

struct VaryingsBase
{
    float4 positionCS_SS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL_WS;
    float3 normalVS : VAR_NORMAL_VS;
    #if defined(_NORMAL_MAP)
    float4 tangentWS : VAR_TANGENT;
    #endif
    float2 baseUV : VAR_BASE_UV;
    float2 UV1 : VAR_UV1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

float3 SpecularStrength(Surface surface, BRDF brdf, Light light, DirectLightSpecData specData)
{
    float3 specularStrength = SpecularStrength(surface, brdf, light);
    #if defined(_SPEC_MASK)
    float2 specUV =
        #if defined(_SPEC_MASK_UV0)
        surface.UV.xy;
        #elif defined(_SPEC_MASK_UV1)
        surface.UV.zw;
        #else
        surface.UV.xy;
        #endif
    float slide = GetParallaxSensitivity();
    float offset = GetParallaxOffset();
    #if defined(_SPEC_PARALLAX)
    float parallaxOffsetV = - surface.viewDirectionWS.y * slide + offset;
    specUV.y += parallaxOffsetV;
    #endif
    float hairSpecMask = SampleParallaxSpecularMask(float2(specUV.x, specUV.y));
    specularStrength *= hairSpecMask;
    #endif
    return specularStrength;
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light, DirectLightSpecData specData)
{
    return SpecularStrength(surface, brdf, light, specData) * brdf.specular + brdf.diffuse;
}

float3 IncomingLight(Surface surface, Light light, DirectLightAttenData attenData)
{
    float halfLambertFactor = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    float attenuationUV = min(
        SigmoidSharp(halfLambertFactor, attenData.offset, attenData.smooth),
        SigmoidSharp(light.shadowAttenuation, attenData.offset, attenData.smooth)
    );
    #if defined(_RAMP_SET)
    float3 lightAttenuation = SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    #else
    float lightAttenuation = attenuationUV;
    #endif
    // return IncomingLight(surface, light);
    return lightAttenuation * light.distanceAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light,
    DirectLightAttenData attenData, DirectLightSpecData specData, RimLightData rimLightData)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light, attenData);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return (DirectBRDF(surface, brdf, light, specData) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, brdf, light, specData) * brdf.specular;
    #endif
    return IncomingLight(surface, light, attenData) *
        (DirectBRDF(surface, brdf, light, specData) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
}

VaryingsBase ToonBasePassVertex(Attributes input)
{
    VaryingsBase output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.normalVS = TransformWorldToViewNormal(output.normalWS);
    #if defined(_NORMAL_MAP)
    output.tangentWS = TransformObjectToWorldTangent(input.tangentOS);
    #endif
    output.baseUV = TransformBaseUV(input.baseUV);
    output.UV1 = TransformUV1(input.UV1);
    return output;
}

float4 ToonBasePassFragment(VaryingsBase input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    ClipLOD(config.fragment, unity_LODFade.x);

    float4 albedo = GetColor(config);
    #if defined(_CLIPPING)
    clip(albedo.a - GetAlphaClip(config));
    #endif

    Surface surface;
    ZERO_INITIALIZE(Surface, surface)
    surface.positionWS = input.positionWS;
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    surface.UV = float4(input.baseUV.xy, input.UV1.xy);

    float faceSign = isFrontFace ? 1.0 : -1.0;
    #if defined(_NORMAL_MAP)
    surface.normalWS = normalize(NormalTangentToWorld(GetNormalTS(config),
        input.normalWS, input.tangentWS)) * faceSign;
    surface.interpolatedNormalWS = normalize(input.normalWS) * faceSign;
    #else
    surface.normalWS = normalize(input.normalWS) * faceSign;
    surface.interpolatedNormalWS = surface.normalWS * faceSign;
    #endif
    surface.normalVS = normalize(input.normalVS) * faceSign;
    
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.metallic = GetMetallic(config);
    surface.roughness = PerceptualSmoothnessToRoughness(GetSmoothness(config));
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    surface.perObjectCasterID = GetPerObjectShadowCasterID();
    
    #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
    #else
    BRDF brdf = GetBRDF(surface);
    #endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    DirectLightAttenData attenData = GetDirectLightAttenData(INPUT_PROPS_DIRECT_ATTEN_PARAMS);
    DirectLightSpecData specData = GetDirectLightSpecData(INPUT_PROPS_DIRECT_SPEC_PARAMS);
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    RimLightData rimLightData = GetRimLightData(GetRimLightScale(), GetRimLightWidth(), GetRimLightDepthBias());

    float3 finalColor = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            finalColor += GetLighting(surface, config.fragment, brdf, light, attenData, specData, rimLightData);
        }
    }
    AccumulatePunctualLighting(config.fragment, surface, brdf, gi, cascadeShadowData, finalColor);
    
    finalColor += GetEmission(config);

    return float4(finalColor, surface.alpha);
}

#endif
