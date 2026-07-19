using ArcToon.Editor;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public enum RegionIDMode
    {
        None = 0,
        Texture = 1,
        VertexColor = 2,
    }

    public class RegionIDSection : ShaderGUISectionBase
    {
        private static readonly GUIContent headerLabel = new("Region ID");
        private static readonly GUIContent regionCountLabel = new("Region Count");
        private static readonly GUIContent idModeLabel = new("ID Source");
        private static readonly GUIContent idChannelLabel = new("ID Channel");
        private static readonly GUIContent idMapLabel = new("ID Map");
        private static readonly GUIContent regionSelectLabel = new("Active Region");

        private MaterialProperty regionCountProperty;
        private MaterialProperty regionIDChannelProperty;
        private MaterialProperty regionIDMapProperty;

        // Persisted across frames so the Popup keeps its state; also mirrored into
        // SectionContext.SelectedRegion each frame for downstream sections to read.
        private int selectedRegion;

        public override void FindProperties(MaterialProperty[] props)
        {
            regionCountProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RegionCount, props, false);
            regionIDChannelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RegionIDChannel, props, false);
            regionIDMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RegionIDMap, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            EditorGUILayout.LabelField(headerLabel, EditorStyles.boldLabel);
            EditorGUILayoutUtils.BeginGUIComponentIndent();

            // Region Count (1~8)
            EditorGUI.BeginChangeCheck();
            int count = regionCountProperty.intValue;
            count = EditorGUILayout.IntSlider(regionCountLabel, count, 1, 8);
            if (EditorGUI.EndChangeCheck())
            {
                regionCountProperty.intValue = count;
            }

            // ID Source Mode
            RegionIDMode currentMode = GetRegionIDMode(materials[0]);
            EditorGUI.BeginChangeCheck();
            RegionIDMode newMode = (RegionIDMode)EditorGUILayout.EnumPopup(idModeLabel, currentMode);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.REGION_ID_TEXTURE, newMode == RegionIDMode.Texture);
                    material.SetKeyword(ShaderKeywords.REGION_ID_VERTEX_COLOR, newMode == RegionIDMode.VertexColor);
                    EditorUtility.SetDirty(material);
                }
            }

            // Channel selector
            if (newMode != RegionIDMode.None)
            {
                materialEditor.BuiltinShaderPropertyDrawer(regionIDChannelProperty, true, idChannelLabel.text);
            }

            // ID Map (only in Texture mode)
            if (newMode == RegionIDMode.Texture && regionIDMapProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(regionIDMapProperty, true, idMapLabel.text);
            }

            // Region selector dropdown
            if (count > 1)
            {
                string[] regionLabels = new string[count];
                for (int i = 0; i < count; i++)
                {
                    regionLabels[i] = $"Region {i}";
                }
                selectedRegion = Mathf.Clamp(selectedRegion, 0, count - 1);
                selectedRegion = EditorGUILayout.Popup(regionSelectLabel, selectedRegion, regionLabels);
            }
            else
            {
                selectedRegion = 0;
            }

            // Publish to shared context so downstream sections pick up the active region.
            if (Context != null)
            {
                Context.SelectedRegion = selectedRegion;
            }

            EditorGUILayoutUtils.EndGUIComponentIndent();
        }

        public override bool IsValid()
        {
            return regionCountProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            RegionIDMode mode = GetRegionIDMode(material);
            material.SetKeyword(ShaderKeywords.REGION_ID_TEXTURE, mode == RegionIDMode.Texture);
            material.SetKeyword(ShaderKeywords.REGION_ID_VERTEX_COLOR, mode == RegionIDMode.VertexColor);
        }

        private static RegionIDMode GetRegionIDMode(Material material)
        {
            if (material.IsKeywordEnabled(ShaderKeywords.REGION_ID_TEXTURE))
                return RegionIDMode.Texture;
            if (material.IsKeywordEnabled(ShaderKeywords.REGION_ID_VERTEX_COLOR))
                return RegionIDMode.VertexColor;
            return RegionIDMode.None;
        }
    }
}
