using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class ShadowCasterSection : ShaderGUISectionBase
    {
        private readonly string casterOptionPropertyID;
        private MaterialProperty casterOptionProperty = null;

        public ShadowCasterSection(string casterOptionPropertyID)
        {
            this.casterOptionPropertyID = casterOptionPropertyID;
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            casterOptionProperty = MaterialEditorUtils.FindProperty(casterOptionPropertyID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUI.BeginChangeCheck();
            if ((casterOptionProperty.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(casterOptionProperty);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool enabled = casterOptionProperty.floatValue < (float)ShadowCasterOption.Off;
                    MaterialEditorUtils.ArcToonGUILog($"Set {material.name} Shadow Caster Option to: {casterOptionProperty.floatValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled("ShadowCaster", enabled);
                    if (enabled)
                    {
                        material.SetKeyword(ShaderKeywords.SHADOWS_DITHER, (ShadowCasterOption)casterOptionProperty.floatValue == ShadowCasterOption.Dither);
                    }
                    EditorUtility.SetDirty(material);
                }
            }
        }

        public override bool IsValid()
        {
            return casterOptionProperty != null;
        }
    }
}