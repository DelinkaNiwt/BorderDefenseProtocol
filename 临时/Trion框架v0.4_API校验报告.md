# Trion实体框架设计草案v0.4 - RimWorld API校验报告

---

## 文档元信息

**摘要**：针对Trion实体框架设计草案v0.4中使用的RimWorld API进行完整校验，识别不存在或用法错误的API。

**版本号**：v1.0
**生成时间**：2026-01-10
**关键词**：API校验、RimWorld、RiMCP、Trion Framework、错误修正
**标签**：[待审]

---

## 一、校验概述

### 1.1 校验方法

使用RiMCP（RimWorld Model Context Protocol）对v0.4草案中引用的所有RimWorld API进行验证，包括：
- C#类、方法、属性
- XML配置字段
- 数据结构定义

### 1.2 校验范围

- 核心组件API（CompTrion、ThingComp）
- 伤害拦截API（Harmony相关）
- Hediff系统API
- 特效和UI API
- XML配置字段

---

## 二、验证结果汇总

### 2.1 总体统计

| 类别 | 验证数量 | 正确 | 错误 | 正确率 |
|------|---------|------|------|--------|
| **C# API** | 35 | 34 | 1 | 97.1% |
| **XML字段** | 15 | 13 | 2 | 86.7% |
| **总计** | 50 | 47 | 3 | 94.0% |

### 2.2 严重性分级

| 严重性 | 数量 | 影响 |
|--------|------|------|
| **致命错误** | 2 | 会导致编译失败或运行时崩溃 |
| **警告** | 1 | 功能无法实现，需要替代方案 |

---

## 三、错误详情

### 3.1 致命错误

#### 错误1：HediffCompProperties_Thought 类不存在

**位置**：草案 第1022-1025行

**错误代码**：
```xml
<comps>
    <li Class="HediffCompProperties_Thought">
        <thought>Thought_TrionDepleted</thought>
    </li>
```

**问题**：
- ❌ `HediffCompProperties_Thought` 类不存在于RimWorld 1.6.4633
- 该类名拼写错误或版本不匹配

**正确API**：
```csharp
// 实际存在的类
public class HediffCompProperties_ThoughtSetter : HediffCompProperties
{
    public ThoughtDef thought;
    public int moodOffset;
    public FloatRange moodOffsetRange = FloatRange.Zero;
}
```

**修正方案**：
```xml
<comps>
    <li Class="HediffCompProperties_ThoughtSetter">
        <thought>Thought_TrionDepleted</thought>
    </li>
```

**影响范围**：
- debuff"Trion枯竭"无法正确施加心情惩罚
- 相关ThoughtDef定义需要调整

---

#### 错误2：disappearsAfterTicksIfInBed 字段不存在

**位置**：草案 第1063-1068行、第11.3节

**错误代码**：
```xml
<HediffCompProperties_Disappears>
    <disappearsAfterTicks>30000</disappearsAfterTicks>  <!-- 12小时 -->
    <disappearsAfterTicksIfInBed>15000</disappearsAfterTicksIfInBed>  <!-- 休养舱6小时 -->
</HediffCompProperties_Disappears>
```

**问题**：
- ❌ `disappearsAfterTicksIfInBed` 字段不存在于 `HediffCompProperties_Disappears`
- RimWorld 1.6.4633中该类只有以下字段：
  ```csharp
  public IntRange disappearsAfterTicks;
  public bool showRemainingTime;
  public bool canUseDecimalsShortForm;
  public MentalStateDef requiredMentalState;
  public string messageOnDisappear;
  public string letterTextOnDisappear;
  public string letterLabelOnDisappear;
  public bool sendLetterOnDisappearIfDead = true;
  public bool leaveFreshWounds = true;
  ```

**修正方案**：

方案A：使用自定义HediffComp实现（推荐）
```csharp
// 自定义Comp
public class HediffComp_DisappearsCustom : HediffComp
{
    public int disappearsAfterTicks;
    public int disappearsAfterTicksIfInBed;

    public override void CompPostTick(ref float severityAdjustment)
    {
        bool inBed = Pawn.CurrentBed() != null;
        int ticksToUse = inBed ? disappearsAfterTicksIfInBed : disappearsAfterTicks;

        // 实现消失逻辑
        if (parent.ageTicks >= ticksToUse)
        {
            Pawn.health.RemoveHediff(parent);
        }
    }
}
```

