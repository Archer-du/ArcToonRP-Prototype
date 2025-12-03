using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class ShadowComponent : ShaderGUIComponentBase
    {
        private static readonly GUIContent label = new("Shadows");
        
        private MaterialProperty receiveShadowsProperty;
        private MaterialProperty casterOptionProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            receiveShadowsProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ReceiveShadows, props, false);
            casterOptionProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Shadows, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (receiveShadowsProperty != null && (receiveShadowsProperty.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(receiveShadowsProperty);
            }
            
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
        }

        public override bool IsValid()
        {
            return receiveShadowsProperty != null || casterOptionProperty != null;
        }
    }
}