using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArcToon.Passes.PostProcessing;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.Overrides
{
    /// <summary>
    /// Custom editor for PostProcessConfig.
    /// Renders the [SerializeReference] polymorphic list with proper foldouts,
    /// enable toggles, and an Add/Remove menu for each VolumeConfig subclass.
    /// </summary>
    [CustomEditor(typeof(PostProcessConfig))]
    public class PostProcessConfigEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, bool> foldoutStates = new();
        private static Type[] cachedVolumeConfigTypes;

        /// <summary>
        /// Discover all concrete subclasses of PostProcessVolumeConfig via reflection.
        /// </summary>
        private static Type[] GetAllVolumeConfigTypes()
        {
            if (cachedVolumeConfigTypes != null) return cachedVolumeConfigTypes;

            cachedVolumeConfigTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm =>
                {
                    try { return asm.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(PostProcessVolumeConfig)))
                .OrderBy(t => t.Name)
                .ToArray();

            return cachedVolumeConfigTypes;
        }

        /// <summary>
        /// Get a human-readable display name from the type name.
        /// e.g. "BloomVolumeConfig" -> "Bloom"
        /// </summary>
        private static string GetDisplayName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("VolumeConfig"))
                name = name[..^"VolumeConfig".Length];
            return name;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var config = (PostProcessConfig)target;
            var settingsList = config.settings;

            // Draw each existing VolumeConfig entry
            for (int i = 0; i < settingsList.Count; i++)
            {
                var item = settingsList[i];
                if (item == null)
                {
                    // Null entry - offer removal
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox($"Element {i} is null (type may have been removed).", MessageType.Warning);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(config, "Remove Null PostProcess VolumeConfig");
                        settingsList.RemoveAt(i);
                        EditorUtility.SetDirty(config);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                var itemType = item.GetType();
                if (!foldoutStates.ContainsKey(itemType))
                    foldoutStates[itemType] = true;

                // Header: foldout + enable toggle + remove button
                EditorGUILayout.BeginVertical(EditorGUILayoutUtils.GUIComponentBoxStyle);

                EditorGUILayout.BeginHorizontal();

                foldoutStates[itemType] = EditorGUILayout.Foldout(
                    foldoutStates[itemType], GetDisplayName(itemType), true, EditorStyles.foldoutHeader);

                // Enable toggle
                EditorGUI.BeginChangeCheck();
                item.enabled = EditorGUILayout.Toggle(item.enabled, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Toggle PostProcess VolumeConfig");
                    EditorUtility.SetDirty(config);
                }

                // Remove button
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    Undo.RecordObject(config, "Remove PostProcess VolumeConfig");
                    settingsList.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // Draw fields if expanded
                if (foldoutStates[itemType])
                {
                    EditorGUI.BeginDisabledGroup(!item.enabled);
                    EditorGUILayoutUtils.BeginGUIComponentIndent();

                    DrawVolumeConfigFields(config, item);

                    EditorGUILayoutUtils.EndGUIComponentIndent();
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndVertical();
            }

            // Add button with dropdown
            EditorGUILayout.Space(4);
            DrawAddButton(config, settingsList);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw all serializable fields of a VolumeConfig instance using reflection.
        /// Skips the 'enabled' field (drawn in header) and abstract properties.
        /// </summary>
        private void DrawVolumeConfigFields(PostProcessConfig config, PostProcessVolumeConfig item)
        {
            var itemType = item.GetType();
            var fields = itemType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.DeclaringType != typeof(PostProcessVolumeConfig)) // skip base fields
                .ToArray();

            foreach (var field in fields)
            {
                EditorGUI.BeginChangeCheck();
                var value = field.GetValue(item);
                var newValue = DrawField(field, value);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, $"Modify {GetDisplayName(itemType)}");
                    field.SetValue(item, newValue);
                    EditorUtility.SetDirty(config);
                }
            }
        }

        /// <summary>
        /// Draw a single field based on its type, respecting Range/Min/ColorUsage attributes.
        /// </summary>
        private object DrawField(FieldInfo field, object value)
        {
            var label = ObjectNames.NicifyVariableName(field.Name);
            var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
            var minAttr = field.GetCustomAttribute<MinAttribute>();
            var colorUsageAttr = field.GetCustomAttribute<ColorUsageAttribute>();

            var fieldType = field.FieldType;

            // Enum
            if (fieldType.IsEnum)
            {
                return EditorGUILayout.EnumPopup(label, (Enum)value);
            }

            // Float
            if (fieldType == typeof(float))
            {
                if (rangeAttr != null)
                    return EditorGUILayout.Slider(label, (float)value, rangeAttr.min, rangeAttr.max);
                if (minAttr != null)
                    return Mathf.Max(minAttr.min, EditorGUILayout.FloatField(label, (float)value));
                return EditorGUILayout.FloatField(label, (float)value);
            }

            // Int
            if (fieldType == typeof(int))
            {
                if (rangeAttr != null)
                    return EditorGUILayout.IntSlider(label, (int)value, (int)rangeAttr.min, (int)rangeAttr.max);
                if (minAttr != null)
                    return Mathf.Max((int)minAttr.min, EditorGUILayout.IntField(label, (int)value));
                return EditorGUILayout.IntField(label, (int)value);
            }

            // Bool
            if (fieldType == typeof(bool))
            {
                return EditorGUILayout.Toggle(label, (bool)value);
            }

            // Color
            if (fieldType == typeof(Color))
            {
                bool showAlpha = colorUsageAttr?.showAlpha ?? true;
                bool hdr = colorUsageAttr?.hdr ?? false;
                return EditorGUILayout.ColorField(new GUIContent(label), (Color)value, true, showAlpha, hdr);
            }

            // Vector3
            if (fieldType == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field(label, (Vector3)value);
            }

            // Vector2
            if (fieldType == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field(label, (Vector2)value);
            }

            // Vector4
            if (fieldType == typeof(Vector4))
            {
                return EditorGUILayout.Vector4Field(label, (Vector4)value);
            }

            // String
            if (fieldType == typeof(string))
            {
                return EditorGUILayout.TextField(label, (string)value);
            }

            // Texture / Texture2D
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                return EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, fieldType, false);
            }

            // Fallback: display as label
            EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
            return value;
        }

        /// <summary>
        /// Draw the "Add Effect" dropdown button.
        /// Only shows VolumeConfig types not already present in the list.
        /// </summary>
        private void DrawAddButton(PostProcessConfig config, List<PostProcessVolumeConfig> settingsList)
        {
            var existingTypes = settingsList
                .Where(s => s != null)
                .Select(s => s.GetType())
                .ToHashSet();

            var availableTypes = GetAllVolumeConfigTypes()
                .Where(t => !existingTypes.Contains(t))
                .ToArray();

            EditorGUI.BeginDisabledGroup(availableTypes.Length == 0);

            var rect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(rect, new GUIContent("Add Effect..."), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                foreach (var type in availableTypes)
                {
                    var capturedType = type;
                    menu.AddItem(new GUIContent(GetDisplayName(type)), false, () =>
                    {
                        Undo.RecordObject(config, "Add PostProcess VolumeConfig");
                        var instance = (PostProcessVolumeConfig)Activator.CreateInstance(capturedType);
                        settingsList.Add(instance);
                        // Sort by execution order
                        settingsList.Sort((a, b) => a.Order.CompareTo(b.Order));
                        EditorUtility.SetDirty(config);
                    });
                }
                menu.DropDown(rect);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
