using System.Runtime.InteropServices;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Jobs;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
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
        private ShadowMapRenderer shadowMapRenderer = new();
        private PerLightDataCollector perLightDataCollector = new();
        private PerObjectShadowCasterManager perObjectShadowCasterManager;

        #region DirectionalLight
        int directionalLightCount;

        private static readonly DirectionalLightBufferData[] directionalLightData =
            new DirectionalLightBufferData[RenderPipelineConfig.MaxDirectionalLightCount];

        BufferHandle directionalLightDataHandle;
        #endregion

        #region SpotLight
        int spotLightCount;

        private static readonly SpotLightBufferData[] spotLightData = new SpotLightBufferData[RenderPipelineConfig.MaxSpotLightCount];

        BufferHandle spotLightDataHandle;
        #endregion

        #region PointLight
        int pointLightCount;

        private static readonly PointLightBufferData[] pointLightData = new PointLightBufferData[RenderPipelineConfig.MaxPointLightCount];

        BufferHandle pointLightDataHandle;
        #endregion

        #region PerObjectShadow
        int perObjectCasterCount;

        private static readonly PerObjectCasterBufferData[] perObjectCasterData =
            new PerObjectCasterBufferData[RenderPipelineConfig.MaxPerObjectCasterCount];

        BufferHandle perObjectShadowCasterDataHandle;
        #endregion

        #region TileForward+
        JobHandle forwardPlusJobHandle;

        NativeArray<float4> spotLightBounds;
        NativeArray<float4> pointLightBounds;

        NativeArray<int> forwardPlusTileData;

        BufferHandle forwardPlusTileBufferHandle;

        private int maxLightCountPerTile;
        private int tileDataSize;

        Vector2 screenUVToTileCoordinates;

        Vector2Int tileCount;
        int TileCount => tileCount.x * tileCount.y;
        #endregion

        void Render(RenderGraphContext context)
        {
            CommandBuffer commandBuffer = context.cmd;
            commandBuffer.SetGlobalInt(InternalShader.PropertyID.DirectionalLightCount, directionalLightCount);
            commandBuffer.SetBufferData(directionalLightDataHandle, directionalLightData,
                0, 0, directionalLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.DirectionalLightData, directionalLightDataHandle);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PerObjectShadowCasterCount, perObjectCasterCount);
            commandBuffer.SetBufferData(perObjectShadowCasterDataHandle, perObjectCasterData, 
                0, 0, perObjectCasterCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PerObjectShadowCasterData, perObjectShadowCasterDataHandle);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.SpotLightCount, spotLightCount);
            commandBuffer.SetBufferData(spotLightDataHandle, spotLightData,
                0, 0, spotLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.SpotLightData, spotLightDataHandle);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PointLightCount, pointLightCount);
            commandBuffer.SetBufferData(pointLightDataHandle, pointLightData,
                0, 0, pointLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PointLightData, pointLightDataHandle);

            shadowMapRenderer.RenderShadowMap(context);

            // block waiting for job result
            forwardPlusJobHandle.Complete();
            commandBuffer.SetBufferData(forwardPlusTileBufferHandle, forwardPlusTileData,
                0, 0, forwardPlusTileData.Length);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.ForwardPlusTileData, forwardPlusTileBufferHandle);
            commandBuffer.SetGlobalVector(InternalShader.PropertyID.ForwardPlusTileSettings,
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

        public static LightingDataHandles Record(RenderGraph renderGraph, CameraRenderer cameraRenderer, Camera camera,
            CullingResults cullingResults,
            Vector2Int attachmentSize,
            ShadowSettings shadowSettings,
            ForwardPlusSettings forwardPlusSettings,
            ScriptableRenderContext context,
            PerObjectShadowCasterManager perObjectShadowCasterManager)
        {
            using RenderGraphBuilder builder = renderGraph.AddRenderPass(
                sampler.name, out LightingPass pass, sampler);

            pass.Setup(cullingResults, camera, attachmentSize, shadowSettings, forwardPlusSettings,
                perObjectShadowCasterManager);
            pass.spotLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(RenderPipelineConfig.MaxSpotLightCount, SpotLightBufferData.stride)
                {
                    name = "Spot Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.pointLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(RenderPipelineConfig.MaxPointLightCount, PointLightBufferData.stride)
                {
                    name = "Point Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.directionalLightDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(RenderPipelineConfig.MaxDirectionalLightCount, DirectionalLightBufferData.stride)
                {
                    name = "Directional Light Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.perObjectShadowCasterDataHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(RenderPipelineConfig.MaxPerObjectCasterCount, PerObjectCasterBufferData.stride)
                {
                    name = "Per Object Shadow Caster Data",
                    target = GraphicsBuffer.Target.Structured
                })
            );
            pass.forwardPlusTileBufferHandle = builder.WriteBuffer(
                renderGraph.CreateBuffer(new BufferDesc(pass.TileCount * pass.tileDataSize, 4)
                {
                    name = "Forward+ Tiles",
                }));

            ShadowMapHandles shadowMapHandles =
                pass.shadowMapRenderer.Record(renderGraph, builder, context);

            builder.AllowPassCulling(false);
            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));

            return new LightingDataHandles(
                pass.directionalLightDataHandle, 
                pass.spotLightDataHandle,
                pass.pointLightDataHandle,
                pass.perObjectShadowCasterDataHandle,
                pass.forwardPlusTileBufferHandle,
                shadowMapHandles);
        }

        public void Setup(CullingResults cullingResults, Camera camera, Vector2Int attachmentSize,
            ShadowSettings shadowSettings, ForwardPlusSettings forwardPlusSettings,
            PerObjectShadowCasterManager perObjectShadowCasterManager)
        {
            this.cullingResults = cullingResults;
            this.perObjectShadowCasterManager = perObjectShadowCasterManager;

            maxLightCountPerTile = forwardPlusSettings.maxLightsPerTile;
            tileDataSize = maxLightCountPerTile + 2;

            spotLightBounds = new NativeArray<float4>(RenderPipelineConfig.MaxSpotLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            pointLightBounds = new NativeArray<float4>(RenderPipelineConfig.MaxPointLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            float tileScreenPixelSize = forwardPlusSettings.tileSize <= 0 ? 64f : (float)forwardPlusSettings.tileSize;
            screenUVToTileCoordinates.x = attachmentSize.x / tileScreenPixelSize;
            screenUVToTileCoordinates.y = attachmentSize.y / tileScreenPixelSize;
            tileCount.x = Mathf.CeilToInt(screenUVToTileCoordinates.x);
            tileCount.y = Mathf.CeilToInt(screenUVToTileCoordinates.y);

            perLightDataCollector.Setup(cullingResults, shadowSettings);
            CollectPerLightData();

            shadowMapRenderer.Setup(cullingResults, camera, shadowSettings, perLightDataCollector,
                perObjectShadowCasterManager);
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
                    case LightType.Directional when directionalLightCount < RenderPipelineConfig.MaxDirectionalLightCount:
                        directionalLightData[directionalLightCount++] =
                            DirectionalLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataDirectional(light, i));
                        break;
                    case LightType.Spot when spotLightCount < RenderPipelineConfig.MaxSpotLightCount:
                        SetupForwardPlusSpot(spotLightCount, visibleLights[i]);
                        spotLightData[spotLightCount++] =
                            SpotLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataSpot(light, i));
                        break;
                    case LightType.Point when pointLightCount < RenderPipelineConfig.MaxPointLightCount:
                        SetupForwardPlusPoint(pointLightCount, visibleLights[i]);
                        pointLightData[pointLightCount++] =
                            PointLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataPoint(light, i));
                        break;
                }
            }

            var visiblePerObjectShadowCasters = perObjectShadowCasterManager.visibleCasters;
            perObjectCasterCount = 0;
            for (int i = 0; i < visiblePerObjectShadowCasters.Count; i++)
            {
                var caster = visiblePerObjectShadowCasters[i];
                if (perObjectCasterCount < RenderPipelineConfig.MaxPerObjectCasterCount)
                {
                    perObjectCasterData[perObjectCasterCount++] =
                        PerObjectCasterBufferData.GenerateStructuredData(
                            perLightDataCollector.ReservePerObjectShadowCasterData(caster, i));
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