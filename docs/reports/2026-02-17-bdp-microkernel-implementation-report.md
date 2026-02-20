---
标题：BorderDefenseProtocol微内核实施报告——设计到实现的差距分析
版本号: v1.0
更新日期: 2026-02-17
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: BorderDefenseProtocol微内核从设计文档到实际实现过程中遇到的问题、设计文档错误、解决方案和经验教训的完整记录。覆盖4个实施问题、3个文档错误、对应解决方案和5条经验总结。
---

# BorderDefenseProtocol微内核实施报告——设计到实现的差距分析

## 执行摘要

本报告记录BorderDefenseProtocol微内核（Trion能量系统）从6.1详细设计文档到实际C#/XML实现过程中发现的问题。
共发现4个实施问题，其中2个源于设计文档错误，2个源于设计遗漏。
所有问题已解决，微内核成功编译（0警告0错误）。

**关键发现**：
- 设计文档的技术校验（v1.1）虽然验证了42个API，但未覆盖XML字段名
- 架构设计（5.1）未明确说明CompTrion需要通过XPath Patch注入
- 实际编译和游戏加载是发现问题的最有效手段

---

## 1. 实施问题清单

### 问题1：Need.IsFrozen访问修饰符错误

**发现阶段**：C#编译

**问题描述**：
```
error CS0507: "Need_Trion.IsFrozen": 当重写"protected"继承成员"Need.IsFrozen"时，无法更改访问修饰符
```

设计文档6.1中标注：
```
IsFrozen (get):
  comp = pawn.GetComp<CompTrion>()
  return base.IsFrozen || (comp?.Frozen ?? false)
```

未明确指定访问修饰符，实现时默认使用了 `public override`，但RimWorld源码中 `Need.IsFrozen` 是 `protected virtual`。

**根本原因**：
- 设计文档未标注访问修饰符
- 技术校验报告（v1.1）未检查属性的访问级别

**解决方案**：
```csharp
// 错误：public override bool IsFrozen
// 正确：
protected override bool IsFrozen => base.IsFrozen || (Comp?.Frozen ?? false);
```

**影响范围**：
- 修改文件：`Need_Trion.cs`
- 更新文档：`6.1_Trion能量系统详细设计.md` v1.2

---

### 问题2：缺少CompTrion注入Patch

**发现阶段**：编写测试指导时

**问题描述**：
CompTrion需要挂载到Pawn的ThingDef.comps列表中才能通过 `GetComp<CompTrion>()` 获取。
设计文档和架构文档均未提及需要XPath Patch将CompTrion注入到Human ThingDef。

**根本原因**：
- 架构设计（5.1）假设CompTrion"可挂载到任意Thing"，但未说明挂载方式
- 详细设计（6.1）专注于C#实现，未覆盖XML配置的完整性

**解决方案**：
创建 `1.6/Patches/Patch_HumanlikePawn.xml`：
```xml
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/ThingDef[@Name="Human"]/comps</xpath>
    <value>
      <li Class="BDP.Core.CompProperties_Trion">
        <showGizmo>true</showGizmo>
        <barColor>(0.2, 0.8, 0.3, 1.0)</barColor>
      </li>
    </value>
  </Operation>
</Patch>
```

**影响范围**：
- 新增文件：`Patch_HumanlikePawn.xml`
- 更新文档：测试指导（原TC-2标注为"缺失"，现已补全）

---

### 问题3：GeneDef字段名错误（causesNeed）

**发现阶段**：游戏加载XML

**问题描述**：
```
XML error: <causesNeed>BDP_Need_Trion</causesNeed> doesn't correspond to any field in type GeneDef
```

设计文档中使用了 `causesNeed` 字段，但RimWorld 1.6中GeneDef的正确字段是 `enablesNeeds`（复数，且为列表）。

**根本原因**：
- 设计文档编写时未查阅RimWorld源码中GeneDef的实际字段
- 技术校验（v1.1）仅验证C# API，未验证XML Def字段名

**验证方法**：
通过grep搜索RimWorld原版GeneDef示例：
```bash
grep -A5 "Need" "C:/NiwtGames/Steam/steamapps/common/RimWorld/Data/Biotech/Defs/GeneDefs/GeneDefs_Misc.xml"
```

发现正确用法：
```xml
<enablesNeeds>
  <li>KillThirst</li>
</enablesNeeds>
```

**解决方案**：
```xml
<!-- 错误 -->
<causesNeed>BDP_Need_Trion</causesNeed>

<!-- 正确 -->
<enablesNeeds>
  <li>BDP_Need_Trion</li>
</enablesNeeds>
```

**影响范围**：
- 修改文件：`GeneDef_TrionGland.xml`

---

### 问题4：NeedDef字段名错误（threshPercents）

**发现阶段**：游戏加载XML

**问题描述**：
```
XML error: <threshPercents><li>0.1</li><li>0.3</li></threshPercents> doesn't correspond to any field in type NeedDef
```

设计文档6.1中NeedDef XML示例包含 `threshPercents` 字段，但RimWorld 1.6的NeedDef中不存在此字段。

**根本原因**：
- 设计文档参考了不准确的信息源或旧版本RimWorld
- 未通过实际游戏加载验证XML正确性

**验证方法**：
检查RimWorld原版NeedDef：
```bash
cat "C:/NiwtGames/Steam/steamapps/common/RimWorld/Data/Biotech/Defs/NeedDefs/Needs.xml"
```

发现所有NeedDef均无 `threshPercents` 字段。需求条上的阈值标记可能通过其他机制实现，或该功能在1.6中不存在。

**解决方案**：
删除 `threshPercents` 字段。如果需要阈值标记，需要通过自定义Need类的C#代码实现。

**影响范围**：
- 修改文件：`NeedDef_Trion.xml`

---

## 2. 设计文档错误汇总

| 文档 | 版本 | 错误类型 | 具体错误 | 严重性 |
|------|------|---------|---------|--------|
| 6.1详细设计 | v1.1 | API访问级别 | IsFrozen未标注protected | P1-编译失败 |
| 6.1详细设计 | v1.1 | XML字段名 | causesNeed应为enablesNeeds | P0-加载失败 |
| 6.1详细设计 | v1.1 | XML字段名 | threshPercents不存在 | P0-加载失败 |
| 5.1架构设计 | v1.1 | 实现遗漏 | 未提及需要Patch注入CompTrion | P1-功能缺失 |

**严重性定义**：
- P0：导致游戏无法加载模组
- P1：导致编译失败或核心功能缺失
- P2：功能可用但不符合设计预期

---

## 3. 解决方案总结

### 3.1 即时修复

| 问题 | 修复方式 | 修复时间 | 验证方法 |
|------|---------|---------|---------|
| IsFrozen访问级别 | 改为protected override | 编译时 | dotnet build |
| 缺少Patch | 新增Patch_HumanlikePawn.xml | 测试准备时 | 游戏加载 |
| causesNeed错误 | 改为enablesNeeds列表 | 游戏加载时 | 游戏加载 |
| threshPercents错误 | 删除该字段 | 游戏加载时 | 游戏加载 |
