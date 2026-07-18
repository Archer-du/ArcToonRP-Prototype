using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.ShaderEditor
{
    public enum ColorBlendMode
    {
        Lerp = 0,
        Multiply = 1,
        Add = 2,
        Overlay = 3,
        Screen = 4,
        SoftLight = 5,
        HardLight = 6,
        ColorDodge = 7,
        ColorBurn = 8,
        Darken = 9,
        Lighten = 10,
        Difference = 11,
        Exclusion = 12
    }
    
    public enum ShadowCasterOption
    {
        On,
        Dither,
        Off
    }

    public enum SmoothNormalSource
    {
        None,
        VertexColor,
        UV1,
    }
    
    public enum SmoothNormalDecoder
    {
        RGAG,
        OCT
    }

    public enum WidthControlMode
    {
        None,
        VertexColor,
    }

    public enum OverrideHighlightType
    {
        BlinnPhong,
        KajiyaKay,
    }

    public enum RefractionType
    {
        Physic,
        Parallax,
    }

    public enum MatCapNormalSource
    {
        Origin,
        SphericalUV,
    }

    public enum TransparencyMode
    {
        OrderedDualFace,
        WeightedAverage,
        DepthPeeling,
    }
    
    public static class ShaderPropertyID
    {
        private static string Auto([CallerMemberName] string name = null)
            => "_" + name;
        
        public static readonly string BaseMap = "_BaseMap";
        public static readonly string BaseColor = "_BaseColor";
        public static readonly string MainTex = "_MainTex";
        public static readonly string Color = "_Color";
        
        public static readonly string NormalMap = "_NormalMap";
        public static readonly string NormalScale = "_NormalScale";
                
        public static readonly string SpecularMask = Auto();
        public static readonly string SpecularMaskUV = Auto();
        public static readonly string SpecularMaskChannel = Auto();
        public static readonly string ParallaxSensitivity = Auto();
        public static readonly string ParallaxOffset = Auto();

        public static readonly string Cutoff = "_Cutoff";

        public static readonly string TransparencyMode = Auto();
        public static readonly string PremulAlpha = Auto();
        
        public static readonly string ReceiveShadows = Auto();
        public static readonly string ReceiveFringeShadows = Auto();
        public static readonly string Shadows = "_Shadows";
        
        public static readonly string RampSet = "_RampSet";
        
        public static readonly string DirectLightAttenOffset = "_DirectLightAttenOffset";
        public static readonly string DirectLightAttenSmoothNew = "_DirectLightAttenSmoothNew";
        public static readonly string DirectLightSpecOffset = "_DirectLightSpecOffset";
        public static readonly string DirectLightSpecSmooth = "_DirectLightSpecSmooth";
        
        public static readonly string MetallicMap = Auto();
        public static readonly string MetallicMapChannel = Auto();
        public static readonly string RoughnessMap = Auto();
        public static readonly string RoughnessMapChannel = Auto();
        public static readonly string OcclusionMap = Auto();
        public static readonly string OcclusionMapChannel = Auto();

        public static readonly string Metallic = Auto();
        public static readonly string Roughness = Auto();
        public static readonly string Occlusion = Auto();
        public static readonly string Fresnel = Auto();

        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
        
        public static readonly string OutlineScale = "_OutlineScale";
        public static readonly string SmoothNormalSource = "_SmoothNormalSource";
        public static readonly string SmoothNormalDecoder = "_SmoothNormalDecoder";
        public static readonly string WidthControlMode = "_WidthControlMode";
        public static readonly string WidthMaskChannel = "_WidthMaskChannel";

        public static readonly string LightMapSDF = "_LightMapSDF";
        public static readonly string LightMapSDFSourceUV = "_LightMapSDFSourceUV";
        public static readonly string ShadowOffsetSDF = "_ShadowOffsetSDF";
        public static readonly string FaceVector = "_FaceVector";
        
        public static readonly string Cull = "_Cull";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string ZWrite = "_ZWrite";
        
        public static readonly string HighlightType = "_HighlightType";
        public static readonly string SpecGloss = "_SpecGloss";
        public static readonly string SpecScale = "_SpecScale";
        
        public static readonly string TangentShiftMap = "_TangentShiftMap";
        public static readonly string TangentShiftMapUV = "_TangentShiftMapUV";
        public static readonly string TangentShiftOffset = "_TangentShiftOffset";
        
        public static readonly string FringeTransparentScale = "_FringeTransparentScale";
        public static readonly string FringeShadowBiasScaleX = "_FringeShadowBiasScaleX";
        public static readonly string FringeShadowBiasScaleY = "_FringeShadowBiasScaleY";
        
        public static readonly string RefractionType = Auto();
        public static readonly string AnteriorChamberHeight = Auto();
        public static readonly string RefractionEdge = Auto();
        public static readonly string RefractionSmooth = Auto();
        public static readonly string ParallaxFlipSignX = Auto();
        public static readonly string ParallaxFlipSignY = Auto();
        
        public static readonly string MatCap = Auto();
        public static readonly string MatCapStrength = Auto();
        public static readonly string MatCapBlendMode = Auto();

        public static readonly string StencilEnabled = Auto();
        public static readonly string Stencil = Auto();
        public static readonly string StencilWriteMask = Auto();
        public static readonly string StencilReadMask = Auto();

        public static readonly string RegionCount = Auto();
        public static readonly string RegionIDChannel = Auto();
        public static readonly string RegionIDMap = Auto();
    }
    
    public static class ShaderKeywords
    {
        private static string Auto([CallerMemberName] string name = null)
            => "_" + name;
        
        public static readonly string CLIPPING = "_CLIPPING";
        
        public static readonly string NORMAL_MAP = "_NORMAL_MAP";
        
        public static readonly string SPEC_MASK = Auto();
        public static readonly string OVERRIDE_HIGHLIGHT = Auto();
        public static readonly string SPEC_PARALLAX = Auto();

        public static readonly string METALLIC_MAP = Auto();
        public static readonly string ROUGHNESS_MAP = Auto();
        public static readonly string OCCLUSION_MAP = Auto();
        
        public static readonly string SHADOWS_DITHER = "_SHADOWS_DITHER";

        public static readonly string RAMP_SET = "_RAMP_SET";
        
        public static readonly string SN_SRC_UV1 = "_SN_SRC_UV1";
        public static readonly string SN_SRC_COLOR = "_SN_SRC_COLOR";
        
        public static readonly string SN_DECODE_RGAG = "_SN_DECODE_RGAG";
        public static readonly string SN_DECODE_OCT = "_SN_DECODE_OCT";
        
        public static readonly string WIDTH_VERTEX_COLOR = "_WIDTH_VERTEX_COLOR";
        
        public static readonly string SDF_LIGHT_MAP = "_SDF_LIGHT_MAP";

        public static readonly string TANGENT_SHIFT_MAP = "_TANGENT_SHIFT_MAP";

        public static readonly string FRINGE_TRANSPARENT = Auto();

        public static readonly string EYE_REFRACTION = Auto();
        
        public static readonly string MATCAP = Auto();
        public static readonly string MATCAP_SPH_NORMAL = Auto();

        public static readonly string REGION_ID_TEXTURE = Auto();
        public static readonly string REGION_ID_VERTEX_COLOR = Auto();
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

        public static string GetRegionPropertyName(string baseName, int regionIndex)
        {
            return $"{baseName}{regionIndex}";
        }

        public static MaterialProperty FindRegionProperty(string baseName, int regionIndex, MaterialProperty[] props)
        {
            string name = GetRegionPropertyName(baseName, regionIndex);
            return FindProperty(name, props, false);
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