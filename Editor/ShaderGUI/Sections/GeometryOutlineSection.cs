using ArcToon.Utils;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class GeometryOutlineSection : ShaderGUISectionBase
    {
        private static readonly GUIContent label = new("Geometry Outline");

        private MaterialProperty outlineColorProperty;
        private MaterialProperty outlineScaleProperty;
        private MaterialProperty smoothNormalSourceProperty;
        private MaterialProperty smoothNormalDecoderProperty;
        private MaterialProperty widthControlModeProperty;
        private MaterialProperty widthMaskChannelProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            outlineColorProperty = MaterialEditorUtils.FindRegionProperty(ShaderPropertyID.OutlineColor, SelectedRegion, props);
            outlineScaleProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.OutlineScale, props, false);
            smoothNormalSourceProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SmoothNormalSource, props, false);
            smoothNormalDecoderProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SmoothNormalDecoder, props, false);
            widthControlModeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.WidthControlMode, props, false);
            widthMaskChannelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.WidthMaskChannel, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.GetShaderPassEnabled(InternalShader.TagId.GeometryOutline.name),
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
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Use Geometry Outline: {newValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled(InternalShader.TagId.GeometryOutline.name, newValue);
                    EditorUtility.SetDirty(material);
                }
            }

            EditorGUILayoutUtils.BeginGUIComponentIndent();

            materialEditor.BuiltinShaderPropertyDrawer(outlineColorProperty, true, "Color");
            materialEditor.BuiltinShaderPropertyDrawer(outlineScaleProperty, true, "Scale");

            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(smoothNormalSourceProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Smooth Normal Source: {(SmoothNormalSource)smoothNormalSourceProperty.intValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.SN_SRC_UV1,
                        (SmoothNormalSource)smoothNormalSourceProperty.intValue == SmoothNormalSource.UV1);
                    material.SetKeyword(ShaderKeywords.SN_SRC_COLOR,
                        (SmoothNormalSource)smoothNormalSourceProperty.intValue == SmoothNormalSource.VertexColor);
                    EditorUtility.SetDirty(material);
                }
            }

            if ((SmoothNormalSource)smoothNormalSourceProperty.intValue != SmoothNormalSource.None)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.BuiltinShaderPropertyDrawer(smoothNormalDecoderProperty);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Smooth Normal Decoder: {(SmoothNormalDecoder)smoothNormalDecoderProperty.intValue}");

                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetKeyword(ShaderKeywords.SN_DECODE_RGAG,
                            (SmoothNormalDecoder)smoothNormalDecoderProperty.intValue == SmoothNormalDecoder.RGAG);
                        material.SetKeyword(ShaderKeywords.SN_DECODE_OCT,
                            (SmoothNormalDecoder)smoothNormalDecoderProperty.intValue == SmoothNormalDecoder.OCT);
                        EditorUtility.SetDirty(material);
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(widthControlModeProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Width Control Mode: {(WidthControlMode)widthControlModeProperty.intValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.WIDTH_VERTEX_COLOR,
                        (WidthControlMode)widthControlModeProperty.intValue == WidthControlMode.VertexColor);
                    EditorUtility.SetDirty(material);
                }
            }

            if ((WidthControlMode)widthControlModeProperty.intValue == WidthControlMode.VertexColor)
            {
                materialEditor.BuiltinShaderPropertyDrawer(widthMaskChannelProperty, true, "Width Control Channel");
            }

            EditorGUILayoutUtils.EndGUIComponentIndent();

            EditorGUILayoutUtils.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return outlineScaleProperty != null && widthControlModeProperty != null && smoothNormalSourceProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            int smoothNormalSourceValue = material.GetInteger(ShaderPropertyID.SmoothNormalSource);
            material.SetKeyword(ShaderKeywords.SN_SRC_UV1,
                (SmoothNormalSource)smoothNormalSourceValue == SmoothNormalSource.UV1);
            material.SetKeyword(ShaderKeywords.SN_SRC_COLOR,
                (SmoothNormalSource)smoothNormalSourceValue == SmoothNormalSource.VertexColor);

            bool hasEncodedSource = (SmoothNormalSource)smoothNormalSourceValue != SmoothNormalSource.None;
            int smoothNormalDecoderValue = material.GetInteger(ShaderPropertyID.SmoothNormalDecoder);
            material.SetKeyword(ShaderKeywords.SN_DECODE_RGAG,
                hasEncodedSource && (SmoothNormalDecoder)smoothNormalDecoderValue == SmoothNormalDecoder.RGAG);
            material.SetKeyword(ShaderKeywords.SN_DECODE_OCT,
                hasEncodedSource && (SmoothNormalDecoder)smoothNormalDecoderValue == SmoothNormalDecoder.OCT);

            int widthControlModeValue = material.GetInteger(ShaderPropertyID.WidthControlMode);
            material.SetKeyword(ShaderKeywords.WIDTH_VERTEX_COLOR,
                (WidthControlMode)widthControlModeValue == WidthControlMode.VertexColor);
        }
    }
}