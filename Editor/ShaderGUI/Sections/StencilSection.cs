using ArcToon.Utils;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class StencilSection : ShaderGUISectionBase
    {
        private static readonly GUIContent toggleLabel = new("Stencil");
        private static readonly GUIContent stencilRefLabel = new("Ref Value");
        private static readonly GUIContent writeMaskLabel = new("Write Mask");
        private static readonly GUIContent readMaskLabel = new("Read Mask");

        private MaterialProperty stencilEnabledProperty;
        private MaterialProperty stencilProperty;
        private MaterialProperty stencilWriteMaskProperty;
        private MaterialProperty stencilReadMaskProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            stencilEnabledProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.StencilEnabled, props, false);
            stencilProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Stencil, props, false);
            stencilWriteMaskProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.StencilWriteMask, props, false);
            stencilReadMaskProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.StencilReadMask, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            bool shouldToggleGroup = !stencilEnabledProperty.hasMixedValue && Mathf.Approximately(stencilEnabledProperty.floatValue, 1);
            EditorGUI.showMixedValue = stencilEnabledProperty.hasMixedValue;

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayoutUtils.BeginTogglePropertyGroup(toggleLabel, shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                stencilEnabledProperty.floatValue = newValue ? 1f : 0f;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Stencil: {newValue}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetShaderPassEnabled(InternalShader.TagId.DepthStencil.name, newValue);
                    material.SetShaderPassEnabled(InternalShader.TagId.DepthOnly.name, !newValue);
                    EditorUtility.SetDirty(material);
                }
            }

            EditorGUILayoutUtils.BeginGUIComponentIndent();

            DrawBitToggleRow(stencilRefLabel, stencilProperty);
            DrawBitToggleRow(writeMaskLabel, stencilWriteMaskProperty);
            DrawBitToggleRow(readMaskLabel, stencilReadMaskProperty);

            EditorGUILayoutUtils.EndGUIComponentIndent();
            EditorGUILayoutUtils.EndTogglePropertyGroup();
        }

        private static readonly float bitButtonWidth = 15f;
        private static readonly float bitButtonSpacing = 1f;
        private static readonly float bitGroupWidth = bitButtonWidth + bitButtonSpacing;

        private static GUIStyle bitButtonStyle;

        private static GUIStyle GetBitButtonStyle()
        {
            if (bitButtonStyle == null)
            {
                bitButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 9,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    fixedHeight = 14f,
                    fixedWidth = bitButtonWidth,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            return bitButtonStyle;
        }

        private static void DrawBitToggleRow(GUIContent label, MaterialProperty property)
        {
            if (property == null) return;

            int currentValue = (int)property.floatValue;

            Rect lineRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth, lineRect.height);
            EditorGUI.LabelField(labelRect, label);

            if (property.hasMixedValue)
            {
                Rect mixedRect = new Rect(
                    labelRect.xMax, lineRect.y,
                    lineRect.width - labelRect.width, lineRect.height);
                EditorGUI.LabelField(mixedRect, "— (mixed)");
                return;
            }

            var style = GetBitButtonStyle();
            float startX = labelRect.xMax + 4f;

            EditorGUI.BeginChangeCheck();
            int newValue = currentValue;
            for (int bit = 7; bit >= 0; bit--)
            {
                float offsetX = startX + (7 - bit) * bitGroupWidth;
                Rect toggleRect = new Rect(offsetX, lineRect.y + 1f, bitButtonWidth, 14f);

                bool isOn = (currentValue & (1 << bit)) != 0;
                bool toggled = GUI.Toggle(toggleRect, isOn, isOn ? "1" : "0", style);
                if (toggled != isOn)
                {
                    if (toggled)
                        newValue |= (1 << bit);
                    else
                        newValue &= ~(1 << bit);
                }
            }

            float valueX = startX + 8 * bitGroupWidth + 8f;
            Rect valueRect = new Rect(valueX, lineRect.y, 50f, lineRect.height);
            EditorGUI.LabelField(valueRect, $"= {newValue}");

            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = newValue;
            }
        }

        public override bool IsValid()
        {
            return stencilEnabledProperty != null && stencilProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;

            bool stencilEnabled = material.GetFloat(ShaderPropertyID.StencilEnabled) > 0.5f;
            material.SetShaderPassEnabled(InternalShader.TagId.DepthStencil.name, stencilEnabled);
            material.SetShaderPassEnabled(InternalShader.TagId.DepthOnly.name, !stencilEnabled);
        }
    }
}
