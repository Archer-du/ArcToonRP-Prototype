using System;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public class Lighting
    {
        private const string bufferName = "Lighting";

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;
        private CullingResults cullingResults;
        private ShadowRenderer shadowRenderer;

        private const int maxDirLightCount = 4;

        private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
        private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        private const int maxSpotLightCount = 64;

        private static int spotLightCountId = Shader.PropertyToID("_SpotLightCount");
        private static int spotLightColorsId = Shader.PropertyToID("_SpotLightColors");
        private static int spotLightPositionsId = Shader.PropertyToID("_SpotLightPositions");
        private static int spotLightDirectionsId = Shader.PropertyToID("_SpotLightDirections");
        private static int spotLightSpotAnglesId = Shader.PropertyToID("_SpotLightSpotAngles");
        private static int spotLightShadowDataId = Shader.PropertyToID("_SpotLightShadowData");

        private static Vector4[] spotLightColors = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightPositions = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightDirections = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightSpotAngles = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightShadowData = new Vector4[maxSpotLightCount];
        
        private const int maxPointLightCount = 16;

        private static int pointLightCountId = Shader.PropertyToID("_PointLightCount");
        private static int pointLightColorsId = Shader.PropertyToID("_PointLightColors");
        private static int pointLightPositionsId = Shader.PropertyToID("_PointLightPositions");
        private static int pointLightDirectionsId = Shader.PropertyToID("_PointLightDirections");
        private static int pointLightShadowDataId = Shader.PropertyToID("_PointLightShadowData");

        private static Vector4[] pointLightColors = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightPositions = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightDirections = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightShadowData = new Vector4[maxPointLightCount];

        public Lighting()
        {
            commandBuffer = new CommandBuffer()
            {
                name = bufferName,
            };
            shadowRenderer = new ShadowRenderer();
        }

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            this.context = context;
            this.cullingResults = cullingResults;

            shadowRenderer.Setup(context, commandBuffer, cullingResults, shadowSettings);
            CollectPerLightData();
            shadowRenderer.RenderShadowMap();

            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
        }

        public void CleanUp()
        {
            shadowRenderer.CleanUp();
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
        }

        private void CollectPerLightData()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

            int dirLightCount = 0;
            int spotLightCount = 0;
            int pointLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional when dirLightCount < maxDirLightCount:
                        ReservePerLightDataDirectional(dirLightCount++, i, visibleLights[i]);
                        break;
                    case LightType.Spot when spotLightCount < maxSpotLightCount:
                        ReservePerLightDataSpot(spotLightCount++, i, visibleLights[i]);
                        break;
                    case LightType.Point when pointLightCount < maxPointLightCount:
                        ReservePerLightDataPoint(pointLightCount++, i, visibleLights[i]);
                        break;
                }
            }

            commandBuffer.SetGlobalInt(dirLightCountId, dirLightCount);
            if (dirLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
                commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
                commandBuffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
            }

            commandBuffer.SetGlobalInt(spotLightCountId, spotLightCount);
            if (spotLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(spotLightColorsId, spotLightColors);
                commandBuffer.SetGlobalVectorArray(spotLightPositionsId, spotLightPositions);
                commandBuffer.SetGlobalVectorArray(spotLightDirectionsId, spotLightDirections);
                commandBuffer.SetGlobalVectorArray(spotLightSpotAnglesId, spotLightSpotAngles);
                commandBuffer.SetGlobalVectorArray(spotLightShadowDataId, spotLightShadowData);
            }
            
            commandBuffer.SetGlobalInt(pointLightCountId, pointLightCount);
            if (pointLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(pointLightColorsId, pointLightColors);
                commandBuffer.SetGlobalVectorArray(pointLightPositionsId, pointLightPositions);
                commandBuffer.SetGlobalVectorArray(pointLightDirectionsId, pointLightDirections);
                // commandBuffer.SetGlobalVectorArray(spotLightSpotAnglesId, spotLightSpotAngles);
                commandBuffer.SetGlobalVectorArray(pointLightShadowDataId, pointLightShadowData);
            }
        }

        private void ReservePerLightDataDirectional(int directionalLightIndex, int visibleLightIndex, in VisibleLight visibleLight)
        {
            dirLightColors[directionalLightIndex] = visibleLight.finalColor;
            // local z in world space
            dirLightDirections[directionalLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirLightShadowData[directionalLightIndex] = 
                shadowRenderer.ReservePerLightShadowDataDirectional(visibleLight.light, visibleLightIndex);
        }

        private void ReservePerLightDataPoint(int pointLightIndex, int visibleLightIndex, in VisibleLight visibleLight)
        {
            pointLightColors[pointLightIndex] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            pointLightPositions[pointLightIndex] = position;
            pointLightDirections[pointLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
            pointLightShadowData[pointLightIndex] = 
                shadowRenderer.ReservePerLightShadowDataPoint(visibleLight.light, visibleLightIndex);
        }
        
        private void ReservePerLightDataSpot(int spotLightIndex, int visibleLightIndex, in VisibleLight visibleLight)
        {
            spotLightColors[spotLightIndex] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            spotLightPositions[spotLightIndex] = position;
            spotLightDirections[spotLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
            Light light = visibleLight.light;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            spotLightSpotAngles[spotLightIndex] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            spotLightShadowData[spotLightIndex] = 
                shadowRenderer.ReservePerLightShadowDataSpot(visibleLight.light, visibleLightIndex);
        }
    }
}