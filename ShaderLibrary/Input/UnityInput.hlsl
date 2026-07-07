#ifndef ARCTOON_UNITY_INPUT_INCLUDED
#define ARCTOON_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;

    float4 unity_LODFade;

    real4 unity_WorldTransformParams;

    float4 unity_RenderingLayer;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;
    float4 unity_ProbesOcclusion;

// TODO: inside cbuffer?
    bool4 unity_MetaFragmentControl;
    float unity_OneOverOutputBoost;
    float unity_MaxOutputValue;

    float4 unity_SpecCube0_HDR;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_prev_MatrixM;
float4x4 unity_prev_MatrixIM;
float4x4 glstate_matrix_projection;

// Built-in projection parameters populated by Unity per camera / setup:
//   .x = +1, or -1 when the projection matrix is Y-flipped (Unity flips Y when
//        rendering to a RenderTexture on APIs with top-left UV origin so the
//        framebuffer orientation stays consistent across APIs; shaders that
//        produce their own UVs use this as the runtime flip condition).
//   .y = camera near plane distance.
//   .z = camera far plane distance.
//   .w = 1 / camera far plane distance.
float4 _ProjectionParams;

// .x = orthographic camera width, .y = orthographic camera height,
// .z = unused, .w = 1.0 when the camera is orthographic, 0.0 when perspective.
float4 unity_OrthoParams;

// .x = pixel width, .y = pixel height, .z = 1 + 1/width, .w = 1 + 1/height.
float4 _ScreenParams;

// Linearizes the raw Z buffer value. Layout depends on UNITY_REVERSED_Z:
//   reversed : .x = far/near - 1, .y = 1,       .z = .x/far, .w = 1/far
//   standard : .x = 1 - far/near, .y = far/near, .z = .x/far, .w = .y/far
float4 _ZBufferParams;

float3 _WorldSpaceCameraPos;

#endif
