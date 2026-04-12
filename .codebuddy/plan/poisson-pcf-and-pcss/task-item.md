# 实施计划：泊松圆盘采样 PCF 与 PCSS 软阴影

- [ ] 1. 扩展 `ShadowSettings` 配置结构
   - 在 `ShadowSettings.cs` 的 `FilterQuality` 枚举中新增 `PoissonDisk` 和 `PCSS` 两个选项
   - 新增 `poissonFilterRadius` 浮点字段（默认值建议 5.0，带 `[Range]` 限制），用于泊松圆盘 PCF 的采样半径
   - 新增 `pcssLightSize` 浮点字段（默认值建议 1.0，带 `[Min(0.001f)]` 限制），用于 PCSS 的光源大小参数
   - 修改 `filterSize` 属性的计算逻辑：当 `FilterQuality` 为 `PoissonDisk` 时返回 4（等效 PCF7x7），为 `PCSS` 时也返回 4，以保证 normalBias 计算正确
   - _需求：1.1、1.2、1.3、4.5_

- [ ] 2. 扩展 `ShadowMapRenderer` 中的关键字管理与参数传递
   - 在 `ShadowMapRenderer.cs` 的 `filterKeywords` 数组中追加 `GlobalKeyword.Create("_POISSON_DISK")` 和 `GlobalKeyword.Create("_PCSS")`
   - 确认 `commandBuffer.SetKeywords(filterKeywords, (int)settings.filterQuality - 1)` 的索引逻辑与新枚举值对齐（PCF2x2=0 对应 index -1 即全部关闭，PCF3x3=1 对应 index 0，以此类推，PoissonDisk 和 PCSS 对应 index 3 和 4）
   - 在设置关键字附近，添加 `commandBuffer.SetGlobalFloat("_PoissonFilterRadius", settings.poissonFilterRadius)` 和 `commandBuffer.SetGlobalFloat("_PcssLightSize", settings.pcssLightSize)` 的参数传递逻辑（仅在对应模式时传递）
   - _需求：4.1、4.2、1.4、6.5_

- [ ] 3. 创建泊松圆盘采样点集的 HLSL 头文件
   - 在 `ShaderLibrary/` 目录下新建 `PoissonDisk.hlsl` 文件
   - 定义 `static const float2 poissonDisk[POISSON_SAMPLE_COUNT]` 数组，包含至少 16 个预计算的泊松圆盘采样点（归一化到 [-1, 1] 的单位圆盘内）
   - 定义 `POISSON_SAMPLE_COUNT` 宏（值为 16）
   - _需求：2.1_

- [ ] 4. 在 `Shadow.hlsl` 中实现泊松圆盘 PCF 过滤路径
   - 在文件顶部的关键字条件编译区域新增 `#elif defined(_POISSON_DISK) || defined(_PCSS)` 分支，`#include "PoissonDisk.hlsl"`
   - 在 CBuffer `_CustomShadows` 中声明 `float _PoissonFilterRadius` 和 `float _PcssLightSize`
   - 实现 `FilterDirectionalShadowPoisson(float3 positionSTS)` 函数：遍历泊松圆盘采样点，偏移量 = `poissonDisk[i] * _PoissonFilterRadius * _DirectionalShadowAtlasSize.y`（其中 `.y` 为 texel size），对每个偏移位置调用 `SAMPLE_TEXTURE2D_SHADOW` 并取均匀加权平均
   - 实现 `FilterPerObjectShadowPoisson(float3 positionSTS)` 函数：逻辑同上，使用 `_PerObjectAtlasSize`
   - 实现 `FilterSpotShadowPoisson(float3 positionSTS, float3 bounds)` 函数：逻辑同上，使用 `_SpotShadowAtlasSize`，采样前需 clamp 到 bounds 范围内
   - 实现 `FilterPointShadowPoisson(float3 positionSTS, float3 bounds)` 函数：逻辑同上，使用 `_PointShadowAtlasSize`，采样前需 clamp 到 bounds 范围内
   - 在各 `FilterXxxShadow` 函数中添加 `#elif defined(_POISSON_DISK)` 分支，调用对应的 Poisson 过滤函数
   - _需求：2.1、2.2、2.3、2.4、2.5_

