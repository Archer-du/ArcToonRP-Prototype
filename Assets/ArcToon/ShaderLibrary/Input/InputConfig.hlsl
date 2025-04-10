#ifndef ARCTOON_INPUT_CONFIG_INCLUDED
#define ARCTOON_INPUT_CONFIG_INCLUDED

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    float2 baseUV;
    float2 detailUV;
    bool useMODSMask;
    bool useDetail;
};


InputConfig GetInputConfig(float2 baseUV, float2 detailUV = 0.0)
{
    InputConfig config;
    config.baseUV = baseUV;
    config.detailUV = detailUV;
    config.useMODSMask = false;
    config.useDetail = false;
    return config;
}


#endif
