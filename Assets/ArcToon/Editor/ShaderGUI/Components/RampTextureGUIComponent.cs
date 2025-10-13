using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class RampTextureGUIComponent : ShaderGUIComponentBase
    {
        private readonly string useRampSetKeyword;
        
        private readonly string rampTextureID;
        private MaterialProperty rampTextureProperty;
        
        private readonly GUIContent label;

        public RampTextureGUIComponent(string useRampSetKeyword, string rampTextureID, string labelName)
        {
            this.useRampSetKeyword = useRampSetKeyword;
            this.rampTextureID = rampTextureID;
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            rampTextureProperty = MaterialEditorUtils.FindProperty(rampTextureID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.TexturePropertySingleLine(label, rampTextureProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasRampSet = material.GetTexture(rampTextureProperty.name) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {useRampSetKeyword} - {hasRampSet}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(useRampSetKeyword, hasRampSet);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        public override bool IsValid()
        {
            return rampTextureProperty != null;
        }
    }
}