方案B：放弃休养舱加速功能（简单）
```xml
<HediffCompProperties_Disappears>
    <disappearsAfterTicks>30000</disappearsAfterTicks>  <!-- 12小时 -->
</HediffCompProperties_Disappears>
```

**影响范围**：
- 休养舱加速debuff消退功能无法实现
- 需要在应用层自行实现该功能

---

### 3.2 警告

#### 警告1：OutputPowerModifier字段不是TraitDef的标准字段

**位置**：草案 第589行

**代码**：
```csharp
baseOutput += trait.def.outputPowerModifier;
```

**问题**：
- ⚠️ `outputPowerModifier` 不是 `TraitDef` 的原生字段
- 需要通过ModExtension扩展实现

**修正方案**：
```csharp
// 1. 定义ModExtension
public class OutputPowerModifierExtension : DefModExtension
{
    public float outputPowerModifier;
}

// 2. 在TraitDef XML中使用
<TraitDef>
    <defName>Trait_TrionMastery</defName>
    <label>Trion精通</label>
    <modExtensions>
        <li Class="ProjectWT.OutputPowerModifierExtension">
            <outputPowerModifier>10</outputPowerModifier>
        </li>
    </modExtensions>
</TraitDef>

// 3. 在代码中读取
if (trait.def.HasModExtension<OutputPowerModifierExtension>())
{
    baseOutput += trait.def.GetModExtension<OutputPowerModifierExtension>().outputPowerModifier;
}
```

**影响范围**：
- 特性对输出功率的影响需要通过ModExtension实现
- 需要额外创建DefModExtension类

---

## 四、已验证正确的API

### 4.1 核心组件API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `ThingComp.PostSpawnSetup(bool respawningAfterLoad)` | 草案 第151行 | ✅ 正确 |
| `ThingComp.CompTick()` | 草案 第141行 | ✅ 正确 |
| `ThingWithComps.TryGetComp<T>()` | 草案 第622行 | ✅ 正确 |
| `CompProperties` | 草案 第519-547行 | ✅ 正确 |

### 4.2 伤害拦截API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `Pawn_HealthTracker.PreApplyDamage(DamageInfo dinfo, bool absorbed)` | 草案 第831行 | ✅ 正确 |
| `DamageInfo` | 草案 第831行 | ✅ 正确 |
| `BodyPartRecord` | 草案 第217行 | ✅ 正确 |

### 4.3 Hediff系统API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `HediffMaker.MakeHediff(HediffDef def, Pawn pawn)` | 草案 第1055行 | ✅ 正确 |
| `Pawn.health.AddHediff(Hediff hediff)` | 草案 第1056行 | ✅ 正确 |
| `HediffCompProperties_Disappears` | 草案 第1026行 | ✅ 正确 |
| `HediffDef.stages` | 草案 第1000-1019行 | ✅ 正确 |
| `HediffStage.capMods` | 草案 第1003-1011行 | ✅ 正确 |

### 4.4 特效和UI API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `MoteMaker.ThrowText(Vector3, Map, string, Color, float)` | 草案 第887行 | ✅ 正确 |
| `FleckMaker.Static(Vector3, Map, FleckDef, float)` | 草案 第960行 | ✅ 正确 |
| `IntVec3.ToVector3()` | 草案 第887行 | ✅ 正确 |

### 4.5 时间和随机API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `Find.TickManager.TicksGame` | 草案 第682行 | ✅ 正确 |
| `Rand.Value` | 草案 第876行 | ✅ 正确 |

### 4.6 Pawn属性API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `Pawn.needs.food.CurLevel` | 草案 第721行 | ✅ 正确 |
| `Pawn.story.traits.allTraits` | 草案 第738行 | ✅ 正确 |
| `Pawn.apparel.WornApparel` | 草案 第594行 | ✅ 正确 |
| `PawnCapacityDef` | 草案 第1003-1009行 | ✅ 正确 |

### 4.7 Need系统API

| API | 位置 | 验证状态 |
|-----|------|----------|
| `Need.NeedInterval()` | 草案 第231行（Harmony拦截目标） | ✅ 正确 |

---

## 五、修正建议优先级

