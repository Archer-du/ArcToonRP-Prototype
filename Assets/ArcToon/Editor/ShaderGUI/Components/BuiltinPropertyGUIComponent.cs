using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class BuiltinPropertyGUIComponent : IShaderGUIComponent
    {
        private readonly string propertyID;
        private MaterialProperty property = null;

        public BuiltinPropertyGUIComponent(string propertyID)
        {
            this.propertyID = propertyID;
        }
        
        public void FindProperties(MaterialProperty[] props)
        {
            property = MaterialEditorUtils.FindProperty(propertyID, props, false);
        }

        public void DrawGUI(MaterialEditor materialEditor, Material[] materials)
        {
            if ((property.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(property);
            }
        }
    }
}