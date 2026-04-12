# ArcToon RP 项目长期记忆

## 项目基本信息
- **核心工作目录**: `Packages/com.arctoon.render-pipeline/`
- **项目类型**: Unity 自定义渲染管线（Toon Rendering）
- **Code Review 文档目录**: `Packages/com.arctoon.render-pipeline/ChatLogs/Bugfix/`

## Code Review 文档规范（2026-04-06 确立）

所有该项目 `ChatLogs/Bugfix/` 目录下的 Code Review 文档必须遵循以下结构：

### 必须包含的两大部分

1. **✅ 已修复** — 每个条目包含：
   - 标题（问题简述）
   - **问题描述**：详细说明 bug 的位置、原因、影响
   - **修复内容**：简要说明做了什么修改（文件:行号 — 改动摘要）

2. **❌ 未修复** — 每个条目包含：
   - 标题（问题简述）
   - **问题描述**：详细说明问题的位置、原因、影响，附代码片段
   - **修复方案**：**详细**的修复步骤，包含具体的代码修改（涉及哪些文件、具体改什么、完整的代码示例），不能只写一两句建议

### 其余部分保持不变
- 文档头部的元信息（日期、审查范围、审查文件列表）
- 📊 总结表格
- 优先修复建议
- 整体评价

## 技术验证规范（2026-04-06 确立）

在该项目工作目录下，遇到不确定的技术问题时，**必须**通过以下途径进行确认和验证，不能凭记忆或推测给出结论：

1. **Unity 官方网页文档**（docs.unity3d.com / docs.unity.cn）
2. **Unity 官方渲染管线源码**（github.com/Unity-Technologies/Graphics），特别是：
   - HDRP 源码（`com.unity.render-pipelines.high-definition`）
   - URP 源码（`com.unity.render-pipelines.universal`）
   - SRP Core 源码（`com.unity.render-pipelines.core`）
3. **Unity C# Reference 源码**（github.com/Unity-Technologies/UnityCsReference）

验证后需在文档或回复中附上具体的源码文件路径或文档链接作为佐证。
