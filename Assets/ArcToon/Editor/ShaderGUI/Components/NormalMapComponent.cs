using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class NormalMapComponent : ShaderGUIComponentBase
    {
        private readonly string useNormalMapKeyword;
        
        private readonly string normalMapID;
        private MaterialProperty normalMapProperty;
        private readonly string bumpScaleID;
        private MaterialProperty bumpScaleProperty;
        
        private readonly GUIContent label;

        public NormalMapComponent(string useNormalMapKeyword, string normalMapID, string bumpScaleID, string labelName)
        {
            this.useNormalMapKeyword = useNormalMapKeyword;
            this.normalMapID = normalMapID;
            this.bumpScaleID = bumpScaleID;
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            normalMapProperty = MaterialEditorUtils.FindProperty(normalMapID, props, false);
            bumpScaleProperty = MaterialEditorUtils.FindProperty(bumpScaleID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUI.BeginChangeCheck();
            if (normalMapProperty.textureValue != null)
            {
                materialEditor.TexturePropertySingleLine(label, normalMapProperty, bumpScaleProperty);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(label, normalMapProperty);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasNormalMap = material.GetTexture(normalMapProperty.name) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {useNormalMapKeyword} - {hasNormalMap}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(useNormalMapKeyword, hasNormalMap);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        public override bool IsValid()
        {
            return normalMapProperty != null && bumpScaleProperty != null;
        }
    }
}