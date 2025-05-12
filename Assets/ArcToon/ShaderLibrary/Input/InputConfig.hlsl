#ifndef ARCTOON_INPUT_CONFIG_INCLUDED
#define ARCTOON_INPUT_CONFIG_INCLUDED

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    float2 detailUV;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float2 detailUV = 0.0)
{
    InputConfig config;
    config.fragment = GetFragment(positionSS);
    config.baseUV = baseUV;
    config.detailUV = detailUV;
    return config;
}


#endif
