using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class ColorTextureComponent : ShaderGUIComponentBase
    {
        private readonly bool isHDRColor;
        
        private readonly string mapID;
        private readonly string colorID;
        
        private MaterialProperty mapProperty;
        private MaterialProperty colorProperty;
        
        private readonly GUIContent label;
        
        public ColorTextureComponent(string labelName, string mapID, string colorID, bool isHDRColor)
        {
            this.mapID = mapID;
            this.colorID = colorID;
            this.isHDRColor = isHDRColor;
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            mapProperty = MaterialEditorUtils.FindProperty(mapID, props, false);
            colorProperty = MaterialEditorUtils.FindProperty(colorID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            materialEditor.TexturePropertyWithColorProperty(label, mapProperty, colorProperty, isHDRColor);
            if (mapProperty.textureValue != null)
            {
                ShaderGUILayout.BeginGUIComponentIndent();
                materialEditor.TextureScaleOffsetProperty(mapProperty);
                ShaderGUILayout.EndGUIComponentIndent();
            }
        }

        public override bool IsValid()
        {
            return mapProperty != null && colorProperty != null;
        }
    }
}