﻿using ArcToon.Runtime;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace ArcToon.Editor.Overrides
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light))]
    [SupportedOnRenderPipeline(typeof(ArcToonRenderPipelineAsset))]
    public class CustomLightEditor : LightEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!settings.lightType.hasMultipleDifferentValues && 
                (LightType)settings.lightType.enumValueIndex == LightType.Spot)
            {
                settings.DrawInnerAndOuterSpotAngle();
                settings.ApplyModifiedProperties();
            }
            var light = target as Light;
            if (light && light.cullingMask != -1) {
                EditorGUILayout.HelpBox(
                    "Culling Mask only affects shadows.",
                    MessageType.Warning
                );
            }
        }
        
    }
}