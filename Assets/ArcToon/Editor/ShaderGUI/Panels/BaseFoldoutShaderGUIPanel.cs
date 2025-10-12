using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Components;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Panels
{
    public class BaseFoldoutShaderGUIPanel
    {
        private readonly string groupName;
        private readonly List<IShaderGUIComponent> components = null;

        private bool foldoutDisplay = true;
        
        public BaseFoldoutShaderGUIPanel(string groupName, List<IShaderGUIComponent> components)
        {
            this.groupName = groupName;
            this.components = components;
        }
        
        public void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            var materials = MaterialEditorUtils.GetTargetMaterials(materialEditor);
            
            foreach (var component in components)
            {
                component.FindProperties(props);
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutDisplay = DrawFoldoutGroup(foldoutDisplay, groupName);
            if (foldoutDisplay)
            {
                EditorGUILayout.Space(6);
                const int indentLevelOffset = 2;
                int originIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel += indentLevelOffset;

                bool isFirstComponent = true;
                foreach (var comp in components)
                {
                    if (isFirstComponent)
                    {
                        isFirstComponent = false;
                    }
                    else
                    {
                        GUILayout.Space(3);
                    }
                    comp.DrawGUI(materialEditor, materials);
                }
                
                EditorGUI.indentLevel = originIndentLevel;
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }
        
        protected bool DrawFoldoutGroup(bool display, string title)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.boldLabel).font;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            style.border = new RectOffset(15, 7, 4, 4);
            const float rectHeight = 30f;
            const float rectWidth = 16f;
            style.fixedHeight = rectHeight;
            style.contentOffset = new Vector2(30f, -2f);

            var rect = GUILayoutUtility.GetRect(rectWidth, rectHeight, style);
            GUI.Box(rect, title, style);

            var evt = Event.current;

            var toggleSize = 16f;
            var toggleRect = new Rect(rect.x + 8f, rect.y + 4f, toggleSize, toggleSize);
            if (evt.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                display = !display;
                evt.Use();
            }

            return display;
        }
    }
}