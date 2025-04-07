using ArcToon.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime
{
    public class ShadowRenderer
    {
        private const string bufferName = "Shadows";

        private ScriptableRenderContext context;
        public CommandBuffer commandBuffer;

        private CullingResults cullingResults;

        private ShadowSettings settings;

        private const int maxShadowedDirectionalLightCount = 4;
        private const int maxCascades = 4;

        private int ShadowedDirectionalLightCount;

        struct DirectionalLightShadowData
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private DirectionalLightShadowData[] directionalLightShadowData =
            new DirectionalLightShadowData[maxShadowedDirectionalLightCount];

        private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        private static Vector4[] cascadeData = new Vector4[maxCascades];
        private static Matrix4x4[] dirShadowVPMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int dirShadowVPMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

        public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, CullingResults cullingResults,
            ShadowSettings settings
        )
        {
            this.context = context;
            this.commandBuffer = commandBuffer;
            this.cullingResults = cullingResults;
            this.settings = settings;

            ShadowedDirectionalLightCount = 0;
        }

        public Vector3 ReserveDirectionalShadowData(Light light, int visibleLightIndex)
        {
            if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
                light.shadows != LightShadows.None &&
                light.shadowStrength > 0f &&
                cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)
               )
            {
                directionalLightShadowData[ShadowedDirectionalLightCount] =
                    new DirectionalLightShadowData
                    {
                        visibleLightIndex = visibleLightIndex,
                        // TODO: interpreting light settings differently than their original purpose, use additional data instead
                        slopeScaleBias = light.shadowBias,
                        nearPlaneOffset = light.shadowNearPlane,
                    };
                return new Vector3(
                    light.shadowStrength,
                    settings.directionalShadowCascade.cascadeCount * ShadowedDirectionalLightCount++,
                    light.shadowNormalBias
                );
            }

            return Vector3.zero;
        }

        public void Render()
        {
            if (ShadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                // for capability
                commandBuffer.GetTemporaryRT(
                    dirShadowAtlasId, 1, 1,
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
                );
            }
        }

        void RenderDirectionalShadows()
        {
            int atlasSize = (int)settings.directionalShadowCascade.atlasSize;
            commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(
                dirShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            // TODO: config
            int tiles = ShadowedDirectionalLightCount * settings.directionalShadowCascade.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < ShadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadowSplitTile(i, split, tileSize);
            }

            // for lighting calculation
            commandBuffer.SetGlobalInt(cascadeCountId, settings.directionalShadowCascade.cascadeCount);
            commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            commandBuffer.SetGlobalMatrixArray(dirShadowVPMatricesId, dirShadowVPMatrices);
            float f = 1f - settings.directionalShadowCascade.cascadeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadeId,
                new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f))
            );
        }

        void RenderDirectionalShadowSplitTile(int shadowedLightIndex, int split, int tileSize)
        {
            DirectionalLightShadowData lightShadowData = directionalLightShadowData[shadowedLightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, lightShadowData.visibleLightIndex);
            int cascadeCount = settings.directionalShadowCascade.cascadeCount;
            Vector3 ratios = settings.directionalShadowCascade.CascadeRatios;
            int tileOffset = shadowedLightIndex * cascadeCount;
            for (int i = 0; i < cascadeCount; i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    lightShadowData.visibleLightIndex,
                    i, cascadeCount, ratios, tileSize,
                    lightShadowData.nearPlaneOffset,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData
                );
                // for performance: the cascades of all directional lights are equivalent
                if (shadowedLightIndex == 0)
                {
                    // for performance: compare the square distance from the sphere's center with a surface fragment square radius
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                shadowSettings.splitData = splitData;
                int tileIndex = tileOffset + i;
                dirShadowVPMatrices[tileIndex] = ConvertToAtlasMatrix(
                    projectionMatrix * viewMatrix,
                    SetTileViewport(tileIndex, split, tileSize),
                    split
                );
                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                commandBuffer.SetGlobalDepthBias(0f, lightShadowData.slopeScaleBias);
                commandBuffer.DrawRendererList(context.CreateShadowRendererList(ref shadowSettings));
                commandBuffer.SetGlobalDepthBias(0f, 0);
            }
        }

        Vector2 SetTileViewport(int tileIndex, int split, float tileSize)
        {
            var offset = new Vector2(tileIndex % split, tileIndex / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        void SetCascadeData(int shadowedLightIndex, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[shadowedLightIndex] = cullingSphere;
            cascadeData[shadowedLightIndex] = new Vector4(
                1f / cullingSphere.w,
                texelSize * 1.4142136f
            );
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }

        public void CleanUp()
        {
            commandBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
        }
    }
}