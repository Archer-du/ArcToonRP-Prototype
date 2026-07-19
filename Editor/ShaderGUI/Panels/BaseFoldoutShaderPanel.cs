using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Sections;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Panels
{
    public class BaseFoldoutShaderPanel
    {
        private readonly string groupName;
        private readonly List<ShaderGUISectionBase> components = null;

        private bool foldoutDisplay = true;
        
        public BaseFoldoutShaderPanel(string groupName, List<ShaderGUISectionBase> components)
        {
            this.groupName = groupName;
            this.components = components;
        }
        
        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props, SectionContext context)
        {
            var materials = MaterialEditorUtils.GetTargetMaterials(materialEditor);
            
            foreach (var component in components)
            {
                component.SetContext(context);
                component.FindProperties(props);
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutDisplay = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(foldoutDisplay, groupName);
            if (foldoutDisplay)
            {
                foreach (var component in components)
                {
                    if (component.IsValid())
                    {
                        component.OnGUI(materialEditor, materials);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        public void Refresh(Material material)
        {
            foreach (var component in components)
            {
                if (component.IsValid())
                {
                    component.Refresh(material);
                }
            }
        }
    }
}