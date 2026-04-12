# 需求文档：泊松圆盘采样 PCF 与 PCSS 软阴影

## 引言

本功能为 ArcToon 渲染管线的阴影系统添加两项新的阴影过滤模式：

1. **泊松圆盘 PCF（Poisson Disk PCF）**：在现有 Tent Filter PCF 的基础上，新增基于泊松圆盘采样分布的 PCF 过滤方式。泊松圆盘采样相比规则的 Tent 滤波器能产生更自然的阴影边缘，减少规则的条纹噪声图案。

2. **PCSS（Percentage Closer Soft Shadows）**：在泊松圆盘 PCF 的基础上进一步实现自适应半影宽度的软阴影。PCSS 能根据遮挡物到受影面的距离自动调整阴影模糊范围——离遮挡物越近阴影越锐利，越远越模糊，更接近真实世界的阴影表现。

### 当前系统概述

当前阴影系统采用全局 `FilterQuality` 枚举控制 PCF 质量（PCF2x2 / PCF3x3 / PCF5x5 / PCF7x7），通过 `_PCF3X3` / `_PCF5X5` / `_PCF7X7` 全局 shader 关键字切换。所有四种阴影类型（方向光、逐物体、聚光灯、点光源）共用同一套过滤质量设置。底层使用 Unity 内置 `ShadowSamplingTent.hlsl` 的 Tent Filter + 硬件 2×2 双线性 PCF。

### 参考来源

