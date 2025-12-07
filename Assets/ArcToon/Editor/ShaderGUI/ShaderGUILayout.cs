using System;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public static class ShaderGUILayout
    {
        public static int GUIComponentSpace = 3;

        public static int GUIComponentIndentLevel = 2;

        public static GUIStyle GUIComponentBoxStyle
        {
            get
            {
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 5, 5),
                    margin = new RectOffset(27, 12, 9, 9),
                };
                return boxStyle;
            }
        }
        
        public static GUIStyle GUIFoldoutGroupBoxStyle => new("ShurikenModuleTitle");

        public static bool DrawGUIComponentFoldoutGroup(bool display, string title)
        {
            var style = GUIFoldoutGroupBoxStyle;
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
        
        public static bool BeginTogglePropertyGroup(GUIContent label, bool toggle, GUIStyle style)
        {
            toggle = EditorGUILayout.ToggleLeft(label, toggle, style);
            EditorGUI.BeginDisabledGroup(!toggle);
            GUILayout.BeginVertical();
            return toggle;
        }

        public static void EndTogglePropertyGroup()
        {
            GUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        public static void BeginGUIComponentIndent()
        {
            EditorGUI.indentLevel += GUIComponentIndentLevel;
        }

        public static void EndGUIComponentIndent()
        {
            EditorGUI.indentLevel -= GUIComponentIndentLevel;
        }

        public static void PredicateMaterialArrayBoolProperty(Material[] materials, Func<Material, bool> predicate, out bool hasMixedValue, out bool shouldToggleGroup)
        {
            hasMixedValue = false;
            bool firstOrOnlyValue = predicate(materials[0]);
            for (int i = 1; i < materials.Length; i++)
            {
                if (predicate(materials[i]) != firstOrOnlyValue)
                {
                    hasMixedValue = true;
                    break;
                }
            }
            shouldToggleGroup = !hasMixedValue && firstOrOnlyValue;
        }
    }
}