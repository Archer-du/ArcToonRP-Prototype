using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public static class MaterialEditorExtensions
    {
        public static void BuiltinShaderPropertyDrawer(this MaterialEditor editor, MaterialProperty property)
        {
            editor.ShaderProperty(
                EditorGUILayout.GetControlRect(true, editor.GetPropertyHeight(property, property.displayName), EditorStyles.layerMaskField), 
                property, property.displayName);
        }
        
        public static void BuiltinShaderPropertyDrawer(this MaterialEditor editor, MaterialProperty property, bool hasLabel, string label)
        {
            editor.ShaderProperty(
                EditorGUILayout.GetControlRect(hasLabel, editor.GetPropertyHeight(property, property.displayName), EditorStyles.layerMaskField), 
                property, label);
        }
        
        public static void TexturePropertyWithColorProperty(this MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProperty, MaterialProperty colorProperty, bool isHDRColor)
        {
            if (isHDRColor)
            {
                materialEditor.TexturePropertyWithHDRColor(label, textureProperty, colorProperty, true);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(label, textureProperty, colorProperty);
            }
        }
        
        public static void SetKeyword(this Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}