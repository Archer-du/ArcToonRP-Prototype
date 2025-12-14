using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class RefractionComponent : ShaderGUIComponentBase
    {
        private MaterialProperty anteriorChamberHeightProperty;
        private MaterialProperty refractionEdgeProperty;
        private MaterialProperty refractionSmoothProperty;
        private MaterialProperty parallaxFlipSignXProperty;
        private MaterialProperty parallaxFlipSignYProperty;
        
        public override void FindProperties(MaterialProperty[] props)
        {
            anteriorChamberHeightProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.AnteriorChamberHeight, props, false);
            refractionEdgeProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RefractionEdge, props, false);
            refractionSmoothProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.RefractionSmooth, props, false);
            parallaxFlipSignXProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxFlipSignX, props, false);
            parallaxFlipSignYProperty = MaterialEditorUtils.FindProperty(ShaderPropertyID.ParallaxFlipSignY, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            
        }

        public override bool IsValid()
        {
            return anteriorChamberHeightProperty != null &&
                   refractionEdgeProperty != null &&
                   refractionSmoothProperty != null;
        }
    }
}