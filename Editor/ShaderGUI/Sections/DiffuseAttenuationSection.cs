using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Sections
{
    // Consolidated diffuse-attenuation model selector. Two mutually-exclusive models:
    //   RampSigmoid     - sigmoid thresholding, optionally colorized by a Ramp Set texture.
    //   LinearPartition - 7-band energy-conserving partition, per-band tint + per-region base colors.
    // The model is persisted in _AttenuationModel and drives the _ATTEN_LINEAR_PARTITION keyword
    // (keyword is the shader-side source of truth). _RAMP_SET is forced off in LinearPartition mode.
    public class DiffuseAttenuationSection : ShaderGUISectionBase
    {
        private static readonly GUIContent rampLabel = new("Ramp Set");

        private MaterialProperty attenuationModelProperty;
        private MaterialProperty rampTextureProperty;
        private MaterialProperty attenOffsetProperty;
        private MaterialProperty attenSmoothProperty;
        private MaterialProperty specOffsetProperty;
        private MaterialProperty specSmoothProperty;

        private MaterialProperty albedoSmoothnessProperty;
        private MaterialProperty diffuseOffsetProperty;
        private MaterialProperty shadowFadeTintProperty;
        private MaterialProperty shadowTintProperty;
        private MaterialProperty shallowFadeTintProperty;
        private MaterialProperty shallowTintProperty;
        private MaterialProperty sssTintProperty;
        private MaterialProperty frontTintProperty;
        private MaterialProperty shadowColorProperty;
        private MaterialProperty shallowColorProperty;

        public override void FindProperties(MaterialProperty[] props)
        {
            attenuationModelProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.AttenuationModel, props, false);
            rampTextureProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RampSet, props, false);
            attenOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DirectLightAttenOffset, props, false);
            attenSmoothProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DirectLightAttenSmoothNew, props, false);
            specOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DirectLightSpecOffset, props, false);
            specSmoothProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DirectLightSpecSmooth, props, false);

            albedoSmoothnessProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.AlbedoSmoothness, props, false);
            diffuseOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.DiffuseOffset, props, false);
            shadowFadeTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ShadowFadeTint, props, false);
            shadowTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ShadowTint, props, false);
            shallowFadeTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ShallowFadeTint, props, false);
            shallowTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ShallowTint, props, false);
            sssTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.SSSTint, props, false);
            frontTintProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.FrontTint, props, false);
            shadowColorProperty = MaterialEditorUtils.FindRegionProperty(ShaderPropertyID.ShadowColor, SelectedRegion, props);
            shallowColorProperty = MaterialEditorUtils.FindRegionProperty(ShaderPropertyID.ShallowColor, SelectedRegion, props);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;

            // Shaders that haven't opted into the linear-partition model omit _AttenuationModel;
            // they show only the Ramp/Sigmoid controls, with no model selector.
            if (attenuationModelProperty == null)
            {
                DrawRampSigmoid(materialEditor, materials);
                return;
            }

            EditorGUI.showMixedValue = attenuationModelProperty.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var newModel = (AttenuationModel)EditorGUILayout.EnumPopup("Attenuation Model",
                (AttenuationModel)attenuationModelProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                attenuationModelProperty.intValue = (int)newModel;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Attenuation Model: {newModel}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    ApplyModelKeywords(material, newModel);
                    EditorUtility.SetDirty(material);
                }
            }
            EditorGUI.showMixedValue = false;

            EditorGUILayoutUtils.BeginGUIComponentIndent();
            if (newModel == AttenuationModel.LinearPartition)
            {
                DrawLinearPartition(materialEditor);
            }
            else
            {
                DrawRampSigmoid(materialEditor, materials);
            }
            EditorGUILayoutUtils.EndGUIComponentIndent();
        }

        private void DrawRampSigmoid(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.TexturePropertySingleLine(rampLabel, rampTextureProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasRampSet = material.GetTexture(ShaderPropertyID.RampSet) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {ShaderKeywords.RAMP_SET} - {hasRampSet}");

                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.RAMP_SET, hasRampSet);
                    EditorUtility.SetDirty(material);
                }
            }

            materialEditor.BuiltinShaderPropertyDrawer(attenOffsetProperty, true, "Sigmoid Attenuation Offset");
            materialEditor.BuiltinShaderPropertyDrawer(attenSmoothProperty, true, "Sigmoid Attenuation Smooth");
            materialEditor.BuiltinShaderPropertyDrawer(specOffsetProperty, true, "Sigmoid Specular Offset");
            materialEditor.BuiltinShaderPropertyDrawer(specSmoothProperty, true, "Sigmoid Specular Smooth");
        }

        private void DrawLinearPartition(MaterialEditor materialEditor)
        {
            materialEditor.BuiltinShaderPropertyDrawer(diffuseOffsetProperty, true, "Diffuse Offset");
            materialEditor.BuiltinShaderPropertyDrawer(albedoSmoothnessProperty, true, "Albedo Smoothness");

            materialEditor.BuiltinShaderPropertyDrawer(shadowFadeTintProperty, true, "Shadow Fade Tint");
            materialEditor.BuiltinShaderPropertyDrawer(shadowTintProperty, true, "Shadow Tint");
            materialEditor.BuiltinShaderPropertyDrawer(shallowFadeTintProperty, true, "Shallow Fade Tint");
            materialEditor.BuiltinShaderPropertyDrawer(shallowTintProperty, true, "Shallow Tint");
            materialEditor.BuiltinShaderPropertyDrawer(sssTintProperty, true, "SSS Tint");
            materialEditor.BuiltinShaderPropertyDrawer(frontTintProperty, true, "Front Tint");

            if (shadowColorProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(shadowColorProperty, true, $"Shadow Color (Region {SelectedRegion})");
            }
            if (shallowColorProperty != null)
            {
                materialEditor.BuiltinShaderPropertyDrawer(shallowColorProperty, true, $"Shallow Color (Region {SelectedRegion})");
            }
        }

        private static void ApplyModelKeywords(Material material, AttenuationModel model)
        {
            bool partition = model == AttenuationModel.LinearPartition;
            material.SetKeyword(ShaderKeywords.ATTEN_LINEAR_PARTITION, partition);
            // RampSigmoid mode derives _RAMP_SET from texture presence; partition mode forces it off.
            bool hasRampSet = !partition && material.GetTexture(ShaderPropertyID.RampSet) != null;
            material.SetKeyword(ShaderKeywords.RAMP_SET, hasRampSet);
        }

        public override bool IsValid()
        {
            return attenOffsetProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            if (!material.HasProperty(ShaderPropertyID.AttenuationModel)) return;
            ApplyModelKeywords(material, (AttenuationModel)material.GetInteger(ShaderPropertyID.AttenuationModel));
        }
    }
}
