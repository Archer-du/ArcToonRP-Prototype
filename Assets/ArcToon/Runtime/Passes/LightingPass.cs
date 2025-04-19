using System.Runtime.InteropServices;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Jobs;
using ArcToon.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ArcToon.Runtime.Passes
{
    public class LightingPass
    {
        static readonly ProfilingSampler sampler = new("Lighting");

        private CullingResults cullingResults;
        private ShadowRenderer shadowRenderer = new();

        BufferHandle forwardPlusTileBufferHandle;

        private const int maxLightCountPerTile = 30;
        private const int tileDataSize = maxLightCountPerTile + 2;
        private const int tileScreenPixelSize = 64;

        Vector2 screenUVToTileCoordinates;

        Vector2Int tileCount;

        int TileCount => tileCount.x * tileCount.y;

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


        // tile job
        JobHandle forwardPlusJobHandle;

        NativeArray<float4> spotLightBounds;
        NativeArray<float4> pointLightBounds;
        
        NativeArray<int> forwardPlusTileData;
        private static int forwardPlusTileDataID = Shader.PropertyToID("_ForwardPlusTileData");
        private static int forwardPlusTileSettingsID = Shader.PropertyToID("_ForwardPlusTileSettings");

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

            // block waiting for job result
            forwardPlusJobHandle.Complete();
            commandBuffer.SetBufferData(forwardPlusTileBufferHandle, forwardPlusTileData,
                0, 0, forwardPlusTileData.Length);
            commandBuffer.SetGlobalBuffer(forwardPlusTileDataID, forwardPlusTileBufferHandle);
            commandBuffer.SetGlobalVector(forwardPlusTileSettingsID,
                new Vector4(screenUVToTileCoordinates.x, screenUVToTileCoordinates.y,
                    asfloat(tileCount.x),
                    asfloat(tileDataSize)
                )
            );

            context.renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            spotLightBounds.Dispose();
            pointLightBounds.Dispose();
            forwardPlusTileData.Dispose();
        }

        public static LightDataHandles Record(RenderGraph renderGraph, CullingResults cullingResults,
            Vector2Int attachmentSize,
            ShadowSettings shadowSettings)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out LightingPass pass, sampler);

            pass.Setup(cullingResults, attachmentSize, shadowSettings);
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
            pass.forwardPlusTileBufferHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(pass.TileCount * tileDataSize, 4)
                {
                    name = "Forward+ Tiles",
                }));

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));

            return new LightDataHandles(pass.directionalLightDataHandle, pass.spotLightDataHandle,
                pass.pointLightDataHandle,
                pass.forwardPlusTileBufferHandle,
                pass.shadowRenderer.GetShadowMapHandles(renderGraph, builder));
        }

        public void Setup(CullingResults cullingResults, Vector2Int attachmentSize,
            ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;

            shadowRenderer.Setup(cullingResults, shadowSettings);

            spotLightBounds = new NativeArray<float4>(maxSpotLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            pointLightBounds = new NativeArray<float4>(maxPointLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            screenUVToTileCoordinates.x = attachmentSize.x / (float)tileScreenPixelSize;
            screenUVToTileCoordinates.y = attachmentSize.y / (float)tileScreenPixelSize;
            tileCount.x = Mathf.CeilToInt(screenUVToTileCoordinates.x);
            tileCount.y = Mathf.CeilToInt(screenUVToTileCoordinates.y);

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
                        SetupForwardPlusSpot(spotLightCount, visibleLights[i]);
                        spotLightData[spotLightCount++] =
                            SpotLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                shadowRenderer.ReservePerLightShadowDataSpot(light, i));
                        break;
                    case LightType.Point when pointLightCount < maxPointLightCount:
                        SetupForwardPlusPoint(pointLightCount, visibleLights[i]);
                        pointLightData[pointLightCount++] =
                            PointLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                shadowRenderer.ReservePerLightShadowDataPoint(light, i));
                        break;
                }
            }

            forwardPlusTileData = new NativeArray<int>(TileCount * tileDataSize, Allocator.TempJob);
            forwardPlusJobHandle = new ForwardPlusTileBoundJob()
            {
                tileData = forwardPlusTileData,
                spotLightBounds = spotLightBounds,
                pointLightBounds = pointLightBounds,
                spotLightCount = spotLightCount,
                pointLightCount = pointLightCount,
                tileScreenUVSize = float2(
                    1f / screenUVToTileCoordinates.x,
                    1f / screenUVToTileCoordinates.y),
                maxLightCountPerTile = maxLightCountPerTile,
                tilesPerRow = tileCount.x,
                tileDataSize = tileDataSize
            }.ScheduleParallel(TileCount, tileCount.x, default);
        }

        void SetupForwardPlusSpot(int spotlightIndex, in VisibleLight visibleLight)
        {
            Rect r = visibleLight.screenRect;
            spotLightBounds[spotlightIndex] = float4(r.xMin, r.yMin, r.xMax, r.yMax);
        }
        void SetupForwardPlusPoint(int pointlightIndex, in VisibleLight visibleLight)
        {
            Rect r = visibleLight.screenRect;
            pointLightBounds[pointlightIndex] = float4(r.xMin, r.yMin, r.xMax, r.yMax);
        }
    }
}