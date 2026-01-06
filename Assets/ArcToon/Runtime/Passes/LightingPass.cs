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
    public class LightingPass : RenderGraphPassBase
    {
        public override ProfilingSampler Sampler => new("Lighting");

        private ShadowMapRenderer shadowMapRenderer = new();
        private PerLightDataCollector perLightDataCollector = new();
        
        private RenderGraph renderGraph;

        #region DirectionalLight
        int directionalLightCount;

        private static readonly DirectionalLightBufferData[] DirectionalLightData =
            new DirectionalLightBufferData[RenderPipelineInfo.MaxDirectionalLightCount];
        #endregion

        #region SpotLight
        int spotLightCount;

        private static readonly SpotLightBufferData[] SpotLightData = 
            new SpotLightBufferData[RenderPipelineInfo.MaxSpotLightCount];
        #endregion

        #region PointLight
        int pointLightCount;

        private static readonly PointLightBufferData[] PointLightData = 
            new PointLightBufferData[RenderPipelineInfo.MaxPointLightCount];
        #endregion

        #region PerObjectShadow
        int perObjectCasterCount;

        private static readonly PerObjectCasterBufferData[] PerObjectCasterData =
            new PerObjectCasterBufferData[RenderPipelineInfo.MaxPerObjectCasterCount];
        #endregion

        #region TileForward+
        JobHandle forwardPlusJobHandle;

        NativeArray<float4> spotLightBounds;
        NativeArray<float4> pointLightBounds;

        NativeArray<int> forwardPlusTileData;

        private int maxLightCountPerTile;
        private int tileDataSize;

        Vector2 screenUVToTileCoordinates;

        Vector2Int tileCount;
        int TileCount => tileCount.x * tileCount.y;
        #endregion

        public override void Initialize(RenderGraphResourceHandle resourceHandle, CameraRenderer renderer)
        {
            base.Initialize(resourceHandle, renderer);

            maxLightCountPerTile = renderer.ForwardPlusSettings.maxLightsPerTile;
            tileDataSize = maxLightCountPerTile + 2;

            spotLightBounds = new NativeArray<float4>(RenderPipelineInfo.MaxSpotLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            pointLightBounds = new NativeArray<float4>(RenderPipelineInfo.MaxPointLightCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            
            float tileScreenPixelSize = renderer.ForwardPlusSettings.tileSize <= 0 ? 64f : (float)renderer.ForwardPlusSettings.tileSize;
            screenUVToTileCoordinates.x = renderer.AttachmentSize.x / tileScreenPixelSize;
            screenUVToTileCoordinates.y = renderer.AttachmentSize.y / tileScreenPixelSize;
            tileCount.x = Mathf.CeilToInt(screenUVToTileCoordinates.x);
            tileCount.y = Mathf.CeilToInt(screenUVToTileCoordinates.y);

            perLightDataCollector.Setup(renderer.CullingResults, renderer.ShadowSettings);
            CollectPerLightData();

            shadowMapRenderer.Initialize(renderer.CullingResults, Camera, renderer.ShadowSettings, perLightDataCollector,
                renderer.PerObjectShadowCasterManager);
        }

        public override void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalInt(InternalShader.PropertyID.DirectionalLightCount, directionalLightCount);
            commandBuffer.SetBufferData(resourceHandle.lightDataDirectional, DirectionalLightData,
                0, 0, directionalLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.DirectionalLightData, resourceHandle.lightDataDirectional);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PerObjectShadowCasterCount, perObjectCasterCount);
            commandBuffer.SetBufferData(resourceHandle.perObjectShadowCasterData, PerObjectCasterData, 
                0, 0, perObjectCasterCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PerObjectShadowCasterData, resourceHandle.perObjectShadowCasterData);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.SpotLightCount, spotLightCount);
            commandBuffer.SetBufferData(resourceHandle.lightDataSpot, SpotLightData,
                0, 0, spotLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.SpotLightData, resourceHandle.lightDataSpot);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PointLightCount, pointLightCount);
            commandBuffer.SetBufferData(resourceHandle.lightDataPoint, PointLightData,
                0, 0, pointLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PointLightData, resourceHandle.lightDataPoint);

            shadowMapRenderer.RenderShadowMap(commandBuffer, context);

            // block waiting for job result
            forwardPlusJobHandle.Complete();
            commandBuffer.SetBufferData(resourceHandle.forwardPlusTileBuffer, forwardPlusTileData,
                0, 0, forwardPlusTileData.Length);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.ForwardPlusTileData, resourceHandle.forwardPlusTileBuffer);
            commandBuffer.SetGlobalVector(InternalShader.PropertyID.ForwardPlusTileSettings,
                new Vector4(screenUVToTileCoordinates.x, screenUVToTileCoordinates.y,
                    asfloat(tileCount.x),
                    asfloat(tileDataSize)
                )
            );

            spotLightBounds.Dispose();
            pointLightBounds.Dispose();
            forwardPlusTileData.Dispose();
        }

        public override void AcquireResource(RenderGraph renderGraph)
        {
            this.renderGraph = renderGraph;
            resourceHandle.lightDataSpot = renderGraph.CreateBuffer(new BufferDesc(RenderPipelineInfo.MaxSpotLightCount, SpotLightBufferData.stride)
            {
                name = "Spot Light Data",
                target = GraphicsBuffer.Target.Structured
            });
            resourceHandle.lightDataPoint = renderGraph.CreateBuffer(new BufferDesc(RenderPipelineInfo.MaxPointLightCount, PointLightBufferData.stride)
            {
                name = "Point Light Data",
                target = GraphicsBuffer.Target.Structured
            });
            resourceHandle.lightDataDirectional = renderGraph.CreateBuffer(new BufferDesc(RenderPipelineInfo.MaxDirectionalLightCount, DirectionalLightBufferData.stride)
            {
                name = "Directional Light Data",
                target = GraphicsBuffer.Target.Structured
            });
            resourceHandle.perObjectShadowCasterData = renderGraph.CreateBuffer(new BufferDesc(RenderPipelineInfo.MaxPerObjectCasterCount, PerObjectCasterBufferData.stride)
            {
                name = "Per Object Shadow Caster Data",
                target = GraphicsBuffer.Target.Structured
            });
            resourceHandle.forwardPlusTileBuffer = renderGraph.CreateBuffer(new BufferDesc(TileCount * tileDataSize, 4)
            {
                name = "Forward+ Tiles",
                target = GraphicsBuffer.Target.Structured
            });
        }

        public override void DeclareResourceUsage(RenderGraphBuilder builder)
        {
            builder.WriteBuffer(resourceHandle.lightDataSpot);
            builder.WriteBuffer(resourceHandle.lightDataPoint);
            builder.WriteBuffer(resourceHandle.lightDataDirectional);
            builder.WriteBuffer(resourceHandle.perObjectShadowCasterData);
            builder.WriteBuffer(resourceHandle.forwardPlusTileBuffer);

            resourceHandle.shadowMapHandle = shadowMapRenderer.Record(renderGraph, builder, renderer.Context);
        }

        private void CollectPerLightData()
        {
            NativeArray<VisibleLight> visibleLights = renderer.CullingResults.visibleLights;

            directionalLightCount = 0;
            spotLightCount = 0;
            pointLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                Light light = visibleLights[i].light;
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional when directionalLightCount < RenderPipelineInfo.MaxDirectionalLightCount:
                        DirectionalLightData[directionalLightCount++] =
                            DirectionalLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataDirectional(light, i));
                        break;
                    case LightType.Spot when spotLightCount < RenderPipelineInfo.MaxSpotLightCount:
                        SetupForwardPlusSpot(spotLightCount, visibleLights[i]);
                        SpotLightData[spotLightCount++] =
                            SpotLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataSpot(light, i));
                        break;
                    case LightType.Point when pointLightCount < RenderPipelineInfo.MaxPointLightCount:
                        SetupForwardPlusPoint(pointLightCount, visibleLights[i]);
                        PointLightData[pointLightCount++] =
                            PointLightBufferData.GenerateStructuredData(visibleLights[i], light,
                                perLightDataCollector.ReservePerLightShadowDataPoint(light, i));
                        break;
                }
            }

            var visiblePerObjectShadowCasters = renderer.PerObjectShadowCasterManager.visibleCasters;
            perObjectCasterCount = 0;
            for (int i = 0; i < visiblePerObjectShadowCasters.Count; i++)
            {
                var caster = visiblePerObjectShadowCasters[i];
                if (perObjectCasterCount < RenderPipelineInfo.MaxPerObjectCasterCount)
                {
                    PerObjectCasterData[perObjectCasterCount++] =
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

        private void SetupForwardPlusSpot(int spotlightIndex, in VisibleLight visibleLight)
        {
            Rect r = visibleLight.screenRect;
            spotLightBounds[spotlightIndex] = float4(r.xMin, r.yMin, r.xMax, r.yMax);
        }

        private void SetupForwardPlusPoint(int pointlightIndex, in VisibleLight visibleLight)
        {
            Rect r = visibleLight.screenRect;
            pointLightBounds[pointlightIndex] = float4(r.xMin, r.yMin, r.xMax, r.yMax);
        }
    }
}