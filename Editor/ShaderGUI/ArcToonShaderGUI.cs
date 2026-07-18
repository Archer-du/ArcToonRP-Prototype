using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Sections;
using ArcToon.Editor.ShaderEditor.Panels;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    // Abstract base: renders a fixed skeleton (RegionID header + a subclass-declared panel list)
    // and mirrors _BaseMap / _BaseColor to _MainTex / _Color for lightmapper compatibility.
    // Subclasses declare which shader features are exposed by returning their panel list from
    // BuildPanels(). No shader-name branching lives here.
    public abstract class ArcToonShaderGUI : ShaderGUI
    {
        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;

        // Shared per-OnGUI context injected into every section.
        // RegionIDSection writes SelectedRegion; downstream sections consume it.
        private readonly SectionContext sectionContext = new SectionContext();

        private RegionIDSection regionIDSection;
        private IReadOnlyList<BaseFoldoutShaderPanel> panels;

        // Subclasses declare which panels to draw, in order.
        protected abstract IReadOnlyList<BaseFoldoutShaderPanel> BuildPanels();

        // Whether to draw the RegionID header section above the panel list. Default: on.
        protected virtual bool UseRegionID => true;

        // Whether to mirror _BaseMap/_BaseColor into _MainTex/_Color on edit (for the lightmapper). Default: on.
        protected virtual bool CopyLightMappingOnChange => true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            EditorGUI.BeginChangeCheck();
            editor = materialEditor;
            materials = materialEditor.targets;
            properties = materialProperties;

            EnsureInitialized();
            sectionContext.Reset();

            if (UseRegionID)
            {
                var targetMaterials = MaterialEditorUtils.GetTargetMaterials(materialEditor);
                regionIDSection.SetContext(sectionContext);
                regionIDSection.FindProperties(materialProperties);
                if (regionIDSection.IsValid())
                {
                    regionIDSection.OnGUI(materialEditor, targetMaterials);
                }
            }

            foreach (var panel in panels)
            {
                panel.OnGUI(materialEditor, materialProperties, sectionContext);
            }

            if (EditorGUI.EndChangeCheck() && CopyLightMappingOnChange)
            {
                CopyLightMappingProperties();
            }
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            EnsureInitialized();

            if (UseRegionID)
            {
                regionIDSection.Refresh(material);
            }
            foreach (var panel in panels)
            {
                panel.Refresh(material);
            }
        }

        private void EnsureInitialized()
        {
            if (UseRegionID)
            {
                regionIDSection ??= new RegionIDSection();
            }
            panels ??= BuildPanels();
        }

        private void CopyLightMappingProperties()
        {
            MaterialProperty mainTex = FindProperty(ShaderPropertyID.MainTex, properties, false);
            MaterialProperty baseMap = FindProperty(ShaderPropertyID.BaseMap, properties, false);
            if (mainTex != null && baseMap != null)
            {
                mainTex.textureValue = baseMap.textureValue;
                mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
            }

            MaterialProperty color = FindProperty(ShaderPropertyID.Color, properties, false);
            MaterialProperty baseColor = FindProperty(ShaderPropertyID.BaseColor, properties, false);
            if (color != null && baseColor != null)
            {
                color.colorValue = baseColor.colorValue;
            }
        }
    }
}