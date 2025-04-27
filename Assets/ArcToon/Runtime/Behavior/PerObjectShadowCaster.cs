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

        public struct PerObjectShadowCasterRenderer
        {
            public Renderer renderer;

            public PerObjectShadowCasterRenderer(Renderer renderer)
            {
                this.renderer = renderer;
            }
        }

        [NonSerialized] public List<PerObjectShadowCasterRenderer> perObjectCasterRenderers = new();
        [NonSerialized] public List<Renderer> renderers = new();
        
        private readonly Lazy<MaterialPropertyBlock> propertyBlock = new();

        private void OnEnable()
        {
            PerObjectShadowCasterManager.Register(this);
            UpdateRendererList();
            UpdateRendererMaterialProperties();
            UpdateShadowCasterRendererList();
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
#if UNITY_EDITOR
            UpdateRendererList();
            UpdateRendererMaterialProperties();
            UpdateShadowCasterRendererList();
#else
            UpdateRendererMaterialProperties();
#endif
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

            foreach (var renderer in renderers)
            {
                List<Material> materialList = ListPool<Material>.Get();
                try
                {
                    renderer.GetSharedMaterials(materialList);
                    for (int i = 0; i < materialList.Count; i++)
                    {
                        Material material = materialList[i];
                        if (TryGetShadowCasterPass(material))
                        {
                            perObjectCasterRenderers.Add(new PerObjectShadowCasterRenderer(renderer));
                        }
                    }
                }
                finally
                {
                    ListPool<Material>.Release(materialList);
                }
            }

            UpdateRendererMaterialProperties();
        }

        private bool TryGetShadowCasterPass(Material material)
        {
            Shader shader = material.shader;
            for (int i = 0; i < shader.passCount; i++)
            {
                if (shader.FindPassTagValue(i, ShaderTagIds.LightMode) == ShaderTagIds.ShadowCaster)
                {
                    return true;
                }
            }
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