using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class HighLightSection : ShaderGUISectionBase
    {
        private MaterialProperty highlightTypeProperty;
        private MaterialProperty specGlossProperty;
        private MaterialProperty specScaleProperty;
        
        private MaterialProperty tangentShiftMapProperty;
        private MaterialProperty tangentShiftMapUVProperty;
        private MaterialProperty tangentShiftOffsetProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            highlightTypeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.HighlightType, props, false);
            specGlossProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecGloss, props, false);
            specScaleProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecScale, props, false);
            
            tangentShiftMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftMap, props, false);
            tangentShiftMapUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftMapUV, props, false);
            tangentShiftOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftOffset, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            
            ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.IsKeywordEnabled(ShaderKeywords.OVERRIDE_HIGHLIGHT), 
                out bool hasMixedValue, out bool shouldToggleGroup);
        
            EditorGUI.showMixedValue = hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(new GUIContent("Override Highlights"), shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Override Highlight: {newValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.OVERRIDE_HIGHLIGHT, newValue);
                    EditorUtility.SetDirty(material);
                }
            }
            
            EditorGUILayoutUtils.BeginGUIComponentIndent();
            EditorGUI.showMixedValue = highlightTypeProperty.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var newHighlightTypeValue = (OverrideHighlightType)EditorGUILayout.EnumPopup("Type", (OverrideHighlightType)highlightTypeProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                highlightTypeProperty.intValue = (int)newHighlightTypeValue;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Highlight Type: {newHighlightTypeValue}");
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    
                    material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP, 
                        (OverrideHighlightType)highlightTypeProperty.intValue == OverrideHighlightType.KajiyaKay);
                    
                    EditorUtility.SetDirty(material);
                }
            }
            EditorGUI.showMixedValue = false;
            
            materialEditor.BuiltinShaderPropertyDrawer(specGlossProperty, true, "Glossiness");
            materialEditor.BuiltinShaderPropertyDrawer(specScaleProperty, true, "Strength");
            switch (newHighlightTypeValue)
            {
                case OverrideHighlightType.BlinnPhong:
                    break;
                case OverrideHighlightType.KajiyaKay:
                    EditorGUI.BeginChangeCheck();
                    materialEditor.TexturePropertySingleLine(new GUIContent("Tangent Shift Map"), tangentShiftMapProperty, tangentShiftMapUVProperty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var material in materials)
                        {
                            if (material == null) continue;
                            MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Highlight Kajiya UV: {tangentShiftMapUVProperty.intValue}");
                            Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    
                            material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP_UV0, tangentShiftMapUVProperty.intValue == 0);
                            material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP_UV1, tangentShiftMapUVProperty.intValue == 1);
                    
                            EditorUtility.SetDirty(material);
                        }
                    }
                    materialEditor.BuiltinShaderPropertyDrawer(tangentShiftOffsetProperty, true, "Shift Offset");
                    break;
            }
            EditorGUILayoutUtils.EndGUIComponentIndent();
            EditorGUILayoutUtils.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return specGlossProperty != null || specScaleProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            if (material.HasProperty(ShaderPropertyID.HighlightType))
            {
                material.SetInteger(ShaderPropertyID.HighlightType, 0);
                material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP, 
                    (OverrideHighlightType)material.GetInteger(ShaderPropertyID.HighlightType) == OverrideHighlightType.KajiyaKay);
            }

            if (material.HasProperty(ShaderPropertyID.TangentShiftMapUV))
            {
                material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP_UV0, material.GetInteger(ShaderPropertyID.TangentShiftMapUV) == 0);
                material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP_UV1, material.GetInteger(ShaderPropertyID.TangentShiftMapUV) == 1);
            }
        }
    }
}