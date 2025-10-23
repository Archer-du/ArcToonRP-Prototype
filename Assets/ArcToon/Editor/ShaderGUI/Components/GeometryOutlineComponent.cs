using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class GeometryOutlineComponent : ShaderGUIComponentBase
    {
        private readonly GUIContent label;

        private readonly string outlineColorID;
        private MaterialProperty outlineColorProperty;
        private readonly string outlineScaleID;
        private MaterialProperty outlineScaleProperty;
        
        private readonly string smoothNormalSourceID;
        private MaterialProperty smoothNormalSourceProperty;
        private readonly string widthControlSourceID;
        private MaterialProperty widthControlSourceProperty;
        
        private static string OutlinePassName = "GeometryOutline";

        public GeometryOutlineComponent(string labelName, string outlineColorID, string outlineScaleID, string smoothNormalSourceID, string widthControlSourceID)
        {
            this.outlineColorID = outlineColorID;
            this.outlineScaleID = outlineScaleID;
            this.smoothNormalSourceID = smoothNormalSourceID;
            this.widthControlSourceID = widthControlSourceID;
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            outlineColorProperty = MaterialEditorUtils.FindProperty(outlineColorID, props, false);
            outlineScaleProperty = MaterialEditorUtils.FindProperty(outlineScaleID, props, false);
            smoothNormalSourceProperty = MaterialEditorUtils.FindProperty(smoothNormalSourceID, props, false);
            widthControlSourceProperty = MaterialEditorUtils.FindProperty(widthControlSourceID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            
            bool hasMixedValue = false;
            bool firstOrOnlyValue = materials[0].GetShaderPassEnabled(OutlinePassName);
            for (int i = 1; i < materials.Length; i++)
            {
                if (materials[i].GetShaderPassEnabled(OutlinePassName) != firstOrOnlyValue)
                {
                    hasMixedValue = true;
                    break;
                }
            }
            bool shouldToggleGroup = !hasMixedValue && firstOrOnlyValue;
            EditorGUI.showMixedValue = hasMixedValue;
            
            EditorGUI.BeginChangeCheck();
            bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(label, shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;
            
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Use Geometry Outline: {newValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled(OutlinePassName, newValue);
                    EditorUtility.SetDirty(material);
                }
            }
            ShaderGUILayout.BeginGUIComponentIndent();
            
            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(smoothNormalSourceProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Smooth Normal Source: {newValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword("_SN_SOURCE_UV1", 
                        (SmoothNormalSource)smoothNormalSourceProperty.intValue == SmoothNormalSource.UV1);
                    material.SetKeyword("_SN_SOURCE_VERTCOL", 
                        (SmoothNormalSource)smoothNormalSourceProperty.intValue == SmoothNormalSource.VertexColor);
                    EditorUtility.SetDirty(material);
                }
            }
            
            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(widthControlSourceProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Width Control Source: {widthControlSourceProperty.intValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword("_OWC_SOURCE_VERTCOL_ALPHA", 
                        (WidthControlSource)widthControlSourceProperty.intValue == WidthControlSource.VertexColorAlpha);
                    EditorUtility.SetDirty(material);
                }
            }
            
            materialEditor.BuiltinShaderPropertyDrawer(outlineColorProperty);
            materialEditor.BuiltinShaderPropertyDrawer(outlineScaleProperty);
            
            ShaderGUILayout.EndGUIComponentIndent();
            
            ShaderGUILayout.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}