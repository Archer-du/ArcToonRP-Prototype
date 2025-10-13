using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public static class MaterialEditorUtils
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
        
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + properties.Length.ToString());
            return null;
        }
                
        public static Material[] GetTargetMaterials(MaterialEditor editor)
        {
            if (editor == null || editor.targets == null) return Array.Empty<Material>();
            return editor.targets.Where(t => t is Material).Cast<Material>().ToArray();
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

        private static bool enableGUILog = true;
        
        public static void ArcToonGUILog(object log)
        {
            if (enableGUILog)
            {
                Debug.Log("[ArcToonGUI] " + log);
            }
        }
        
        // public static bool ToggleKeyword(Material[] materials, string label, string keyword)
        // {
        //     if (materials == null || materials.Length == 0) return false;
        //
        //     bool mixed = false;
        //     bool firstOrOnlyValue = materials[0].IsKeywordEnabled(keyword);
        //     for (int i = 1; i < materials.Length; i++)
        //     {
        //         if (materials[i].IsKeywordEnabled(keyword) != firstOrOnlyValue)
        //         {
        //             mixed = true;
        //             break;
        //         }
        //     }
        //
        //     EditorGUI.showMixedValue = mixed;
        //     EditorGUI.BeginChangeCheck();
        //     bool newValue = EditorGUILayout.Toggle(label, firstOrOnlyValue);
        //     EditorGUI.showMixedValue = false;
        //
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         RegisterUndo(materials, $"Toggle {keyword}");
        //         foreach (var mat in materials)
        //         {
        //             if (mat == null) continue;
        //             if (newValue) mat.EnableKeyword(keyword);
        //             else mat.DisableKeyword(keyword);
        //             EditorUtility.SetDirty(mat);
        //         }
        //     }
        //
        //     return newValue;
        // }

    }
}