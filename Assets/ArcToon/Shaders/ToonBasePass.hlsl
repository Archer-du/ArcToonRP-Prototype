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
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

float3 SpecularStrength(Surface surface, BRDF brdf, Light light, DirectLightSpecData specData)
{
    #if defined(_RAMP_SET)
    // float SpecularFactor = SpecularStrength(surface, brdf, light);
    // float specularUV = SigmoidSharp(SpecularFactor, specData.offset + 2, specData.smooth);
    // float specularStrength = 10.0 * SampleRampSetChannel(specularUV, RAMP_DIRECT_LIGHTING_SPECULAR_CHANNEL);
    // return specularStrength;
    return SpecularStrength(surface, brdf, light);
    #else
    return SpecularStrength(surface, brdf, light);
    #endif
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light, DirectLightSpecData specData)
{
    return SpecularStrength(surface, brdf, light, specData) * brdf.specular + brdf.diffuse;
}

float3 IncomingLight(Surface surface, Light light, DirectLightAttenData attenData)
{
    float3 lightAttenuation = 0.0f;
    #if defined(_RAMP_SET)
    float halfLambertFactor = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    float attenuationUV = min(
        SigmoidSharp(halfLambertFactor, attenData.offset, attenData.smooth),
        SigmoidSharp(light.shadowAttenuation, attenData.offset, attenData.smooth)
    );
    lightAttenuation = SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    // lightAttenuation = attenuationUV;
    return lightAttenuation * light.distanceAttenuation * light.color * surface.occlusion;
    #else
    return IncomingLight(surface, light);
    #endif
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
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    #endif
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 ToonBasePassFragment(VaryingsBase input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    ClipLOD(config.fragment, unity_LODFade.x);

    float4 albedo = GetColor(config);
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif

    Surface surface;
    ZERO_INITIALIZE(Surface, surface)
    surface.positionWS = input.positionWS;
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    #if defined(_NORMAL_MAP)
    surface.normalWS = normalize(NormalTangentToWorld(GetNormalTS(config),
        input.normalWS, input.tangentWS));
    surface.interpolatedNormalWS = normalize(input.normalWS);
    #else
    surface.normalWS = normalize(input.normalWS);
    surface.interpolatedNormalWS = surface.normalWS;
    #endif
    surface.normalVS = normalize(input.normalVS);
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.metallic = GetMetallic(config);
    surface.roughness = PerceptualSmoothnessToRoughness(GetSmoothness(config));
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    surface.perObjectCasterID = GetPerObjectShadowCasterID();
    
    BRDF brdf = GetBRDF(surface);
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

    return float4(finalColor, GetFinalAlpha(config));
}

#endif
