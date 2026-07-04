using ArcToon.Utils;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class FringeSection : ShaderGUISectionBase
    {
        private MaterialProperty fringeTransparentScaleProperty;
        private MaterialProperty fringeShadowBiasScaleXProperty;
        private MaterialProperty fringeShadowBiasScaleYProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            fringeTransparentScaleProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.FringeTransparentScale, props, false);
            fringeShadowBiasScaleXProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.FringeShadowBiasScaleX, props, false);
            fringeShadowBiasScaleYProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.FringeShadowBiasScaleY, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            // TODO: link to global stencil settings
            {
                ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.GetShaderPassEnabled(InternalShader.TagId.EyeLashesReceiver.name), 
                    out bool hasMixedValue, out bool shouldToggleGroup);
                
                EditorGUI.showMixedValue = hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(new GUIContent("Transparent Fringe"), shouldToggleGroup, EditorStyles.label);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Eyelashes Receiver: {newValue}");
                    
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetShaderPassEnabled(InternalShader.TagId.EyeLashesReceiver.name, newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                
                materialEditor.BuiltinShaderPropertyDrawer(fringeTransparentScaleProperty, true, "Alpha");
                
                EditorGUILayoutUtils.EndGUIComponentIndent();
                EditorGUILayoutUtils.EndTogglePropertyGroup();
            }
            
            {
                ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.GetShaderPassEnabled(InternalShader.TagId.FringeShadowReceiver.name), 
                    out bool hasMixedValue, out bool shouldToggleGroup);
                
                EditorGUI.showMixedValue = hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(new GUIContent("Fringe Shadow"), shouldToggleGroup, EditorStyles.label);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Fringe Shadow Receiver: {newValue}");
                    
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetShaderPassEnabled(InternalShader.TagId.FringeShadowReceiver.name, newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                EditorGUILayoutUtils.BeginGUIComponentIndent();

                var displayShadowOffsetValue = new Vector2(fringeShadowBiasScaleXProperty.floatValue, fringeShadowBiasScaleYProperty.floatValue);
                EditorGUI.BeginChangeCheck();
                var newShadowOffsetValue = EditorGUILayout.Vector2Field(new GUIContent("Shadow Offset"), displayShadowOffsetValue);
                if (EditorGUI.EndChangeCheck())
                {
                    fringeShadowBiasScaleXProperty.floatValue = newShadowOffsetValue.x;
                    fringeShadowBiasScaleYProperty.floatValue = newShadowOffsetValue.y;
                }
                
                EditorGUILayoutUtils.EndGUIComponentIndent();
                EditorGUILayoutUtils.EndTogglePropertyGroup();
            }
        }

        public override bool IsValid()
        {
            return fringeTransparentScaleProperty != null && fringeShadowBiasScaleXProperty != null && fringeShadowBiasScaleYProperty != null;
        }
    }
}