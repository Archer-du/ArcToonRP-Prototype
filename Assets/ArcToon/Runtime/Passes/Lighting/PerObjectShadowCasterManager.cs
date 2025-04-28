using System;
using System.Collections.Generic;
using ArcToon.Runtime.Behavior;
using ArcToon.Runtime.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class PerObjectShadowCasterManager
    {
        private static readonly HashSet<PerObjectShadowCaster> perObjectCasters = new();
        private static int perObjectShadowCasterGUID = 1;
        
        public static void Register(PerObjectShadowCaster caster)
        {
            if (perObjectCasters.Add(caster))
            {
                caster.perObjectShadowCasterID = perObjectShadowCasterGUID;
                perObjectShadowCasterGUID++;
            }
        }

        public static void Unregister(PerObjectShadowCaster caster) => perObjectCasters.Remove(caster);
        
        private readonly List<int> rendererIndexList = new();
        private readonly List<PerObjectShadowCaster> casterCullResults = new();

        public unsafe void Cull(Camera camera)
        {
            rendererIndexList.Clear();
            casterCullResults.Clear();
            if (perObjectCasters.Count <= 0)
            {
                return;
            }
            // float4* frustumCorners = stackalloc float4[CullUtilities.FrustumCornerCount];
            // CullUtilities.SetFrustumEightCorners(frustumCorners, camera);
            // PerObjectShadowCullingParams param = new PerObjectShadowCullingParams()
            // {
            //     
            // };
            foreach (var caster in perObjectCasters)
            {
                // caster.UpdateCasterInfo();
                // if (!caster.CanCastShadow(baseArgs.Usage))
                // {
                //     continue;
                // }
                // int rendererIndexInitialCount = rendererIndexList.Count;
                if (!caster.TryGetWorldBounds(out Bounds bounds))
                {
                    continue;
                }
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
                bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                if (!isVisible)
                {
                    continue;
                }

                DrawDebugBounds(bounds, Color.cyan);
                casterCullResults.Add(caster);
            }
        }

        void DrawDebugBounds(Bounds bounds, Color color)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
    
            Vector3[] vertices = new Vector3[8];
            vertices[0] = new Vector3(min.x, min.y, min.z);
            vertices[1] = new Vector3(max.x, min.y, min.z);
            vertices[2] = new Vector3(max.x, max.y, min.z);
            vertices[3] = new Vector3(min.x, max.y, min.z);
            vertices[4] = new Vector3(min.x, min.y, max.z);
            vertices[5] = new Vector3(max.x, min.y, max.z);
            vertices[6] = new Vector3(max.x, max.y, max.z);
            vertices[7] = new Vector3(min.x, max.y, max.z);
    
            Debug.DrawLine(vertices[0], vertices[1], color);
            Debug.DrawLine(vertices[1], vertices[2], color);
            Debug.DrawLine(vertices[2], vertices[3], color);
            Debug.DrawLine(vertices[3], vertices[0], color);
    
            Debug.DrawLine(vertices[4], vertices[5], color);
            Debug.DrawLine(vertices[5], vertices[6], color);
            Debug.DrawLine(vertices[6], vertices[7], color);
            Debug.DrawLine(vertices[7], vertices[4], color);
    
            Debug.DrawLine(vertices[0], vertices[4], color);
            Debug.DrawLine(vertices[1], vertices[5], color);
            Debug.DrawLine(vertices[2], vertices[6], color);
            Debug.DrawLine(vertices[3], vertices[7], color);
        }
    }
}