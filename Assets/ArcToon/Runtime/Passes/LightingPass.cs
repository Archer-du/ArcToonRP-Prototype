using ArcToon.Runtime.Data;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes
{
    public class LightingPass
    {
        static readonly ProfilingSampler sampler = new("Lighting");

        private CullingResults cullingResults;
        private ShadowRenderer shadowRenderer = new();

        private const int maxDirLightCount = 4;

        private static int dirLightCountID = Shader.PropertyToID("_DirectionalLightCount");
        private static int dirLightColorsID = Shader.PropertyToID("_DirectionalLightColors");
        private static int dirLightDirectionsID = Shader.PropertyToID("_DirectionalLightDirections");
        private static int dirLightShadowDataID = Shader.PropertyToID("_DirectionalLightShadowData");

        private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
        private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        private const int maxSpotLightCount = 64;

        private static int spotLightCountID = Shader.PropertyToID("_SpotLightCount");
        private static int spotLightColorsID = Shader.PropertyToID("_SpotLightColors");
        private static int spotLightPositionsID = Shader.PropertyToID("_SpotLightPositions");
        private static int spotLightDirectionsID = Shader.PropertyToID("_SpotLightDirections");
        private static int spotLightSpotAnglesID = Shader.PropertyToID("_SpotLightSpotAngles");
        private static int spotLightShadowDataID = Shader.PropertyToID("_SpotLightShadowData");

        private static Vector4[] spotLightColors = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightPositions = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightDirections = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightSpotAngles = new Vector4[maxSpotLightCount];
        private static Vector4[] spotLightShadowData = new Vector4[maxSpotLightCount];

        private const int maxPointLightCount = 16;

        private static int pointLightCountID = Shader.PropertyToID("_PointLightCount");
        private static int pointLightColorsID = Shader.PropertyToID("_PointLightColors");
        private static int pointLightPositionsID = Shader.PropertyToID("_PointLightPositions");
        private static int pointLightDirectionsID = Shader.PropertyToID("_PointLightDirections");
        private static int pointLightShadowDataID = Shader.PropertyToID("_PointLightShadowData");

        private static Vector4[] pointLightColors = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightPositions = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightDirections = new Vector4[maxPointLightCount];
        private static Vector4[] pointLightShadowData = new Vector4[maxPointLightCount];

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            commandBuffer.SetGlobalInt(dirLightCountID, dirLightCount);
            if (dirLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(dirLightColorsID, dirLightColors);
                commandBuffer.SetGlobalVectorArray(dirLightDirectionsID, dirLightDirections);
                commandBuffer.SetGlobalVectorArray(dirLightShadowDataID, dirLightShadowData);
            }

            commandBuffer.SetGlobalInt(spotLightCountID, spotLightCount);
            if (spotLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(spotLightColorsID, spotLightColors);
                commandBuffer.SetGlobalVectorArray(spotLightPositionsID, spotLightPositions);
                commandBuffer.SetGlobalVectorArray(spotLightDirectionsID, spotLightDirections);
                commandBuffer.SetGlobalVectorArray(spotLightSpotAnglesID, spotLightSpotAngles);
                commandBuffer.SetGlobalVectorArray(spotLightShadowDataID, spotLightShadowData);
            }

            commandBuffer.SetGlobalInt(pointLightCountID, pointLightCount);
            if (pointLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(pointLightColorsID, pointLightColors);
                commandBuffer.SetGlobalVectorArray(pointLightPositionsID, pointLightPositions);
                commandBuffer.SetGlobalVectorArray(pointLightDirectionsID, pointLightDirections);
                commandBuffer.SetGlobalVectorArray(pointLightShadowDataID, pointLightShadowData);
            }

            shadowRenderer.RenderShadowMap(context);

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static ShadowTextureData Record(
            RenderGraph renderGraph,
            CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            using RenderGraphBuilder builder =
                renderGraph.AddRenderPass(sampler.name, out LightingPass pass, sampler);

            pass.Setup(cullingResults, shadowSettings);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));

            return pass.shadowRenderer.GetRenderTextures(renderGraph, builder);
        }

        public void Setup(CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;

            shadowRenderer.Setup(cullingResults, shadowSettings);
            CollectPerLightData();
        }

        int dirLightCount = 0;
        int spotLightCount = 0;
        int pointLightCount = 0;

        private void CollectPerLightData()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

            dirLightCount = 0;
            spotLightCount = 0;
            pointLightCount = 0;
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
        }

        private void ReservePerLightDataDirectional(int directionalLightIndex, int visibleLightIndex,
            in VisibleLight visibleLight)
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