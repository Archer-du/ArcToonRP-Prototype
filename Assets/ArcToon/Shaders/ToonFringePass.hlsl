#ifndef ARCTOON_TOON_HAIR_PASS_INCLUDED
#define ARCTOON_TOON_HAIR_PASS_INCLUDED

struct VaryingsHair
{
    float4 positionCS_SS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL_WS;
    float3 normalVS : VAR_NORMAL_VS;
    float4 tangentWS : VAR_TANGENT;
    float3 bitangentWS : VAR_BITANGENT;
    float2 baseUV : VAR_BASE_UV;
    float2 UV1 : VAR_UV1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

// overrides
float3 SpecularStrength(Surface surface, BRDF brdf, Light light, HairSpecData hairSpecData)
{
    float3 specularStrength;
    #if defined(_OVERRIDE_HIGHLIGHT)
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
        #if defined(_TANGENT_SHIFT_MAP)
        float2 hairUV =
            #if defined(_TANGENT_SHIFT_MAP_UV0)
            hairSpecData.UVData.xy;
            #elif defined(_TANGENT_SHIFT_MAP_UV1)
            hairSpecData.UVData.zw;
            #else
            hairSpecData.UVData.xy;
            #endif
        float shiftScale = SampleTangentShiftNoise(hairUV) + GetTangentShiftOffset();
        float3 bitangentWS = SafeNormalize(hairSpecData.bitangentWS + shiftScale * surface.normalWS);
        float dotTH = dot(bitangentWS, h);
        // avoid sqrt crashes caused by floating point precision
        float cosTH = saturate(dotTH);
        float sinTH = sqrt(saturate(1.0 - cosTH * cosTH));
        float dirAttenuation = smoothstep(-1.0, 0.0, dotTH);
        specularStrength = dirAttenuation * pow(sinTH, GetSpecGloss()) * GetSpecScale();
        #else
        float dotNH = saturate(dot(surface.normalWS, h));
        specularStrength = GetSpecScale() * pow(dotNH, GetSpecGloss());
        #endif
    #else
    specularStrength = SpecularStrength(surface, brdf, light);
    #endif
    
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

float3 DirectBRDF(Surface surface, BRDF brdf, Light light, HairSpecData hairSpecData)
{
    return SpecularStrength(surface, brdf, light, hairSpecData) * brdf.specular + brdf.diffuse;
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

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light, DirectLightAttenData attenData,
                   RimLightData rimLightData, HairSpecData hairSpecData)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light, attenData);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return (DirectBRDF(surface, brdf, light, hairSpecData) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, light, hairSpecData) * brdf.specular;
    #endif
    return IncomingLight(surface, light, attenData) *
        (DirectBRDF(surface, brdf, light, hairSpecData) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
}

VaryingsHair ToonFringePassVertex(Attributes input)
{
    VaryingsHair output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.normalVS = TransformWorldToViewNormal(output.normalWS);
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.bitangentWS = cross(output.normalWS, output.tangentWS.xyz) * sign;
    output.baseUV = TransformBaseUV(input.baseUV);
    output.UV1 = TransformUV1(input.UV1);
    return output;
}

float4 ToonFringePassFragment(VaryingsHair input) : SV_TARGET
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
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    HairSpecData hairSpecData = GetHairSpecData(input.baseUV, input.UV1, input.bitangentWS);
    RimLightData rimLightData = GetRimLightData(GetRimLightScale(), GetRimLightWidth(), GetRimLightDepthBias());

    float3 finalColor = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);

    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            finalColor += GetLighting(surface, config.fragment, brdf, light, attenData, rimLightData, hairSpecData);
        }
    }
    AccumulatePunctualLighting(config.fragment, surface, brdf, gi, cascadeShadowData, finalColor);

    finalColor += GetEmission(config);

    return float4(finalColor, lerp(surface.alpha, GetFringeTransparentScale(), config.fragment.stencilMask.STENCIL_MASK_CHANNEL_EYE_LASHES));
}

#endif
