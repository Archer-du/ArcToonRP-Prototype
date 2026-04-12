# 实施计划

- [ ] 1. 创建 `EditorGUILayoutUtils` 通用布局工具类
   - 在 `Editor/` 目录下新建 `EditorGUILayoutUtils.cs` 文件，命名空间为 `ArcToon.Editor`
   - 从 `ShaderGUILayout` 中提取可复用的、与 `SerializedProperty` 无关的纯 GUI 样式方法到此工具类：
     - `GUIComponentBoxStyle` 属性（返回自定义 box 样式）
     - `GUIFoldoutGroupBoxStyle` 属性（返回 `ShurikenModuleTitle` 样式）
     - `DrawGUIComponentFoldoutGroup(bool display, string title)` 方法（绘制可折叠标题栏，14px 加粗、30px 高度）
     - `BeginTogglePropertyGroup` / `EndTogglePropertyGroup` 方法（Toggle 组模式）
     - `BeginGUIComponentIndent` / `EndGUIComponentIndent` 方法（缩进控制）
     - `GUIComponentSpace` 和 `GUIComponentIndentLevel` 常量
   - 修改原 `ShaderGUILayout` 类中的对应方法，改为委托调用 `EditorGUILayoutUtils` 的实现（保持向后兼容，ShaderGUI 侧代码无需改动）
   - _需求：8.2、7.1、7.2、7.3、7.4_

- [ ] 2. 创建 `ArcToonRenderPipelineAssetEditor` 主编辑器类
   - 在 `Editor/Overrides/` 目录下新建 `ArcToonRenderPipelineAssetEditor.cs`
   - 使用命名空间 `ArcToon.Editor.Overrides`，添加 `[CustomEditor(typeof(ArcToonRenderPipelineAsset))]` 特性
   - 继承 `UnityEditor.Editor`，重写 `OnInspectorGUI()` 方法
   - 在 `OnEnable()` 中通过 `serializedObject.FindProperty("config")` 获取 `RenderPipelineConfig` 的 `SerializedProperty`，并进一步获取各子属性的 `SerializedProperty`（`useSRPBatcher`、`cameraBufferSettings`、`globalShadowSettings`、`forwardPlusSettings`、`globalPostFXConfig`）
   - 使用 `Dictionary<string, bool>` 维护每个面板的折叠状态，并通过 `SessionState` 持久化
   - 在 `OnInspectorGUI()` 中按顺序调用 5 个绘制方法：`DrawGeneralPanel()`、`DrawCameraBufferPanel()`、`DrawShadowsPanel()`、`DrawForwardPlusPanel()`、`DrawPostProcessingPanel()`，每个面板间使用 `EditorGUILayout.Space(6)` 间隔
   - 在各绘制方法的开头和结尾包裹 `serializedObject.Update()` 和 `serializedObject.ApplyModifiedProperties()` 以支持 Undo/Redo
   - _需求：1.1、1.2、1.3、8.1、8.3_

- [ ] 3. 实现 General 面板绘制
   - 在 `ArcToonRenderPipelineAssetEditor` 中实现 `DrawGeneralPanel()` 方法
   - 使用 `EditorGUILayoutUtils.DrawGUIComponentFoldoutGroup` 绘制可折叠标题栏 "General"
   - 内容使用 `EditorGUILayout.BeginVertical(EditorStyles.helpBox)` / `EndVertical()` 包裹
   - 面板内绘制 `useSRPBatcher` 的 `EditorGUILayout.PropertyField` 控件
   - _需求：2.1、2.2_

- [ ] 4. 实现 Camera Buffer 面板绘制
   - 在 `ArcToonRenderPipelineAssetEditor` 中实现 `DrawCameraBufferPanel()` 方法
   - 使用可折叠标题栏 "Camera Buffer"，内容区包裹 `helpBox` 样式
   - 绘制 `enableHDR`（Toggle PropertyField）、`renderScale`（Slider PropertyField，范围 0.5~2，由数据上的 `[Range]` 特性自动处理）、`bicubicRescalingMode`（枚举 PropertyField）
   - 绘制 FXAA 子区域：使用 `EditorGUILayoutUtils.GUIComponentBoxStyle` 包裹
     - 通过 `fxaaSettings.FindPropertyRelative("enabled")` 获取 FXAA 开启状态
     - 使用 `EditorGUILayoutUtils.BeginTogglePropertyGroup` 绘制 FXAA enabled Toggle
     - 在 Toggle 组内部绘制 `fixedThreshold`、`relativeThreshold`、`subpixelBlending`、`quality` 的 PropertyField
     - 使用 `EditorGUILayoutUtils.EndTogglePropertyGroup` 结束 Toggle 组
   - _需求：3.1、3.2、3.3_

