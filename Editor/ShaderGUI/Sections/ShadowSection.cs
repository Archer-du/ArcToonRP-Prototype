using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class ShadowSection : ShaderGUISectionBase
    {
        private static readonly GUIContent label = new("Shadows");
        
        private MaterialProperty receiveShadowsProperty;
        private MaterialProperty receiveFringeShadowsProperty;
        private MaterialProperty casterOptionProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            receiveShadowsProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ReceiveShadows, props, false);
            receiveFringeShadowsProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ReceiveFringeShadows, props, false);
            casterOptionProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Shadows, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (casterOptionProperty != null)
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
                        MaterialEditorUtils.ArcToonGUILog($"Set {material.name} Shadow Caster Option to: {(ShadowCasterOption)casterOptionProperty.floatValue}");
                        
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
            if (receiveShadowsProperty != null && (receiveShadowsProperty.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(receiveShadowsProperty);
            }
            if (receiveFringeShadowsProperty != null && (receiveFringeShadowsProperty.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(receiveFringeShadowsProperty);
            }
        }

        public override bool IsValid()
        {
            return receiveShadowsProperty != null || casterOptionProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            ShadowCasterOption casterOptionValue = (ShadowCasterOption)material.GetFloat(ShaderPropertyID.Shadows);
            if (casterOptionValue < ShadowCasterOption.Off)
            {
                material.SetKeyword(ShaderKeywords.SHADOWS_DITHER, casterOptionValue == ShadowCasterOption.Dither);
            }
        }
    }
}