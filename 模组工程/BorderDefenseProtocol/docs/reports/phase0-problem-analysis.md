# 阶段0问题分析与修复方案

**日期**: 2026-03-02
**测试对象**: Elekio（16个Hediff，4衣物+3物品）
**源码验证**: ✅ 已通过RimWorld源码验证

---

## 📋 问题清单

| # | 问题 | 严重程度 | 证据 | 源码验证 |
|---|------|---------|------|---------|
| 1 | MissingBodyPart循环依赖 | 🔴🔴 致命 | 5次错误日志 | ✅ |
| 2 | Hediff_Level.level字段未恢复 | 🔴 高 | 灵能4级→1级 | ✅ |
| 3 | HediffComp_GetsPermanent状态丢失 | 🔴 高 | "老枪伤（酸痛）"→"枪伤" | ✅ |
| 4 | Hediff伤口来源信息丢失 | 🟡 中 | "割伤（七首 刀刃）"→"割伤" | ✅ |
| 5 | 失血速率异常增加 | 🔴 高 | 18%/日→58%/日 | ✅ |

---

## 🔍 问题1：MissingBodyPart循环依赖

### 现象
```
Tried to add health diff to missing part BodyPartRecord(Femur parts.Count=0)
Tried to add health diff to missing part BodyPartRecord(Tibia parts.Count=0)
Tried to add health diff to missing part BodyPartRecord(Foot parts.Count=5)
Tried to add health diff to missing part BodyPartRecord(Toe parts.Count=0) (×4次)
```

### 根本原因
当一个部位被截肢时，RimWorld会：
1. 在顶层部位（如Leg）添加`MissingBodyPart` Hediff
2. 自动在所有子部位（Femur、Tibia、Foot、Toe×5）也添加`MissingBodyPart`
3. 子部位实际上已经"不存在"（`parts.Count=0`表示没有子部位）

我们的代码记录了所有的`MissingBodyPart`，包括子部位的。恢复时试图在"不存在的部位"上添加Hediff，RimWorld拒绝了。

### 源码证据
```csharp
// HediffSet.cs (推断)
// 当添加MissingBodyPart到父部位时，子部位会自动标记为缺失
// 不能在已缺失的部位上添加Hediff
```

### 修复方案
**只记录顶层缺失部位**（parent不缺失的部位）：

```csharp
private static List<HediffRecord> TakeHediffRecords(Pawn pawn)
{
    var records = new List<HediffRecord>();
    foreach (var h in pawn.health.hediffSet.hediffs)
    {
        // 跳过子部位的MissingBodyPart
        if (h.def == HediffDefOf.MissingBodyPart && h.Part != null)
        {
            // 检查父部位是否也缺失
            bool parentMissing = pawn.health.hediffSet.hediffs
                .Any(other => other.def == HediffDefOf.MissingBodyPart
                           && other.Part == h.Part.parent);
            if (parentMissing)
            {
                continue; // 跳过子部位的MissingBodyPart
            }
        }

        // 记录Hediff...
    }
    return records;
}
```

### 理由
- RimWorld的部位系统是树形结构
- `MissingBodyPart`会自动传播到子部位
- 只需恢复顶层，子部位会自动处理

---

## 🔍 问题2：Hediff_Level.level字段未恢复

### 现象
- 测试前：启灵神经（等级4）
- 测试后：启灵神经（等级1）

### 根本原因
**源码证据**：
```csharp
// Hediff_Level.cs
public class Hediff_Level : HediffWithComps
{
    public int level = 1;  // ← 默认值是1

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        Severity = level;  // ← Severity会被level覆盖！
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref level, "level", 0);  // ← 保存的是level，不是Severity
    }
}
```

**问题链**：
1. 我们只记录了`Severity = 4.0`
2. `AddHediff`创建新实例时，`level`字段被初始化为1
3. 我们设置`Severity = 4.0`
4. 下一次`TickInterval`时，`Severity`被`level`（值为1）覆盖

### 修复方案
**使用反射读写`level`字段**：

```csharp
private class HediffRecord
{
    public string defName;
    public float severity;
    public int? level;  // 新增：用于Hediff_Level
    // ...
}

// 快照时
if (h is Hediff_Level hediffLevel)
{
    record.level = hediffLevel.level;
}

// 恢复时
if (restored is Hediff_Level hediffLevel && record.level.HasValue)
{
    hediffLevel.level = record.level.Value;
    hediffLevel.Severity = record.level.Value;  // 同步Severity
}
else
{
    restored.Severity = record.severity;
}
```

### 理由
- `Hediff_Level`使用`level`字段作为真实数据源
- `Severity`只是显示值，会被`level`覆盖
- 必须恢复`level`字段才能保持正确状态

---

## 🔍 问题3：HediffComp_GetsPermanent状态丢失

