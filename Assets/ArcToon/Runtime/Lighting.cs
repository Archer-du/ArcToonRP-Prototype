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
            shadowRenderer.Render();

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
            for (int i = 0; i < visibleLights.Length; i++)
            {
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional when dirLightCount < maxDirLightCount:
                        dirLightCount++;
                        ReservePerLightDataDirectional(i, visibleLights[i]);
                        break;
                }
            }

            commandBuffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
            commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            commandBuffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        private void ReservePerLightDataDirectional(int index, in VisibleLight visibleLight)
        {
            dirLightColors[index] = visibleLight.finalColor;
            // local z in world space
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirLightShadowData[index] = shadowRenderer.ReservePerLightShadowDataDirectional(visibleLight.light, index);
        }
    }
}