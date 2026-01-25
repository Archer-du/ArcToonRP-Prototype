using System.Diagnostics;
using ArcToon.Runtime.Utils;
using ArcToon.Runtime.Utils.Extensions;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public static class CameraDebugger
{
    const string panelName = "Tiled Forward+";
    
    static bool showDebugTile;
    
    static readonly int debugTileOpacityID = Shader.PropertyToID("_DebugOpacity");

    static float debugTileOpacity = 0.5f;

    public static bool IsActive => showDebugTile && debugTileOpacity > 0f;

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Initialize()
    {
        DebugManager.instance.GetPanel(panelName, true).children.Add(
            new DebugUI.FloatField
            {
                displayName = "Opacity",
                tooltip = "Opacity of the debug overlay.",
                min = static () => 0f,
                max = static () => 1f,
                getter = static () => debugTileOpacity,
                setter = static value => debugTileOpacity = value
            },
            new DebugUI.BoolField
            {
                displayName = "Show Tiles",
                tooltip = "Whether the debug overlay is shown.",
                getter = static () => showDebugTile,
                setter = static value => showDebugTile = value
            }
        );
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Cleanup()
    {
        DebugManager.instance.RemovePanel(panelName);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Render(CommandBuffer commandBuffer, ScriptableRenderContext context)
    {
        commandBuffer.SetGlobalFloat(debugTileOpacityID, debugTileOpacity);
        commandBuffer.DrawScreenFilledTriangle(ShaderResourceManager.AcquireTransientMaterial(InternalShader.Path.CameraDebug), 0);
    }
}