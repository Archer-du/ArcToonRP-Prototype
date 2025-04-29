#ifndef ARCTOON_LIGHTING_INCLUDED
#define ARCTOON_LIGHTING_INCLUDED

#include "../Ramp.hlsl"
#include "../BRDF.hlsl"
#include "DirectionalLight.hlsl"
#include "SpotLight.hlsl"
#include "PointLight.hlsl"
#include "../GI.hlsl"

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

float3 ScreenSpaceRimLight(Fragment fragment, Surface surface, Light light, RimLightData rimData)
{
    float3 normalHVS = SafeNormalize(float3(surface.normalVS.x, surface.normalVS.y, 0.0));
    float3 lightDirVS = SafeNormalize(TransformWorldToViewDir(light.directionWS));
    float3 lightDirHVS = SafeNormalize(float3(lightDirVS.x, lightDirVS.y, 0.0));
    float NdotLFactor = dot(normalHVS, lightDirHVS) * 0.5 + 0.5;
    float rimThreshold = 0.2 + 0.3 * rimData.width;
    float NdotVFactor = 1 - smoothstep(rimThreshold, rimThreshold + 0.1,
                                       dot(surface.normalWS, surface.viewDirectionWS));
    uint texelNum = rimData.width * 10;
    float2 offsetUV = float2(
        fragment.screenUV.x + normalHVS.x * texelNum * _CameraBufferSize.x,
        fragment.screenUV.y + normalHVS.y * texelNum * _CameraBufferSize.y);
    float offsetBufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, offsetUV);
    float offsetBufferLinearDepth = IsOrthographicCamera()
                                        ? OrthographicDepthBufferToLinear(offsetBufferDepth)
                                        : LinearEyeDepth(offsetBufferDepth, _ZBufferParams);
    float bias = offsetBufferLinearDepth - fragment.linearDepth;
    float rimFactor = step(rimData.depthBias, bias);
    return rimData.scale * rimFactor * NdotLFactor * NdotVFactor * surface.color;
}

// punctual lights avoid gradient unroll
float3 IncomingLight(Surface surface, Light light)
{
    float lightAttenuation = saturate(dot(surface.normalWS, light.directionWS) *
        light.shadowAttenuation * light.distanceAttenuation);
    return lightAttenuation * light.color * surface.occlusion;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    #if defined(_DEBUG_INCOMING_LIGHT)
    return IncomingLight(surface, light);
    #endif
    #if defined(_DEBUG_DIRECT_BRDF)
    return DirectBRDF(surface, brdf, light);
    #endif
    #if defined(_DEBUG_SPECULAR)
    return SpecularStrength(surface, brdf, light) * brdf.specular;
    #endif
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

void AccumulatePunctualLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi,
                                CascadeShadowData cascadeShadowData, inout float3 color)
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
            color += GetLighting(surface, brdf, light);
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
            color += GetLighting(surface, brdf, light);
        }
    }
}

float3 GetLighting(Fragment fragment, Surface surface, BRDF brdf, GI gi)
{
    CascadeShadowData cascadeShadowData = GetCascadeShadowData(surface);
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light = GetDirectionalLight(i, surface, cascadeShadowData, gi);
        if (RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }

    AccumulatePunctualLighting(fragment, surface, brdf, gi, cascadeShadowData, color);

    return color;
}

#endif
