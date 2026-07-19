using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class AlphaClippingSection : ShaderGUISectionBase
    {
        private static readonly GUIContent label = new("Alpha Clipping");

        private MaterialProperty cutOffProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            cutOffProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Cutoff, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.IsKeywordEnabled(ShaderKeywords.CLIPPING),
                out bool hasMixedValue, out bool shouldToggleGroup);

            EditorGUI.showMixedValue = hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(label, shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Clipping: {newValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.CLIPPING, newValue);
                    EditorUtility.SetDirty(material);
                }
            }
            EditorGUILayoutUtils.BeginGUIComponentIndent();
            materialEditor.BuiltinShaderPropertyDrawer(cutOffProperty, true, "Cutoff");
            EditorGUILayoutUtils.EndGUIComponentIndent();

            EditorGUILayoutUtils.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return cutOffProperty != null;
        }
    }
}
