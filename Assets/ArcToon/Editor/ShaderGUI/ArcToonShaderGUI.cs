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

        private BaseFoldoutShaderGUIPanel generalFoldoutPanel = null;
        private BaseFoldoutShaderGUIPanel shadowFoldoutPanel = null;
        private BaseFoldoutShaderGUIPanel pbrFoldoutPanel = null;
        private BaseFoldoutShaderGUIPanel engineFoldoutPanel = null;
        
        enum ShadowMode
        {
            On,
            Clip,
            Dither,
            Off
        }

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

            generalFoldoutPanel ??= new BaseFoldoutShaderGUIPanel("General", new List<IShaderGUIComponent>()
            {
                new ColorTextureGUIComponent("_BaseMap", "_BaseColor", "Base Map", false),
                new NormalMapGUIComponent("_NORMAL_MAP", "_NormalMap", "_NormalScale", "Normal Map"),
                new AlphaClippingGUIComponent("_CLIPPING", "_Clipping", "_Cutoff", "Alpha Clipping"),
            });
            
            shadowFoldoutPanel ??= new BaseFoldoutShaderGUIPanel("Shadow", new List<IShaderGUIComponent>()
            {
                new BuiltinPropertyGUIComponent("_ReceiveShadows"),
                new ShadowCasterGUIComponent("_Shadows"),
            });
            
            pbrFoldoutPanel ??= new BaseFoldoutShaderGUIPanel("PBR", new List<IShaderGUIComponent>()
            {
                new ColorTextureGUIComponent("_EmissionMap", "_EmissionColor", "Emission Map", true),
            });
            
            engineFoldoutPanel ??= new BaseFoldoutShaderGUIPanel("Engine", new List<IShaderGUIComponent>()
            {
                new BuiltinPropertyGUIComponent("_Cull"),
                new BuiltinPropertyGUIComponent("_SrcBlend"),
                new BuiltinPropertyGUIComponent("_DstBlend"),
                new BuiltinPropertyGUIComponent("_ZWrite"),
                new EngineGUIComponent(),
            });
            
            // EditorGUILayout.HelpBox("test", MessageType.Info);

            generalFoldoutPanel.DrawGUI(materialEditor, materialProperties);
            shadowFoldoutPanel.DrawGUI(materialEditor, materialProperties);
            pbrFoldoutPanel.DrawGUI(materialEditor, materialProperties);
            engineFoldoutPanel.DrawGUI(materialEditor, materialProperties);
            
            base.OnGUI(materialEditor, materialProperties);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateLightingDebugKeywords();
                CopyLightMappingProperties();
            }
        }

        bool SetProperty(string name, float value)
        {
            var property = FindProperty(name, properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }

            return false;
        }

        void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1f : 0f))
            {
                SetKeyword(keyword, value);
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
            MaterialProperty property = FindProperty("_LightingDebugMode", properties, false);
            if (property == null || property.hasMixedValue)
                return;

            switch ((LightingDebugMode)property.floatValue)
            {
                case LightingDebugMode.IncomingLight:
                    SetKeyword("_DEBUG_INCOMING_LIGHT", true);
                    SetKeyword("_DEBUG_DIRECT_BRDF", false);
                    SetKeyword("_DEBUG_SPECULAR", false);
                    SetKeyword("_DEBUG_DIFFUSE", false);

                    break;
                case LightingDebugMode.DirectBRDF:
                    SetKeyword("_DEBUG_INCOMING_LIGHT", false);
                    SetKeyword("_DEBUG_DIRECT_BRDF", true);
                    SetKeyword("_DEBUG_SPECULAR", false);
                    SetKeyword("_DEBUG_DIFFUSE", false);

                    break;
                case LightingDebugMode.Specular:
                    SetKeyword("_DEBUG_INCOMING_LIGHT", false);
                    SetKeyword("_DEBUG_DIRECT_BRDF", false);
                    SetKeyword("_DEBUG_SPECULAR", true);
                    SetKeyword("_DEBUG_DIFFUSE", false);
                    break;
                case LightingDebugMode.Diffuse:
                    SetKeyword("_DEBUG_INCOMING_LIGHT", false);
                    SetKeyword("_DEBUG_DIRECT_BRDF", false);
                    SetKeyword("_DEBUG_SPECULAR", false);
                    SetKeyword("_DEBUG_DIFFUSE", true);
                    break;
                default:
                    SetKeyword("_DEBUG_INCOMING_LIGHT", false);
                    SetKeyword("_DEBUG_DIRECT_BRDF", false);
                    SetKeyword("_DEBUG_SPECULAR", false);
                    SetKeyword("_DEBUG_DIFFUSE", false);
                    break;
            }
        }

        void CopyLightMappingProperties()
        {
            MaterialProperty mainTex = FindProperty("_MainTex", properties, false);
            MaterialProperty baseMap = FindProperty("_BaseMap", properties, false);
            if (mainTex != null && baseMap != null)
            {
                mainTex.textureValue = baseMap.textureValue;
                mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
            }

            MaterialProperty color = FindProperty("_Color", properties, false);
            MaterialProperty baseColor = FindProperty("_BaseColor", properties, false);
            if (color != null && baseColor != null)
            {
                color.colorValue = baseColor.colorValue;
            }
        }
    }
}