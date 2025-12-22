#ifndef ARCTOON_TOON_PUPIL_PASS_INCLUDED
#define ARCTOON_TOON_PUPIL_PASS_INCLUDED

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
    UNITY_VERTEX_INPUT_INSTANCE_ID
    GI_VARYINGS_DATA
};

Varyings ToonPupilPassVertex(Attributes input)
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
    output.UV1 = TransformUV1(input.UV1);
    return output;
}

float4 ToonPupilPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    // float2 baseUV = input.baseUV;
    // #if defined(_SPEC_MASK)
    // float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    // float parallaxOffsetV = - viewDirectionWS.y * 0.07 + 0;
    // baseUV.y += parallaxOffsetV;
    // float parallaxOffsetU = - viewDirectionWS.x * 0.07 + 0;
    // baseUV.x += parallaxOffsetU;
    // #endif
    
    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV.xy, input.UV1.xy);
    ClipLOD(config.fragment, unity_LODFade.x);

    Surface surface;
    ZERO_INITIALIZE(Surface, surface)
    surface.positionWS = input.positionWS;
    surface.UV = float4(input.baseUV.xy, input.UV1.xy);
    
    float faceSign = isFrontFace ? 1.0 : -1.0;
    float3x3 tangentToWorld = CreateTangentToWorld(input.normalWS, input.tangentWS.xyz, input.tangentWS.w);
    #if defined(_NORMAL_MAP)
    surface.normalWS = normalize(mul(GetNormalTS(config), tangentToWorld)) * faceSign;
    surface.interpolatedNormalWS = normalize(input.normalWS) * faceSign;
    #else
    surface.normalWS = normalize(input.normalWS) * faceSign;
    surface.interpolatedNormalWS = surface.normalWS * faceSign;
    #endif
    
    surface.normalVS = normalize(input.normalVS) * faceSign;
    surface.linearDepth = -TransformWorldToView(input.positionWS).z;
    surface.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);

    // TODO: UV post process
    float2 refractedUV = config.baseUV;
    #if defined(_EYE_REFRACTION)
    refractedUV = GetParallaxRefractionUV(config.baseUV, surface.viewDirectionWS, tangentToWorld);
    #endif
    
    float4 albedo = GetAlbedo(refractedUV);
    #if defined(_CLIPPING)
    clip(albedo.a - GetAlphaClip(config));
    #endif
    
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
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
    RimLightData rimLightData = GetRimLightData(GetRimLightScale(), GetRimLightWidth(), GetRimLightDepthBias());
    
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
    
    finalColor += GetEmission(config);

    float2 matCapUV = GetMatCapUV(config.baseUV, surface.normalVS);
    finalColor = PostProcessFinalColor(finalColor, matCapUV);

    return float4(finalColor, surface.alpha);
}

#endif