- [ ] 5. 实现 Shadows 面板绘制 — 全局参数与条件字段
   - 在 `ArcToonRenderPipelineAssetEditor` 中实现 `DrawShadowsPanel()` 方法
   - 使用可折叠标题栏 "Shadows"，内容区包裹 `helpBox` 样式
   - 绘制全局阴影参数：
     - `filterQuality`（枚举 PropertyField）
     - `maxDistance`（Float PropertyField）
     - `distanceFade`（Slider PropertyField，范围 0.001~1）
   - 读取 `filterQuality` 的 `enumValueIndex`，映射回 `FilterQuality` 枚举值：
     - IF 值为 `PoissonDisk` 或 `PCSS` THEN 显示 `poissonFilterRadius` 的 Slider PropertyField（范围 0.01~10）
     - IF 值为 `PCSS` THEN 额外显示 `pcssLightSize` 的 Float PropertyField（最小 0.001）
   - _需求：4.1、4.2、4.3_

- [ ] 6. 实现 Shadows 面板绘制 — 四种阴影类型子区域
   - 在 `DrawShadowsPanel()` 方法中，在全局参数之后依次绘制 4 个子区域
   - 每个子区域使用 `EditorGUILayoutUtils.GUIComponentBoxStyle` 包裹并添加粗体标签
   - **Directional Cascade Shadow** 子区域：
     - 绘制 `directionalCascadeShadow.atlasSize`（枚举 PropertyField）
     - 绘制 `directionalCascadeShadow.blendMode`（枚举 PropertyField）
     - 绘制 `directionalCascadeShadow.cascadeCount`（IntSlider PropertyField，范围 1~4）
     - 根据 `cascadeCount` 的值动态显示 `cascadeRatio1`（count >= 2）、`cascadeRatio2`（count >= 3）、`cascadeRatio3`（count == 4）
     - 绘制 `directionalCascadeShadow.edgeFade`（Slider PropertyField，范围 0.001~1）
   - **Per Object Shadow** 子区域：绘制 `perObjectShadow.atlasSize`
   - **Spot Shadow** 子区域：绘制 `spotShadow.atlasSize`
   - **Point Shadow** 子区域：绘制 `pointShadow.atlasSize`
   - _需求：4.4、4.5_

- [ ] 7. 实现 Forward+ 面板绘制
   - 在 `ArcToonRenderPipelineAssetEditor` 中实现 `DrawForwardPlusPanel()` 方法
   - 使用可折叠标题栏 "Forward+"，内容区包裹 `helpBox` 样式
   - 绘制 `tileSize`（枚举 PropertyField）和 `maxLightsPerTile`（IntSlider PropertyField，范围 0~99）
   - _需求：5.1_

- [ ] 8. 实现 Post Processing 面板绘制
   - 在 `ArcToonRenderPipelineAssetEditor` 中实现 `DrawPostProcessingPanel()` 方法
   - 使用可折叠标题栏 "Post Processing"，内容区包裹 `helpBox` 样式
   - 绘制 `globalPostFXConfig` 的 `EditorGUILayout.PropertyField`（ObjectField，类型自动限制为 `PostFXConfig`）
   - IF `globalPostFXConfig.objectReferenceValue == null` THEN 使用 `EditorGUILayout.HelpBox("No Post FX Config assigned.", MessageType.Warning)` 显示警告提示
   - _需求：6.1、6.2_

- [ ] 9. 验证与调试
   - 在 Unity Editor 中选中 `ArcToonRenderPipelineAsset` 资源，确认：
     - 5 个面板正确显示且可折叠/展开
     - 折叠标题栏使用 ShurikenModuleTitle 风格，视觉与 ShaderGUI 一致
     - FXAA 禁用时子字段正确置灰
     - Shadow filterQuality 切换时条件字段正确显隐
     - Directional Cascade cascadeCount 变化时 ratio 字段正确显隐
     - Post Processing 为 null 时警告信息正确显示
     - 所有属性修改支持 Undo/Redo
     - ShaderGUI 的面板折叠功能未被工具类重构影响
   - _需求：1.1、1.2、1.3、7.1、7.2、7.3、7.4_
