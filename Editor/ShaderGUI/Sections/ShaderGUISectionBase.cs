using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public abstract class ShaderGUISectionBase
    {
        // Shared context, injected once per OnGUI frame by the owning panel before FindProperties runs.
        // Sections that need cross-section state (e.g. the active region index) read it directly.
        // Sections that don't need it can ignore it.
        protected SectionContext Context { get; private set; }

        protected int SelectedRegion => Context?.SelectedRegion ?? 0;

        public void SetContext(SectionContext context)
        {
            Context = context;
        }

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