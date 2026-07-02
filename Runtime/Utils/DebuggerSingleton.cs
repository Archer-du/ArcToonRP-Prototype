using System.Diagnostics;
using ArcToon.Utils.Extensions;
using UnityEngine.Rendering;

namespace ArcToon.Utils
{
    /// <summary>
    /// Central hub for the debug pipeline: owns the debug UI panel and the state read by the
    /// debug passes. Screen debug is a full-screen overlay (Forward+ tile heat map); geometry
    /// debug re-renders scene geometry to visualize per-vertex / per-surface quantities.
    /// Panel registration is stripped from release builds; the state stays at its inert default
    /// so the shipping render path runs unchanged.
    /// </summary>
    public static class DebuggerSingleton
    {
        const string geometryPanelName = "Geometry";
        const string screenPanelName = "Screen";

        // ─── Screen debug (Forward+ tile overlay) ───
        static bool showDebugTile;
        static float debugTileOpacity = 0.5f;

        public static bool IsScreenDebugActive => showDebugTile && debugTileOpacity > 0f;

        // ─── Geometry debug (replaces the geometry + post-process chain) ───
        public enum GeometryDebug
        {
            None,
            VertexColorRGB,
            VertexColorR,
            VertexColorG,
            VertexColorB,
            VertexColorA,
            Specular,
            DirectBRDF,
            IncomingLight,
        }

        static GeometryDebug geometryDebugMode;

        public static GeometryDebug GeometryDebugMode => geometryDebugMode;

        public static bool IsGeometryDebugActive => geometryDebugMode != GeometryDebug.None;

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Initialize()
        {
            DebugManager.instance.GetPanel(geometryPanelName, true).children.Add(
                new DebugUI.EnumField
                {
                    displayName = "Geometry Debug Mode",
                    tooltip = "Re-render scene geometry to visualize vertex color / lighting terms.",
                    autoEnum = typeof(GeometryDebug),
                    getter = static () => (int)geometryDebugMode,
                    setter = static value => geometryDebugMode = (GeometryDebug)value,
                    getIndex = static () => (int)geometryDebugMode,
                    setIndex = static value => geometryDebugMode = (GeometryDebug)value,
                }
            );

            DebugManager.instance.GetPanel(screenPanelName, true).children.Add(
                new DebugUI.FloatField
                {
                    displayName = "Tile Overlay Opacity",
                    tooltip = "Opacity of the Forward+ tile overlay.",
                    min = static () => 0f,
                    max = static () => 1f,
                    getter = static () => debugTileOpacity,
                    setter = static value => debugTileOpacity = value
                },
                new DebugUI.BoolField
                {
                    displayName = "Show Tiles",
                    tooltip = "Whether the Forward+ tile overlay is shown.",
                    getter = static () => showDebugTile,
                    setter = static value => showDebugTile = value
                }
            );
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Cleanup()
        {
            DebugManager.instance.RemovePanel(geometryPanelName);
            DebugManager.instance.RemovePanel(screenPanelName);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void RenderScreenDebug(CommandBuffer commandBuffer, ScriptableRenderContext context)
        {
            commandBuffer.SetGlobalFloat(InternalShader.PropertyID.DebugOpacity, debugTileOpacity);
            commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.ScreenDebug), 0);
        }
    }
}
