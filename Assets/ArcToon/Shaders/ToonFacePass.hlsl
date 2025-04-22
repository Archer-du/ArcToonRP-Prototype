#ifndef ARCTOON_TOON_FACE_PASS_INCLUDED
#define ARCTOON_TOON_FACE_PASS_INCLUDED

#include "../ShaderLibrary/Light/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_ATTRIBUTES_DATA
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
    float2 faceUV : VAR_FACE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

// overrides
float3 SpecularStrength(Light light,
    FaceData faceData)
{
    float3 faceDirHWS = SafeNormalize(float3(faceData.directionWS.x, 0.0, faceData.directionWS.z));
    float3 lightDirHWS = SafeNormalize(float3(light.directionWS.x, 0.0, light.directionWS.z));
    float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - faceData.positionWS);
    float3 viewDirHWS = SafeNormalize(float3(viewDirWS.x, 0.0, viewDirWS.z));
    float3 halfVecHWS = SafeNormalize(viewDirHWS + lightDirHWS);
    float HdotN = dot(halfVecHWS, faceDirHWS);
    float clipCenter = clamp(-1.7071 * 1.5 * (HdotN - 1.0), 0.001, 0.999);
    float flipSign = cross(halfVecHWS, faceDirHWS).y;
    float2 faceUV = faceData.faceUV;
    if (flipSign > 0.0f)
    {
        faceUV.x = 1 - faceUV.x;
    }
    float specFactorNoseSDF1 = SampleSDFLightMapNoseSpecular1(faceUV);
    float specFactorNoseSDF2 = SampleSDFLightMapNoseSpecular2(faceUV);
    float specularUV =
        SigmoidSharp(specFactorNoseSDF1, clipCenter, GetNoseSpecularSmooth()) *
        SigmoidSharp(specFactorNoseSDF2, 1 - clipCenter, GetNoseSpecularSmooth());
    float specularStrength = specularUV;
    // TODO: config
    if (HdotN < 0.6095) specularStrength = lerp(specularStrength, 0, saturate((0.6095 - HdotN) * 20));
    return specularStrength * GetNoseSpecularStrength();
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light, FaceData faceData)
{
    return SpecularStrength(light, faceData) * brdf.specular + brdf.diffuse;
}

float3 IncomingLight(Surface surface, Light light,
    DirectLightAttenData attenData, FaceData faceData)
{
    float3 lightAttenuation = 0.0f;
    
    #if defined(_RAMP_SET)
    float attenuationUV = 0.0;
    #if defined(_SDF_LIGHT_MAP)
    float3 faceDirHWS = SafeNormalize(float3(faceData.directionWS.x, 0.0, faceData.directionWS.z));
    float3 lightDirHWS = SafeNormalize(float3(light.directionWS.x, 0.0, light.directionWS.z));
    float FdotL = dot(faceDirHWS, lightDirHWS);
    float clipCenter = - FdotL * 0.5 + 0.5 + GetSDFShadowOffset();
    float flipSign = cross(faceDirHWS, lightDirHWS).y;
    float2 faceUV = faceData.faceUV;
    if (flipSign > 0.0f)
    {
        faceUV.x = 1 - faceUV.x;
    }
    float attenFactorSDF = SampleSDFLightMap(faceUV);
    float shadowMaskFactorSDF = SampleSDFLightMapShadowMask(faceUV);
    attenuationUV = min(
        SigmoidSharp(attenFactorSDF, clipCenter, attenData.smooth),
        SigmoidSharp(shadowMaskFactorSDF, attenData.offset, attenData.smooth)
    );
    #else
    float halfLambertFactor = GetHalfLambertFactor(surface.normalWS, light.directionWS);
    attenuationUV = min(
        SigmoidSharp(halfLambertFactor, attenData.offset, attenData.smooth),
        SigmoidSharp(light.shadowAttenuation, attenData.offset, attenData.smooth)
    );
    #endif
    lightAttenuation = SampleRampSetChannel(attenuationUV, RAMP_DIRECT_LIGHTING_SHADOW_CHANNEL);
    return lightAttenuation * light.distanceAttenuation * light.color * surface.occlusion;
    #else
    return IncomingLight(surface, light);
    #endif
}

float3 GetLighting(Surface surface, BRDF brdf, Light light,
    DirectLightAttenData attenData, FaceData faceData)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light, attenData, faceData);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light, faceData);
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, brdf, light, attenData, faceData) * brdf.specular;
    #endif
    return IncomingLight(surface, light, attenData, faceData) * DirectBRDF(surface, brdf, light, faceData);
}

Varyings ToonFacePassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    output.faceUV = TransformFaceUV(input.baseUV);
    return output;
}

float4 ToonFacePassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    
    ClipLOD(config.fragment, unity_LODFade.x);
    
    float4 albedo = GetColor(config);
    #if defined(_CLIPPING)
    clip(color.a - GetAlphaClip(config));
    #endif

    Surface surface;
    surface.positionWS = input.positionWS;
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    surface.normalWS = normalize(input.normalWS);
    surface.interpolatedNormalWS = surface.normalWS;
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.metallic = GetMetallic(config);
    surface.roughness = PerceptualSmoothnessToRoughness(GetSmoothness(config));
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);

    BRDF brdf = GetBRDF(surface);
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    DirectLightAttenData attenData = GetDirectLightAttenData(INPUT_PROPS_DIRECT_ATTEN_PARAMS);
    FaceData faceData = GetFaceData(input.faceUV);
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    
    float3 finalColor = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);

    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        finalColor += GetLighting(surface, brdf, light, attenData, faceData);
    }

    AccumulatePunctualLighting(config.fragment, surface, brdf, gi, cascadeShadowData, finalColor);

    finalColor += GetEmission(config);

    return float4(finalColor, GetFinalAlpha(surface.alpha));
}

#endif
