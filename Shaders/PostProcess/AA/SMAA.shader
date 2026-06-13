Shader "Hidden/ArcToon/PostProcess/SMAA"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        // -------------------------------------------------------------
        // Pass 0: Edge Detection  (source color -> edges texture)
        // -------------------------------------------------------------
        Pass
        {
            Name "SMAA Edge Detection"

            HLSLPROGRAM
            #pragma target 3.5

            // Quality preset keywords. Keep them local_fragment — the preset only
            // affects fragment-side algorithm constants, and we want them to
            // coexist peacefully with the global variant space.
            #pragma multi_compile _ _SMAA_PRESET_LOW _SMAA_PRESET_MEDIUM _SMAA_PRESET_HIGH

            #include "SMAAPasses.hlsl"

            #pragma vertex SMAAEdgePassVertex
            #pragma fragment SMAAEdgePassFragment
            ENDHLSL
        }

        // -------------------------------------------------------------
        // Pass 1: Blending Weight Calculation  (edges -> blend weights)
        // -------------------------------------------------------------
        Pass
        {
            Name "SMAA Blending Weights"

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile _ _SMAA_PRESET_LOW _SMAA_PRESET_MEDIUM _SMAA_PRESET_HIGH

            #include "SMAAPasses.hlsl"

            #pragma vertex SMAABlendPassVertex
            #pragma fragment SMAABlendPassFragment
            ENDHLSL
        }

        // -------------------------------------------------------------
        // Pass 2: Neighborhood Blending  (source color + blend -> destination)
        // -------------------------------------------------------------
        Pass
        {
            Name "SMAA Neighborhood Blending"

            HLSLPROGRAM
            #pragma target 3.5

            // This pass doesn't use the preset-sensitive algorithm branches but
            // we still keep the keyword list symmetrical so the material's
            // keyword state doesn't cause a recompile mid-frame.
            #pragma multi_compile _ _SMAA_PRESET_LOW _SMAA_PRESET_MEDIUM _SMAA_PRESET_HIGH

            #include "SMAAPasses.hlsl"

            #pragma vertex SMAANeighborPassVertex
            #pragma fragment SMAANeighborPassFragment
            ENDHLSL
        }
    }
}
