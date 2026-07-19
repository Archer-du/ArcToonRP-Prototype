using System;
using ArcToon.Config;
using ArcToon.Passes.PostProcessing;
using ArcToon.Settings.Attributes;
using UnityEngine.Serialization;

namespace ArcToon.Settings
{
    [Serializable]
    public class RenderPipelineConfig
    {
        // No [FoldoutGroup] => drawn under the implicit "General" group.
        public bool useSRPBatcher = true;

        [FoldoutGroup("Camera Buffer")]
        public CameraBufferSettings cameraBufferSettings;

        [FoldoutGroup("Shadows")]
        public ShadowSettings shadowSettings;

        [FoldoutGroup("Forward+")]
        public ForwardPlusSettings forwardPlusSettings;

        [FoldoutGroup("Post Processing")]
        [HelpBoxIfNull("No Post Process Config assigned.")]
        public PostProcessConfig globalPostProcessConfig;
    }
}