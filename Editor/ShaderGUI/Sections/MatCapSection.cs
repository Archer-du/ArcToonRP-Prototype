using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class MatCapSection : ShaderGUISectionBase
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
            EditorGUI.BeginChangeCheck();
            if (matCapProperty.textureValue != null)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("MatCap"), matCapProperty, matCapStrengthProperty);
                
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                {
                    materialEditor.TextureScaleOffsetProperty(matCapProperty);
                    
                    EditorGUI.showMixedValue = matCapBlendModeProperty.hasMixedValue;
                    EditorGUI.BeginChangeCheck();
                    var newBlendModeValue = (ColorBlendMode)EditorGUILayout.EnumPopup("Blend Mode", (ColorBlendMode)matCapBlendModeProperty.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        matCapBlendModeProperty.intValue = (int)newBlendModeValue;
                    }
                    EditorGUI.showMixedValue = false;
                }
                EditorGUILayoutUtils.EndGUIComponentIndent();
            }
            else
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("MatCap"), matCapProperty);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasMatCap = material.GetTexture(matCapProperty.name) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {ShaderKeywords.MATCAP} - {hasMatCap}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.MATCAP, hasMatCap);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        public override bool IsValid()
        {
            return matCapProperty != null;
        }
    }
}