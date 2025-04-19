using System.Runtime.InteropServices;
using ArcToon.Runtime.Buffers;
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


        // directional light
        int directionalLightCount;

        private const int maxDirectionalLightCount = 4;

        private static readonly DirectionalLightBufferData[] directionalLightData =
            new DirectionalLightBufferData[maxDirectionalLightCount];

        private static int directionalLightCountID = Shader.PropertyToID("_DirectionalLightCount");
        private static int directionalLightDataID = Shader.PropertyToID("_DirectionalLightData");

        BufferHandle directionalLightDataHandle;


        // spot light
        int spotLightCount;

        private const int maxSpotLightCount = 64;

        private static readonly SpotLightBufferData[] spotLightData = new SpotLightBufferData[maxSpotLightCount];

        private static int spotLightCountID = Shader.PropertyToID("_SpotLightCount");
        private static int spotLightDataID = Shader.PropertyToID("_SpotLightData");

        BufferHandle spotLightDataHandle;


        // point light
        int pointLightCount;

        private const int maxPointLightCount = 16;

        private static readonly PointLightBufferData[] pointLightData = new PointLightBufferData[maxPointLightCount];

        private static int pointLightCountID = Shader.PropertyToID("_PointLightCount");
        private static int pointLightDataID = Shader.PropertyToID("_PointLightData");

        BufferHandle pointLightDataHandle;

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            commandBuffer.SetGlobalInt(directionalLightCountID, directionalLightCount);
            commandBuffer.SetBufferData(directionalLightDataHandle, directionalLightData,
                0, 0, directionalLightCount);
            commandBuffer.SetGlobalBuffer(directionalLightDataID, directionalLightDataHandle);

            commandBuffer.SetGlobalInt(spotLightCountID, spotLightCount);
            commandBuffer.SetBufferData(spotLightDataHandle, spotLightData,
                0, 0, spotLightCount);
            commandBuffer.SetGlobalBuffer(spotLightDataID, spotLightDataHandle);

            commandBuffer.SetGlobalInt(pointLightCountID, pointLightCount);
            commandBuffer.SetBufferData(pointLightDataHandle, pointLightData,
                0, 0, pointLightCount);
            commandBuffer.SetGlobalBuffer(pointLightDataID, pointLightDataHandle);

            shadowRenderer.RenderShadowMap(context);

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public static LightDataHandles Record(RenderGraph renderGraph, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out LightingPass pass, sampler);

            pass.Setup(cullingResults, shadowSettings);
            pass.spotLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxSpotLightCount, SpotLightBufferData.stride)
                {
                    name = "Spot Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.pointLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxPointLightCount, PointLightBufferData.stride)
                {
                    name = "Point Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.directionalLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(maxDirectionalLightCount, DirectionalLightBufferData.stride)
                {
                    name = "Directional Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));

            return new LightDataHandles(pass.directionalLightDataHandle, pass.spotLightDataHandle, pass.pointLightDataHandle,
                pass.shadowRenderer.GetShadowMapHandles(renderGraph, builder));
        }

        public void Setup(CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;

            shadowRenderer.Setup(cullingResults, shadowSettings);

            CollectPerLightData();
        }

        private void CollectPerLightData()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

            directionalLightCount = 0;
            spotLightCount = 0;
            pointLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                Light light = visibleLights[i].light;
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional when directionalLightCount < maxDirectionalLightCount:
                        directionalLightData[directionalLightCount++] =
                            DirectionalLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                shadowRenderer.ReservePerLightShadowDataDirectional(light, i));
                        break;
                    case LightType.Spot when spotLightCount < maxSpotLightCount:
                        spotLightData[spotLightCount++] =
                            SpotLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                shadowRenderer.ReservePerLightShadowDataSpot(light, i));
                        break;
                    case LightType.Point when pointLightCount < maxPointLightCount:
                        pointLightData[pointLightCount++] =
                            PointLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                shadowRenderer.ReservePerLightShadowDataPoint(light, i));
                        break;
                }
            }
        }
    }
}