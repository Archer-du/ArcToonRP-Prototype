using ArcToon.Settings;
using UnityEditor;
using UnityEngine;

namespace ArcToon.Editor.Overrides
{
    [CustomEditor(typeof(ArcToonRenderPipelineAsset))]
    public class ArcToonRenderPipelineAssetEditor : UnityEditor.Editor
    {
        // Root config property
        private SerializedProperty configProp;

        // General
        private SerializedProperty useSRPBatcherProp;

        // Camera Buffer
        private SerializedProperty cameraBufferSettingsProp;
        private SerializedProperty enableHDRProp;
        private SerializedProperty renderScaleProp;
        private SerializedProperty bicubicRescalingModeProp;
        private SerializedProperty fxaaSettingsProp;
        private SerializedProperty fxaaEnabledProp;
        private SerializedProperty fxaaFixedThresholdProp;
        private SerializedProperty fxaaRelativeThresholdProp;
        private SerializedProperty fxaaSubpixelBlendingProp;
        private SerializedProperty fxaaQualityProp;

        // Shadows
        private SerializedProperty shadowSettingsProp;
        private SerializedProperty filterQualityProp;
        private SerializedProperty maxDistanceProp;
        private SerializedProperty distanceFadeProp;
        private SerializedProperty poissonFilterRadiusProp;

        // Directional Cascade Shadow
        private SerializedProperty directionalCascadeShadowProp;
        private SerializedProperty dirAtlasSizeProp;
        private SerializedProperty dirBlendModeProp;
        private SerializedProperty dirCascadeCountProp;
        private SerializedProperty dirCascadeRatio1Prop;
        private SerializedProperty dirCascadeRatio2Prop;
        private SerializedProperty dirCascadeRatio3Prop;
        private SerializedProperty dirEdgeFadeProp;

        // Per Object Shadow
        private SerializedProperty perObjectShadowProp;
        private SerializedProperty perObjAtlasSizeProp;

        // Spot Shadow
        private SerializedProperty spotShadowProp;
        private SerializedProperty spotAtlasSizeProp;

        // Point Shadow
        private SerializedProperty pointShadowProp;
        private SerializedProperty pointAtlasSizeProp;

        // Forward+
        private SerializedProperty forwardPlusSettingsProp;
        private SerializedProperty tileSizeProp;
        private SerializedProperty maxLightsPerTileProp;

        // Post Processing
        private SerializedProperty globalPostFXConfigProp;
        private SerializedProperty globalPostProcessConfigProp;

        // Foldout states - persisted via SessionState
        private const string SessionPrefix = "ArcToonRPAssetEditor_";
        private bool generalFoldout;
        private bool cameraBufferFoldout;
        private bool shadowsFoldout;
        private bool forwardPlusFoldout;
        private bool postProcessingFoldout;

