using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    // Screen-space rim light section. Exposes the three per-material rim parameters
    // consumed by RimLightInterface.hlsl (ScreenSpaceRimLight). Rim light itself is
    // unconditionally composed in ToonLightingAssembly.GetLighting, so this section
    // owns no keywords and performs no keyword writes; setting Scale to zero already
    // disables the effect at the shader math level.
    public class RimLightSection : ShaderGUISectionBase
    {
        private static readonly GUIContent HeaderLabel = new GUIContent("Rim Light");
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale",
            "Screen-space rim intensity. 0 disables rim contribution entirely.");
        private static readonly GUIContent WidthLabel = new GUIContent("Width",
            "Rim band width along the screen-space normal, in world-space texels.");
        private static readonly GUIContent DepthBiasLabel = new GUIContent("Depth Bias",
            "Minimum linear-depth gap (offset sample vs. current pixel) required to accept a texel as rim.");

        private MaterialProperty rimScaleProperty;
        private MaterialProperty rimWidthProperty;
        private MaterialProperty rimDepthBiasProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            rimScaleProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RimScale, props, false);
            rimWidthProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RimWidth, props, false);
            rimDepthBiasProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RimDepthBias, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField(HeaderLabel, EditorStyles.boldLabel);

            EditorGUILayoutUtils.BeginGUIComponentIndent();
            if (rimScaleProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(rimScaleProperty, true, ScaleLabel.text);
            }
            if (rimWidthProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(rimWidthProperty, true, WidthLabel.text);
            }
            if (rimDepthBiasProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(rimDepthBiasProperty, true, DepthBiasLabel.text);
            }
            EditorGUILayoutUtils.EndGUIComponentIndent();
        }

        public override bool IsValid()
        {
            return rimScaleProperty != null
                || rimWidthProperty != null
                || rimDepthBiasProperty != null;
        }
    }
}
