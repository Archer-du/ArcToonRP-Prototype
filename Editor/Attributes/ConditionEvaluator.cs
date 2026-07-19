using System;
using System.Collections.Generic;
using System.Reflection;
using ArcToon.Settings.Attributes;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.Attributes
{
    /// <summary>
    /// Evaluates [ShowIf] / [ShowIfEnum] conditions for a field against its sibling properties.
    /// The sibling source is the containing (parent) SerializedProperty, so FindPropertyRelative
    /// resolves within the same struct/class scope.
    /// Fail-open: a missing / unsupported condition field logs once and shows the field.
    /// </summary>
    internal static class ConditionEvaluator
    {
        private static readonly HashSet<string> warnedPaths = new();

        /// <summary>
        /// Returns true if all [ShowIf] and the optional [ShowIfEnum] on the field pass (AND-combined).
        /// A null FieldInfo (unresolved) is treated as unconditional (shown).
        /// </summary>
        public static bool PassesAll(SerializedProperty parent, FieldInfo field)
        {
            if (field == null || parent == null) return true;

            foreach (var showIf in field.GetCustomAttributes<ShowIfAttribute>())
            {
                if (!EvaluateShowIf(parent, showIf, field)) return false;
            }

            var showIfEnum = field.GetCustomAttribute<ShowIfEnumAttribute>();
            if (showIfEnum != null && !EvaluateShowIfEnum(parent, showIfEnum, field))
            {
                return false;
            }

            return true;
        }

        private static bool EvaluateShowIf(SerializedProperty parent, ShowIfAttribute attr, FieldInfo field)
        {
            var condition = parent.FindPropertyRelative(attr.ConditionField);
            if (condition == null)
            {
                WarnOnce(field, attr.ConditionField);
                return true;
            }

            var current = ReadLong(condition);
            return attr.Operator switch
            {
                CompareOp.Equal => current == attr.CompareValue,
                CompareOp.GreaterEqual => current >= attr.CompareValue,
                _ => true,
            };
        }

        private static bool EvaluateShowIfEnum(SerializedProperty parent, ShowIfEnumAttribute attr, FieldInfo field)
        {
            var condition = parent.FindPropertyRelative(attr.ConditionField);
            if (condition == null)
            {
                WarnOnce(field, attr.ConditionField);
                return true;
            }

            var current = ReadLong(condition);
            foreach (var value in attr.Values)
            {
                if (Convert.ToInt64(value) == current) return true;
            }
            return false;
        }

        // Underlying stored value for integer / bool / enum properties (not the enum ordinal).
        private static long ReadLong(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.longValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? 1 : 0;
                case SerializedPropertyType.Enum:
                    return property.intValue;
                default:
                    return 0;
            }
        }

        private static void WarnOnce(FieldInfo field, string missingField)
        {
            var key = $"{field.DeclaringType?.FullName}.{field.Name}->{missingField}";
            if (warnedPaths.Add(key))
            {
                Debug.LogWarning(
                    $"[ArcToonRP] AttributeInspectorDrawer: condition field '{missingField}' " +
                    $"not found next to '{field.DeclaringType?.Name}.{field.Name}'. Showing field unconditionally.");
            }
        }
    }
}
