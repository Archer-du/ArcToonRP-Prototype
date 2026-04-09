using System;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor
{
    /// <summary>
    /// Shared GUI layout utilities for both ShaderGUI panels and CustomEditor inspectors.
    /// Provides consistent visual styling (foldout groups, box styles, toggle groups, indentation).
    /// </summary>
    public static class EditorGUILayoutUtils
    {
        public static readonly int GUIComponentSpace = 3;

        public static readonly int GUIComponentIndentLevel = 2;

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

        /// <summary>
        /// Draws a foldout group header with ShurikenModuleTitle style.
        /// 14px bold font, 30px height.
        /// </summary>
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

        /// <summary>
        /// Begins a toggle property group. The toggle controls whether child fields are enabled.
        /// </summary>
        public static bool BeginTogglePropertyGroup(GUIContent label, bool toggle, GUIStyle style)
        {
            toggle = EditorGUILayout.ToggleLeft(label, toggle, style);
            EditorGUI.BeginDisabledGroup(!toggle);
            GUILayout.BeginVertical();
            return toggle;
        }

        /// <summary>
        /// Ends a toggle property group started by BeginTogglePropertyGroup.
        /// </summary>
        public static void EndTogglePropertyGroup()
        {
            GUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Increases indent level for component content.
        /// </summary>
        public static void BeginGUIComponentIndent()
        {
            EditorGUI.indentLevel += GUIComponentIndentLevel;
        }

        /// <summary>
        /// Restores indent level after component content.
        /// </summary>
        public static void EndGUIComponentIndent()
        {
            EditorGUI.indentLevel -= GUIComponentIndentLevel;
        }
    }
}