- [ ] 5. 在 `Shadow.hlsl` 中声明 PCSS 所需的非比较式采样器
   - 在 `SAMPLER_CMP(SHADOW_SAMPLER)` 声明之后，添加 `#if defined(_PCSS)` 条件包围的非比较式采样器声明：`SAMPLER(sampler_DirectionalShadowAtlas)`（或使用通用 `sampler_linear_clamp` 命名）
   - 该采样器用于 Blocker Search 阶段通过 `SAMPLE_TEXTURE2D_LOD` 读取 Shadow Map 的原始深度值
   - _需求：5.1、5.2、5.3_

- [ ] 6. 实现 PCSS Blocker Search 函数
   - 在 `Shadow.hlsl` 中实现 `BlockerSearch_Directional(float3 positionSTS, float searchRadius)` 函数：
     - 遍历泊松圆盘采样点，使用非比较式采样器 `SAMPLE_TEXTURE2D_LOD` 采样原始深度
     - 将深度值小于 `positionSTS.z` 的采样标记为遮挡物，累计遮挡物深度和计数
     - 返回结构体或 `float2`：`(avgBlockerDepth, blockerCount / totalSamples)`
   - 实现对应的 `BlockerSearch_Spot`、`BlockerSearch_Point` 版本（采样前 clamp 到 tile bounds 内）
   - 处理边界情况：blockerCount == 0（全亮）和 blockerCount == totalSamples（全暗），以及 avgBlockerDepth 接近 0 的除零保护
   - _需求：3.1(Step1)、3.2、3.3、3.4、3.5_

- [ ] 7. 实现 PCSS 完整三步算法（方向光 + 逐物体阴影）
   - 在 `Shadow.hlsl` 中实现 `FilterDirectionalShadowPCSS(float3 positionSTS)` 函数：
     - Step 1：调用 `BlockerSearch_Directional` 获取平均遮挡物深度 `zBlocker`
     - Step 2：半影宽度计算 `penumbraWidth = _PcssLightSize * (zReceiver - zBlocker) / zBlocker`（注意：方向光为正交投影，此公式可能需调整为 `_PcssLightSize * (zReceiver - zBlocker)`，待实现时验证）
     - Step 3：使用 `penumbraWidth` 作为动态半径执行泊松圆盘 PCF 过滤
     - 包含早退优化：无遮挡物返回 1.0，全遮挡返回 0.0
   - 实现 `FilterPerObjectShadowPCSS(float3 positionSTS)` 函数，逻辑同上
   - 在 `FilterDirectionalShadow` 和 `FilterPerObjectShadow` 中添加 `#elif defined(_PCSS)` 分支
   - _需求：3.1(Step1/2/3)、3.2、3.3_

- [ ] 8. 实现 PCSS 完整三步算法（聚光灯 + 点光源）
   - 实现 `FilterSpotShadowPCSS(float3 positionSTS, float3 bounds)` 函数：
     - 三步 PCSS 算法，与方向光版本类似
     - Blocker Search 和 PCF 阶段均需 clamp 采样坐标到 bounds 范围内
   - 实现 `FilterPointShadowPCSS(float3 positionSTS, float3 bounds)` 函数：逻辑同上
   - 在 `FilterSpotShadow` 和 `FilterPointShadow` 中添加 `#elif defined(_PCSS)` 分支
   - _需求：3.5_

- [ ] 9. 兼容性验证与集成测试
   - 确认原有 PCF2x2 / PCF3x3 / PCF5x5 / PCF7x7 模式的代码路径未受到修改，行为保持不变
   - 验证 `_POISSON_DISK` 和 `_PCSS` 关键字启用时，原有 `_PCF3X3` / `_PCF5X5` / `_PCF7X7` 关键字均被正确禁用（由 `SetKeywords` 的互斥机制保证）
   - 验证级联阴影混合（`_CASCADE_BLEND_SOFT` / Dither）在新模式下正常工作
   - 验证 Shadow Mask（Always / Distance）在新模式下正常工作
   - 验证逐物体阴影在新模式下正常工作
   - _需求：6.1、6.2、6.3、6.4、6.5_
