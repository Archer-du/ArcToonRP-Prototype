using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace ArcToon.Editor.AttributeDrawing
{
    /// <summary>
    /// Maps a SerializedProperty to the backing FieldInfo so layout attributes can be read.
    /// Results are cached by (targetType, propertyPath). Handles the boundary cases that
    /// naive reflection gets wrong: List/array element paths (".Array.data[i]"), nested
    /// struct/class hosts, base-type field inheritance, and private [SerializeField] fields.
    /// </summary>
    internal static class SerializedPropertyReflection
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly Dictionary<(Type, string), FieldInfo> cache = new();

        /// <summary>
        /// Resolve the FieldInfo backing a SerializedProperty, or null if it cannot be resolved
        /// (e.g. the path points at a SerializeReference-managed value the host type doesn't declare).
        /// </summary>
        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            if (property?.serializedObject?.targetObject == null) return null;

            var targetType = property.serializedObject.targetObject.GetType();
            var key = (targetType, property.propertyPath);
            if (cache.TryGetValue(key, out var cached)) return cached;

            var resolved = ResolvePath(targetType, property.propertyPath);
            cache[key] = resolved;
            return resolved;
        }

        public static T GetAttribute<T>(SerializedProperty property) where T : Attribute
        {
            return GetFieldInfo(property)?.GetCustomAttribute<T>();
        }

        private static FieldInfo ResolvePath(Type hostType, string propertyPath)
        {
            // Collapse "foo.Array.data[3]" into "foo[3]" so array/list access is a single segment.
            var path = propertyPath.Replace(".Array.data[", "[");

            FieldInfo field = null;
            foreach (var segment in path.Split('.'))
            {
                var name = segment;
                var bracket = segment.IndexOf('[');
                var isElement = bracket >= 0;
                if (isElement) name = segment.Substring(0, bracket);

                field = FindInHierarchy(hostType, name);
                if (field == null) return null;

                var nextType = field.FieldType;
                if (isElement) nextType = GetEnumerableElementType(nextType) ?? nextType;
                hostType = nextType;
            }

            return field;
        }

        private static FieldInfo FindInHierarchy(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, Flags);
                if (field != null) return field;
            }
            return null;
        }

        private static Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType) return type.GetGenericArguments()[0];
            return null;
        }
    }
}
