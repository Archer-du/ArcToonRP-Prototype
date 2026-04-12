# 需求文档：RenderPipelineConfig 自定义 GUI

## 引言

当前 `ArcToonRenderPipelineAsset` 在 Unity Inspector 中使用默认的序列化绘制方式展示 `RenderPipelineConfig` 的所有字段。项目的 Editor 模块中已有一套成熟的自定义 GUI 风格（用于 ShaderGUI），采用**可折叠面板分组** + **组件化布局** + **自定义样式盒子**的设计，视觉效果美观且层次清晰。

本功能的目标是为 `ArcToonRenderPipelineAsset` 创建一个自定义 `CustomEditor`，将 `RenderPipelineConfig` 及其子设置类的 Inspector 绘制重写为与现有 ShaderGUI 相同的可折叠面板风格，提供一致的视觉体验和更好的用户交互。

### 涉及的数据结构

- **RenderPipelineConfig**：顶层配置类
  - `bool useSRPBatcher`
  - `CameraBufferSettings cameraBufferSettings`
  - `ShadowSettings globalShadowSettings`
  - `ForwardPlusSettings forwardPlusSettings`
  - `PostFXConfig globalPostFXConfig`（ScriptableObject 引用）

- **CameraBufferSettings**：HDR、渲染缩放、双三次重采样模式、FXAA 设置
- **ShadowSettings**：方向光级联阴影、逐物体阴影、聚光灯阴影、点光源阴影、滤波质量、PCSS 相关参数等
- **ForwardPlusSettings**：Tile 大小、每 Tile 最大灯光数
- **PostFXConfig**：ScriptableObject 引用（ObjectField），不需展开绘制其内部字段

### 现有 GUI 风格参考

参考 `BaseFoldoutShaderPanel` + `ShaderGUILayout` 的风格：
- 使用 `ShurikenModuleTitle` 风格的折叠标题栏，字体 14px 加粗，高度 30px
- 子区域使用 `helpBox` 样式包裹
- 组件内部使用 `GUI.skin.box` 派生样式，带合适的 padding/margin
- 缩进使用 `EditorGUI.indentLevel += 2` 的方式
- Toggle 属性组使用 `BeginTogglePropertyGroup` / `EndTogglePropertyGroup` 模式

---

## 需求

### 需求 1：创建 ArcToonRenderPipelineAsset 自定义 Editor

**用户故事：** 作为一名使用 ArcToon 渲染管线的开发者，我希望在选中 RenderPipelineAsset 时看到与 ShaderGUI 风格一致的自定义 Inspector 界面，以便获得统一且美观的编辑体验。

#### 验收标准

1. WHEN 用户在 Unity Editor 中选中 `ArcToonRenderPipelineAsset` 资源 THEN 系统 SHALL 显示自定义的 Inspector 界面，而非 Unity 默认的序列化字段展示。
2. WHEN 自定义 Inspector 显示时 THEN 系统 SHALL 将 `RenderPipelineConfig` 的各子设置按逻辑分组为多个可折叠面板：General、Camera Buffer、Shadows、Forward+、Post Processing。
3. WHEN 用户修改任何属性值 THEN 系统 SHALL 正确标记资源为 dirty 并支持 Undo/Redo 操作。

### 需求 2：General 面板绘制

**用户故事：** 作为一名开发者，我希望在 General 面板中看到管线的基础配置选项，以便快速切换 SRP Batcher 等全局设置。

#### 验收标准

1. WHEN General 面板展开 THEN 系统 SHALL 显示 `useSRPBatcher` 的 Toggle 控件。
2. WHEN 用户折叠 General 面板 THEN 系统 SHALL 隐藏其内部字段，仅显示折叠标题栏。

### 需求 3：Camera Buffer 面板绘制

**用户故事：** 作为一名开发者，我希望在 Camera Buffer 面板中看到相机缓冲区相关配置（HDR、渲染缩放、FXAA 等），以便方便地调整渲染品质参数。

#### 验收标准

1. WHEN Camera Buffer 面板展开 THEN 系统 SHALL 显示 `enableHDR`（Toggle）、`renderScale`（Slider 0.5~2）、`bicubicRescalingMode`（枚举下拉）字段。
2. WHEN Camera Buffer 面板展开 THEN 系统 SHALL 在 FXAA 子区域中显示 `enabled`（Toggle）、`fixedThreshold`（Slider）、`relativeThreshold`（Slider）、`subpixelBlending`（Slider）、`quality`（枚举下拉）字段。
3. IF FXAA 的 `enabled` 为 false THEN 系统 SHALL 将 FXAA 子区域的其余字段置为禁用（灰色不可编辑）状态。

