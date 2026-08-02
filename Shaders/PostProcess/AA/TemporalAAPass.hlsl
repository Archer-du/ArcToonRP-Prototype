#ifndef ARCTOON_TEMPORAL_AA_PASS_INCLUDED
#define ARCTOON_TEMPORAL_AA_PASS_INCLUDED

#include "Packages/com.arctoon.render-pipeline/Shaders/PostProcess/PostProcessInput.hlsl"

// Step 1 scaffolding: straight pass-through of the jittered source. Reprojection and
// temporal accumulation are layered on top of this in later steps.
float4 TemporalAACopyPassFragment(Varyings_Default input) : SV_TARGET
{
    return SampleSource(input.screenUV);
}

#endif
