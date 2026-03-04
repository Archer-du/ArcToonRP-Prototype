using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class RefractionComponent : ShaderGUIComponentBase
    {
        private MaterialProperty refractionTypeProperty;
        private MaterialProperty anteriorChamberHeightProperty;
        private MaterialProperty refractionEdgeProperty;
        private MaterialProperty refractionSmoothProperty;
        private MaterialProperty parallaxFlipSignXProperty;
        private MaterialProperty parallaxFlipSignYProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            refractionTypeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RefractionType, props, false);
            anteriorChamberHeightProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.AnteriorChamberHeight, props, false);
            refractionEdgeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RefractionEdge, props, false);
            refractionSmoothProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RefractionSmooth, props, false);
            parallaxFlipSignXProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxFlipSignX, props, false);
            parallaxFlipSignYProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxFlipSignY, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            
            ShaderGUILayout.PredicateMaterialArrayBoolProperty(materials, material => material.IsKeywordEnabled(ShaderKeywords.EYE_REFRACTION), 
                out bool hasMixedValue, out bool shouldToggleGroup);
            
            EditorGUI.showMixedValue = hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool newValue = ShaderGUILayout.BeginTogglePropertyGroup(new GUIContent("Eye Refraction"), shouldToggleGroup, EditorStyles.label);
            {
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in materials)
                    {
                        if (material == null) continue;
                        MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Use Eye Refraction: {newValue}");
                        
                        Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        material.SetKeyword(ShaderKeywords.EYE_REFRACTION, newValue);
                        EditorUtility.SetDirty(material);
                    }
                }
                
                ShaderGUILayout.BeginGUIComponentIndent();
                {
                    materialEditor.BuiltinShaderPropertyDrawer(anteriorChamberHeightProperty, true, "Anterior Chamber Height");
                    
                    EditorGUI.showMixedValue = refractionTypeProperty.hasMixedValue;
                    EditorGUI.BeginChangeCheck();
                    var newRefractionTypeValue = (RefractionType)EditorGUILayout.EnumPopup("Refraction Type", (RefractionType)refractionTypeProperty.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        refractionTypeProperty.intValue = (int)newRefractionTypeValue;
                        // TODO:
                        // foreach (var material in materials)
                        // {
                        //     if (material == null) continue;
                        //     MaterialEditorUtils.ArcToonGUILog($"Update {material.name} Refraction Type: {newRefractionTypeValue}");
                        //     Undo.RecordObject(material, Undo.GetCurrentGroupName());
                        //     
                        //     material.SetKeyword(ShaderKeywords.TANGENT_SHIFT_MAP, 
                        //         (OverrideHighlightType)highlightTypeProperty.intValue == OverrideHighlightType.KajiyaKay);
                        //     
                        //     EditorUtility.SetDirty(material);
                        // }
                    }
                    EditorGUI.showMixedValue = false;

                    materialEditor.BuiltinShaderPropertyDrawer(refractionEdgeProperty, true, "Distance Mask Edge");
                    materialEditor.BuiltinShaderPropertyDrawer(refractionSmoothProperty, true, "Distance Mask Smooth");
                    
                    EditorGUI.BeginChangeCheck();
                    var displayFlipSignValue = new Vector2Int(parallaxFlipSignXProperty.intValue, parallaxFlipSignYProperty.intValue);
                    EditorGUI.BeginChangeCheck();
                    var newFlipSignValue = EditorGUILayout.Vector2IntField(new GUIContent("Parallax Flip Sign"), displayFlipSignValue);
                    newFlipSignValue = new Vector2Int(newFlipSignValue.x < 0 ? -1 : 1, newFlipSignValue.y < 0 ? -1 : 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        parallaxFlipSignXProperty.intValue = newFlipSignValue.x;
                        parallaxFlipSignYProperty.intValue = newFlipSignValue.y;
                    }
                }
                ShaderGUILayout.EndGUIComponentIndent();
            }
            ShaderGUILayout.EndTogglePropertyGroup();
        }

        public override bool IsValid()
        {
            return anteriorChamberHeightProperty != null &&
                   refractionEdgeProperty != null &&
                   refractionSmoothProperty != null;
        }
    }
}