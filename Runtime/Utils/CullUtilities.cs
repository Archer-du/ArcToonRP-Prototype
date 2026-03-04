using System.Diagnostics;
using System.Runtime.CompilerServices;
using ArcToon.Runtime.Passes.Lighting;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace ArcToon.Runtime.Utils
{
    [BurstCompile]
    public static class CullUtilities
    {
        public const int FrustumCornerCount = 8;
        public const int FrustumTriangleCount = 12;
        
        private static readonly Vector3[] s_FrustumCornerBuffer = new Vector3[4];

        public static readonly int[] FrustumTriangleIndices = new int[FrustumTriangleCount * 3]
        {
            0, 3, 1,
            1, 3, 2,
            2, 3, 7,
            2, 7, 6,
            0, 5, 4,
            0, 1, 5,
            1, 2, 5,
            2, 6, 5,
            0, 7, 3,
            0, 4, 7,
            4, 7, 5,
            5, 7, 6,
        };
                
        private static readonly float4x4 s_FlipZMatrix = new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, -1, 0,
            0, 0, 0, 1
        );

        public static unsafe void SetFrustumEightCorners(float4* frustumEightCorners, Camera camera)
        {
            Transform transform = camera.transform;
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            if (camera.orthographic)
            {
                // The orthographicSize is half the size of the vertical viewing volume.
                // The horizontal size of the viewing volume depends on the aspect ratio.
                float top = camera.orthographicSize;
                float right = top * camera.aspect;

                frustumEightCorners[0] = TransformPoint(transform, -right, -top, near);
                frustumEightCorners[1] = TransformPoint(transform, -right, +top, near);
                frustumEightCorners[2] = TransformPoint(transform, +right, +top, near);
                frustumEightCorners[3] = TransformPoint(transform, +right, -top, near);
                frustumEightCorners[4] = TransformPoint(transform, -right, -top, far);
                frustumEightCorners[5] = TransformPoint(transform, -right, +top, far);
                frustumEightCorners[6] = TransformPoint(transform, +right, +top, far);
                frustumEightCorners[7] = TransformPoint(transform, +right, -top, far);
            }
            else
            {
                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.CalculateFrustumCorners.html
                // The order of the corners is lower left, upper left, upper right, lower right.
                Rect viewport = new Rect(0, 0, 1, 1);
                const Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono;

                camera.CalculateFrustumCorners(viewport, near, eye, s_FrustumCornerBuffer);
                for (int i = 0; i < 4; i++)
                {
                    frustumEightCorners[i] = TransformPoint(transform, s_FrustumCornerBuffer[i]);
                }

                camera.CalculateFrustumCorners(viewport, far, eye, s_FrustumCornerBuffer);
                for (int i = 0; i < 4; i++)
                {
                    frustumEightCorners[i + 4] = TransformPoint(transform, s_FrustumCornerBuffer[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 TransformPoint(Transform transform, float x, float y, float z)
        {
            return TransformPoint(transform, new Vector3(x, y, z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 TransformPoint(Transform transform, Vector3 point)
        {
            return new float4(transform.TransformPoint(point), 1);
        }

                
        private ref struct TriangleData
        {
            public float3 P0;
            public float3 P1;
            public float3 P2;
            public bool IsCulled;
        }

        private enum EdgeType
        {
            Min,
            Max,
        }

        private ref struct EdgeData
        {
            public int ComponentIndex;
            public float Value;
            public EdgeType Type;
        }
        
        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public static bool ComputePerObjectShadowMatricesAndCullingPrimitives(in PerObjectShadowCullingParams args,
            out float4x4 viewMatrix, out float4x4 projectionMatrix, out float width, out float height)
        {
            float3 aabbCenter = (args.AABBMin + args.AABBMax) * 0.5f;
            float3 cameraUp = args.cameraLocalToWorldMatrix.c1.xyz;
            float3 lightForward = args.lightLocalToWorldMatrix.c2.xyz;
            quaternion lightRotation = quaternion.LookRotation(lightForward, cameraUp);
            viewMatrix = inverse(float4x4.TRS(aabbCenter, lightRotation, 1));
            viewMatrix = mul(s_FlipZMatrix, viewMatrix);

            return GetProjectionMatrix(in args, in viewMatrix, out projectionMatrix, out width, out height);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetProjectionMatrix(in PerObjectShadowCullingParams args,
            in float4x4 viewMatrix, out float4x4 projectionMatrix, out float width, out float height)
        {
            GetViewSpaceShadowAABB(in args, in viewMatrix, out float3 shadowMin, out float3 shadowMax);

            if (AdjustViewSpaceShadowAABB(in args, in viewMatrix, ref shadowMin, ref shadowMax))
            {
                // DebugDrawViewSpaceAABB(in shadowMin, in shadowMax, in viewMatrix, Color.blue);
            }

            float length = max(shadowMax.x, shadowMax.y);
            width = length * 2;
            height = length * 2;
            float zNear = -shadowMax.z;
            float zFar = -shadowMin.z;
            projectionMatrix = float4x4.Ortho(width, height, zNear, zFar);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetViewSpaceShadowAABB(in PerObjectShadowCullingParams args,
            in float4x4 viewMatrix, out float3 shadowMin, out float3 shadowMax)
        {
            // 8 个顶点
            float4* points = stackalloc float4[8]
            {
                float4(args.AABBMin, 1),
                float4(args.AABBMax.x, args.AABBMin.y, args.AABBMin.z, 1),
                float4(args.AABBMin.x, args.AABBMax.y, args.AABBMin.z, 1),
                float4(args.AABBMin.x, args.AABBMin.y, args.AABBMax.z, 1),
                float4(args.AABBMax.x, args.AABBMax.y, args.AABBMin.z, 1),
                float4(args.AABBMax.x, args.AABBMin.y, args.AABBMax.z, 1),
                float4(args.AABBMin.x, args.AABBMax.y, args.AABBMax.z, 1),
                float4(args.AABBMax, 1),
            };
        
            shadowMin = float3(float.PositiveInfinity);
            shadowMax = float3(float.NegativeInfinity);
        
            for (int i = 0; i < 8; i++)
            {
                float3 p = mul(viewMatrix, points[i]).xyz;
                shadowMin = min(shadowMin, p);
                shadowMax = max(shadowMax, p);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool AdjustViewSpaceShadowAABB(in PerObjectShadowCullingParams args,
            in float4x4 viewMatrix, ref float3 shadowMin, ref float3 shadowMax)
        {
            float3* frustumCorners = stackalloc float3[FrustumCornerCount];
        
            for (int i = 0; i < FrustumCornerCount; i++)
            {
                frustumCorners[i] = mul(viewMatrix, args.frustumCorners[i]).xyz;
            }
        
            EdgeData* edges = stackalloc EdgeData[4]
            {
                new() { ComponentIndex = 0, Value = shadowMin.x, Type = EdgeType.Min },
                new() { ComponentIndex = 0, Value = shadowMax.x, Type = EdgeType.Max },
                new() { ComponentIndex = 1, Value = shadowMin.y, Type = EdgeType.Min },
                new() { ComponentIndex = 1, Value = shadowMax.y, Type = EdgeType.Max },
            };
        
            // 最坏情况：1 个三角形被拆成 2**4 = 16 个三角形
            TriangleData* triangles = stackalloc TriangleData[16];
        
            bool isVisibleXY = false;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
        
            for (int i = 0; i < FrustumTriangleCount; i++)
            {
                int triangleCount = 1;
                triangles[0].P0 = frustumCorners[FrustumTriangleIndices[i * 3 + 0]];
                triangles[0].P1 = frustumCorners[FrustumTriangleIndices[i * 3 + 1]];
                triangles[0].P2 = frustumCorners[FrustumTriangleIndices[i * 3 + 2]];
                triangles[0].IsCulled = false;
        
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < triangleCount; k++)
                    {
                        CullTriangle(triangles, ref k, ref triangleCount, in edges[j]);
                    }
                }
        
                for (int j = 0; j < triangleCount; j++)
                {
                    ref TriangleData tri = ref triangles[j];
        
                    if (tri.IsCulled)
                    {
                        continue;
                    }
        
                    // DebugDrawViewSpaceTriangle(in tri, in viewMatrix, Color.red);
        
                    isVisibleXY = true;
                    minZ = min(minZ, min(tri.P0.z, min(tri.P1.z, tri.P2.z)));
                    maxZ = max(maxZ, max(tri.P0.z, max(tri.P1.z, tri.P2.z)));
                }
            }
        
            if (isVisibleXY && minZ < shadowMax.z && maxZ > shadowMin.z)
            {
                // 为了阴影的完整性，不应该修改 shadowMax.z
                shadowMin.z = max(shadowMin.z, minZ);
                return true;
            }
        
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CullTriangle([NoAlias] TriangleData* triangles,
            ref int triangleIndex, ref int triangleCount, in EdgeData edge)
        {
            ref TriangleData tri = ref triangles[triangleIndex];

            if (tri.IsCulled)
            {
                return;
            }

            int insideInfo = 0b000;
            if (IsPointInsideEdge(in edge, in tri.P0)) insideInfo |= 0b001;
            if (IsPointInsideEdge(in edge, in tri.P1)) insideInfo |= 0b010;
            if (IsPointInsideEdge(in edge, in tri.P2)) insideInfo |= 0b100;

            bool isOnePointInside;

            // 将在边界里的点移动到 [P0, P1, P2] 列表的前面
            switch (insideInfo)
            {
                // 没有点在里面
                case 0b000: tri.IsCulled = true; return;

                // 有一个点在里面
                case 0b001: isOnePointInside = true; break;
                case 0b010: isOnePointInside = true; Swap(ref tri.P0, ref tri.P1); break;
                case 0b100: isOnePointInside = true; Swap(ref tri.P0, ref tri.P2); break;

                // 有两个点在里面
                case 0b011: isOnePointInside = false; break;
                case 0b101: isOnePointInside = false; Swap(ref tri.P1, ref tri.P2); break;
                case 0b110: isOnePointInside = false; Swap(ref tri.P0, ref tri.P2); break;

                // 所有点在里面
                case 0b111: return;

                // Unreachable
                default: Debug.LogError("Unknown triangleInsideInfo"); return;
            }

            if (isOnePointInside)
            {
                // 只有 P0 在里面
                float3 v01 = tri.P1 - tri.P0;
                float3 v02 = tri.P2 - tri.P0;

                float dist = edge.Value - tri.P0[edge.ComponentIndex];
                tri.P1 = v01 * rcp(v01[edge.ComponentIndex]) * dist + tri.P0;
                tri.P2 = v02 * rcp(v02[edge.ComponentIndex]) * dist + tri.P0;
            }
            else
            {
                // 只有 P2 在外面
                float3 v20 = tri.P0 - tri.P2;
                float3 v21 = tri.P1 - tri.P2;

                float dist = edge.Value - tri.P2[edge.ComponentIndex];
                float3 p0 = v20 * rcp(v20[edge.ComponentIndex]) * dist + tri.P2;
                float3 p1 = v21 * rcp(v21[edge.ComponentIndex]) * dist + tri.P2;

                // 第一个三角形
                tri.P2 = p0;

                // 把下一个三角形拷贝到列表最后新的位置上，然后把新三角形数据写入到下个位置
                // 新的三角形必定三个点都在边界内，所以 ++triangleIndex 跳过检查
                ref TriangleData newTri = ref triangles[++triangleIndex];
                triangles[triangleCount++] = newTri;

                // 第二个三角形
                newTri.P0 = p0;
                newTri.P1 = tri.P1;
                newTri.P2 = p1;
                newTri.IsCulled = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointInsideEdge(in EdgeData edge, in float3 p)
        {
            // EdgeType.Min => p[edge.ComponentIndex] > edge.Value
            // EdgeType.Max => p[edge.ComponentIndex] < edge.Value

            float delta = p[edge.ComponentIndex] - edge.Value;
            return select(-delta, delta, edge.Type == EdgeType.Min) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref float3 a, ref float3 b) => (a, b) = (b, a);

        [Conditional("UNITY_EDITOR")]
        private static unsafe void DebugDrawViewSpaceAABB(in float3 aabbMin, in float3 aabbMax,
            in float4x4 viewMatrix, Color color)
        {
            float3* points = stackalloc float3[8]
            {
                float3(aabbMin),
                float3(aabbMax.x, aabbMin.y, aabbMin.z),
                float3(aabbMin.x, aabbMax.y, aabbMin.z),
                float3(aabbMin.x, aabbMin.y, aabbMax.z),
                float3(aabbMax.x, aabbMax.y, aabbMin.z),
                float3(aabbMax.x, aabbMin.y, aabbMax.z),
                float3(aabbMin.x, aabbMax.y, aabbMax.z),
                float3(aabbMax),
            };

            float4x4 invViewMatrix = inverse(viewMatrix);

            // View Space 的 AABB 在 World Space 可能是斜的，所以 8 个点都要转换到 World Space
            for (int i = 0; i < 8; i++)
            {
                points[i] = mul(invViewMatrix, float4(points[i], 1)).xyz;
            }

            Debug.DrawLine(points[0], points[1], color);
            Debug.DrawLine(points[0], points[2], color);
            Debug.DrawLine(points[0], points[3], color);
            Debug.DrawLine(points[1], points[4], color);
            Debug.DrawLine(points[1], points[5], color);
            Debug.DrawLine(points[2], points[4], color);
            Debug.DrawLine(points[2], points[6], color);
            Debug.DrawLine(points[3], points[5], color);
            Debug.DrawLine(points[3], points[6], color);
            Debug.DrawLine(points[4], points[7], color);
            Debug.DrawLine(points[5], points[7], color);
            Debug.DrawLine(points[6], points[7], color);
            Debug.DrawLine(points[0], points[7], Color.cyan);
        }

        [Conditional("UNITY_EDITOR")]
        private static void DebugDrawViewSpaceTriangle(in TriangleData triangle,
            in float4x4 viewMatrix, Color color)
        {
            float4x4 invViewMatrix = inverse(viewMatrix);
            float3 w0 = mul(invViewMatrix, float4(triangle.P0, 1)).xyz;
            float3 w1 = mul(invViewMatrix, float4(triangle.P1, 1)).xyz;
            float3 w2 = mul(invViewMatrix, float4(triangle.P2, 1)).xyz;

            Debug.DrawLine(w0, w1, color);
            Debug.DrawLine(w0, w2, color);
            Debug.DrawLine(w1, w2, color);
        }
    }
}