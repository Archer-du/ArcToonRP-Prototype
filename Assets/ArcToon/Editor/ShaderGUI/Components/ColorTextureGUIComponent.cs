using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class ColorTextureGUIComponent : IShaderGUIComponent
    {
        private readonly bool isHDRColor;
        
        private readonly string mapID;
        private MaterialProperty mapProperty;
        private readonly string colorID;
        private MaterialProperty colorProperty;
        
        private readonly GUIContent label;
        
        public ColorTextureGUIComponent(string mapID, string colorID, string labelName, bool isHDRColor)
        {
            this.mapID = mapID;
            this.colorID = colorID;
            this.isHDRColor = isHDRColor;
            label = new GUIContent(labelName);
        }
        
        public void FindProperties(MaterialProperty[] props)
        {
            mapProperty = MaterialEditorUtils.FindProperty(mapID, props, false);
            colorProperty = MaterialEditorUtils.FindProperty(colorID, props, false);
        }

        public void DrawGUI(MaterialEditor materialEditor, Material[] materials)
        {
            if (mapProperty.textureValue != null)
            {
                DrawMiniTextureWithColorProperty(materialEditor);
                const int indentLevelOffset = 2;
                int originIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel += indentLevelOffset;
                materialEditor.TextureScaleOffsetProperty(mapProperty);
                EditorGUI.indentLevel = originIndentLevel;
            }
            else
            {
                DrawMiniTextureWithColorProperty(materialEditor);
            }
        }

        private void DrawMiniTextureWithColorProperty(MaterialEditor materialEditor)
        {
            if (isHDRColor)
            {
                materialEditor.TexturePropertyWithHDRColor(label, mapProperty, colorProperty, true);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(label, mapProperty, colorProperty);
            }
        }
    }
}