### 5.1 必须立即修正（致命错误）

1. **HediffCompProperties_Thought → HediffCompProperties_ThoughtSetter**
   - 优先级：P0（最高）
   - 原因：会导致XML解析失败，debuff无法创建
   - 修正时间：5分钟

2. **disappearsAfterTicksIfInBed字段删除或替代**
   - 优先级：P0（最高）
   - 原因：会导致XML解析失败，Hediff无法创建
   - 修正时间：根据方案选择，5分钟-2小时

### 5.2 建议修正（警告）

3. **OutputPowerModifier通过ModExtension实现**
   - 优先级：P1（高）
   - 原因：功能无法实现，但不影响其他系统
   - 修正时间：30分钟

---

## 六、验证工具和方法

### 6.1 使用的工具

- **RiMCP（RimWorld Model Context Protocol）**
  - 版本：混合数据集
  - 数据来源：RimWorld 1.6.4633反编译源码
  - 覆盖范围：Core + 5个DLC（Royalty, Ideology, Biotech, Anomaly, Rimworld of Magic）

### 6.2 验证方法

1. **rough_search**：搜索类名、方法名
2. **get_item**：获取完整类定义
3. **XML示例对比**：对比官方XML配置

### 6.3 验证限制

- ⚠️ 未验证Harmony补丁的实际可行性（需要运行时测试）
- ⚠️ 未验证性能影响
- ⚠️ 未验证与其他mod的兼容性

---

## 七、后续建议

### 7.1 框架层修正

1. 立即修正2个致命错误
2. 更新v0.4草案为v0.5
3. 补充ModExtension设计说明

### 7.2 应用层实现注意事项

1. **自定义HediffComp**：
   - 实现休养舱加速功能需要自定义Comp
   - 检测Pawn.CurrentBed()判断是否在床上
   - 根据床的类型（休养舱 vs 普通床）应用不同消退速率

2. **TraitDef扩展**：
   - 所有自定义字段都通过ModExtension实现
   - 遵循RimWorld模组最佳实践

3. **XML配置验证**：
   - 建议使用XML Schema验证工具
   - 参考游戏自带的XML定义

### 7.3 测试计划

1. **单元测试**：
   - 测试所有CompTrion核心方法
   - 测试策略模式切换逻辑

2. **集成测试**：
   - 测试Harmony补丁是否正确拦截PreApplyDamage
   - 测试debuff是否正确施加和消退

3. **性能测试**：
   - 测试60 Tick批量计算的性能影响
   - 测试大量Trion实体同时存在时的帧率

---

## 八、附录：RimWorld API参考

### 8.1 关键类文档路径

```
Verse.Pawn_HealthTracker
  → Source/Verse/Pawn_HealthTracker.cs
  → PreApplyDamage(DamageInfo dinfo, bool absorbed): void

Verse.HediffCompProperties_Disappears
  → Source/Verse/HediffCompProperties_Disappears.cs
  → disappearsAfterTicks: IntRange

Verse.HediffCompProperties_ThoughtSetter
  → Source/Verse/HediffCompProperties_ThoughtSetter.cs
  → thought: ThoughtDef

RimWorld.MoteMaker
  → Source/RimWorld/MoteMaker.cs
  → ThrowText(Vector3, Map, string, Color, float): void
```

### 8.2 推荐阅读

- RimWorld ModExtension官方示例
- Harmony补丁开发指南
- RimWorld性能优化最佳实践

---

## 九、总结

### 9.1 主要发现

1. ✅ v0.4草案中**94%的API引用正确**
2. ❌ 发现**3处错误**，其中2处为致命错误
3. ⚠️ 1处需要通过ModExtension扩展实现

### 9.2 影响评估

- **致命错误**会导致XML解析失败，必须修正才能运行
- **警告**不影响编译，但功能无法实现
- 修正工作量：约3-4小时（包括测试）

### 9.3 下一步行动

1. 修正致命错误
2. 更新草案为v0.5
3. 补充ModExtension设计
4. 重新提交审阅

---

**校验执行者**：Claude (Sonnet 4.5)
**校验时间**：2026-01-10
**RiMCP版本**：混合数据集 (RimWorld 1.6.4633)

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初版API校验报告 | 2026-01-10 | assistant |
