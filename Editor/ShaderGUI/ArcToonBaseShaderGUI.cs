using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Panels;
using ArcToon.Editor.ShaderEditor.Sections;

namespace ArcToon.Editor.ShaderEditor
{
    // GUI for the fully-featured ArcToon shaders (ToonBase / ToonFace / ToonPupil / ToonFringe / ToonTransparent).
    // Owns the canonical panel factory methods; the Unlit GUI reuses the applicable ones directly.
    public sealed class ArcToonBaseShaderGUI : ArcToonShaderGUI
    {
        protected override IReadOnlyList<BaseFoldoutShaderPanel> BuildPanels() => new[]
        {
            BuildGeneralPanel(),
            BuildShadowPanel(),
            BuildPBRPanel(),
            BuildToonPanel(),
            BuildEnginePanel(),
        };

        internal static BaseFoldoutShaderPanel BuildGeneralPanel() =>
            new BaseFoldoutShaderPanel("General", new List<ShaderGUISectionBase>
            {
                new ColorTextureSection("Base Map", ShaderPropertyID.BaseMap, ShaderPropertyID.BaseColor, true),
                new NormalMapSection("Normal Map", ShaderPropertyID.NormalMap, ShaderPropertyID.NormalScale, ShaderKeywords.NORMAL_MAP),
                new SpecularMaskSection(),
                new AlphaClippingSection(),
                new TransparencySection(),
            });

        internal static BaseFoldoutShaderPanel BuildShadowPanel() =>
            new BaseFoldoutShaderPanel("Shadow", new List<ShaderGUISectionBase>
            {
                new ShadowSection(),
            });

        internal static BaseFoldoutShaderPanel BuildPBRPanel() =>
            new BaseFoldoutShaderPanel("PBR", new List<ShaderGUISectionBase>
            {
                new PBRSection(),
                new ColorTextureSection("Emission Map", ShaderPropertyID.EmissionMap, ShaderPropertyID.EmissionColor, true),
            });

        internal static BaseFoldoutShaderPanel BuildToonPanel() =>
            new BaseFoldoutShaderPanel("Toon", new List<ShaderGUISectionBase>
            {
                new RampTextureSection("Ramp Set"),
                new GeometryOutlineSection(),
                new HighLightSection(),
                new LightMapSDFSection(),
                new FringeSection(),
                new RefractionSection(),
                new MatCapSection(),
                new HeaderPropertySection("Sigmoid Attenuation",
                    new[] { "Offset", "Smooth" },
                    new[] { ShaderPropertyID.DirectLightAttenOffset, ShaderPropertyID.DirectLightAttenSmoothNew }),
                new HeaderPropertySection("Sigmoid Specular",
                    new[] { "Offset", "Smooth" },
                    new[] { ShaderPropertyID.DirectLightSpecOffset, ShaderPropertyID.DirectLightSpecSmooth }),
            });

        internal static BaseFoldoutShaderPanel BuildEnginePanel() =>
            new BaseFoldoutShaderPanel("Engine", new List<ShaderGUISectionBase>
            {
                new DefaultPropertySection(ShaderPropertyID.Cull),
                new HeaderPropertySection("Blend Factor",
                    new[] { "Source", "Destination" },
                    new[] { ShaderPropertyID.SrcBlend, ShaderPropertyID.DstBlend }),
                new DefaultPropertySection(ShaderPropertyID.ZWrite),
                new StencilSection(),
                new EngineSection(),
            });
    }
}
