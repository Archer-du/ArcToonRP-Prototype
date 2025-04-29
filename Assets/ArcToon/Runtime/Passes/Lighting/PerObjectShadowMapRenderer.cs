// using ArcToon.Runtime.Buffers;
// using ArcToon.Runtime.Data;
// using ArcToon.Runtime.Settings;
// using ArcToon.Runtime.Utils;
// using Unity.Collections;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RenderGraphModule;
//
// namespace ArcToon.Runtime.Passes.Lighting
// {
//     public class PerObjectShadowMapRenderer
//     {
//         struct RenderInfo
//         {
//             public RendererListHandle handle;
//
//             public Matrix4x4 view, projection;
//         }
//
//         #region Global
//         private const int maxTilesPerLight = 6;
//
//         private static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
//         private static int shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");
//         
//         private static readonly GlobalKeyword[] shadowMaskKeywords =
//         {
//             GlobalKeyword.Create("_SHADOW_MASK_ALWAYS"),
//             GlobalKeyword.Create("_SHADOW_MASK_DISTANCE"),
//         };
//         private static readonly GlobalKeyword[] filterKeywords =
//         {
//             GlobalKeyword.Create("_PCF3X3"),
//             GlobalKeyword.Create("_PCF5X5"),
//             GlobalKeyword.Create("_PCF7X7"),
//         };
//         
//         private CommandBuffer commandBuffer;
//
//         private CullingResults cullingResults;
//
//         private ShadowSettings settings;
//         
//         NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight;
//
//         NativeArray<ShadowSplitData> shadowSplitDataPerLight;
//
//         private bool useShadowMask;
//         #endregion
//         
//         #region Directional Light
//         private const int maxShadowedDirectionalLightCount = 4;
//         private const int maxCascades = 4;
//
//         private static Matrix4x4[] directionalShadowVPMatrices =
//             new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
//         private static int directionalShadowVPMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
//
//         private static readonly ShadowCascadeBufferData[] cascadeShadowData =
//             new ShadowCascadeBufferData[maxCascades];
//         private static int cascadeShadowDataID = Shader.PropertyToID("_ShadowCascadeData");
//         
//         private static Vector4 directionalAtlasSizes;
//         private static int directionalShadowAtlasSizeID = Shader.PropertyToID("_DirectionalShadowAtlasSize");
//         
//         private static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
//
//         private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
//
//         private static readonly GlobalKeyword[] cascadeBlendKeywords =
//         {
//             GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
//         };
//
//         private TextureHandle directionalAtlas;
//
//         private BufferHandle cascadeShadowDataHandle;
//         private BufferHandle directionalShadowMatricesHandle;
//
//         private ShadowMapRenderer.RenderInfo[] directionalRenderInfo =
//             new ShadowMapRenderer.RenderInfo[maxShadowedDirectionalLightCount * maxCascades];
//
//         private int directionalSplit, directionalTileSize;
//         
//         private int shadowedDirectionalLightCount;
//         #endregion
//
//         public ShadowMapHandles GetShadowMapHandles(
//             RenderGraph renderGraph,
//             RenderGraphBuilder builder,
//             ScriptableRenderContext context)
//         {
//             int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
//             var desc = new TextureDesc(atlasSize, atlasSize)
//             {
//                 depthBufferBits = DepthBits.Depth32,
//                 isShadowMap = true,
//                 name = "Directional Shadow Atlas"
//             };
//             directionalAtlas = shadowedDirectionalLightCount > 0
//                 ? builder.WriteTexture(renderGraph.CreateTexture(desc))
//                 : renderGraph.defaultResources.defaultShadowTexture;
//
//             cascadeShadowDataHandle = builder.WriteBuffer(
//                 renderGraph.CreateBuffer(new BufferDesc(maxCascades, ShadowCascadeBufferData.stride)
//                 {
//                     name = "Shadow Cascades",
//                     target = GraphicsBuffer.Target.Structured
//                 })
//             );
//
//             directionalShadowMatricesHandle = builder.WriteBuffer(
//                 renderGraph.CreateBuffer(new BufferDesc(maxShadowedDirectionalLightCount * maxCascades, 4 * 16)
//                 {
//                     name = "Directional Shadow Matrices",
//                     target = GraphicsBuffer.Target.Structured
//                 })
//             );
//
//             BuildRendererLists(renderGraph, builder, context);
//
//             return new ShadowMapHandles(directionalAtlas, spotAtlas, pointAtlas,
//                 cascadeShadowDataHandle, directionalShadowMatricesHandle, spotShadowDataHandle, pointShadowDataHandle);
//         }
//
//
//         void BuildRendererLists(
//             RenderGraph renderGraph,
//             RenderGraphBuilder builder,
//             ScriptableRenderContext context)
//         {
//             if (shadowedDirectionalLightCount > 0)
//             {
//                 int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
//                 int tiles =
//                     shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount;
//                 directionalSplit = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
//                 directionalTileSize = atlasSize / directionalSplit;
//
//                 for (int i = 0; i < shadowedDirectionalLightCount; i++)
//                 {
//                     BuildDirectionalRendererList(i, renderGraph, builder);
//                 }
//             }
//
//             if (shadowedDirectionalLightCount + shadowedSpotLightCount + shadowedPointLightCount > 0)
//             {
//                 context.CullShadowCasters(
//                     cullingResults,
//                     new ShadowCastersCullingInfos
//                     {
//                         perLightInfos = cullingInfoPerLight,
//                         splitBuffer = shadowSplitDataPerLight
//                     });
//             }
//         }
//
//         void BuildDirectionalRendererList(
//             int shadowedDirectionalLightIndex,
//             RenderGraph renderGraph,
//             RenderGraphBuilder builder)
//         {
//             ShadowMapRenderer.ShadowMapDataDirectional lightShadowData = shadowMapDataDirectionals[shadowedDirectionalLightIndex];
//             var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex)
//             {
//                 useRenderingLayerMaskTest = true
//             };
//             int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
//             Vector3 ratios = settings.directionalCascadeShadow.CascadeRatios;
//             float cullingFactor = Mathf.Max(0f, 1f - settings.directionalCascadeShadow.edgeFade);
//             int splitOffset = lightShadowData.visibleLightIndex * maxTilesPerLight;
//             for (int i = 0; i < cascadeCount; i++)
//             {
//                 ref ShadowMapRenderer.RenderInfo info = ref directionalRenderInfo[shadowedDirectionalLightIndex * maxCascades + i];
//                 cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
//                     lightShadowData.visibleLightIndex, i, cascadeCount, ratios,
//                     directionalTileSize, lightShadowData.nearPlaneOffset, out info.view,
//                     out info.projection, out ShadowSplitData splitData);
//                 splitData.shadowCascadeBlendCullingFactor = cullingFactor;
//                 shadowSplitDataPerLight[splitOffset + i] = splitData;
//                 if (shadowedDirectionalLightIndex == 0)
//                 {
//                     // for performance: compare the square distance from the sphere's center with a surface fragment square radius
//                     cascadeShadowData[i] = new ShadowCascadeBufferData(
//                         splitData.cullingSphere,
//                         directionalTileSize, settings.filterSize);
//                 }
//
//                 info.handle = builder.UseRendererList(renderGraph.CreateShadowRendererList(ref shadowSettings));
//             }
//
//             cullingInfoPerLight[lightShadowData.visibleLightIndex] =
//                 new LightShadowCasterCullingInfo
//                 {
//                     projectionType = BatchCullingProjectionType.Orthographic,
//                     splitRange = new RangeInt(splitOffset, cascadeCount)
//                 };
//         }
//
//         public void Setup(CullingResults cullingResults,
//             ShadowSettings settings
//         )
//         {
//             this.cullingResults = cullingResults;
//             this.settings = settings;
//
//             shadowedDirectionalLightCount = shadowedSpotLightCount = shadowedPointLightCount = 0;
//
//             useShadowMask = false;
//
//             cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(
//                 cullingResults.visibleLights.Length, Allocator.Temp);
//             shadowSplitDataPerLight = new NativeArray<ShadowSplitData>(
//                 cullingInfoPerLight.Length * maxTilesPerLight,
//                 Allocator.Temp, NativeArrayOptions.UninitializedMemory);
//         }
//
//         struct ShadowMapDataDirectional
//         {
//             public int visibleLightIndex;
//             public float slopeScaleBias;
//             public float nearPlaneOffset;
//         }
//
//         private ShadowMapRenderer.ShadowMapDataDirectional[] shadowMapDataDirectionals =
//             new ShadowMapRenderer.ShadowMapDataDirectional[maxShadowedDirectionalLightCount];
//         
//         public Vector4 ReservePerLightShadowDataDirectional(Light light, int visibleLightIndex)
//         {
//             if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
//             {
//                 LightBakingOutput lightBaking = light.bakingOutput;
//                 float maskChannel = -1;
//                 if (lightBaking is
//                     { lightmapBakeType: LightmapBakeType.Mixed, mixedLightingMode: MixedLightingMode.Shadowmask })
//                 {
//                     useShadowMask = true;
//                     maskChannel = lightBaking.occlusionMaskChannel;
//                 }
//
//                 // only baked shadows are used
//                 if (shadowedDirectionalLightCount >= maxShadowedDirectionalLightCount ||
//                     !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
//                 {
//                     // a trick to only sample baked shadow
//                     return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
//                 }
//
//                 int shadowedDirectionalLightIndex = shadowedDirectionalLightCount++;
//                 shadowMapDataDirectionals[shadowedDirectionalLightIndex] =
//                     new ShadowMapRenderer.ShadowMapDataDirectional
//                     {
//                         visibleLightIndex = visibleLightIndex,
//                         // TODO: interpreting light settings differently than their original purpose, use additional data instead
//                         slopeScaleBias = light.shadowBias,
//                         nearPlaneOffset = light.shadowNearPlane,
//                     };
//                 return new Vector4(
//                     light.shadowStrength,
//                     shadowedDirectionalLightIndex * settings.directionalCascadeShadow.cascadeCount,
//                     light.shadowNormalBias, maskChannel
//                 );
//             }
//
//             return new Vector4(0f, 0f, 0f, -1f);
//         }
//
//
//         public void RenderShadowMap(RenderGraphContext context)
//         {
//             commandBuffer = context.cmd;
//
//             if (shadowedDirectionalLightCount > 0)
//             {
//                 RenderDirectionalShadowMap();
//             }
//
//             commandBuffer.SetGlobalDepthBias(0f, 0f);
//             commandBuffer.SetGlobalBuffer(
//                 cascadeShadowDataID, cascadeShadowDataHandle);
//             commandBuffer.SetGlobalBuffer(
//                 directionalShadowVPMatricesID, directionalShadowMatricesHandle);
//             commandBuffer.SetGlobalBuffer(spotShadowDataID, spotShadowDataHandle);
//             commandBuffer.SetGlobalBuffer(pointShadowDataID, pointShadowDataHandle);
//
//             commandBuffer.SetGlobalTexture(dirShadowAtlasID, directionalAtlas);
//             commandBuffer.SetGlobalTexture(spotShadowAtlasID, spotAtlas);
//             commandBuffer.SetGlobalTexture(pointShadowAtlasID, pointAtlas);
//
//             commandBuffer.SetKeywords(filterKeywords, (int)settings.filterQuality - 1);
//
//             commandBuffer.SetKeywords(shadowMaskKeywords,
//                 useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
//
//             commandBuffer.SetGlobalInt(cascadeCountId,
//                 shadowedDirectionalLightCount > 0 ? settings.directionalCascadeShadow.cascadeCount : -1);
//
//             float f = 1f - settings.directionalCascadeShadow.edgeFade;
//             commandBuffer.SetGlobalVector(shadowDistanceFadeID,
//                 new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
//
//             context.renderContext.ExecuteCommandBuffer(commandBuffer);
//             commandBuffer.Clear();
//         }
//
//         void RenderDirectionalShadowMap()
//         {
//             int atlasSize = (int)settings.directionalCascadeShadow.atlasSize;
//             directionalAtlasSizes.x = directionalAtlasSizes.y = 1f / atlasSize;
//             directionalAtlasSizes.z = directionalAtlasSizes.w = atlasSize;
//
//             commandBuffer.BeginSample("Directional Shadows");
//             commandBuffer.SetRenderTarget(
//                 directionalAtlas,
//                 RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
//             );
//             commandBuffer.ClearRenderTarget(true, false, Color.clear);
//             commandBuffer.SetGlobalFloat(shadowPancakingID, 1f);
//             for (int i = 0; i < shadowedDirectionalLightCount; i++)
//             {
//                 RenderDirectionalShadowSplitTile(i);
//             }
//
//             commandBuffer.SetGlobalVector(directionalShadowAtlasSizeID, directionalAtlasSizes);
//             commandBuffer.SetBufferData(
//                 cascadeShadowDataHandle, cascadeShadowData,
//                 0, 0, settings.directionalCascadeShadow.cascadeCount);
//
//             commandBuffer.SetBufferData(
//                 directionalShadowMatricesHandle, directionalShadowVPMatrices,
//                 0, 0, shadowedDirectionalLightCount * settings.directionalCascadeShadow.cascadeCount);
//             commandBuffer.SetKeywords(
//                 cascadeBlendKeywords, (int)settings.directionalCascadeShadow.blendMode - 1
//             );
//             commandBuffer.EndSample("Directional Shadows");
//         }
//
//         void RenderDirectionalShadowSplitTile(int shadowedDirectionalLightIndex)
//         {
//             int cascadeCount = settings.directionalCascadeShadow.cascadeCount;
//             int tileOffset = shadowedDirectionalLightIndex * cascadeCount;
//             float tileScale = 1.0f / directionalSplit;
//             commandBuffer.SetGlobalDepthBias(0f,
//                 shadowMapDataDirectionals[shadowedDirectionalLightIndex].slopeScaleBias);
//             for (int i = 0; i < cascadeCount; i++)
//             {
//                 ShadowMapRenderer.RenderInfo info = directionalRenderInfo[shadowedDirectionalLightIndex * maxCascades + i];
//                 int tileIndex = tileOffset + i;
//                 Vector2 offset = commandBuffer.SetTileViewport(tileIndex, directionalSplit, directionalTileSize);
//
//                 directionalShadowVPMatrices[tileIndex] =
//                     ShadowMapHelpers.ConvertToAtlasMatrix(info.projection * info.view, offset, tileScale);
//
//                 commandBuffer.SetViewProjectionMatrices(info.view, info.projection);
//                 commandBuffer.DrawRendererList(info.handle);
//             }
//         }
//
//     }
// }