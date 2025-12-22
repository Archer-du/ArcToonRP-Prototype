using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class MatCapComponent : ShaderGUIComponentBase
    {
        private MaterialProperty matCapProperty;
        private MaterialProperty matCapStrengthProperty;
        private MaterialProperty matCapBlendModeProperty;
        public override void FindProperties(MaterialProperty[] props)
        {
            matCapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.MatCap, props, false);
            matCapStrengthProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.MatCapStrength, props, false);
            matCapBlendModeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.MatCapBlendMode, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (matCapProperty.textureValue != null)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("MatCap"), matCapProperty, matCapStrengthProperty);
                ShaderGUILayout.BeginGUIComponentIndent();
                materialEditor.TextureScaleOffsetProperty(matCapProperty);
                ShaderGUILayout.EndGUIComponentIndent();
            }
            else
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("MatCap"), matCapProperty);
            }
            
            ShaderGUILayout.BeginGUIComponentIndent();
            EditorGUI.showMixedValue = matCapBlendModeProperty.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var newBlendModeValue = (ColorBlendMode)EditorGUILayout.EnumPopup("Blend Mode", (ColorBlendMode)matCapBlendModeProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                matCapBlendModeProperty.intValue = (int)newBlendModeValue;
            }
            EditorGUI.showMixedValue = false;
            ShaderGUILayout.EndGUIComponentIndent();
            
            ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.IsKeywordEnabled(ShaderKeywords.MATCAP_SPH_NORMAL), 
                out bool hasMixedValue, out bool shouldToggleGroup);
        }

        public override bool IsValid()
        {
            return matCapProperty != null;
        }
    }
}