### 需求 4：Shadows 面板绘制

**用户故事：** 作为一名开发者，我希望在 Shadows 面板中看到所有阴影相关配置的分组展示，以便精确调整各类型阴影的参数。

#### 验收标准

1. WHEN Shadows 面板展开 THEN 系统 SHALL 显示以下全局阴影参数：`filterQuality`（枚举下拉）、`maxDistance`（Float 字段）、`distanceFade`（Slider 0.001~1）。
2. IF `filterQuality` 为 `PoissonDisk` 或 `PCSS` THEN 系统 SHALL 额外显示 `poissonFilterRadius`（Slider 0.01~10）字段。
3. IF `filterQuality` 为 `PCSS` THEN 系统 SHALL 额外显示 `pcssLightSize`（Float 字段，最小 0.001）字段。
4. WHEN Shadows 面板展开 THEN 系统 SHALL 将 Directional Cascade Shadow、Per Object Shadow、Spot Shadow、Point Shadow 分别作为子区域显示，每个子区域包含各自的 `atlasSize` 枚举下拉。
5. WHEN Directional Cascade Shadow 子区域展开 THEN 系统 SHALL 额外显示 `blendMode`（枚举下拉）、`cascadeCount`（IntSlider 1~4）、`cascadeRatio1/2/3`（Slider 0~1，根据 cascadeCount 决定显示数量）、`edgeFade`（Slider 0.001~1）。

### 需求 5：Forward+ 面板绘制

**用户故事：** 作为一名开发者，我希望在 Forward+ 面板中看到前向+渲染的 Tile 配置，以便调整光照裁剪粒度。

#### 验收标准

1. WHEN Forward+ 面板展开 THEN 系统 SHALL 显示 `tileSize`（枚举下拉）和 `maxLightsPerTile`（IntSlider 0~99）字段。

### 需求 6：Post Processing 面板绘制

**用户故事：** 作为一名开发者，我希望在 Post Processing 面板中看到后处理配置的 ScriptableObject 引用字段，以便关联或切换全局后处理配置资源。

#### 验收标准

1. WHEN Post Processing 面板展开 THEN 系统 SHALL 显示 `globalPostFXConfig` 的 ObjectField（类型限制为 `PostFXConfig`），允许用户拖入或选择 PostFXConfig 资源。
2. WHEN `globalPostFXConfig` 字段为 null THEN 系统 SHALL 在面板中显示提示信息 "No Post FX Config assigned"。

### 需求 7：视觉风格一致性

**用户故事：** 作为一名开发者，我希望 RenderPipelineAsset 的 Inspector GUI 与现有 ShaderGUI 的折叠面板风格保持一致，以便获得统一的视觉体验。

#### 验收标准

1. WHEN 可折叠面板绘制时 THEN 系统 SHALL 使用与 `ShaderGUILayout.DrawGUIComponentFoldoutGroup` 相同的视觉风格（ShurikenModuleTitle 样式、14px 加粗字体、30px 高度标题栏）。
2. WHEN 面板内部子区域绘制时 THEN 系统 SHALL 使用与 `ShaderGUILayout.GUIComponentBoxStyle` 相同的 box 样式包裹。
3. WHEN 面板之间绘制间距 THEN 系统 SHALL 使用 `EditorGUILayout.Space(6)` 保持与 ShaderGUI 一致的间距。
4. WHEN 需要条件显隐字段时 THEN 系统 SHALL 使用与 `ShaderGUILayout.BeginTogglePropertyGroup` 相同的 Toggle 组模式。

### 需求 8：代码架构与可维护性

**用户故事：** 作为一名开发者，我希望自定义 Editor 的代码结构清晰且可维护，以便将来扩展新的设置面板。

#### 验收标准

1. WHEN 创建自定义 Editor 类 THEN 系统 SHALL 将其放置在 `Editor/Overrides/` 目录下，使用 `ArcToon.Editor.Overrides` 命名空间。
2. WHEN 需要复用 GUI 布局工具方法时 THEN 系统 SHALL 将与 SerializedProperty 相关的通用布局方法抽取到独立的工具类中（如 `EditorGUILayoutUtils`），使其可被 ShaderGUI 和 CustomEditor 共同使用。
3. WHEN 代码组织时 THEN 系统 SHALL 保持每个面板绘制逻辑的方法独立，便于后续增减面板。