        private void OnEnable()
        {
            configProp = serializedObject.FindProperty("config");
            if (configProp == null) return;

            // General
            useSRPBatcherProp = configProp.FindPropertyRelative("useSRPBatcher");

            // Camera Buffer
            cameraBufferSettingsProp = configProp.FindPropertyRelative("cameraBufferSettings");
            enableHDRProp = cameraBufferSettingsProp.FindPropertyRelative("enableHDR");
            renderScaleProp = cameraBufferSettingsProp.FindPropertyRelative("renderScale");
            bicubicRescalingModeProp = cameraBufferSettingsProp.FindPropertyRelative("bicubicRescalingMode");
            fxaaSettingsProp = cameraBufferSettingsProp.FindPropertyRelative("fxaaSettings");
            fxaaEnabledProp = fxaaSettingsProp.FindPropertyRelative("enabled");
            fxaaFixedThresholdProp = fxaaSettingsProp.FindPropertyRelative("fixedThreshold");
            fxaaRelativeThresholdProp = fxaaSettingsProp.FindPropertyRelative("relativeThreshold");
            fxaaSubpixelBlendingProp = fxaaSettingsProp.FindPropertyRelative("subpixelBlending");
            fxaaQualityProp = fxaaSettingsProp.FindPropertyRelative("quality");

            // Shadows
            shadowSettingsProp = configProp.FindPropertyRelative("shadowSettings");
            filterQualityProp = shadowSettingsProp.FindPropertyRelative("filterQuality");
            maxDistanceProp = shadowSettingsProp.FindPropertyRelative("maxDistance");
            distanceFadeProp = shadowSettingsProp.FindPropertyRelative("distanceFade");
            poissonFilterRadiusProp = shadowSettingsProp.FindPropertyRelative("poissonFilterRadius");

            directionalCascadeShadowProp = shadowSettingsProp.FindPropertyRelative("directionalCascadeShadow");
            dirAtlasSizeProp = directionalCascadeShadowProp.FindPropertyRelative("atlasSize");
            dirBlendModeProp = directionalCascadeShadowProp.FindPropertyRelative("blendMode");
            dirCascadeCountProp = directionalCascadeShadowProp.FindPropertyRelative("cascadeCount");
            dirCascadeRatio1Prop = directionalCascadeShadowProp.FindPropertyRelative("cascadeRatio1");
            dirCascadeRatio2Prop = directionalCascadeShadowProp.FindPropertyRelative("cascadeRatio2");
            dirCascadeRatio3Prop = directionalCascadeShadowProp.FindPropertyRelative("cascadeRatio3");
            dirEdgeFadeProp = directionalCascadeShadowProp.FindPropertyRelative("edgeFade");

            perObjectShadowProp = shadowSettingsProp.FindPropertyRelative("perObjectShadow");
            perObjAtlasSizeProp = perObjectShadowProp.FindPropertyRelative("atlasSize");

            spotShadowProp = shadowSettingsProp.FindPropertyRelative("spotShadow");
            spotAtlasSizeProp = spotShadowProp.FindPropertyRelative("atlasSize");

            pointShadowProp = shadowSettingsProp.FindPropertyRelative("pointShadow");
            pointAtlasSizeProp = pointShadowProp.FindPropertyRelative("atlasSize");

            // Forward+
            forwardPlusSettingsProp = configProp.FindPropertyRelative("forwardPlusSettings");
            tileSizeProp = forwardPlusSettingsProp.FindPropertyRelative("tileSize");
            maxLightsPerTileProp = forwardPlusSettingsProp.FindPropertyRelative("maxLightsPerTile");

            // Post Processing
            globalPostFXConfigProp = configProp.FindPropertyRelative("globalPostFXConfig");
            globalPostProcessConfigProp = configProp.FindPropertyRelative("globalPostProcessConfig");

            // Restore foldout states
            generalFoldout = SessionState.GetBool(SessionPrefix + "General", true);
            cameraBufferFoldout = SessionState.GetBool(SessionPrefix + "CameraBuffer", true);
            shadowsFoldout = SessionState.GetBool(SessionPrefix + "Shadows", true);
            forwardPlusFoldout = SessionState.GetBool(SessionPrefix + "ForwardPlus", true);
            postProcessingFoldout = SessionState.GetBool(SessionPrefix + "PostProcessing", true);
        }

