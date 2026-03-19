using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class HeaderPropertySection : ShaderGUISectionBase
    {
        private readonly string headerLabel;
        
        private readonly string[] propertyIDs;
        private readonly string[] propertyLabels;
        private readonly List<MaterialProperty> properties = new();

        public HeaderPropertySection(string headerLabel, string[] propertyLabels, string[] propertyIDs)
        {
            this.propertyIDs = propertyIDs;
            this.propertyLabels = propertyLabels;
            this.headerLabel = headerLabel;
        }
        
        public HeaderPropertySection(string headerLabel, string[] propertyIDs)
        {
            this.propertyIDs = propertyIDs;
            this.propertyLabels = Array.Empty<string>();
            this.headerLabel = headerLabel;
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            properties.Clear();
            foreach (var propertyID in propertyIDs)
            {
                var property = MaterialEditorUtils.FindProperty(propertyID, props, false);
                properties.Add(property);
            }
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (properties.Count == 0) return;
            
            EditorGUILayout.LabelField(headerLabel, EditorStyles.label);
            EditorGUILayout.BeginVertical();
            EditorGUILayoutUtils.BeginGUIComponentIndent();
            
            for (int i = 0; i < properties.Count; i++)
            {
                string label = i < propertyLabels.Length ? propertyLabels[i] : properties[i].displayName;
                materialEditor.BuiltinShaderPropertyDrawer(properties[i], false, label);
            }
            
            EditorGUILayoutUtils.EndGUIComponentIndent();
            EditorGUILayout.EndVertical();
        }

        public override bool IsValid()
        {
            return properties.All(x => x != null);
        }
    }
}