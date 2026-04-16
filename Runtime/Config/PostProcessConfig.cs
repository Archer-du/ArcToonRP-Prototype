using System.Collections.Generic;
using System.Linq;
using ArcToon.Passes.PostProcessing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ArcToon.Config
{
    /// <summary>
    /// Composable post-processing configuration.
    /// Holds a polymorphic list of PostProcessVolumeConfig that can be freely added/removed in Inspector.
    /// Each VolumeConfig owns its paired PostProcessor instance (lazily created).
    /// </summary>
    [CreateAssetMenu(menuName = "Rendering/ArcToon Post Process Config")]
    public class PostProcessConfig : ScriptableObject
    {
        [FormerlySerializedAs("settings")] [SerializeReference]
        public List<PostProcessVolumeConfig> volumeConfigs = new();

        /// <summary>
        /// Get settings of a specific type. Returns null if not found.
        /// </summary>
        public T GetVolumeConfig<T>() where T : PostProcessVolumeConfig
        {
            return volumeConfigs.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Check if this config contains settings of a specific type.
        /// </summary>
        public bool HasVolumeConfig<T>() where T : PostProcessVolumeConfig
        {
            return volumeConfigs.OfType<T>().Any();
        }
        
        /// <summary>
        /// Check if post-processing should be applied to the given camera.
        /// </summary>
        public static bool AreApplicableTo(Camera camera)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView &&
                !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                return false;
            }
#endif
            return camera.cameraType <= CameraType.SceneView;
        }
    }
}
