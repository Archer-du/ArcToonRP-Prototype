using System;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public static class ShaderGUILayout
    {
        public static void PredicateMaterialArrayBoolProperty(Material[] materials, Func<Material, bool> predicate, out bool hasMixedValue, out bool consistentValue)
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
            consistentValue = !hasMixedValue && firstOrOnlyValue;
        }
    }
}