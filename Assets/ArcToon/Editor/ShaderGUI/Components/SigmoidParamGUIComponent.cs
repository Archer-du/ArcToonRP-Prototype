using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class SigmoidParamGUIComponent : ShaderGUIComponentBase
    {
        private readonly string offsetID;
        private MaterialProperty offsetProperty;
        private readonly string smoothID;
        private MaterialProperty smoothProperty;
        
        private readonly GUIContent label;

        public SigmoidParamGUIComponent(string offsetID, string smoothID, string labelName)
        {
            this.offsetID = offsetID;
            this.smoothID = smoothID;
            label = new GUIContent(labelName);
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            offsetProperty = MaterialEditorUtils.FindProperty(offsetID, props, false);
            smoothProperty = MaterialEditorUtils.FindProperty(smoothID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField(label, EditorStyles.label);
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            ShaderGUILayout.BeginGUIComponentIndent();
            materialEditor.BuiltinShaderPropertyDrawer(offsetProperty, false, "Offset");
            materialEditor.BuiltinShaderPropertyDrawer(smoothProperty,  false, "Smooth");
            ShaderGUILayout.EndGUIComponentIndent();
            EditorGUILayout.EndVertical();
        }

        public override bool IsValid()
        {
            return offsetProperty != null && smoothProperty != null;
        }
    }
}