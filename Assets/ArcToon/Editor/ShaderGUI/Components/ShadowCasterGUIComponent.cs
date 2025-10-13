using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class ShadowCasterGUIComponent : ShaderGUIComponentBase
    {
        private readonly string casterOptionPropertyID;
        private MaterialProperty casterOptionProperty = null;

        enum ShadowMode
        {
            On,
            Clip,
            Dither,
            Off
        }
        
        public ShadowCasterGUIComponent(string casterOptionPropertyID)
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
                    bool enabled = casterOptionProperty.floatValue < (float)ShadowMode.Off;
                    MaterialEditorUtils.ArcToonGUILog($"Set {material.name} Shadow Caster Option to: {casterOptionProperty.floatValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled("ShadowCaster", enabled);
                    if (enabled)
                    {
                        material.SetKeyword("_SHADOWS_CLIP", (ShadowMode)casterOptionProperty.floatValue == ShadowMode.Clip);
                        material.SetKeyword("_SHADOWS_DITHER", (ShadowMode)casterOptionProperty.floatValue == ShadowMode.Dither);
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