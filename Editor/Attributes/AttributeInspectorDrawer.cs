using System;
using System.Collections.Generic;
using ArcToon.Settings.Attributes;
using UnityEditor;

namespace ArcToon.Editor.Attributes
{
    /// <summary>
    /// Recursively draws a SerializedProperty tree using layout attributes read from the backing
    /// fields ([FoldoutGroup] / [BoxGroup] / [ShowIf] / [ShowIfEnum] / [HelpBoxIfNull]).
    /// Every leaf is drawn via EditorGUILayout.PropertyField, so Undo, prefab-override markers,
    /// and multi-object editing all keep working. Visual styling reuses EditorGUILayoutUtils to
    /// stay pixel-consistent with the previous hand-written inspector.
    /// </summary>
    public static class AttributeInspectorDrawer
    {
        private const string SessionKeyPrefix = "ArcToonAttrGUI_";
        private const float FoldoutSpacing = 6f;

        /// <summary>
        /// Entry point. Draws all serialized children of <paramref name="root"/>.
        /// Top-level children without a [FoldoutGroup] are collected into an implicit "General" group,
        /// drawn first (matching the previous layout); grouped children follow in declaration order.
        /// </summary>
        public static void DrawProperties(SerializedProperty root)
        {
            if (root == null) return;

            var children = GetDirectChildren(root);
            var ungrouped = new List<SerializedProperty>();
            var grouped = new List<SerializedProperty>();
            foreach (var child in children)
            {
                if (SerializedPropertyReflection.GetAttribute<FoldoutGroupAttribute>(child) != null)
                    grouped.Add(child);
                else
                    ungrouped.Add(child);
            }

            var first = true;

            if (ungrouped.Count > 0)
            {
                DrawFoldoutGroup(root, "General", () =>
                {
                    foreach (var child in ungrouped) DrawChild(child, root);
                });
                first = false;
            }

            foreach (var child in grouped)
            {
                if (!first) EditorGUILayout.Space(FoldoutSpacing);
                DrawChild(child, root);
                first = false;
            }
        }

        /// <summary>
        /// Draw a single property: evaluate conditions, then dispatch to foldout / box / leaf.
        /// <paramref name="parent"/> is the sibling source used to resolve [ShowIf] conditions.
        /// </summary>
        private static void DrawChild(SerializedProperty property, SerializedProperty parent)
        {
            var field = SerializedPropertyReflection.GetFieldInfo(property);
            if (!ConditionEvaluator.PassesAll(parent, field)) return;

            var foldout = SerializedPropertyReflection.GetAttribute<FoldoutGroupAttribute>(property);
            if (foldout != null)
            {
                DrawFoldoutGroup(property, foldout.Title, () => DrawBody(property));
                return;
            }

            var box = SerializedPropertyReflection.GetAttribute<BoxGroupAttribute>(property);
            if (box != null)
            {
                DrawBoxSection(box.Title, () => DrawBody(property));
                return;
            }

            DrawLeaf(property);
        }

        /// <summary>
        /// Draw the body of a group: recurse into a nested serializable's children, or draw the
        /// property itself as a leaf when it has no inline children (e.g. an Object reference).
        /// </summary>
        private static void DrawBody(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
            {
                foreach (var child in GetDirectChildren(property)) DrawChild(child, property);
            }
            else
            {
                DrawLeaf(property);
            }
        }

        // A leaf field is the sole owner of horizontal indentation: it draws itself one indent
        // level in from its containing frame. Foldout / box frames add no indent of their own,
        // so nesting composes naturally (a box inside a foldout is offset only by its frame margin,
        // and the box's own fields are indented relative to the box).
        private static void DrawLeaf(SerializedProperty property)
        {
            EditorGUILayoutUtils.BeginGUIComponentIndent();

            EditorGUILayout.PropertyField(property, true);

            var helpBox = SerializedPropertyReflection.GetAttribute<HelpBoxIfNullAttribute>(property);
            if (helpBox != null
                && property.propertyType == SerializedPropertyType.ObjectReference
                && property.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(helpBox.Message, MapMessageType(helpBox.Level));
            }

            EditorGUILayoutUtils.EndGUIComponentIndent();
        }

        private static void DrawFoldoutGroup(SerializedProperty keyProperty, string title, Action drawContent)
        {
            var key = SessionKeyPrefix + KeyOf(keyProperty) + "/" + title;
            var expanded = SessionState.GetBool(key, true);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var newExpanded = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(expanded, title);
            if (newExpanded != expanded) SessionState.SetBool(key, newExpanded);

            if (newExpanded) drawContent();

            EditorGUILayout.EndVertical();
        }

        private static void DrawBoxSection(string title, Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorGUILayoutUtils.GUIComponentBoxStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            drawContent();
            EditorGUILayout.EndVertical();
        }

        private static IEnumerable<SerializedProperty> GetDirectChildren(SerializedProperty parent)
        {
            var iterator = parent.Copy();
            var end = parent.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (SerializedProperty.EqualContents(iterator, end)) break;
                yield return iterator.Copy();
            }
        }

        private static string KeyOf(SerializedProperty property)
        {
            var id = property.serializedObject.targetObject.GetInstanceID();
            return id + "_" + property.propertyPath;
        }

        private static MessageType MapMessageType(MessageTypeLevel level) => level switch
        {
            MessageTypeLevel.Info => MessageType.Info,
            MessageTypeLevel.Warning => MessageType.Warning,
            MessageTypeLevel.Error => MessageType.Error,
            _ => MessageType.None,
        };
    }
}
