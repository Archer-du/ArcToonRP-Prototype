using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public enum ShadowCasterOption
    {
        On,
        Dither,
        Off
    }

    public enum SmoothNormalSource
    {
        UV1,
        VertexColor,
    }
    
    public enum SmoothNormalDecoder
    {
        RGAG,
        OCT
    }

    public enum WidthControlMode
    {
        None,
        VertexColorAlpha,
        NiloOffset,
    }

    public enum OverrideHighlightType
    {
        BlinnPhong,
        KajiyaKay,
        Parallax,
    }
    
    public static class ShaderPropertyID
    {
        public const string BaseMap = "_BaseMap";
        public const string BaseColor = "_BaseColor";
        public const string MainTex = "_MainTex";
        public const string Color = "_Color";
        
        public const string NormalMap = "_NormalMap";
        public const string NormalScale = "_NormalScale";
        
        public const string Clipping = "_Clipping";
        public const string Cutoff = "_Cutoff";
        
        public const string ReceiveShadows = "_ReceiveShadows";
        public const string Shadows = "_Shadows";
        
        public const string RampSet = "_RampSet";
        
        public const string DirectLightAttenOffset = "_DirectLightAttenOffset";
        public const string DirectLightAttenSmoothNew = "_DirectLightAttenSmoothNew";
        public const string DirectLightSpecOffset = "_DirectLightSpecOffset";
        public const string DirectLightSpecSmooth = "_DirectLightSpecSmooth";
        
        public const string EmissionMap = "_EmissionMap";
        public const string EmissionColor = "_EmissionColor";
        
        public const string OutlineColor = "_OutlineColor";
        public const string OutlineScale = "_OutlineScale";
        public const string SmoothNormalSource = "_SmoothNormalSource";
        public const string SmoothNormalDecoder = "_SmoothNormalDecoder";
        public const string WidthControlMode = "_WidthControlMode";

        public const string LightMapSDF = "_LightMapSDF";
        public const string LightMapSDFSourceUV = "_LightMapSDFSourceUV";
        public const string ShadowOffsetSDF = "_ShadowOffsetSDF";
        public const string FaceVector = "_FaceVector";
        
        public const string Cull = "_Cull";
        public const string SrcBlend = "_SrcBlend";
        public const string DstBlend = "_DstBlend";
        public const string ZWrite = "_ZWrite";
        
        public const string LightingDebugMode = "_LightingDebugMode";
        
        public const string HighlightType = "_HighlightType";
        public const string SpecGloss = "_SpecGloss";
        public const string SpecScale = "_SpecScale";
        
        public const string TangentShiftMap = "_TangentShiftMap";
        public const string TangentShiftMapUV = "_TangentShiftMapUV";
        public const string TangentShiftOffset = "_TangentShiftOffset";
        
        public const string ParallaxSpecMap = "_ParallaxSpecMap";
        public const string ParallaxSpecMapUV = "_ParallaxSpecMapUV";
        public const string ParallaxSensitivity = "_ParallaxSensitivity";
        public const string ParallaxOffset = "_ParallaxOffset";
        
        // Transparent Fringe
        public const string FringeTransparentScale = "_FringeTransparentScale";
        public const string FringeShadowBiasScaleX = "_FringeShadowBiasScaleX";
        public const string FringeShadowBiasScaleY = "_FringeShadowBiasScaleY";
    }
    
    public static class ShaderKeywords
    {
        public const string _CLIPPING = "_CLIPPING";
        
        public const string NORMAL_MAP = "_NORMAL_MAP";
        
        public const string RAMP_SET = "_RAMP_SET";
        
        public const string SN_SRC_UV1 = "_SN_SRC_UV1";
        public const string SN_SRC_COLOR = "_SN_SRC_COLOR";
        
        public const string SN_DECODE_RGAG = "_SN_DECODE_RGAG";
        public const string SN_DECODE_OCT = "_SN_DECODE_OCT";
        
        public const string WIDTH_VERTCOLORA = "_WIDTH_VERTCOLORA";
        
        public const string SDF_LIGHT_MAP = "_SDF_LIGHT_MAP";
        public const string SDF_UV0 = "_SDF_UV0";
        public const string SDF_UV1 = "_SDF_UV1";
        
        public const string HIGHLIGHT_KAJIYA = "_HIGHLIGHT_KAJIYA";
        public const string HIGHLIGHT_KAJIYA_UV0 = "_HIGHLIGHT_KAJIYA_UV0";
        public const string HIGHLIGHT_KAJIYA_UV1 = "_HIGHLIGHT_KAJIYA_UV1";
        public const string HIGHLIGHT_PARALLAX = "_HIGHLIGHT_PARALLAX";
        public const string HIGHLIGHT_PARALLAX_UV0 = "_HIGHLIGHT_PARALLAX_UV0";
        public const string HIGHLIGHT_PARALLAX_UV1 = "_HIGHLIGHT_PARALLAX_UV1";
        
        public const string SHADOWS_DITHER = "_SHADOWS_DITHER";
        
        public const string DEBUG_INCOMING_LIGHT = "_DEBUG_INCOMING_LIGHT";
        public const string DEBUG_DIRECT_BRDF = "_DEBUG_DIRECT_BRDF";
        public const string DEBUG_SPECULAR = "_DEBUG_SPECULAR";
        public const string DEBUG_DIFFUSE = "_DEBUG_DIFFUSE";
    }

    public static class MaterialEditorUtils
    {
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + properties.Length.ToString());
            return null;
        }
                
        public static Material[] GetTargetMaterials(MaterialEditor editor)
        {
            if (editor == null || editor.targets == null) return Array.Empty<Material>();
            return editor.targets.Where(t => t is Material).Cast<Material>().ToArray();
        }

        private static bool enableGUILog = true;
        
        public static void ArcToonGUILog(object log)
        {
            if (enableGUILog)
            {
                Debug.Log("[ArcToonGUI] " + log);
            }
        }
    }
}