using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public class Lighting {

        private const string bufferName = "Lighting";

        private CommandBuffer commandBuffer = new CommandBuffer {
            name = bufferName
        };
	
        private CullingResults cullingResults;

        private const int maxDirLightCount = 4;

        private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

        private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
        
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults) 
        {
            this.cullingResults = cullingResults;

            SetupLights();
            
            ArcToonRenderPipelineInstance.ConsumeCommandBuffer(context, commandBuffer);
        }
        private void SetupLights() 
        {
            commandBuffer.BeginSample(bufferName);

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            
            int dirLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++) 
            {
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional when dirLightCount < maxDirLightCount:
                        dirLightCount++;
                        SetupDirectionalLight(i, visibleLights[i]);
                        break;
                }
            }
            commandBuffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
            commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            
            commandBuffer.EndSample(bufferName);

        }
        private void SetupDirectionalLight(int index, in VisibleLight visibleLight) 
        {
            dirLightColors[index] = visibleLight.finalColor;
            // local z in world space
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        }
    }
}