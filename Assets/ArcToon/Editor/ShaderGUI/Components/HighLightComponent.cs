using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class HighLightComponent : ShaderGUIComponentBase
    {
        private static readonly GUIContent label = new("Highlights");
        
        private MaterialProperty highlightTypeProperty;
        private MaterialProperty specGlossProperty;
        private MaterialProperty specScaleProperty;
        
        private MaterialProperty tangentShiftMapProperty;
        private MaterialProperty tangentShiftMapUVProperty;
        private MaterialProperty tangentShiftOffsetProperty;
        
        private MaterialProperty parallaxSpecMapProperty;
        private MaterialProperty parallaxSpecMapUVProperty;
        private MaterialProperty parallaxSensitivityProperty;
        private MaterialProperty parallaxOffsetProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            highlightTypeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.HighlightType, props, false);
            specGlossProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecGloss, props, false);
            specScaleProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecScale, props, false);
            
            tangentShiftMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftMap, props, false);
            tangentShiftMapUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftMapUV, props, false);
            tangentShiftOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TangentShiftOffset, props, false);
            
            parallaxSpecMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxSpecMap, props, false);
            parallaxSpecMapUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxSpecMapUV, props, false);
            parallaxSensitivityProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxSensitivity, props, false);
            parallaxOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxOffset, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            
            // bool hasMixedValue = false;
            // bool firstOrOnlyValue = materials[0].GetInteger(OutlinePassName);
            // for (int i = 1; i < materials.Length; i++)
            // {
            //     if (materials[i].GetShaderPassEnabled(OutlinePassName) != firstOrOnlyValue)
            //     {
            //         hasMixedValue = true;
            //         break;
            //     }
            // }
            // bool shouldToggleGroup = !hasMixedValue && firstOrOnlyValue;
            // EditorGUI.showMixedValue = hasMixedValue;
            //
            // EditorGUI.BeginChangeCheck();
            // bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(label, shouldToggleGroup, EditorStyles.label);
            // EditorGUI.showMixedValue = false;
            EditorGUILayout.LabelField("Override Highlights");
            
            ShaderGUILayout.BeginGUIComponentIndent();
            EditorGUI.showMixedValue = highlightTypeProperty.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var newHighlightTypeValue = (OverrideHighlightType)EditorGUILayout.EnumPopup("Type", (OverrideHighlightType)highlightTypeProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                highlightTypeProperty.intValue = (int)newHighlightTypeValue;
                MaterialEditorUtils.ArcToonGUILog($"Update {highlightTypeProperty}: {(OverrideHighlightType)highlightTypeProperty.intValue}");
            }
            EditorGUI.showMixedValue = false;
            ShaderGUILayout.EndGUIComponentIndent();
            
            
            switch (newHighlightTypeValue)
            {
                case OverrideHighlightType.BlinnPhong:
                    EditorGUILayout.LabelField("Blinn-Phong");
                    ShaderGUILayout.BeginGUIComponentIndent();
                    
                    materialEditor.BuiltinShaderPropertyDrawer(specGlossProperty, true, "Glossiness");
                    materialEditor.BuiltinShaderPropertyDrawer(specScaleProperty, true, "Strength");
                    
                    ShaderGUILayout.EndGUIComponentIndent();
                    break;
                case OverrideHighlightType.KajiyaKay:
                    EditorGUILayout.LabelField("Kajiya Kay");
                    ShaderGUILayout.BeginGUIComponentIndent();
                    
                    materialEditor.BuiltinShaderPropertyDrawer(specGlossProperty, true, "Glossiness");
                    materialEditor.BuiltinShaderPropertyDrawer(specScaleProperty, true, "Strength");
                    
                    materialEditor.TexturePropertySingleLine(new GUIContent("Tangent Shift Map"), tangentShiftMapProperty, tangentShiftMapUVProperty);
                    materialEditor.BuiltinShaderPropertyDrawer(tangentShiftOffsetProperty, true, "Shift Offset");
                    
                    ShaderGUILayout.EndGUIComponentIndent();
                    break;
                case OverrideHighlightType.Parallax:
                    EditorGUILayout.LabelField("Parallax");
                    ShaderGUILayout.BeginGUIComponentIndent();
                    
                    materialEditor.BuiltinShaderPropertyDrawer(specGlossProperty, true, "Glossiness");
                    materialEditor.BuiltinShaderPropertyDrawer(specScaleProperty, true, "Strength");
                    
                    materialEditor.TexturePropertySingleLine(new GUIContent("Specular Mask"), parallaxSpecMapProperty, parallaxSpecMapUVProperty);
                    materialEditor.BuiltinShaderPropertyDrawer(parallaxSensitivityProperty, true, "Sensitivity");
                    materialEditor.BuiltinShaderPropertyDrawer(parallaxOffsetProperty, true, "Offset");
                    
                    ShaderGUILayout.EndGUIComponentIndent();
                    break;
            }
        }

        public override bool IsValid()
        {
            return specGlossProperty != null || specScaleProperty != null;
        }
    }
}