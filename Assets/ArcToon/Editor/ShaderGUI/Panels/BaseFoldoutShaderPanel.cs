using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Components;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Panels
{
    public class BaseFoldoutShaderPanel
    {
        private readonly string groupName;
        private readonly List<ShaderGUIComponentBase> components = null;

        private bool foldoutDisplay = true;
        
        public BaseFoldoutShaderPanel(string groupName, List<ShaderGUIComponentBase> components)
        {
            this.groupName = groupName;
            this.components = components;
        }
        
        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            var materials = MaterialEditorUtils.GetTargetMaterials(materialEditor);
            
            foreach (var component in components)
            {
                component.FindProperties(props);
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutDisplay = ShaderGUILayout.DrawGUIComponentFoldoutGroup(foldoutDisplay, groupName);
            if (foldoutDisplay)
            {
                // EditorGUILayout.Space(ShaderGUILayout.GUIComponentSpace);
                // ShaderGUILayout.BeginGUIComponentIndent();

                foreach (var component in components)
                {
                    if (component.IsValid())
                    {
                        component.OnGUI(materialEditor, materials);
                    }
                }
                
                // ShaderGUILayout.EndGUIComponentIndent();
                // EditorGUILayout.Space(ShaderGUILayout.GUIComponentSpace);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }
    }
}