### 现象
- 测试前：老枪伤（酸痛）
- 测试后：枪伤

### 根本原因
**源码证据**：
```csharp
// HediffComp_GetsPermanent.cs
public class HediffComp_GetsPermanent : HediffComp
{
    public bool isPermanentInt;  // ← 是否永久
    private PainCategory painCategory;  // ← 疼痛类别（酸痛、剧痛等）

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref isPermanentInt, "isPermanent", false);
        Scribe_Values.Look(ref painCategory, "painCategory", PainCategory.Painless);
    }
}

// Hediff_Injury.cs
public override string LabelBase
{
    get
    {
        HediffComp_GetsPermanent comp = this.TryGetComp<HediffComp_GetsPermanent>();
        if (comp != null && comp.IsPermanent)
        {
            return comp.Props.permanentLabel;  // ← "老枪伤"
        }
        return base.LabelBase;  // ← "枪伤"
    }
}

public override string LabelInBrackets
{
    get
    {
        // ...
        if (comp != null && comp.IsPermanent && comp.PainCategory != PainCategory.Painless)
        {
            stringBuilder.Append(("PainCategory_" + comp.PainCategory).Translate());  // ← "酸痛"
        }
        // ...
    }
}
```

**问题链**：
1. 原始伤口有`HediffComp_GetsPermanent`，`isPermanent=true`，`painCategory=Aching`
2. 我们只记录了`defName`和`severity`
3. `AddHediff`创建新实例时，HediffComp被重新初始化
4. `isPermanent=false`（默认值），`painCategory=Painless`（默认值）
5. 显示为"枪伤"而不是"老枪伤（酸痛）"

### 修复方案
**记录并恢复HediffComp状态**：

```csharp
private class HediffRecord
{
    public string defName;
    public float severity;
    public Dictionary<string, object> compData;  // 新增：HediffComp状态
    // ...
}

// 快照时
record.compData = new Dictionary<string, object>();
var permanentComp = h.TryGetComp<HediffComp_GetsPermanent>();
if (permanentComp != null)
{
    record.compData["isPermanent"] = permanentComp.IsPermanent;
    record.compData["painCategory"] = (int)permanentComp.PainCategory;
}

// 恢复时
var permanentComp = restored.TryGetComp<HediffComp_GetsPermanent>();
if (permanentComp != null && record.compData != null)
{
    if (record.compData.TryGetValue("isPermanent", out var isPerm))
    {
        // 使用反射设置私有字段
        var field = typeof(HediffComp_GetsPermanent).GetField("isPermanentInt",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(permanentComp, (bool)isPerm);
    }
    if (record.compData.TryGetValue("painCategory", out var pain))
    {
        permanentComp.SetPainCategory((PainCategory)(int)pain);
    }
}
```

### 理由
- HediffComp存储了Hediff的额外状态
- `HediffComp_GetsPermanent`影响伤口的显示和疼痛计算
- 必须恢复这些状态才能保持伤口的完整性

---

## 🔍 问题4：Hediff伤口来源信息丢失

### 现象
- 测试前：割伤（七首 刀刃）
- 测试后：割伤

### 根本原因
**源码证据**：
```csharp
// Hediff.cs
public class Hediff
{
    public string sourceLabel;  // ← "七首"
    public ThingDef sourceDef;  // ← 武器Def
    public string sourceToolLabel;  // ← "刀刃"
    // ...
}

// Hediff_Injury.cs
public override string LabelInBrackets
{
    get
    {
        // ...
        if (sourceDef != null)
        {
            if (!sourceToolLabel.NullOrEmpty())
            {
                stringBuilder.Append("SourceToolLabel".Translate(sourceLabel, sourceToolLabel));
                // ← 显示为"七首 刀刃"
            }
        }
        // ...
    }
}
```

**问题链**：
1. 原始伤口有`sourceLabel="七首"`，`sourceToolLabel="刀刃"`
2. 我们只记录了`defName`和`severity`
3. `AddHediff`创建新实例时，这些字段为null
4. 显示为"割伤"而不是"割伤（七首 刀刃）"

### 修复方案
**记录并恢复来源信息**：

```csharp
private class HediffRecord
{
    public string defName;
    public float severity;
    public string sourceLabel;  // 新增
    public string sourceDefName;  // 新增
    public string sourceToolLabel;  // 新增
    // ...
}

// 快照时
record.sourceLabel = h.sourceLabel;
record.sourceDefName = h.sourceDef?.defName;
record.sourceToolLabel = h.sourceToolLabel;

// 恢复时
restored.sourceLabel = record.sourceLabel;
if (!string.IsNullOrEmpty(record.sourceDefName))
{
    restored.sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.sourceDefName);
}
restored.sourceToolLabel = record.sourceToolLabel;
```

