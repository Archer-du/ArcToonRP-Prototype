using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Sections;
using ArcToon.Editor.ShaderEditor.Panels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.ShaderEditor
{
    public class ArcToonShaderGUI : ShaderGUI
    {
        private const string ToonUnlitShaderName = "ArcToon/ToonUnlit";

        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;

        // Shared per-OnGUI context injected into every section.
        // RegionIDSection is drawn first and writes SelectedRegion; downstream sections consume it.
        private readonly SectionContext sectionContext = new SectionContext();

        private RegionIDSection regionIDSection = null;
        private BaseFoldoutShaderPanel generalFoldoutPanel = null;
        private BaseFoldoutShaderPanel shadowFoldoutPanel = null;
        private BaseFoldoutShaderPanel pbrFoldoutPanel = null;
        private BaseFoldoutShaderPanel toonFoldoutPanel = null;
        private BaseFoldoutShaderPanel engineFoldoutPanel = null;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            EditorGUI.BeginChangeCheck();
            editor = materialEditor;
            materials = materialEditor.targets;
            properties = materialProperties;
            
            TryInitGUIPanels();

            sectionContext.Reset();

            var targetMaterials = MaterialEditorUtils.GetTargetMaterials(materialEditor);
            bool isUnlit = IsToonUnlit(targetMaterials);
            regionIDSection.SetContext(sectionContext);
            regionIDSection.FindProperties(materialProperties);
            if (regionIDSection.IsValid())
            {
                regionIDSection.OnGUI(materialEditor, targetMaterials);
            }

            generalFoldoutPanel.OnGUI(materialEditor, materialProperties, sectionContext);
            shadowFoldoutPanel.OnGUI(materialEditor, materialProperties, sectionContext);
            if (!isUnlit)
            {
                pbrFoldoutPanel.OnGUI(materialEditor, materialProperties, sectionContext);
                toonFoldoutPanel.OnGUI(materialEditor, materialProperties, sectionContext);
            }
            engineFoldoutPanel.OnGUI(materialEditor, materialProperties, sectionContext);
            
            if (EditorGUI.EndChangeCheck())
            {
                CopyLightMappingProperties();
            }
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            
            TryInitGUIPanels();

            bool isUnlit = IsToonUnlit(material);
            regionIDSection.Refresh(material);
            generalFoldoutPanel.Refresh(material);
            shadowFoldoutPanel.Refresh(material);
            if (!isUnlit)
            {
                pbrFoldoutPanel.Refresh(material);
                toonFoldoutPanel.Refresh(material);
            }
            engineFoldoutPanel.Refresh(material);
        }

        private static bool IsToonUnlit(Material material)
        {
            return material != null && material.shader != null && material.shader.name == ToonUnlitShaderName;
        }

        private static bool IsToonUnlit(Material[] materials)
        {
            if (materials == null || materials.Length == 0) return false;
            foreach (var material in materials)
            {
                if (!IsToonUnlit(material)) return false;
            }
            return true;
        }

        private void TryInitGUIPanels()
        {
            regionIDSection ??= new RegionIDSection();

            generalFoldoutPanel ??= new BaseFoldoutShaderPanel("General", new List<ShaderGUISectionBase>()
            {
                new ColorTextureSection("Base Map", ShaderPropertyID.BaseMap, ShaderPropertyID.BaseColor, true),
                new NormalMapSection("Normal Map", ShaderPropertyID.NormalMap, ShaderPropertyID.NormalScale, ShaderKeywords.NORMAL_MAP),
                new SpecularMaskSection(),
                new AlphaClippingSection(),
                new TransparencySection()
            });
            
            shadowFoldoutPanel ??= new BaseFoldoutShaderPanel("Shadow", new List<ShaderGUISectionBase>()
            {
                new ShadowSection(),
            });
            
            pbrFoldoutPanel ??= new BaseFoldoutShaderPanel("PBR", new List<ShaderGUISectionBase>()
            {
                new PBRSection(),
                new ColorTextureSection("Emission Map", ShaderPropertyID.EmissionMap, ShaderPropertyID.EmissionColor, true),
            });

            toonFoldoutPanel ??= new BaseFoldoutShaderPanel("Toon", new List<ShaderGUISectionBase>()
            {
                new RampTextureSection("Ramp Set"),
                new GeometryOutlineSection(),
                new HighLightSection(),
                new LightMapSDFSection(),
                new FringeSection(),
                new RefractionSection(),
                new MatCapSection(),
                new HeaderPropertySection("Sigmoid Attenuation", 
                    new[] { "Offset", "Smooth" }, 
                    new[] { ShaderPropertyID.DirectLightAttenOffset, ShaderPropertyID.DirectLightAttenSmoothNew }),
                new HeaderPropertySection("Sigmoid Specular", 
                    new[] { "Offset", "Smooth" }, 
                    new[] { ShaderPropertyID.DirectLightSpecOffset, ShaderPropertyID.DirectLightSpecSmooth }),
            });
            
            engineFoldoutPanel ??= new BaseFoldoutShaderPanel("Engine", new List<ShaderGUISectionBase>()
            {
                new DefaultPropertySection(ShaderPropertyID.Cull),
                new HeaderPropertySection("Blend Factor",
                    new[] { "Source", "Destination" },
                    new [] { ShaderPropertyID.SrcBlend, ShaderPropertyID.DstBlend }),
                new DefaultPropertySection(ShaderPropertyID.ZWrite),
                new StencilSection(),
                new EngineSection(),
            });
        }
        
        void CopyLightMappingProperties()
        {
            MaterialProperty mainTex = FindProperty(ShaderPropertyID.MainTex, properties, false);
            MaterialProperty baseMap = FindProperty(ShaderPropertyID.BaseMap, properties, false);
            if (mainTex != null && baseMap != null)
            {
                mainTex.textureValue = baseMap.textureValue;
                mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
            }

            MaterialProperty color = FindProperty(ShaderPropertyID.Color, properties, false);
            MaterialProperty baseColor = FindProperty(ShaderPropertyID.BaseColor, properties, false);
            if (color != null && baseColor != null)
            {
                color.colorValue = baseColor.colorValue;
            }
        }
    }
}