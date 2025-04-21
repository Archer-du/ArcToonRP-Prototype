using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.GUI
{
    public class ArcToonShaderGUI : ShaderGUI
    {
        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;

        enum ShadowMode
        {
            On,
            Clip,
            Dither,
            Off
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            EditorGUI.BeginChangeCheck();

            base.OnGUI(materialEditor, materialProperties);
            editor = materialEditor;
            materials = materialEditor.targets;
            properties = materialProperties;

            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateShadowCasterPass();
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

        void UpdateShadowCasterPass()
        {
            MaterialProperty property = FindProperty("_Shadows", properties, false);
            if (property == null || property.hasMixedValue)
                return;

            bool enabled = property.floatValue < (float)ShadowMode.Off;
            foreach (var o in materials)
            {
                var material = (Material)o;
                material.SetShaderPassEnabled("ShadowCaster", enabled);
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