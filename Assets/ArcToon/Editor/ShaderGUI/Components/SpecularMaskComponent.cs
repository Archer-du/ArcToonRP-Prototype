using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class SpecularMaskComponent : ShaderGUIComponentBase
    {
        private MaterialProperty SpecularMaskProperty;
        private MaterialProperty specularMaskUVProperty;
        private MaterialProperty parallaxSensitivityProperty;
        private MaterialProperty parallaxOffsetProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            SpecularMaskProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecularMask, props, false);
            specularMaskUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SpecularMaskUV, props, false);
            parallaxSensitivityProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxSensitivity, props, false);
            parallaxOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxOffset, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            // if (SpecularMaskProperty.textureValue != null)
            // {
            //     EditorGUI.BeginChangeCheck();
            //     materialEditor.TexturePropertySingleLine(new GUIContent("Specular Mask"), SpecularMaskProperty, specularMaskUVProperty);
            //     if (EditorGUI.EndChangeCheck())
            //     {
            //         foreach (var material in materials)
            //         {
            //             if (material == null) continue;
            //             MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Specular Mask UV: {specularMaskUVProperty.intValue}");
            //             Undo.RecordObject(material, Undo.GetCurrentGroupName());
            //             
            //             material.SetKeyword(ShaderKeywords., specularMaskUVProperty.intValue == 0);
            //             material.SetKeyword(ShaderKeywords.HIGHLIGHT_PARALLAX_UV1, specularMaskUVProperty.intValue == 1);
            //             
            //             EditorUtility.SetDirty(material);
            //         }
            //     }
            //     Toggle
            //     materialEditor.BuiltinShaderPropertyDrawer(parallaxSensitivityProperty, true, "Sensitivity");
            //     materialEditor.BuiltinShaderPropertyDrawer(parallaxOffsetProperty, true, "Offset");
            // }
            // else
            // {
            //     materialEditor.TexturePropertySingleLine(new GUIContent("Specular Mask"), SpecularMaskProperty);
            // }
        }

        public override bool IsValid()
        {
            return SpecularMaskProperty != null;
        }
    }
}