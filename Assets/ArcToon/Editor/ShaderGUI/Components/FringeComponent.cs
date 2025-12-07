using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class FringeComponent : ShaderGUIComponentBase
    {
        public override void FindProperties(MaterialProperty[] props)
        {
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            // TODO: link to global stencil settings
            {
                ShaderGUILayout.CheckShouldToggleGroupByShaderPass(materials, "EyeLashesReceiver", out bool hasMixedValue, out bool shouldToggleGroup);
                
                EditorGUI.showMixedValue = hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(new GUIContent("Transparent Fringe"), shouldToggleGroup, EditorStyles.label);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Eyelashes Receiver: {newValue}");
                    
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetShaderPassEnabled("EyeLashesReceiver", newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                ShaderGUILayout.BeginGUIComponentIndent();
                
                ShaderGUILayout.EndGUIComponentIndent();
                ShaderGUILayout.EndTogglePropertyGroup();
            }
            
            {
                ShaderGUILayout.CheckShouldToggleGroupByShaderPass(materials, "FringeShadowReceiver", out bool hasMixedValue, out bool shouldToggleGroup);
                
                EditorGUI.showMixedValue = hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(new GUIContent("Fringe Shadow"), shouldToggleGroup, EditorStyles.label);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Fringe Shadow Receiver: {newValue}");
                    
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetShaderPassEnabled("FringeShadowReceiver", newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                ShaderGUILayout.BeginGUIComponentIndent();
                
                ShaderGUILayout.EndGUIComponentIndent();
                ShaderGUILayout.EndTogglePropertyGroup();
            }
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}