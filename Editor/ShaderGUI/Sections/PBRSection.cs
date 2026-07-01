using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    public class PBRSection : ShaderGUISectionBase
    {
        private MaterialProperty metallicMapProperty;
        private MaterialProperty metallicMapChannelProperty;
        private MaterialProperty roughnessMapProperty;
        private MaterialProperty roughnessMapChannelProperty;
        private MaterialProperty occlusionMapProperty;
        private MaterialProperty occlusionMapChannelProperty;

        private MaterialProperty metallicProperty;
        private MaterialProperty roughnessProperty;
        private MaterialProperty occlusionProperty;
        private MaterialProperty fresnelProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            metallicMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.MetallicMap, props, false);
            metallicMapChannelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.MetallicMapChannel, props, false);
            roughnessMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RoughnessMap, props, false);
            roughnessMapChannelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RoughnessMapChannel, props, false);
            occlusionMapProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.OcclusionMap, props, false);
            occlusionMapChannelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.OcclusionMapChannel, props, false);

            metallicProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Metallic, props, false);
            roughnessProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Roughness, props, false);
            occlusionProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Occlusion, props, false);
            fresnelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.Fresnel, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            DrawMapRow(materialEditor, materials, "Metallic Map",
                metallicMapProperty, metallicMapChannelProperty, ShaderKeywords.METALLIC_MAP);
            DrawMapRow(materialEditor, materials, "Roughness Map",
                roughnessMapProperty, roughnessMapChannelProperty, ShaderKeywords.ROUGHNESS_MAP);
            DrawMapRow(materialEditor, materials, "Occlusion Map",
                occlusionMapProperty, occlusionMapChannelProperty, ShaderKeywords.OCCLUSION_MAP);

            materialEditor.BuiltinShaderPropertyDrawer(metallicProperty, true, "Metallic");
            materialEditor.BuiltinShaderPropertyDrawer(roughnessProperty, true, "Roughness");
            materialEditor.BuiltinShaderPropertyDrawer(occlusionProperty, true, "Occlusion");
            materialEditor.BuiltinShaderPropertyDrawer(fresnelProperty, true, "Fresnel");
        }

        private void DrawMapRow(MaterialEditor materialEditor, Material[] materials, string label,
            MaterialProperty mapProperty, MaterialProperty channelProperty, string useMapKeyword)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.TexturePropertySingleLine(new GUIContent(label), mapProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasMap = material.GetTexture(mapProperty.name) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {useMapKeyword} - {hasMap}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(useMapKeyword, hasMap);
                    EditorUtility.SetDirty(material);
                }
            }

            if (mapProperty.textureValue != null)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                materialEditor.BuiltinShaderPropertyDrawer(channelProperty, true, "Channel");
                EditorGUILayoutUtils.EndGUIComponentIndent();
            }
        }

        public override bool IsValid()
        {
            return metallicMapProperty != null && roughnessMapProperty != null && occlusionMapProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            SyncMapKeyword(material, ShaderPropertyID.MetallicMap, ShaderKeywords.METALLIC_MAP);
            SyncMapKeyword(material, ShaderPropertyID.RoughnessMap, ShaderKeywords.ROUGHNESS_MAP);
            SyncMapKeyword(material, ShaderPropertyID.OcclusionMap, ShaderKeywords.OCCLUSION_MAP);
        }

        private static void SyncMapKeyword(Material material, string mapID, string useMapKeyword)
        {
            if (!material.HasProperty(mapID)) return;
            material.SetKeyword(useMapKeyword, material.GetTexture(mapID) != null);
        }
    }
}