        public override void OnInspectorGUI()
        {
            if (configProp == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            DrawGeneralPanel();
            EditorGUILayout.Space(6);
            DrawCameraBufferPanel();
            EditorGUILayout.Space(6);
            DrawShadowsPanel();
            EditorGUILayout.Space(6);
            DrawForwardPlusPanel();
            EditorGUILayout.Space(6);
            DrawPostProcessingPanel();

            serializedObject.ApplyModifiedProperties();
        }

        #region General Panel

        private void DrawGeneralPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newFoldout = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(generalFoldout, "General");
            if (newFoldout != generalFoldout)
            {
                generalFoldout = newFoldout;
                SessionState.SetBool(SessionPrefix + "General", generalFoldout);
            }

            if (generalFoldout)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                EditorGUILayout.PropertyField(useSRPBatcherProp);
                EditorGUILayoutUtils.EndGUIComponentIndent();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Camera Buffer Panel

        private void DrawCameraBufferPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newFoldout = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(cameraBufferFoldout, "Camera Buffer");
            if (newFoldout != cameraBufferFoldout)
            {
                cameraBufferFoldout = newFoldout;
                SessionState.SetBool(SessionPrefix + "CameraBuffer", cameraBufferFoldout);
            }

            if (cameraBufferFoldout)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();  

                EditorGUILayout.PropertyField(enableHDRProp);
                EditorGUILayout.PropertyField(renderScaleProp);
                EditorGUILayout.PropertyField(bicubicRescalingModeProp);

                EditorGUILayoutUtils.EndGUIComponentIndent();
                
                // FXAA sub-section
                EditorGUILayout.BeginVertical(EditorGUILayoutUtils.GUIComponentBoxStyle);
                {
                    bool fxaaEnabled = EditorGUILayoutUtils.BeginTogglePropertyGroup(
                        new GUIContent("FXAA"),
                        fxaaEnabledProp.boolValue,
                        EditorStyles.boldLabel
                    );
                    if (fxaaEnabled != fxaaEnabledProp.boolValue)
                    {
                        fxaaEnabledProp.boolValue = fxaaEnabled;
                    }
                    EditorGUILayoutUtils.BeginGUIComponentIndent();

                    EditorGUILayout.PropertyField(fxaaFixedThresholdProp);
                    EditorGUILayout.PropertyField(fxaaRelativeThresholdProp);
                    EditorGUILayout.PropertyField(fxaaSubpixelBlendingProp);
                    EditorGUILayout.PropertyField(fxaaQualityProp);

                    EditorGUILayoutUtils.EndGUIComponentIndent();
                    EditorGUILayoutUtils.EndTogglePropertyGroup();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Shadows Panel

        private void DrawShadowsPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newFoldout = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(shadowsFoldout, "Shadows");
            if (newFoldout != shadowsFoldout)
            {
                shadowsFoldout = newFoldout;
                SessionState.SetBool(SessionPrefix + "Shadows", shadowsFoldout);
            }

            if (shadowsFoldout)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();

                EditorGUILayout.PropertyField(filterQualityProp);
                EditorGUILayout.PropertyField(maxDistanceProp);
                EditorGUILayout.PropertyField(distanceFadeProp);
                
                var filterQuality = (ShadowSettings.FilterQuality)filterQualityProp.intValue;
                if (filterQuality == ShadowSettings.FilterQuality.PoissonDisk ||
                    filterQuality == ShadowSettings.FilterQuality.PCSS)
                {
                    EditorGUILayout.PropertyField(poissonFilterRadiusProp);
                }

                EditorGUILayoutUtils.EndGUIComponentIndent();

                // Directional Cascade Shadow sub-section
                DrawShadowSubSection("Directional Cascade Shadow", () =>
                {
                    EditorGUILayout.PropertyField(dirAtlasSizeProp);
                    EditorGUILayout.PropertyField(dirBlendModeProp);
                    EditorGUILayout.PropertyField(dirCascadeCountProp);

                    int cascadeCount = dirCascadeCountProp.intValue;
                    if (cascadeCount >= 2)
                        EditorGUILayout.PropertyField(dirCascadeRatio1Prop);
                    if (cascadeCount >= 3)
                        EditorGUILayout.PropertyField(dirCascadeRatio2Prop);
                    if (cascadeCount >= 4)
                        EditorGUILayout.PropertyField(dirCascadeRatio3Prop);

                    EditorGUILayout.PropertyField(dirEdgeFadeProp);
                });

                // Per Object Shadow sub-section
                DrawShadowSubSection("Per Object Shadow", () =>
                {
                    EditorGUILayout.PropertyField(perObjAtlasSizeProp);
                });

                // Spot Shadow sub-section
                DrawShadowSubSection("Spot Shadow", () =>
                {
                    EditorGUILayout.PropertyField(spotAtlasSizeProp);
                });

                // Point Shadow sub-section
                DrawShadowSubSection("Point Shadow", () =>
                {
                    EditorGUILayout.PropertyField(pointAtlasSizeProp);
                });
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawShadowSubSection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorGUILayoutUtils.GUIComponentBoxStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayoutUtils.BeginGUIComponentIndent();
            drawContent?.Invoke();
            EditorGUILayoutUtils.EndGUIComponentIndent();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Forward+ Panel

        private void DrawForwardPlusPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newFoldout = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(forwardPlusFoldout, "Forward+");
            if (newFoldout != forwardPlusFoldout)
            {
                forwardPlusFoldout = newFoldout;
                SessionState.SetBool(SessionPrefix + "ForwardPlus", forwardPlusFoldout);
            }

            if (forwardPlusFoldout)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                EditorGUILayout.PropertyField(tileSizeProp);
                EditorGUILayout.PropertyField(maxLightsPerTileProp);
                EditorGUILayoutUtils.EndGUIComponentIndent();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Post Processing Panel

        private void DrawPostProcessingPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newFoldout = EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup(postProcessingFoldout, "Post Processing");
            if (newFoldout != postProcessingFoldout)
            {
                postProcessingFoldout = newFoldout;
                SessionState.SetBool(SessionPrefix + "PostProcessing", postProcessingFoldout);
            }

            if (postProcessingFoldout)
            {
                EditorGUILayoutUtils.BeginGUIComponentIndent();
                EditorGUILayout.PropertyField(globalPostFXConfigProp);

                if (globalPostFXConfigProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No Post FX Config assigned.", MessageType.Warning);
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(globalPostProcessConfigProp);

                if (globalPostProcessConfigProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No Post Process Config assigned.", MessageType.Warning);
                }

                EditorGUILayoutUtils.EndGUIComponentIndent();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
