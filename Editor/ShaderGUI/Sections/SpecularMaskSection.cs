using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class SpecularMaskSection : ShaderGUISectionBase
    {
        private MaterialProperty SpecularMaskProperty;
        private MaterialProperty specularMaskUVProperty;
        private MaterialProperty parallaxSensitivityProperty;
        private MaterialProperty parallaxOffsetProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            SpecularMaskProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecularMask, props, false);
            specularMaskUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecularMaskUV, props, false);
            parallaxSensitivityProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxSensitivity, props, false);
            parallaxOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxOffset, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (SpecularMaskProperty.textureValue != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular Mask"), SpecularMaskProperty, specularMaskUVProperty);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        bool hasSpecMask = material.GetTexture(SpecularMaskProperty.name) != null;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Specular Mask UV: {specularMaskUVProperty.intValue}");
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        
                        material.SetKeyword(ShaderKeywords.SPEC_MASK, hasSpecMask);

                        EditorUtility.SetDirty(material);
                    }
                }
                
                ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.IsKeywordEnabled(ShaderKeywords.SPEC_PARALLAX), 
                    out var hasMixedValue, out var keywordEnabled);
                
                EditorGUI.showMixedValue = hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(new GUIContent("Parallax"), keywordEnabled, EditorStyles.label);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Is Parallax Mask: {newValue}");
                    
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetKeyword(ShaderKeywords.SPEC_PARALLAX, newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                materialEditor.BuiltinShaderPropertyDrawer(parallaxSensitivityProperty, true, "Sensitivity");
                materialEditor.BuiltinShaderPropertyDrawer(parallaxOffsetProperty, true, "Offset");
                
                EditorGUILayoutUtils.EndGUIComponentIndent();
                EditorGUILayoutUtils.EndTogglePropertyGroup();
            }
            else
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular Mask"), SpecularMaskProperty);
            }
        }

        public override bool IsValid()
        {
            return SpecularMaskProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if(material == null) return;
            bool hasSpecMask = material.GetTexture(ShaderPropertyID.SpecularMask) != null;
            material.SetKeyword(ShaderKeywords.SPEC_MASK, hasSpecMask);
        }
    }
}