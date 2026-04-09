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
        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;

        private BaseFoldoutShaderPanel generalFoldoutPanel = null;
        private BaseFoldoutShaderPanel shadowFoldoutPanel = null;
        private BaseFoldoutShaderPanel pbrFoldoutPanel = null;
        private BaseFoldoutShaderPanel toonFoldoutPanel = null;
        private BaseFoldoutShaderPanel engineFoldoutPanel = null;

        enum LightingDebugMode
        {
            None,
            IncomingLight,
            DirectBRDF,
            Specular,
            Diffuse,
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            EditorGUI.BeginChangeCheck();
            editor = materialEditor;
            materials = materialEditor.targets;
            properties = materialProperties;
            
            TryInitGUIPanels();
            
            generalFoldoutPanel.OnGUI(materialEditor, materialProperties);
            shadowFoldoutPanel.OnGUI(materialEditor, materialProperties);
            pbrFoldoutPanel.OnGUI(materialEditor, materialProperties);
            toonFoldoutPanel.OnGUI(materialEditor, materialProperties);
            engineFoldoutPanel.OnGUI(materialEditor, materialProperties);
            
            base.OnGUI(materialEditor, materialProperties);
            if (EditorGUI.EndChangeCheck())
            {
                CopyLightMappingProperties();
            }
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            
            TryInitGUIPanels();
            
            generalFoldoutPanel.Refresh(material);
            shadowFoldoutPanel.Refresh(material);
            pbrFoldoutPanel.Refresh(material);
            toonFoldoutPanel.Refresh(material);
            engineFoldoutPanel.Refresh(material);
        }

        private void TryInitGUIPanels()
        {
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