using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Components;
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
                UpdateLightingDebugKeywords();
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
            generalFoldoutPanel ??= new BaseFoldoutShaderPanel("General", new List<ShaderGUIComponentBase>()
            {
                new ColorTextureComponent("Base Map", ShaderPropertyID.BaseMap, ShaderPropertyID.BaseColor, false),
                new NormalMapComponent("Normal Map", ShaderPropertyID.NormalMap, ShaderPropertyID.NormalScale, ShaderKeywords.NORMAL_MAP),
                new AlphaClippingComponent(),
            });
            
            shadowFoldoutPanel ??= new BaseFoldoutShaderPanel("Shadow", new List<ShaderGUIComponentBase>()
            {
                new ShadowComponent(),
            });
            
            pbrFoldoutPanel ??= new BaseFoldoutShaderPanel("PBR", new List<ShaderGUIComponentBase>()
            {
                new ColorTextureComponent("Emission Map", ShaderPropertyID.EmissionMap, ShaderPropertyID.EmissionColor, true),
            });

            toonFoldoutPanel ??= new BaseFoldoutShaderPanel("Toon", new List<ShaderGUIComponentBase>()
            {
                new RampTextureComponent("Ramp Set", ShaderPropertyID.RampSet, ShaderKeywords.RAMP_SET),
                new GeometryOutlineComponent(),
                new HeaderPropertyComponent("Sigmoid Attenuation", 
                    new[] { "Offset", "Smooth" }, 
                    new[] { ShaderPropertyID.DirectLightAttenOffset, ShaderPropertyID.DirectLightAttenSmoothNew }),
                new HeaderPropertyComponent("Sigmoid Specular", 
                    new[] { "Offset", "Smooth" }, 
                    new[] { ShaderPropertyID.DirectLightSpecOffset, ShaderPropertyID.DirectLightSpecSmooth }),
            });
            
            engineFoldoutPanel ??= new BaseFoldoutShaderPanel("Engine", new List<ShaderGUIComponentBase>()
            {
                new DefaultPropertyComponent(ShaderPropertyID.Cull),
                new HeaderPropertyComponent("Blend Factor",  
                    new[] { "Source", "Destination" }, 
                    new [] { ShaderPropertyID.SrcBlend, ShaderPropertyID.DstBlend }),
                new DefaultPropertyComponent(ShaderPropertyID.ZWrite),
                new EngineComponent(),
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
        
        void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                foreach (var obj in materials)
                {
                    var material = (Material)obj;
                    material.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (var obj in materials)
                {
                    var material = (Material)obj;
                    material.DisableKeyword(keyword);
                }
            }
        }

        void UpdateLightingDebugKeywords()
        {
            MaterialProperty property = FindProperty(ShaderPropertyID.LightingDebugMode, properties, false);
            if (property == null || property.hasMixedValue)
                return;

            switch ((LightingDebugMode)property.floatValue)
            {
                case LightingDebugMode.IncomingLight:
                    SetKeyword(ShaderKeywords.DEBUG_INCOMING_LIGHT, true);
                    SetKeyword(ShaderKeywords.DEBUG_DIRECT_BRDF, false);
                    SetKeyword(ShaderKeywords.DEBUG_SPECULAR, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIFFUSE, false);

                    break;
                case LightingDebugMode.DirectBRDF:
                    SetKeyword(ShaderKeywords.DEBUG_INCOMING_LIGHT, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIRECT_BRDF, true);
                    SetKeyword(ShaderKeywords.DEBUG_SPECULAR, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIFFUSE, false);

                    break;
                case LightingDebugMode.Specular:
                    SetKeyword(ShaderKeywords.DEBUG_INCOMING_LIGHT, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIRECT_BRDF, false);
                    SetKeyword(ShaderKeywords.DEBUG_SPECULAR, true);
                    SetKeyword(ShaderKeywords.DEBUG_DIFFUSE, false);
                    break;
                case LightingDebugMode.Diffuse:
                    SetKeyword(ShaderKeywords.DEBUG_INCOMING_LIGHT, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIRECT_BRDF, false);
                    SetKeyword(ShaderKeywords.DEBUG_SPECULAR, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIFFUSE, true);
                    break;
                default:
                    SetKeyword(ShaderKeywords.DEBUG_INCOMING_LIGHT, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIRECT_BRDF, false);
                    SetKeyword(ShaderKeywords.DEBUG_SPECULAR, false);
                    SetKeyword(ShaderKeywords.DEBUG_DIFFUSE, false);
                    break;
            }
        }

    }
}