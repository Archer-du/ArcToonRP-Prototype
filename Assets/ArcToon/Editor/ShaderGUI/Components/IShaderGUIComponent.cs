using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor.Components
{
    public interface IShaderGUIComponent
    {
        void FindProperties(MaterialProperty[] props);
        void DrawGUI(MaterialEditor materialEditor, Material[] materials);
    }
}