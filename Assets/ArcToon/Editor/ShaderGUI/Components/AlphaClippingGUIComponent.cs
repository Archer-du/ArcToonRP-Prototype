using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class AlphaClippingGUIComponent : IShaderGUIComponent
    {
        private readonly string alphaClippingKeyword;

        private readonly string useAlphaClipID;
        private MaterialProperty useAlphaClipProperty;
        private readonly string cutOffID;
        private MaterialProperty cutOffProperty;
        
        private readonly GUIContent label;

        public AlphaClippingGUIComponent(string alphaClippingKeyword, string useAlphaClipID, string cutOffID, string labelName)
        {
            this.alphaClippingKeyword = alphaClippingKeyword;
            this.useAlphaClipID = useAlphaClipID;
            this.cutOffID = cutOffID;
            label = new GUIContent(labelName);
        }
        public void FindProperties(MaterialProperty[] props)
        {
            useAlphaClipProperty = MaterialEditorUtils.FindProperty(useAlphaClipID, props, false);
            cutOffProperty = MaterialEditorUtils.FindProperty(cutOffID, props, false);
        }

        public void DrawGUI(MaterialEditor materialEditor, Material[] materials)
        {
            bool shouldToggleGroup = !useAlphaClipProperty.hasMixedValue && Mathf.Approximately(useAlphaClipProperty.floatValue, 1);
            EditorGUI.showMixedValue = useAlphaClipProperty.hasMixedValue;
            
            EditorGUI.BeginChangeCheck();
            bool newClippingValue = ShaderGUILayout.BeginTogglePropertyGroup(label, shouldToggleGroup, EditorStyles.label);
            EditorGUI.showMixedValue = false;
            
            if (EditorGUI.EndChangeCheck())
            {
                useAlphaClipProperty.floatValue = newClippingValue ? 1f : 0f;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Clipping: {newClippingValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(alphaClippingKeyword, newClippingValue);
                    EditorUtility.SetDirty(material);
                }
            }
            const int indentLevelOffset = 2;
            int originIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel += indentLevelOffset;
            
            materialEditor.BuiltinShaderPropertyDrawer(cutOffProperty);
            
            EditorGUI.indentLevel = originIndentLevel;
            ShaderGUILayout.EndTogglePropertyGroup();
        }
    }
}