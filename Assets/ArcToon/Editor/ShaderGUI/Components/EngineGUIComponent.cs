using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class EngineGUIComponent : ShaderGUIComponentBase
    {
        public override void FindProperties(MaterialProperty[] props) { }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                materialEditor.RenderQueueField();
            
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}