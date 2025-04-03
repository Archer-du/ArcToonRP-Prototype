using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Editor.GUI
{
    public class CustomShaderGUI : ShaderGUI 
    {
        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;
        
        bool Clipping 
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        bool PremultiplyAlpha 
        {
            set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
        }

        BlendMode SrcBlend 
        {
            set => SetProperty("_SrcBlend", (float)value);
        }

        BlendMode DstBlend 
        {
            set => SetProperty("_DstBlend", (float)value);
        }

        bool ZWrite 
        {
            set => SetProperty("_ZWrite", value ? 1f : 0f);
        }
        
        RenderQueue RenderQueue 
        {
            set {
                foreach (var o in materials)
                {
                    var material = (Material)o;
                    material.renderQueue = (int)value;
                }
            }
        }
        
        bool showPresets;
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties) 
        {
            base.OnGUI(materialEditor, materialProperties);
            editor = materialEditor;
            materials = materialEditor.targets;
            properties = materialProperties;
            
            EditorGUILayout.Space();
            showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
            if (showPresets) {
                OpaquePreset();
                AlphaClipPreset();
                FadePreset();
                TransparentPreset();
            }
        }
        
        bool SetProperty(string name, float value) 
        {
            var property = FindProperty(name, properties, false);
            if (property != null) {
                property.floatValue = value;
                return true;
            }
            return false;
        }
        
        void SetProperty(string name, string keyword, bool value) 
        {
            if (SetProperty(name, value ? 1f : 0f))
            {
                SetKeyword(keyword, value);
            }
        }
        
        void SetKeyword(string keyword, bool enabled) 
        {
            if (enabled) 
            {
                foreach (var obj in materials)
                {
                    var material = (Material)obj;
                    material.EnableKeyword(keyword);
                }
            }
            else 
            {
                foreach (var obj in materials)
                {
                    var material = (Material)obj;
                    material.DisableKeyword(keyword);
                }
            }
        }

        bool PresetButton(string name) 
        {
            if (GUILayout.Button(name)) 
            {
                editor.RegisterPropertyChangeUndo(name);
                return true;
            }
            return false;
        }
        
        void OpaquePreset() 
        {
            if (PresetButton("Opaque")) 
            {
                Clipping = false;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.Geometry;
            }
        }
        
        void AlphaClipPreset() 
        {
            if (PresetButton("AlphaClip")) 
            {
                Clipping = true;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.AlphaTest;
            }
        }
        
        void FadePreset() 
        {
            if (PresetButton("Fade")) 
            {
                Clipping = false;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.SrcAlpha;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
            }
        }
        
        void TransparentPreset()
        {
            if (FindProperty("_PremulAlpha", properties, false) == null)
                return;
            if (PresetButton("Transparent")) 
            {
                Clipping = false;
                PremultiplyAlpha = true;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
            }
        }
    }
}