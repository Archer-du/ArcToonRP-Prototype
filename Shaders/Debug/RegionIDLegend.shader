Shader "Hidden/ArcToon/RegionID Legend"
{
    // On-screen legend for the RegionID geometry-debug mode. Draws a swatch + digit
    // for every region index at the bottom-left of the screen. Purely diagnostic:
    // never used by the shipping path (the pass only runs while geometry debug is on).
    // The region colors come from the _RegionDebugColors array in RegionID.hlsl, which
    // GetRegionDebugColor reads in the geometry debug pass, so the swatches always
    // match what the RegionID mode draws.

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.arctoon.render-pipeline/ShaderLibrary/Common.hlsl"
        #include "Packages/com.arctoon.render-pipeline/Shaders/Debug/RegionIDLegendPass.hlsl"
        ENDHLSL

        Pass
        {
            Name "Region ID Legend"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex DefaultPassVertex
            #pragma fragment RegionIDLegendFragment
            ENDHLSL
        }
    }
}