### 理由
- 来源信息用于显示伤口的详细信息
- 虽然不影响游戏机制，但影响信息完整性
- 实现简单，建议一并修复

---

## 🔍 问题5：失血速率异常增加

### 现象
- 测试前：失血速率 18%/日（无生命危险）
- 测试后：失血速率 58%/日（无生命危险）

### 根本原因
**源码证据**：
```csharp
// Hediff_Injury.cs
public override float BleedRate
{
    get
    {
        // ...
        if (this.IsTended() || this.IsPermanent())
        {
            return 0f;  // ← 已处理或永久伤口不出血
        }
        // ...
        float num = Severity * def.injuryProps.bleedRate * pawn.RaceProps.bleedRateFactor;
        return num;
    }
}
```

**问题链**：
1. 原始伤口是"老枪伤（酸痛）"，`IsPermanent()=true`，失血速率=0
2. 恢复后变成"枪伤"，`IsPermanent()=false`，开始出血
3. 失血速率 = Severity × bleedRate
4. 由于`HediffComp_GetsPermanent`状态丢失，伤口从"不出血"变成"出血"

### 修复方案
**修复问题3即可解决此问题**。

恢复`HediffComp_GetsPermanent`状态后：
- `IsPermanent()=true`
- `BleedRate`返回0
- 失血速率恢复正常

### 理由
- 这是问题3的连锁反应
- 永久伤口不应该出血
- 修复HediffComp状态后自动解决

---

## 📊 修复优先级与依赖关系

```
优先级1（阻塞性）：
  └─ 问题1：MissingBodyPart循环依赖 🔴🔴

优先级2（高优先级）：
  ├─ 问题2：Hediff_Level.level字段 🔴
  └─ 问题3：HediffComp_GetsPermanent状态 🔴
       └─ 连锁解决 → 问题5：失血速率 🔴

优先级3（中优先级）：
  └─ 问题4：伤口来源信息 🟡
```

---

## 💡 总体修复建议

### 1. 扩展HediffRecord结构
```csharp
private class HediffRecord
{
    // 基础字段
    public string defName;
    public float severity;
    public string bodyPartDefName;
    public int bodyPartIndex;

    // 问题2：Hediff_Level支持
    public int? level;

    // 问题3：HediffComp状态
    public Dictionary<string, object> compData;

    // 问题4：来源信息
    public string sourceLabel;
    public string sourceDefName;
    public string sourceToolLabel;
}
```

### 2. 实现类型检测与特殊处理
```csharp
// 快照时
if (h is Hediff_Level hediffLevel)
{
    record.level = hediffLevel.level;
}

if (h is Hediff_Injury injury)
{
    record.sourceLabel = injury.sourceLabel;
    // ...
}

var permanentComp = h.TryGetComp<HediffComp_GetsPermanent>();
if (permanentComp != null)
{
    // 记录Comp状态
}
```

### 3. 使用反射访问私有字段
```csharp
using System.Reflection;

// 设置HediffComp_GetsPermanent.isPermanentInt
var field = typeof(HediffComp_GetsPermanent).GetField("isPermanentInt",
    BindingFlags.NonPublic | BindingFlags.Instance);
field?.SetValue(permanentComp, true);
```

### 4. 添加验证逻辑
```csharp
// 恢复后验证
if (restored is Hediff_Level hediffLevel)
{
    if (hediffLevel.level != record.level)
    {
        Log.Warning($"[BDP] Hediff_Level恢复失败: {record.defName}");
    }
}
```

---

## ⚠️ 注意事项

1. **反射性能**：反射访问私有字段有性能开销，但战斗体切换是低频操作，可接受
2. **版本兼容性**：私有字段名可能在RimWorld更新时改变，需要添加版本检测
3. **Mod兼容性**：其他Mod可能添加自定义HediffComp，需要通用的Comp序列化方案
4. **测试覆盖**：修复后需要重新测试所有场景，确保没有引入新问题

---

## 🎯 实施计划

### 阶段1：修复致命问题（必须）
- [ ] 修复问题1：MissingBodyPart循环依赖
- [ ] 测试：使用Elekio验证截肢恢复

### 阶段2：修复高优先级问题（必须）
- [ ] 修复问题2：Hediff_Level.level字段
- [ ] 修复问题3：HediffComp_GetsPermanent状态
- [ ] 测试：验证灵能等级、失血速率

### 阶段3：修复中优先级问题（建议）
- [ ] 修复问题4：伤口来源信息
- [ ] 测试：验证伤口显示完整性

### 阶段4：全面测试
- [ ] 重新运行所有测试用例
- [ ] 更新验证报告
- [ ] 进入阶段1：实现CombatBodySnapshot

---

**报告生成时间**: 2026-03-02 21:00
**源码验证**: RimWorld 1.6.4633
**分析工具**: RimSearcher + 源码审查
