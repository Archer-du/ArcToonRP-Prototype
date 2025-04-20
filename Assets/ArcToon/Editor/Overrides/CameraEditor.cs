using ArcToon.Runtime;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace ArcToon.Editor.Overrides
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(ArcToonRenderPipelineAsset))]
    public class CameraEditor : UnityEditor.Editor
    {
        // TODO:
    }
}