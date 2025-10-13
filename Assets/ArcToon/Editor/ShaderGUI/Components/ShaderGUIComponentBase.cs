using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public abstract class ShaderGUIComponentBase
    {
        public abstract void FindProperties(MaterialProperty[] props);
        
        protected abstract void DrawProperties(MaterialEditor materialEditor, Material[] materials);

        public abstract bool IsValid();
        
        public void OnGUI(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.BeginVertical(ShaderGUILayout.GUIComponentBoxStyle);
            DrawProperties(materialEditor, materials);
            EditorGUILayout.EndVertical();
        }
    }
}