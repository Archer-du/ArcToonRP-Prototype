using ArcToon.Behavior;
using UnityEngine;

namespace ArcToon.Utils.Extensions
{
    public static class CameraExtensions
    {
        public static Vector2Int GetAttachmentSize(this Camera camera, float renderScale)
        {
            renderScale = Mathf.Clamp(renderScale, CameraAdditiveData.renderScaleMin, CameraAdditiveData.renderScaleMax);
            bool useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                useScaledRendering = false;
            }
#endif
            Vector2Int attachmentSize = default;
            if (useScaledRendering)
            {
                attachmentSize.x = (int)(camera.pixelWidth * renderScale);
                attachmentSize.y = (int)(camera.pixelHeight * renderScale);
            }
            else
            {
                attachmentSize.x = camera.pixelWidth;
                attachmentSize.y = camera.pixelHeight;
            }

            return attachmentSize;
        }
    }
}