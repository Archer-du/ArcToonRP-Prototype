using System.Runtime.InteropServices;
using ArcToon.Runtime.Buffers;
using ArcToon.Runtime.Data;
using ArcToon.Runtime.Jobs;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Settings;
using ArcToon.Runtime.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ArcToon.Runtime.Passes
{
    public class LightingPass : RenderPassBase
    {
        public override string Name => "Lighting";

        private ShadowMapRenderer shadowMapRenderer = new();
        private PerLightDataCollector perLightDataCollector = new();

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

        public override void Setup(RenderResources resources, CameraRenderer renderer)
        {
            base.Setup(resources, renderer);

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

            // Allocate forward+ tile buffer based on current tile count
            resources.AllocateForwardPlusTileBuffer(TileCount, tileDataSize);

            perLightDataCollector.Setup(renderer.CullingResults, renderer.ShadowSettings);
            CollectPerLightData();

            shadowMapRenderer.Initialize(renderer.CullingResults, Camera, renderer.ShadowSettings, perLightDataCollector,
                renderer.PerObjectShadowCasterManager);
            shadowMapRenderer.SetupResources(resources);
        }

        public override void PrepareRendererLists(ScriptableRenderContext context)
        {
            shadowMapRenderer.BuildRendererLists(context);
        }

        public override void Execute(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalInt(InternalShader.PropertyID.DirectionalLightCount, directionalLightCount);
            commandBuffer.SetBufferData(resources.directionalLightData, DirectionalLightData,
                0, 0, directionalLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.DirectionalLightData, resources.directionalLightData);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PerObjectShadowCasterCount, perObjectCasterCount);
            commandBuffer.SetBufferData(resources.perObjectShadowCasterData, PerObjectCasterData, 
                0, 0, perObjectCasterCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PerObjectShadowCasterData, resources.perObjectShadowCasterData);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.SpotLightCount, spotLightCount);
            commandBuffer.SetBufferData(resources.spotLightData, SpotLightData,
                0, 0, spotLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.SpotLightData, resources.spotLightData);

            commandBuffer.SetGlobalInt(InternalShader.PropertyID.PointLightCount, pointLightCount);
            commandBuffer.SetBufferData(resources.pointLightData, PointLightData,
                0, 0, pointLightCount);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.PointLightData, resources.pointLightData);

            shadowMapRenderer.RenderShadowMap(commandBuffer, context);

            // block waiting for job result
            forwardPlusJobHandle.Complete();
            commandBuffer.SetBufferData(resources.forwardPlusTileBuffer, forwardPlusTileData,
                0, 0, forwardPlusTileData.Length);
            commandBuffer.SetGlobalBuffer(InternalShader.PropertyID.ForwardPlusTileData, resources.forwardPlusTileBuffer);
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