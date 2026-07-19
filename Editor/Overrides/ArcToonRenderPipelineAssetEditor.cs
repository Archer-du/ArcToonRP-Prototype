using ArcToon.Editor.Attributes;
using UnityEditor;

namespace ArcToon.Editor.Overrides
{
    /// <summary>
    /// Inspector for ArcToonRenderPipelineAsset.
    /// Layout is fully attribute-driven: panels, sub-sections, conditional fields and null
    /// warnings are declared on the RenderPipelineConfig fields via [FoldoutGroup] / [BoxGroup] /
    /// [ShowIf] / [ShowIfEnum] / [HelpBoxIfNull] and rendered by AttributeInspectorDrawer.
    /// Adding, removing or renaming a settings field requires no change here.
    /// </summary>
    [CustomEditor(typeof(ArcToonRenderPipelineAsset))]
    public class ArcToonRenderPipelineAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty configProp;

        private void OnEnable()
        {
            configProp = serializedObject.FindProperty("config");
        }

        public override void OnInspectorGUI()
        {
            if (configProp == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();
            AttributeInspectorDrawer.DrawProperties(configProp);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
