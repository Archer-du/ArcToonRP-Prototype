using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class RampTextureComponent : ShaderGUIComponentBase
    {
        private MaterialProperty rampTextureProperty;
        
        private readonly GUIContent label;

        public RampTextureComponent(string labelName)
        {
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            rampTextureProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RampSet, props, false);
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
                    bool hasRampSet = material.GetTexture(ShaderPropertyID.RampSet) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {ShaderKeywords.RAMP_SET} - {hasRampSet}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.RAMP_SET, hasRampSet);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        public override bool IsValid()
        {
            return rampTextureProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            bool hasRampSet = material.GetTexture(ShaderPropertyID.RampSet) != null;
            material.SetKeyword(ShaderKeywords.RAMP_SET, hasRampSet);
        }
    }
}