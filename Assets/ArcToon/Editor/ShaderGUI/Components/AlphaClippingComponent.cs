using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class AlphaClippingComponent : ShaderGUIComponentBase
    {
        private static readonly GUIContent label = new("Alpha Clipping");
        
        private MaterialProperty useAlphaClipProperty;
        private MaterialProperty cutOffProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            useAlphaClipProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Clipping, props, false);
            cutOffProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Cutoff, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            bool shouldToggleGroup = !useAlphaClipProperty.hasMixedValue && Mathf.Approximately(useAlphaClipProperty.floatValue, 1);
            EditorGUI.showMixedValue = useAlphaClipProperty.hasMixedValue;
            
            EditorGUI.BeginChangeCheck();
            bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(label, shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;
            
            if (EditorGUI.EndChangeCheck())
            {
                useAlphaClipProperty.floatValue = newValue ? 1f : 0f;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Clipping: {newValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords._CLIPPING, newValue);
                    EditorUtility.SetDirty(material);
                }
            }
            ShaderGUILayout.BeginGUIComponentIndent();
            materialEditor.BuiltinShaderPropertyDrawer(cutOffProperty, true, "Cutoff");
            ShaderGUILayout.EndGUIComponentIndent();
            
            ShaderGUILayout.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return useAlphaClipProperty != null && cutOffProperty != null;
        }
    }
}