using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public static class ShaderGUILayout
    {
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
    }
}