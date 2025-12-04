using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class LightMapSDFComponent : ShaderGUIComponentBase
    {
        private static readonly GUIContent label = new("SDF Light Map");
        
        private MaterialProperty lightMapSDFProperty;
        private MaterialProperty lightMapSDFSourceUVProperty;
        private MaterialProperty lightMapSDFOffsetProperty;
        private MaterialProperty faceVectorProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            lightMapSDFProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.LightMapSDF, props, false);
            lightMapSDFSourceUVProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.LightMapSDFSourceUV, props, false);
            lightMapSDFOffsetProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ShadowOffsetSDF, props, false);
            faceVectorProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.FaceVector, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.TexturePropertySingleLine(label, lightMapSDFProperty);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    bool hasLightMap = material.GetTexture(ShaderPropertyID.LightMapSDF) != null;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Keyword: {ShaderKeywords.SDF_LIGHT_MAP} - {hasLightMap}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.SDF_LIGHT_MAP, hasLightMap);
                    EditorUtility.SetDirty(material);
                }
            }
            
            ShaderGUILayout.BeginGUIComponentIndent();

            bool disabledAdvanced = lightMapSDFProperty.textureValue == null;
            EditorGUI.BeginDisabledGroup(disabledAdvanced);
            EditorGUI.BeginChangeCheck();
            materialEditor.BuiltinShaderPropertyDrawer(lightMapSDFSourceUVProperty, true, "UV Source");
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    MaterialEditorUtils.ArcToonGUILog($"Update {material.name} SDF Source UV: {(SDFSourceUV)lightMapSDFSourceUVProperty.intValue}");
                    
                    Undo.RecordObject(material, Undo.GetCurrentGroupName());
                    material.SetKeyword(ShaderKeywords.SDF_UV0, 
                        (SDFSourceUV)lightMapSDFSourceUVProperty.intValue == SDFSourceUV.UV0);
                    material.SetKeyword(ShaderKeywords.SDF_UV1, 
                        (SDFSourceUV)lightMapSDFSourceUVProperty.intValue == SDFSourceUV.UV1);
                    EditorUtility.SetDirty(material);
                }
            }
            materialEditor.BuiltinShaderPropertyDrawer(lightMapSDFOffsetProperty, true, "Shadow Offset");
            EditorGUI.BeginChangeCheck();
            var newFaceVectorValue = EditorGUILayout.Vector3Field("Face Vector", faceVectorProperty.vectorValue);
            if (EditorGUI.EndChangeCheck())
            {
                faceVectorProperty.vectorValue = new Vector4(newFaceVectorValue.x, newFaceVectorValue.y, newFaceVectorValue.z, 0);
            }
            EditorGUI.EndDisabledGroup();
            
            ShaderGUILayout.EndGUIComponentIndent();
        }

        public override bool IsValid()
        {
            return lightMapSDFProperty != null && lightMapSDFSourceUVProperty != null && lightMapSDFOffsetProperty != null && faceVectorProperty != null;
        }

        public override void Refresh(Material material)
        {
            base.Refresh(material);
            if (material == null) return;
            if (material.HasProperty(ShaderPropertyID.LightMapSDF))
            {
                bool hasLightMap = material.GetTexture(ShaderPropertyID.LightMapSDF) != null;
                material.SetKeyword(ShaderKeywords.SDF_LIGHT_MAP, hasLightMap);
            }

            if (material.HasProperty(ShaderPropertyID.LightMapSDFSourceUV))
            {
                int value = material.GetInteger(ShaderPropertyID.LightMapSDFSourceUV);
                material.SetKeyword(ShaderKeywords.SDF_UV0, 
                    (SDFSourceUV)value == SDFSourceUV.UV0);
                material.SetKeyword(ShaderKeywords.SDF_UV1, 
                    (SDFSourceUV)value == SDFSourceUV.UV1);
            }
        }
    }
}