using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class TransparencyComponent : ShaderGUIComponentBase
    {
        private MaterialProperty transparencyModeProperty;
        private MaterialProperty premulAlphaProperty;
        
        private MaterialProperty srcBlendProperty;
        private MaterialProperty dstBlendProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            transparencyModeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.TransparencyMode, props, false);
            premulAlphaProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.PremulAlpha, props, false);
            srcBlendProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SrcBlend, props, false);
            dstBlendProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DstBlend, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(transparencyModeProperty);
            TransparencyMode currentMode = (TransparencyMode)transparencyModeProperty.intValue;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Transparency Mode: {(TransparencyMode)transparencyModeProperty.intValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled("ToonForwardTransparentBackFace", currentMode == TransparencyMode.OrderedDualFace);
                    material.SetShaderPassEnabled("ToonForwardTransparentFrontFace", currentMode == TransparencyMode.OrderedDualFace);
                    material.SetShaderPassEnabled("ToonForwardWeightedAverage", currentMode == TransparencyMode.WeightedAverage);
                    material.SetShaderPassEnabled("ToonForwardDepthPeeling", currentMode == TransparencyMode.DepthPeeling);
                    EditorUtility.SetDirty(material);
                }
            }
            if (currentMode == TransparencyMode.OrderedDualFace)
            {
                materialEditor.BuiltinShaderPropertyDrawer(premulAlphaProperty);
                srcBlendProperty.floatValue = premulAlphaProperty.intValue == 1 ? (float)BlendMode.One : (float)BlendMode.SrcAlpha;
                dstBlendProperty.floatValue = (float)BlendMode.OneMinusSrcAlpha;
            }
            else
            {
                srcBlendProperty.floatValue = (float)BlendMode.SrcAlpha;
                dstBlendProperty.floatValue = (float)BlendMode.OneMinusSrcAlpha;
            }
        }

        public override bool IsValid()
        {
            return transparencyModeProperty != null;
        }
    }
}