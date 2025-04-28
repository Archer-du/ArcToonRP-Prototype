using System;
using System.Collections.Generic;
using ArcToon.Runtime.Passes.Lighting;
using ArcToon.Runtime.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Behavior
{
    public class PerObjectShadowCaster : MonoBehaviour
    {
        [NonSerialized] public int perObjectShadowCasterID = -1;

        [NonSerialized] public List<PerObjectShadowCasterRenderer> perObjectCasterRenderers = new();
        [NonSerialized] public List<Renderer> renderers = new();

        private readonly Lazy<MaterialPropertyBlock> propertyBlock = new();

        private void OnEnable()
        {
            PerObjectShadowCasterManager.Register(this);
            UpdateCasterInfo();
        }

        private void OnDisable()
        {
            PerObjectShadowCasterManager.Unregister(this);
            renderers.Clear();
            perObjectCasterRenderers.Clear();
            if (propertyBlock.IsValueCreated)
            {
                propertyBlock.Value.Clear();
            }
        }

        private void Update()
        {
            UpdateCasterInfo();
        }

        public void UpdateCasterInfo()
        {
            UpdateRendererList();
            UpdateRendererMaterialProperties();
            UpdateShadowCasterRendererList();
        }

        private void UpdateRendererList()
        {
            renderers.Clear();
            GetComponentsInChildren(true, renderers);
        }

        private void UpdateRendererMaterialProperties()
        {
            foreach (var renderer in renderers)
            {
                List<Material> materialList = ListPool<Material>.Get();
                try
                {
                    // instantiate materials
                    renderer.GetMaterials(materialList);
                    for (int i = 0; i < materialList.Count; i++)
                    {
                        Material material = materialList[i];
                        material.SetFloat(PropertyIDs._PerObjectShadowCasterID, perObjectShadowCasterID);
                    }
                }
                finally
                {
                    ListPool<Material>.Release(materialList);
                }
            }
        }

        private void UpdateShadowCasterRendererList()
        {
            perObjectCasterRenderers.Clear();

            for (int i = 0; i < renderers.Count; i++)
            {
                if (CheckValidShadowCasterRenderer(renderers[i]))
                {
                    perObjectCasterRenderers.Add(new PerObjectShadowCasterRenderer(renderers[i],
                        GetShadowCasterRendererDrawCallData(renderers[i])));
                }
            }

            UpdateRendererMaterialProperties();
        }

        public bool TryGetWorldBounds(out Bounds worldBounds)
        {
            worldBounds = default;
            bool firstBounds = true;

            for (int i = 0; i < perObjectCasterRenderers.Count; i++)
            {
                var entry = perObjectCasterRenderers[i];

                if (firstBounds)
                {
                    worldBounds = entry.renderer.bounds;
                    firstBounds = false;
                }
                else
                {
                    worldBounds.Encapsulate(entry.renderer.bounds);
                }
            }

            return !firstBounds;
        }

        private bool CheckValidShadowCasterRenderer(Renderer renderer)
        {
            bool haveShadowCasterPass = false;
            List<Material> materialList = ListPool<Material>.Get();
            try
            {
                renderer.GetSharedMaterials(materialList);
                foreach (var material in materialList)
                {
                    if (TryGetShadowCasterPass(material, out int passIndex))
                    {
                        haveShadowCasterPass = true;
                        break;
                    }
                }
            }
            finally
            {
                ListPool<Material>.Release(materialList);
            }

            return haveShadowCasterPass && renderer.isVisible && renderer.enabled &&
                   renderer.gameObject.activeInHierarchy && renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        private List<DrawCallData> GetShadowCasterRendererDrawCallData(Renderer renderer)
        {
            List<DrawCallData> drawCallList = new();
            List<Material> materialList = ListPool<Material>.Get();
            try
            {
                renderer.GetSharedMaterials(materialList);
                for (int i = 0; i < materialList.Count; i++)
                {
                    var material = materialList[i];
                    if (TryGetShadowCasterPass(material, out int passIndex))
                    {
                        drawCallList.Add(new(material, i, passIndex));
                    }
                }
            }
            finally
            {
                ListPool<Material>.Release(materialList);
            }

            return drawCallList;
        }

        private bool TryGetShadowCasterPass(Material material, out int passIndex)
        {
            Shader shader = material.shader;
            for (int i = 0; i < shader.passCount; i++)
            {
                if (shader.FindPassTagValue(i, ShaderTagIds.LightMode) == ShaderTagIds.ShadowCaster)
                {
                    passIndex = i;
                    return true;
                }
            }

            passIndex = -1;
            return false;
        }

        private static class ShaderTagIds
        {
            public static readonly ShaderTagId LightMode = MemberNameHelpers.ShaderTagId();
            public static readonly ShaderTagId ShadowCaster = MemberNameHelpers.ShaderTagId();
        }

        private static class PropertyIDs
        {
            public static readonly int _PerObjectShadowCasterID = MemberNameHelpers.ShaderPropertyID();
        }
    }
}