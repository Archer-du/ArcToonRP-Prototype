using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public class BuiltinPropertyGUIComponent : ShaderGUIComponentBase
    {
        private readonly string propertyID;
        private MaterialProperty property = null;

        public BuiltinPropertyGUIComponent(string propertyID)
        {
            this.propertyID = propertyID;
        }
        
        public override void FindProperties(MaterialProperty[] props)
        {
            property = MaterialEditorUtils.FindProperty(propertyID, props, false);
        }

        protected override void DrawProperties(MaterialEditor materialEditor, Material[] materials)
        {
            if ((property.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
            {
                materialEditor.BuiltinShaderPropertyDrawer(property);
            }
        }

        public override bool IsValid()
        {
            return property != null;
        }
    }
}