- [实时阴影技术（1）Shadow Mapping - KillerAery](https://www.cnblogs.com/KillerAery/p/15201310.html#percentage-closer-filteringpcf)
- NVIDIA GPU Gems - Percentage-Closer Soft Shadows

---

## 需求

### 需求 1：扩展阴影过滤模式配置

**用户故事：** 作为一名技术美术/渲染工程师，我希望能在渲染管线的 ShadowSettings 中选择新的阴影过滤模式（泊松圆盘 PCF 和 PCSS），以便根据项目需求选择最合适的阴影质量与性能平衡。

#### 验收标准

1. WHEN 用户在 `ShadowSettings` 中设置 `FilterQuality` THEN 系统 SHALL 提供以下可选项：原有的 `PCF2x2`、`PCF3x3`、`PCF5x5`、`PCF7x7`，以及新增的 `PoissonDisk`、`PCSS`。
2. WHEN 用户选择 `PCSS` 模式 THEN `ShadowSettings` SHALL 额外暴露 `lightSize`（光源大小，UV 空间）参数，用于控制 PCSS 的半影范围。
3. WHEN 用户选择 `PoissonDisk` 模式 THEN `ShadowSettings` SHALL 额外暴露 `poissonFilterRadius`（泊松采样半径）参数，用于控制固定半径的泊松圆盘 PCF 模糊范围。
4. IF 用户选择了 `PoissonDisk` 或 `PCSS` THEN 系统 SHALL 在 C# 端设置对应的全局 shader 关键字（`_POISSON_DISK` 或 `_PCSS`），并禁用原有的 Tent PCF 关键字。

---

### 需求 2：泊松圆盘采样 PCF 的 HLSL 实现

**用户故事：** 作为一名技术美术，我希望阴影边缘使用泊松圆盘采样进行柔化，以便获得比规则 Tent Filter 更自然、更少条纹痕迹的阴影边缘效果。

#### 验收标准

1. WHEN `_POISSON_DISK` 关键字被启用 THEN `Shadow.hlsl` SHALL 使用预定义的泊松圆盘采样点集（至少 16 个采样点）进行阴影过滤。
2. WHEN 执行泊松圆盘 PCF 过滤 THEN 系统 SHALL 在每个采样点位置调用 `SAMPLE_TEXTURE2D_SHADOW`（利用硬件 2×2 双线性 PCF），并将所有采样结果取均匀加权平均。
3. WHEN 执行泊松圆盘 PCF 过滤 THEN 系统 SHALL 使用 C# 端传入的 `poissonFilterRadius` 参数乘以 atlas 的 texel 大小来缩放泊松圆盘采样偏移量。
4. WHEN 对聚光灯和点光源执行泊松圆盘 PCF THEN 系统 SHALL 将采样坐标 clamp 到 tile 边界内（与现有 Tent PCF 行为一致），以防止采样越界到相邻 tile。
5. WHEN 泊松圆盘 PCF 应用于四种阴影类型（方向光、逐物体、聚光灯、点光源）THEN 各自的 `FilterXxxShadow` 函数 SHALL 分别实现对应的泊松圆盘过滤路径。

---

### 需求 3：PCSS 软阴影的 HLSL 实现

**用户故事：** 作为一名技术美术/渲染工程师，我希望方向光阴影能够根据遮挡物与受影面的距离自适应地调整半影宽度，以便获得更加真实的软阴影效果（近处硬、远处软）。

#### 验收标准

1. WHEN `_PCSS` 关键字被启用 THEN 系统 SHALL 对方向光（包括逐物体阴影）执行完整的三步 PCSS 算法：
   - **Step 1 - Blocker Search（遮挡物搜索）**：在 Shadow Map 上使用泊松圆盘采样点进行非比较式深度采样（`SAMPLE_TEXTURE2D`），统计遮挡物的平均深度。
   - **Step 2 - Penumbra Estimation（半影估计）**：根据光源大小（`lightSize`）、遮挡物平均深度（`zBlocker`）和接收面深度（`zReceiver`）计算自适应的半影宽度 `penumbraWidth = lightSize * (zReceiver - zBlocker) / zBlocker`。
   - **Step 3 - PCF Filtering**：使用泊松圆盘采样 + 动态半影宽度执行 PCF 过滤。
2. WHEN Blocker Search 阶段未找到任何遮挡物 THEN 系统 SHALL 直接返回 1.0（完全照亮），跳过后续步骤以优化性能。
3. WHEN 所有采样点均被遮挡 THEN 系统 SHALL 直接返回 0.0（完全阴影），跳过 PCF 过滤步骤以优化性能。
4. WHEN `_PCSS` 关键字被启用 THEN Blocker Search 阶段 SHALL 需要额外声明一个非比较式的 `SAMPLER`（普通 `sampler_linear_clamp`）用于读取 Shadow Map 的原始深度值。
5. WHEN PCSS 应用于聚光灯和点光源 THEN 系统 SHALL 同样实现对应的 PCSS 路径，并将采样坐标 clamp 到 tile 边界内。

---

### 需求 4：C# 端参数传递与关键字管理

**用户故事：** 作为一名渲染工程师，我希望新的过滤模式的配置参数能够正确地从 C# 端传递到 GPU 端，以便 shader 能够获取到正确的运行时参数。

#### 验收标准

1. WHEN 渲染阴影时 THEN `ShadowMapRenderer` SHALL 根据 `FilterQuality` 设置正确地启用 / 禁用对应的全局 shader 关键字（`_PCF3X3` / `_PCF5X5` / `_PCF7X7` / `_POISSON_DISK` / `_PCSS`）。
2. WHEN `PoissonDisk` 或 `PCSS` 模式启用 THEN `ShadowMapRenderer` SHALL 通过 `CommandBuffer.SetGlobalFloat` 传递相关参数（`_PoissonFilterRadius` 或 `_PcssLightSize`）到 GPU。
3. WHEN `PCSS` 模式启用 THEN 系统 SHALL 在 `Shadow.hlsl` 的 CBuffer 中声明对应的浮点参数（`_PcssLightSize`）以接收 C# 端传递的值。
4. WHEN `PoissonDisk` 模式启用 THEN 系统 SHALL 在 `Shadow.hlsl` 的 CBuffer 中声明对应的浮点参数（`_PoissonFilterRadius`）以接收 C# 端传递的值。
5. IF `FilterQuality` 设置为 `PoissonDisk` 或 `PCSS` THEN `filterSize` 属性 SHALL 返回一个合理的值以保证 normalBias 计算的正确性（建议等效于 PCF5x5 或 PCF7x7 的 filterSize）。

---

### 需求 5：PCSS 需额外的非比较式纹理采样器

**用户故事：** 作为一名渲染工程师，我需要 PCSS 的 Blocker Search 阶段能够读取 Shadow Map 的原始深度值（而非深度比较结果），以便正确计算遮挡物平均深度。

#### 验收标准

1. WHEN `_PCSS` 关键字启用 THEN `Shadow.hlsl` SHALL 额外声明一个普通的（非比较式的）`SAMPLER` 用于 Blocker Search 阶段读取 Shadow Map 原始深度。
2. WHEN 使用非比较式采样器读取 Shadow Map 深度 THEN 系统 SHALL 使用 `SAMPLE_TEXTURE2D_LOD`（指定 LOD 为 0）对 Shadow Atlas 进行采样以获取原始深度值。
3. WHEN Blocker Search 使用非比较式采样 THEN 系统 SHALL 只在 `_PCSS` 关键字激活时编译此路径，避免在其他模式下产生额外开销。

---

### 需求 6：与现有系统的兼容性

**用户故事：** 作为一名渲染工程师，我希望新增的过滤模式不会破坏现有的阴影功能（级联混合、Shadow Mask、逐物体阴影等），以便现有项目能够平滑升级。

#### 验收标准

1. WHEN 用户选择原有的 `PCF2x2`、`PCF3x3`、`PCF5x5` 或 `PCF7x7` THEN 系统 SHALL 保持与当前完全一致的行为，不引入任何变化。
2. WHEN 使用 `PoissonDisk` 或 `PCSS` 模式 THEN 级联阴影混合（Dither / Soft）SHALL 继续正常工作。
3. WHEN 使用 `PoissonDisk` 或 `PCSS` 模式 THEN Shadow Mask（Always / Distance 模式）的混合逻辑 SHALL 继续正常工作。
4. WHEN 使用 `PoissonDisk` 或 `PCSS` 模式 THEN 逐物体阴影（PerObject Shadow）SHALL 继续正常工作。
5. WHEN `_PCSS` 或 `_POISSON_DISK` 被启用 THEN 原有的 `_PCF3X3` / `_PCF5X5` / `_PCF7X7` 关键字 SHALL 全部被禁用，确保不会同时激活多套过滤逻辑。

---

## 技术约束与边界情况

### 性能考量
- 泊松圆盘 PCF 使用 16 个采样点时，每像素将执行 16 次纹理采样（每次硬件 2×2），性能开销高于 PCF7x7 的 16 次 Tent 采样（因为 Tent 的权重分布更优化）。
- PCSS 需要两轮采样（Blocker Search + PCF），开销约为单次泊松 PCF 的 2 倍。
- 建议在 Editor 中为 `PoissonDisk` 和 `PCSS` 模式标注性能警告提示。

### 数值精度
- PCSS 的 `penumbraWidth` 计算中，当 `zBlocker` 接近 0 时需要做保护性 clamp 以避免除零错误。
- 泊松圆盘采样偏移量需要乘以 atlas texel size 才能得到正确的 UV 空间偏移。

### 方向光 PCSS 的特殊性
- 方向光使用正交投影，其 Shadow Map 的深度分布与透视投影不同。PCSS 的 penumbra 公式 `(zReceiver - zBlocker) / zBlocker` 在正交投影下可能需要调整为 `(zReceiver - zBlocker) * lightSize`（去掉 `/zBlocker` 的透视除法），需在实现阶段进一步验证。
