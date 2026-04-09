using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public abstract class ShaderGUISectionBase
    {
        public abstract void FindProperties(MaterialProperty[] props);
        
        protected abstract void DrawProperties(MaterialEditor materialEditor, Material[] materials);

        public abstract bool IsValid();
        
        public void OnGUI(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.BeginVertical(EditorGUILayoutUtils.GUIComponentBoxStyle);
            DrawProperties(materialEditor, materials);
            EditorGUILayout.EndVertical();
        }
        
        public virtual void Refresh(Material material) { }
    }
}