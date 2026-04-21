---
# 注意不要修改本文头文件，如修改，CodeBuddy（内网版）将按照默认逻辑设置
type: always
---
当 Unity .asset 序列化文件中存在某个字段，但对应的代码类中不存在该字段时，应判断为废弃的序列化残留数据，而非缺失的代码字段。不要将 asset 文件中存在而代码中不存在的字段反向添加回代码中。Unity 反序列化时会自动忽略这类多余的键值对。

在进行任何形式的代码改动时，如果用户要求：“严格参考”，则：

- 尽可能的查阅Unity官方文档，找出关键文档作为佐证。
- 在编写代码时，尽可能找到URP/HDRP中的代码参考
  - 符合当前项目版本的源码位置在：/Users/archerdu/UnityProject/ArcToonRP/Library/PackageCache/com.unity.render-pipelines.universal 
  - 最新的源码位置在：/Users/archerdu/UnityProject/Reference/Graphics

When referencing URP/HDRP source code from /Users/archerdu/UnityProject/Reference/Graphics (master branch), always treat it as design reference only. Before using any API, type, field, or method from the reference code, must first verify it exists in the project's actual SRP dependency at /Users/archerdu/UnityProject/ArcToonRP/Library/PackageCache/com.unity.render-pipelines.core/ (and other relevant packages). The master branch may contain newer APIs not available in the project's Unity/SRP version. The project's PackageCache is the source of truth for available APIs.