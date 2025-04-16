using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawGizmos();
        partial void DrawGizmosBeforeFX();

        partial void DrawGizmosAfterFX();
        partial void DrawUnsupportedGeometry();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();


#if UNITY_EDITOR
        private static ShaderTagId[] legacyShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        static Material errorMaterial;

        private string sampleName { get; set; }

        partial void DrawUnsupportedGeometry()
        {
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            RendererListDesc desc = new(legacyShaderTagIds, cullingResults, camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.all,
                overrideMaterial = errorMaterial
            };
            commandBuffer.DrawRendererList(context.CreateRendererList(desc));
        }

        partial void DrawGizmosBeforeFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                if (useIntermediateBuffer)
                {
                    CopyCameraTexture(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, 
                        CopyChannel.DepthAttachment);
                }

                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
        }

        partial void DrawGizmosAfterFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                if (postFXStack.IsActive)
                {
                    CopyCameraTexture(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, 
                        CopyChannel.DepthAttachment);
                }

                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void PrepareForSceneWindow()
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                useScaledRendering = false;
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            commandBuffer.name = sampleName = camera.name;
            Profiler.EndSample();
        }
#else
        private string sampleName => bufferName;
#endif
    }
}