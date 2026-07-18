using System.Collections.Generic;
using ArcToon.Editor.ShaderEditor.Panels;

namespace ArcToon.Editor.ShaderEditor
{
    // GUI for the ArcToon/ToonUnlit shader. Exposes only lighting-agnostic panels
    // (General / Shadow / Engine); PBR and Toon panels are omitted by design.
    // Region ID header stays on so per-material stencil-like grouping still works.
    public sealed class ArcToonUnlitShaderGUI : ArcToonShaderGUI
    {
        protected override IReadOnlyList<BaseFoldoutShaderPanel> BuildPanels() => new[]
        {
            ArcToonBaseShaderGUI.BuildGeneralPanel(),
            ArcToonBaseShaderGUI.BuildShadowPanel(),
            ArcToonBaseShaderGUI.BuildEnginePanel(),
        };
    }
}
