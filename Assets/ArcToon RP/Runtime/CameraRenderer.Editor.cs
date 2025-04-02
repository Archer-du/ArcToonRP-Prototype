using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace ArcToon_RP.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawGizmos();
        partial void DrawUnsupportedGeometry();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();

        
#if UNITY_EDITOR
        static ShaderTagId[] legacyShaderTagIds = {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };
        static Material errorMaterial;
        
        private string sampleName { get; set; }
        
        partial void DrawUnsupportedGeometry() 
        {
            if (errorMaterial == null) {
                errorMaterial =
                    new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            RendererListDesc desc = new(legacyShaderTagIds, cullingResults, camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.all,
                overrideMaterial = errorMaterial
            };
            commandBuffer.DrawRendererList(context.CreateRendererList(desc));
        }
        partial void DrawGizmos() {
            if (Handles.ShouldRenderGizmos()) {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
	    partial void PrepareForSceneWindow() {
		    if (camera.cameraType == CameraType.SceneView) {
			    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		    }
	    }
        partial void PrepareBuffer() {
            Profiler.BeginSample("Editor Only");
            commandBuffer.name = sampleName = camera.name;
            Profiler.EndSample();
	    }
#else
        private string sampleName => bufferName;
#endif
    }
}