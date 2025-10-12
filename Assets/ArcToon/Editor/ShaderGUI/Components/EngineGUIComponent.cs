using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class EngineGUIComponent : IShaderGUIComponent
    {
        public void FindProperties(MaterialProperty[] props) { }

        public void DrawGUI(MaterialEditor materialEditor, Material[] materials)
        {
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                materialEditor.RenderQueueField();
            
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }
    }
}