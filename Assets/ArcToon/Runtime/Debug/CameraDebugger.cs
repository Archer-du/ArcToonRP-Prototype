using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public static class CameraDebugger
{
    const string panelName = "Tiled Forward+";
    
    static readonly int opacityID = Shader.PropertyToID("_DebugOpacity");

    static Material material;

    static bool showTiles;

    static float opacity = 0.5f;

    public static bool IsActive => showTiles && opacity > 0f;

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Initialize(Shader cameraDebugShader)
    {
        material = CoreUtils.CreateEngineMaterial(cameraDebugShader);
        DebugManager.instance.GetPanel(panelName, true).children.Add(
            new DebugUI.FloatField
            {
                displayName = "Opacity",
                tooltip = "Opacity of the debug overlay.",
                min = static () => 0f,
                max = static () => 1f,
                getter = static () => opacity,
                setter = static value => opacity = value
            },
            new DebugUI.BoolField
            {
                displayName = "Show Tiles",
                tooltip = "Whether the debug overlay is shown.",
                getter = static () => showTiles,
                setter = static value => showTiles = value
            }
        );
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Cleanup()
    {
        CoreUtils.Destroy(material);
        DebugManager.instance.RemovePanel(panelName);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Render(RenderGraphContext context)
    {
        CommandBuffer commandBuffer = context.cmd;
        commandBuffer.SetGlobalFloat(opacityID, opacity);
        commandBuffer.DrawProcedural(
            Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        context.renderContext.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}