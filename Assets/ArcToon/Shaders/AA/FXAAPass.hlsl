#ifndef ARCTOON_FXAA_PASS_INCLUDED
#define ARCTOON_FXAA_PASS_INCLUDED

float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    return SampleSource(input.screenUV);
}

#endif
