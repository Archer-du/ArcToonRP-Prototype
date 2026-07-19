#ifndef ARCTOON_SCREEN_DEBUG_INCLUDED
#define ARCTOON_SCREEN_DEBUG_INCLUDED

#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Input/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

float _DebugOpacity;

float4 ForwardPlusTilesPassFragment(Varyings_Default input) : SV_TARGET
{
    ForwardPlusTile tile = GetForwardPlusTile(input.screenUV);
    float3 color;
    if (tile.IsMinimumEdgePixel(input.screenUV))
    {
        color = 1.0;
    }
    else
    {
        color = OverlayHeatMap(
            input.screenUV * _CameraBufferSize.zw, tile.GetScreenSize(),
            tile.GetLightCount(), tile.GetMaxLightsPerTile(), 1.0).rgb;
    }
    return float4(color, _DebugOpacity);
}
#endif
