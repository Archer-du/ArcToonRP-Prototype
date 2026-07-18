using ArcToon.Editor.ShaderEditor;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor
{
    public static class RegionIDMaterialMigration
    {
        [MenuItem("ArcToon/Migrate Materials (Region ID)")]
        public static void MigrateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int migrated = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                if (material.shader == null) continue;
                if (!material.shader.name.StartsWith("ArcToon/")) continue;

                if (MigrateMaterial(material))
                {
                    EditorUtility.SetDirty(material);
                    migrated++;
                }
            }

            if (migrated > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[ArcToon] Region ID migration complete: {migrated} material(s) updated.");
            }
            else
            {
                Debug.Log("[ArcToon] Region ID migration: no materials needed migration.");
            }
        }

        [MenuItem("ArcToon/Reset Outline Color Defaults")]
        public static void ResetOutlineColorDefaults()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int updated = 0;
            Color oldDefault = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            Color newDefault = new Color(0.1f, 0.1f, 0.1f, 1.0f);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                if (material.shader == null) continue;
                if (!material.shader.name.StartsWith("ArcToon/")) continue;

                bool changed = false;
                for (int i = 0; i < RegionPropertyHelper.MaxRegionCount; i++)
                {
                    string propName = MaterialEditorUtils.GetRegionPropertyName(
                        RegionPropertyHelper.BaseName.OutlineColor, i);
                    if (!material.HasProperty(propName)) continue;

                    Color current = material.GetColor(propName);
                    if (current == oldDefault)
                    {
                        material.SetColor(propName, newDefault);
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(material);
                    updated++;
                }
            }

            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[ArcToon] Outline color defaults reset: {updated} material(s) updated.");
            }
            else
            {
                Debug.Log("[ArcToon] Outline color defaults reset: no materials needed update.");
            }
        }

        private static bool MigrateMaterial(Material material)
        {
            bool changed = false;

            var so = new SerializedObject(material);
            var savedProps = so.FindProperty("m_SavedProperties");
            var colors = savedProps.FindPropertyRelative("m_Colors");
            var floats = savedProps.FindPropertyRelative("m_Floats");

            foreach (string oldName in RegionPropertyHelper.MigratedProperties)
            {
                string newName = MaterialEditorUtils.GetRegionPropertyName(oldName, 0);
                if (!material.HasProperty(newName)) continue;

                changed |= MigrateColorProperty(material, so, colors, oldName, newName);
                changed |= MigrateFloatProperty(material, so, floats, oldName, newName);
            }

            // Ensure _RegionCount defaults to 1
            if (material.HasProperty(ShaderPropertyID.RegionCount))
            {
                int regionCount = material.GetInteger(ShaderPropertyID.RegionCount);
                if (regionCount < 1)
                {
                    material.SetInteger(ShaderPropertyID.RegionCount, 1);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool MigrateColorProperty(Material material, SerializedObject so,
            SerializedProperty colors, string oldName, string newName)
        {
            for (int i = 0; i < colors.arraySize; i++)
            {
                var element = colors.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("first");
                if (key.stringValue != oldName) continue;

                Color oldValue = element.FindPropertyRelative("second").colorValue;
                Color currentNew = material.GetColor(newName);

                if (currentNew == new Color(0.5f, 0.5f, 0.5f, 1.0f) && oldValue != currentNew)
                {
                    material.SetColor(newName, oldValue);
                }

                colors.DeleteArrayElementAtIndex(i);
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
            return false;
        }

        private static bool MigrateFloatProperty(Material material, SerializedObject so,
            SerializedProperty floats, string oldName, string newName)
        {
            for (int i = 0; i < floats.arraySize; i++)
            {
                var element = floats.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("first");
                if (key.stringValue != oldName) continue;

                float oldValue = element.FindPropertyRelative("second").floatValue;
                float currentNew = material.GetFloat(newName);

                if (Mathf.Approximately(currentNew, 0.5f) && !Mathf.Approximately(oldValue, currentNew))
                {
                    material.SetFloat(newName, oldValue);
                }

                floats.DeleteArrayElementAtIndex(i);
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
            return false;
        }
    }
}
