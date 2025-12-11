#ifndef ARCTOON_TOON_LIGHTING_IMPL_INCLUDED
#define ARCTOON_TOON_LIGHTING_IMPL_INCLUDED

#include "../ShaderLibrary/Ramp.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Light/DirectionalLight.hlsl"
#include "../ShaderLibrary/Light/SpotLight.hlsl"
#include "../ShaderLibrary/Light/PointLight.hlsl"
#include "../ShaderLibrary/GI.hlsl"

float3 GetMainLightDirection()
{
    if (_DirectionalLightCount <= 0) return 1.0;
    DirectionalLightBufferData bufferData = _DirectionalLightData[0];
    return bufferData.direction.xyz;
}

bool RenderingLayersOverlap(Surface surface, Light light)
{
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float MinimalCookTorranceSpecularTerm(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
    float nh2 = Square(saturate(dot(surface.normalWS, h)));
    float lh2 = Square(saturate(dot(light.directionWS, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 ToonSpecularStrength(Surface surface, BRDF brdf, Light light)
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
        float3 bitangentWS = SafeNormalize(surface.bitangentWS + shiftScale * surface.normalWS);
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
    specularStrength = MinimalCookTorranceSpecularTerm(surface, brdf, light);
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

float3 ToonDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return ToonSpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnelStrength = surface.fresnelStrength *
        Pow4(1.0 - saturate(dot(surface.normalWS, surface.viewDirectionWS)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return (diffuse * brdf.diffuse + reflection) * surface.occlusion;
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

float3 ScreenSpaceRimLight(Fragment fragment, Surface surface, Light light, RimLightData rimData)
{
    float3 normalHVS = SafeNormalize(float3(surface.normalVS.x, surface.normalVS.y, 0.0));
    float3 lightDirVS = SafeNormalize(TransformWorldToViewDir(light.directionWS));
    float3 lightDirHVS = SafeNormalize(float3(lightDirVS.x, lightDirVS.y, 0.0));
    float NdotLFactor = dot(normalHVS, lightDirHVS) * 0.5 + 0.5;
    float texelNum = rimData.width / GetTexelSizeWorldSpace(fragment.linearDepth);
    // TODO: config
    texelNum = clamp(texelNum, rimData.width * 0.01, rimData.width * 200);
    float2 offsetUV = float2(
        fragment.screenUV.x + normalHVS.x * texelNum * _CameraBufferSize.x,
        fragment.screenUV.y + normalHVS.y * texelNum * _CameraBufferSize.y);
    float offsetBufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, offsetUV);
    float offsetBufferLinearDepth = IsOrthographicCamera()
                                        ? OrthographicDepthBufferToLinear(offsetBufferDepth)
                                        : LinearEyeDepth(offsetBufferDepth, _ZBufferParams);
    float bias = offsetBufferLinearDepth - fragment.linearDepth;
    float rimFactor = step(rimData.depthBias, bias);
    return rimData.scale * rimFactor * NdotLFactor * surface.color;
}

float3 GetLighting(Surface surface, Fragment fragment, BRDF brdf, Light light,
                   DirectLightAttenData attenData, RimLightData rimLightData)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light, attenData);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return (ToonDirectBRDF(surface, brdf, light) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
    #endif
    #if defined(_DEBUG_SPECULAR)
    return ToonSpecularStrength(surface, brdf, light) * brdf.specular;
    #endif
    return IncomingLight(surface, light, attenData) *
        (ToonDirectBRDF(surface, brdf, light) + ScreenSpaceRimLight(fragment, surface, light, rimLightData));
}





float3 PhysicDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return MinimalCookTorranceSpecularTerm(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.directionWS) *
        light.shadowAttenuation * light.distanceAttenuation);
    return lightAttenuation * light.color * surface.occlusion;
}

void AccumulatePunctualLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi,
                                CascadeShadowData cascadeShadowData,
                                inout float3 color)
{
    ForwardPlusTile tile = GetForwardPlusTile(fragment.screenUV);
    int firstLightIndex = tile.GetFirstLightIndexInTile();

    int spotLightCount = tile.GetSpotLightCount();
    for (int j = 0; j < spotLightCount; j++)
    {
        int spotLightIndex = tile.GetLightIndex(firstLightIndex + j);
        Light light = GetSpotLight(spotLightIndex, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += IncomingLight(surface, light) * PhysicDirectBRDF(surface, brdf, light);
        }
    }
    firstLightIndex += spotLightCount;
    int pointLightCount = tile.GetPointLightCount();
    for (int k = 0; k < pointLightCount; k++)
    {
        int pointLightIndex = tile.GetLightIndex(firstLightIndex + k);
        Light light = GetPointLight(pointLightIndex, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += IncomingLight(surface, light) * PhysicDirectBRDF(surface, brdf, light);
        }
    }
}
#endif
