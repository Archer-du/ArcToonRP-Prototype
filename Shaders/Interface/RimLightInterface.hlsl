#ifndef ARCTOON_RIM_LIGHT_INTERFACE_INCLUDED
#define ARCTOON_RIM_LIGHT_INTERFACE_INCLUDED

// Interface tier: screen-space rim light. Binds the per-material rim parameters directly, so the
// shading function reads them at point of use (no RimLightData bag).
// Reads the per-material CBUFFER; include after the shader's CBUFFER.
// Requires: SurfaceSampling.hlsl (INPUT_PROP), Common.hlsl (SafeNormalize,
// TransformWorldToViewDir, GetTexelSizeWorldSpace, sampler_point_clamp, depth helpers).

float GetRimLightScale()
{
    return INPUT_PROP(_RimScale) * 12.5;
}

float GetRimLightWidth()
{
    return INPUT_PROP(_RimWidth) * 0.005;
}

float GetRimLightDepthBias()
{
    return INPUT_PROP(_RimDepthBias) * 0.1;
}

float3 ScreenSpaceRimLight(Fragment fragment, Surface surface, Light light)
{
    float3 normalHVS = SafeNormalize(float3(surface.normalVS.x, surface.normalVS.y, 0.0));
    float3 lightDirVS = SafeNormalize(TransformWorldToViewDir(light.directionWS));
    float3 lightDirHVS = SafeNormalize(float3(lightDirVS.x, lightDirVS.y, 0.0));
    float NdotLFactor = dot(normalHVS, lightDirHVS) * 0.5 + 0.5;
    float width = GetRimLightWidth();
    float texelNum = width / GetTexelSizeWorldSpace(fragment.linearDepth);
    // TODO: config
    // texelNum = clamp(texelNum, width * 0.01, width * 200);
    float2 offsetUV = float2(
        fragment.screenUV.x + normalHVS.x * texelNum * _CameraBufferSize.x,
        fragment.screenUV.y + normalHVS.y * texelNum * _CameraBufferSize.y);
    float offsetBufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, offsetUV);
    float offsetBufferLinearDepth = IsOrthographicCamera()
                                        ? OrthographicDepthBufferToLinear(offsetBufferDepth)
                                        : LinearEyeDepth(offsetBufferDepth, _ZBufferParams);
    float bias = offsetBufferLinearDepth - fragment.linearDepth;
    float rimFactor = step(GetRimLightDepthBias(), bias);
    return GetRimLightScale() * rimFactor * NdotLFactor * surface.color;
}

#endif
