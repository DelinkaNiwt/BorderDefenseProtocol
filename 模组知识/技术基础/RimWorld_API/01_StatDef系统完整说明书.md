# RimWorld StatDef 系统完整说明书

**摘要**：详细阐述 RimWorld 中 StatDef 系统的完整架构、核心属性、计算机制和代码调用方式。涵盖 StatCategoryDef 分类、ToStringStyle 显示格式、StatPart 计算部件、StatRequest 请求对象、StatWorker 计算逻辑、缓存机制以及与其他系统（Gene、Hediff、Apparel、Trait）的交互方式。提供完整的 XML 定义示例和 C# 代码示例。

**版本**：v1.0
**修改时间**：2026-01-19
**关键词**：StatDef、StatCategoryDef、StatWorker、StatPart、StatRequest、ToStringStyle、属性系统、RimWorld模组开发
**标签**：[定稿]、技术基础、RimWorld_API

---

## 目录

1. [概述](#1-概述)
2. [StatDef 核心属性](#2-statdef-核心属性)
3. [StatCategoryDef 分类系统](#3-statcategorydef-分类系统)
4. [ToStringStyle 显示格式](#4-tostringstyle-显示格式)
5. [ToStringNumberSense 数值含义](#5-tostringnumbersense-数值含义)
6. [StatPart 计算部件](#6-statpart-计算部件)
7. [StatRequest 请求对象](#7-statrequest-请求对象)
8. [StatWorker 计算逻辑](#8-statworker-计算逻辑)
9. [缓存机制](#9-缓存机制)
10. [代码调用方式](#10-代码调用方式)
11. [完整示例](#11-完整示例)
12. [与其他系统的交互](#12-与其他系统的交互)
13. [调试技巧](#13-调试技巧)
14. [常见问题](#14-常见问题)
15. [验证状态](#15-验证状态)

---

## 1. 概述

**StatDef** 是 RimWorld 中定义"属性/数值"的 XML + C# 系统，代表游戏中所有可计算的数值属性。

### 核心设计原则

| 原则 | 说明 |
|------|------|
| **声明式定义** | 在 XML 中声明属性元数据 |
| **计算逻辑分离** | 计算由 `StatWorker` 类处理 |
| **模块化计算** | 通过 `StatPart` 组合多种修正 |
| **自动缓存** | 智能缓存计算结果，优化性能 |
| **精细显示控制** | 控制属性在何时、何处显示 |

---

## 2. StatDef 核心属性

### 2.1 标识与描述

```xml
<StatDef>
    <defName>TrionMaxCap</defName>              <!-- 唯一标识符 (必需) -->
    <label>Trion总量上限</label>                 <!-- UI显示名称 -->
    <description>角色的最大Trion能量上限。</description>  <!-- 悬停说明 -->
    <labelForFullStatList/>                     <!-- 完整列表中的标签 -->
    <offsetLabel/>                              <!-- 偏移标签 -->
</StatDef>
```

### 2.2 分类与优先级

```xml
<category>PawnHealth</category>                 <!-- 所属分类 (必需) -->
<displayPriorityInCategory>100</displayPriorityInCategory>  <!-- 同分类内排序 -->
```

### 2.3 数值配置

```xml
<defaultBaseValue>100</defaultBaseValue>        <!-- 基础默认值 -->
<minValue>0</minValue>                          <!-- 最小值 -->
<maxValue>999999</maxValue>                     <!-- 最大值 -->
<valueIfMissing>0</valueIfMissing>              <!-- 数据缺失时的值 -->
```

### 2.4 显示控制（重点）

```xml
<!-- 基础显示控制 -->
<showOnPawns>true</showOnPawns>                 <!-- 角色面板显示 -->
<showOnAnimals>false</showOnAnimals>            <!-- 动物面板显示 -->
<showOnHumanlikes>true</showOnHumanlikes>      <!-- 类人生物 -->
<showOnMechanoids>true</showOnMechanoids>      <!-- 机械族 -->
<showOnEntities>true</showOnEntities>          <!-- 实体 -->
<showOnDrones>true</showOnDrones>              <!-- 无人机 -->
<showOnSlavesOnly>false</showOnSlavesOnly>     <!-- 仅奴隶显示 -->

<!-- 进阶显示控制 -->
<showNonAbstract>true</showNonAbstract>         <!-- 显示非抽象实例 -->
<showIfUndefined>true</showIfUndefined>        <!-- 无修改源时显示 -->
<showZeroBaseValue>false</showZeroBaseValue>   <!-- 基础值为0时显示 -->
<showOnDefaultValue>true</showOnDefaultValue>  <!-- 显示默认值 -->

<!-- 条件显示 -->
<showIfModsLoaded>                             <!-- 指定Mod加载时显示 -->
    <li>Ludeon.RimWorld.Biotech</li>
</showIfModsLoaded>
<showIfModsLoadedAny>                          <!-- 任一Mod加载时显示 -->
    <li>Ludeon.RimWorld.Royalty</li>
    <li>Ludeon.RimWorld.Biotech</li>
</showIfModsLoadedAny>
<showIfHediffsPresent>                         <!-- 有特定Hediff时显示 -->
    <li>HediffDefOf</li>
</showIfHediffsPresent>
<showOnPawnKind>                               <!-- 仅特定PawnKind显示 -->
    <li>PawnKindDefOf</li>
</showOnPawnKind>
<showDevelopmentalStageFilter>Baby|Child|Adult</showDevelopmentalStageFilter>  <!-- 发育阶段筛选 -->

<!-- 隐藏控制 -->
<alwaysHide>true</alwaysHide>                   <!-- ★ 完全隐藏 (推荐内部属性用) -->
<forInformationOnly>false</forInformationOnly>  <!-- 仅信息显示 -->
<hideAtValue>-2147483648</hideAtValue>          <!-- 当值等于此值时隐藏 -->
<hideInClassicMode>false</hideInClassicMode>    <!-- 经典模式隐藏 -->
```

### 2.5 格式化配置

```xml
<toStringStyle>FloatOne</toStringStyle>         <!-- 显示风格 -->
<formatString>{0}点</formatString>              <!-- 自定义格式 -->
<toStringNumberSense>Absolute</toStringNumberSense>  <!-- 数值含义 -->
```

### 2.6 数值修正配置

```xml
<!-- 技能修正 (Offset) -->
<skillNeedOffsets>
    <li Class="SkillNeed_SkillLevel">
        <skill>Shooting</skill>
    </li>
</skillNeedOffsets>
<noSkillOffset>0</noSkillOffset>               <!-- 无技能时的默认值 -->

<!-- 容量修正 (Offset) -->
<capacityOffsets>
    <li>
        <capacity>Consciousness</capacity>
        <offset>0.1</offset>
    </li>
</capacityOffsets>

<!-- 技能修正 (Factor) -->
<skillNeedFactors>
    <li Class="SkillNeed_SkillLevel">
        <skill>Shooting</skill>
    </li>
</skillNeedFactors>
<noSkillFactor>1</noSkillFactor>               <!-- 无技能时的倍率 -->

<!-- 容量修正 (Factor) -->
<capacityFactors>
    <li>
        <capacity>Moving</capacity>
        <weight>0.5</weight>
    </li>
</capacityFactors>

<!-- 直接修正 -->
<statFactors>                                  <!-- 乘数因子列表 -->
    <li>OtherStatDef</li>
</statFactors>
<applyFactorsIfNegative>true</applyFactorsIfNegative>  <!-- 负值也应用因子 -->

<!-- 后处理曲线 -->
<postProcessCurve>                             <!-- 后处理修正曲线 -->
    <Curve>
        <point>0,0</point>
        <point>1,1</point>
    </Curve>
</postProcessCurve>
<postProcessStatFactors>                       <!-- 后处理因子 -->
    <li>SomeStatDef</li>
</postProcessStatFactors>
```

### 2.7 特殊控制

```xml
<workerClass>typeof(StatWorker)</workerClass>  <!-- 自定义计算类 -->
<toStringStyleUnfinalized/>                   <!-- 未最终值的显示风格 -->
<finalizeEquippedStatOffset>true</finalizeEquippedStatOffset>  <!-- 装备偏移最终化 -->
<statFactorsExplanationHeader/>               <!-- 因子说明标题 -->
<roundValue>false</roundValue>                 <!-- 是否取整 -->
<roundToFiveOver>3.4028235E+38</roundToFiveOver>  <!-- 5的倍数取整 -->
<cacheable>false</cacheable>                   <!-- 是否可缓存 -->
<immutable>false</immutable>                   <!-- 是否不可变 -->
<minifiedThingInherits>true</minifiedThingInherits>  <!-- 简化物品继承 -->
<supressDisabledError>false</supressDisabledError>  <!-- 抑制禁用错误 -->
<neverDisabled>false</neverDisabled>           <!-- 从不禁用 -->
<scenarioRandomizable>false</scenarioRandomizable>  <!-- 场景随机化 -->
<disableIfSkillDisabled/>                      <!-- 技能禁用时禁用 -->
<overridesHideStats>false</overridesHideStats>  <!-- 覆盖隐藏设置 -->
```

---

## 3. StatCategoryDef 分类系统

### 3.1 分类定义

```xml
<StatCategoryDef>
    <defName>PawnHealth</defName>
    <label>Health</label>
    <displayOrder>17</displayOrder>    <!-- 数字越小排越前 -->
</StatCategoryDef>
```

### 3.2 常用分类列表

| 分类 | 显示名称 | 用途 |
|------|----------|------|
| `PawnHealth` | Health | 角色生命相关 |
| `PawnCombat` | Combat | 战斗相关 |
| `PawnGeneral` | General | 通用属性 |
| `PawnWork` | Work | 工作相关 |
| `PawnSocial` | Social | 社交相关 |
| `PawnMental` | Mental | 心理相关 |
| `PawnMoving` | Moving | 移动相关 |
| `PawnSensing` | Sensing | 感知相关 |
| `PawnFood` | Food | 食物相关 |
| `PawnMisc` | Misc | 杂项 |
| `PawnHeal` | Healing | 治疗相关 |
| `Basics` | Basics | 基础属性 |
| `BasicsNonPawn` | Basics (Non-Pawn) | 非角色基础 |
| `Building` | Building | 建筑属性 |
| `EquippedStatOffsets` | Offsets when equipped | 装备时的偏移 |

### 3.3 分类文件位置

```
Core/Defs/Stats/StatCategories.xml
```

---

## 4. ToStringStyle 显示格式

### 4.1 可用格式

| 格式 | 说明 | 示例 |
|------|------|------|
| `Integer` | 整数 | `100` |
| `FloatOne` | 1位小数 | `100.5` |
| `FloatTwo` | 2位小数 | `100.55` |
| `FloatThree` | 3位小数 | `100.555` |
| `PercentZero` | 百分比(0位小数) | `100%` |
| `PercentOne` | 百分比(1位小数) | `100.5%` |
| `PercentTwo` | 百分比(2位小数) | `100.55%` |
| `SignedFloatOne` | 带符号1位小数 | `+100.5` |
| `SignedPercentOne` | 带符号百分比 | `+100.5%` |
| `Temperature` | 温度 | `21°C` |
| `Talents` | 天赋点 | `5` |
| `Rating` | 评级 | `5` |
| `Quality` | 品质 | `Excellent` |

### 4.2 使用示例

```xml
<!-- 能量类：带单位 -->
<StatDef>
    <defName>TrionMaxCap</defName>
    <toStringStyle>FloatOne</toStringStyle>
    <formatString>{0}点</formatString>
</StatDef>

<!-- 百分比类 -->
<StatDef>
    <defName>EfficiencyMultiplier</defName>
    <toStringStyle>PercentOne</toStringStyle>
</StatDef>

<!-- 纯粹的数值 -->
<StatDef>
    <defName>Level</defName>
    <toStringStyle>Integer</toStringStyle>
</StatDef>
```

---

## 5. ToStringNumberSense 数值含义

### 5.1 可用选项

| 选项 | 说明 | 用途 |
|------|------|------|
| `Absolute` | 绝对值 | 普通数值（如最大生命值） |
| `Factor` | 乘数因子 | 倍率（如伤害加成 1.5x） |
| `Offset` | 偏移量 | 增量（如 +10 点） |

### 5.2 何时使用

- **Factor**: 当你的属性是"倍率"时使用（如伤害倍率 1.5 表示 150%）
- **Offset**: 当你的属性是"增量"时使用（如 +10 点）
- **Absolute**: 普通数值（默认值）

```xml
<!-- 伤害倍率 -->
<StatDef>
    <defName>MeleeDamageFactor</defName>
    <toStringNumberSense>Factor</toStringNumberSense>
    <defaultBaseValue>1</defaultBaseValue>
</StatDef>

<!-- 数值偏移 -->
<StatDef>
    <defName>PsychicEntropyMaxOffset</defName>
    <toStringNumberSense>Offset</toStringNumberSense>
    <defaultBaseValue>0</defaultBaseValue>
</StatDef>

<!-- 普通数值 -->
<StatDef>
    <defName>TrionMaxCap</defName>
    <toStringNumberSense>Absolute</toStringNumberSense>
    <defaultBaseValue>500</defaultBaseValue>
</StatDef>
```

---

## 6. StatPart 计算部件

### 6.1 什么是 StatPart

`StatPart` 是 StatDef 计算链中的"修正模块"，负责在基础值之上应用各种修正。

### 6.2 常用 StatPart 类型

| 类型 | 作用 |
|------|------|
| `StatPart_GearStatFactor` | 装备提供的乘数因子 |
| `StatPart_GearStatOffset` | 装备提供的偏移量 |
| `StatPart_BedStat` | 床铺修正 |
| `StatPart_SkillNeed` | 技能需求修正 |
| `StatPart_AgeOffset` | 年龄修正 |
| `StatPart_Genes` | 基因修正 |
| `StatPart_Resting` | 休息状态修正 |
| `StatPart_Quality_Offset` | 品质偏移 |
| `StatPart_Quality_Factor` | 品质因子 |
| `StatPart_EnvironmentalEffects` | 环境效应 |
| `StatPart_WorkTableOutdoors` | 户外工作台 |
| `StatPart_Terror` | 恐怖效果 |
| `StatPart_FertilityByGenderAge` | 生育率修正 |

### 6.3 使用示例

```xml
<StatDef>
    <defName>SomeStat</defName>
    ...
    <parts>
        <!-- 技能等级修正 -->
        <li Class="StatPart_SkillNeed">
            <skill>Shooting</skill>
            <weight>1</weight>          <!-- 权重 -->
            <maxLevel>20</maxLevel>     <!-- 最大等级 -->
        </li>
        
        <!-- 年龄修正 -->
        <li Class="StatPart_AgeOffset">
            <startAge>0</startAge>
            <endAge>13</endAge>
            <offset>0.5</offset>
        </li>
        
        <!-- 装备因子修正 -->
        <li Class="StatPart_GearStatFactor">
            <!-- 无参数，自动应用所有装备的 statFactors -->
        </li>
        
        <!-- 装备偏移修正 -->
        <li Class="StatPart_GearStatOffset">
            <!-- 无参数，自动应用所有装备的 statOffsets -->
        </li>
        
        <!-- 质量修正 -->
        <li Class="StatPart_Quality_Factor">
            <factor> <!-- 不同品质对应的因子 -->
                <li> <!-- Legendary -->
                    <quality>Legendary</quality>
                    <factor>1.3</factor>
                </li>
            </factor>
        </li>
    </parts>
</StatDef>
```

### 6.4 自定义 StatPart

```csharp
using RimWorld;

public class StatPart_TrionBonus : StatPart
{
    // 应用于修正值
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.HasThing && req.Thing is Pawn pawn)
        {
            // 根据某些条件修改 val
            val += pawn.GetStatValue(StatDefOf.TrionMaxCap) * 0.1f;
        }
    }
    
    // 提供说明文本
    public override string ExplanationPart(StatRequest req)
    {
        return "Trion Bonus: +10%";
    }
}
```

```xml
<StatDef>
    <defName>SomeStat</defName>
    ...
    <parts>
        <li Class="YourNamespace.StatPart_TrionBonus"/>
    </parts>
</StatDef>
```

---

## 7. StatRequest 请求对象

### 7.1 什么是 StatRequest

`StatRequest` 是 StatWorker 的输入参数，封装了"为谁计算属性"的所有上下文信息。

### 7.2 创建方式

```csharp
// 方式1: 为 Thing 计算（最常用）
StatRequest request = StatRequest.For(pawn);

// 方式2: 为 Thing + Pawn 组合计算
StatRequest request = StatRequest.For(weapon, pawn);

// 方式3: 为 BuildableDef 计算（抽象值）
StatRequest request = StatRequest.For(thingDef, stuffDef, quality);

// 方式4: 为 AbilityDef 计算
StatRequest request = StatRequest.For(abilityDef, pawn);

// 方式5: 空请求
StatRequest request = StatRequest.ForEmpty();
```

### 7.3 属性访问

```csharp
public struct StatRequest
{
    public Thing Thing { get; }          // 具体物品
    public Def Def { get; }              // 定义对象
    public BuildableDef BuildableDef { get; }  // 可建造对象
    public AbilityDef AbilityDef { get; }  // 能力对象
    public Pawn Pawn { get; }            // 关联的 Pawn
    public ThingDef StuffDef { get; }    // 材料定义
    public QualityCategory QualityCategory { get; }  // 品质
    public Faction Faction { get; }      // 派系
    public bool HasThing { get; }        // 是否有具体物品
    public bool Empty { get; }           // 是否为空
    public bool ForAbility { get; }      // 是否为能力
    public List<StatModifier> StatBases { get; }  // 基础修正列表
}
```

### 7.4 使用示例

```csharp
public float GetStatValueExample(Pawn pawn, StatDef stat)
{
    // 创建请求
    StatRequest req = StatRequest.For(pawn);
    
    // 获取值
    float value = stat.worker.GetValue(req);
    
    return value;
}

public string GetStatExplanationExample(Thing thing)
{
    StatRequest req = StatRequest.For(thing);
    return StatUtility.GetOffsetsAndFactorsFor(StatDefOf.MeleeDamageFactor, thing);
}
```

---

## 8. StatWorker 计算逻辑

### 8.1 默认计算流程

```
GetValue()
    ↓
GetValueUnfinalized()
    ├── GetBaseValueFor()          基础值
    ├── skillNeedOffsets           技能Offset修正
    ├── capacityOffsets            容量Offset修正
    ├── traits Offset              特质Offset修正
    ├── hediffs Offset             HediffOffset修正
    ├── precepts Offset            信条Offset修正
    ├── genes Offset               基因Offset修正
    ├── gear Offset                装备Offset修正
    ├── traits Factor              特质Factor修正
    ├── hediffs Factor             HediffFactor修正
    ├── precepts Factor            信条Factor修正
    ├── genes Factor               基因Factor修正
    └── stuff Factors/Offsets      材料修正
    ↓
FinalizeValue()
    ├── ApplyPostProcessCurve      后处理曲线
    ├── Clamp(minValue, maxValue)  范围限制
    └── RoundValue                 取整
```

### 8.2 核心方法

```csharp
public class StatWorker
{
    // 获取最终值（最常用）
    public float GetValue(Thing thing, bool applyPostProcess = true);
    public float GetValue(StatRequest req, bool applyPostProcess = true);
    
    // 获取抽象值（不依赖具体实体）
    public float GetValueAbstract(BuildableDef def, ThingDef stuffDef = null);
    public float GetValueAbstract(AbilityDef def, Pawn forPawn = null);
    
    // 获取未最终值（不含后处理）
    public virtual float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true);
    
    // 值转字符串
    public virtual string ValueToString(float val, bool finalized, ToStringNumberSense numberSense);
    
    // 生成显示标签
    public virtual string GetStatDrawEntryLabel(StatDef stat, float value, ...);
    
    // 生成说明文本
    public virtual string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense);
    public virtual string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal);
    
    // 判断是否应该显示
    public virtual bool ShouldShowFor(StatRequest req);
}
```

### 8.3 自定义 StatWorker

```csharp
using RimWorld;

public class StatWorker_TrionCapacity : StatWorker
{
    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        float baseValue = base.GetValueUnfinalized(req, applyPostProcess);
        
        if (req.HasThing && req.Thing is Pawn pawn)
        {
            // 添加自定义逻辑
            var gene = pawn.genes?.GetGene(GeneDefOf.TrionCore);
            if (gene != null)
            {
                baseValue *= gene.def.statFactors.GetStatFactorFromList(stat);
            }
        }
        
        return baseValue;
    }
}
```

```xml
<StatDef>
    <defName>TrionMaxCap</defName>
    <workerClass>YourNamespace.StatWorker_TrionCapacity</workerClass>
</StatDef>
```

---

## 9. 缓存机制

### 9.1 缓存类型

```csharp
public class StatWorker
{
    // 临时缓存（默认）
    private Dictionary<Thing, StatCacheEntry> temporaryStatCache;
    
    // 不可变缓存
    private ConcurrentDictionary<Thing, float> immutableStatCache;
}
```

### 9.2 缓存策略

| 缓存类型 | 使用场景 | 生命周期 |
|----------|----------|----------|
| **无缓存** | 每次值都不同 | 每次重新计算 |
| **临时缓存** | 动态变化的属性 | 过期时间可配置 |
| **不可变缓存** | 静态属性 | 游戏内永久 |

### 9.3 配置缓存

```xml
<StatDef>
    <defName>TrionMaxCap</defName>
    <cacheable>true</cacheable>    <!-- 启用临时缓存 -->
    <immutable>false</immutable>   <!-- 是否不可变 -->
</StatDef>
```

### 9.4 手动控制缓存

```csharp
// 在自定义 StatWorker 中
public class StatWorker_TrionCapacity : StatWorker
{
    public override float GetValue(StatRequest req, bool applyPostProcess = true)
    {
        // 强制不使用缓存
        return GetValue(req, applyPostProcess, cacheStaleAfterTicks: -1);
    }
}
```

---

## 10. 代码调用方式

### 10.1 静态引用（推荐）

```csharp
public static class StatDefOf
{
    // 静态定义常用 StatDef
    public static readonly StatDef TrionMaxCap = DefDatabase<StatDef>.GetNamed("TrionMaxCap");
    public static readonly StatDef TrionOutput = DefDatabase<StatDef>.GetNamed("TrionOutput");
    public static readonly StatDef TrionAvailable = DefDatabase<StatDef>.GetNamed("TrionAvailable");
}
```

### 10.2 获取属性值

```csharp
// 方式1: 通过 Thing 获取
float value = pawn.GetStatValue(StatDefOf.MeleeDamageFactor);

// 方式2: 通过 Thing 和 StatDef
float value = pawn.GetStatValue(StatDefOf.TrionMaxCap);

// 方式3: 直接访问 StatWorker
float value = StatDefOf.TrionMaxCap.worker.GetValue(StatRequest.For(pawn));

// 方式4: 抽象值（无具体实体）
float value = StatDefOf.MeleeDamageFactor.worker.GetValueAbstract(thingDef, stuffDef);
```

### 10.3 检查属性是否启用

```csharp
// 检查属性是否应该显示
if (StatDefOf.TrionMaxCap.Worker.ShouldShowFor(StatRequest.For(pawn)))
{
    // 显示属性
}

// 检查属性是否被禁用
bool disabled = pawn.GetStatValue(StatDefOf.TrionMaxCap) == StatDefOf.TrionMaxCap.valueIfMissing;
```

---

## 11. 完整示例

### 11.1 完整的 StatDef XML 定义

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Defs>
    <!-- ==================== 分类定义 ==================== -->
    <StatCategoryDef>
        <defName>RimTrion</defName>
        <label>Trion</label>
        <displayOrder>100</displayOrder>
    </StatCategoryDef>
    
    <!-- ==================== 隐藏属性（内部使用） ==================== -->
    <StatDef>
        <defName>TrionMaxCap</defName>
        <label>Trion总量上限</label>
        <description>角色的最大Trion能量上限，决定变身战斗体的持续时间。</description>
        <category>RimTrion</category>
        <alwaysHide>true</alwaysHide>
        <defaultBaseValue>500</defaultBaseValue>
        <minValue>0</minValue>
        <maxValue>10000</maxValue>
        <toStringStyle>FloatOne</toStringStyle>
        <toStringNumberSense>Absolute</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <displayPriorityInCategory>1</displayPriorityInCategory>
    </StatDef>
    
    <StatDef>
        <defName>TrionOutput</defName>
        <label>Trion输出功率</label>
        <description>决定能够驱动多大功率的触发器。输出不足时无法使用某些能力。</description>
        <category>RimTrion</category>
        <alwaysHide>true</alwaysHide>
        <defaultBaseValue>10</defaultBaseValue>
        <minValue>1</minValue>
        <maxValue>100</maxValue>
        <toStringStyle>FloatOne</toStringStyle>
        <toStringNumberSense>Absolute</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <displayPriorityInCategory>2</displayPriorityInCategory>
    </StatDef>
    
    <StatDef>
        <defName>TrionRecoveryRate</defName>
        <label>Trion恢复速度</label>
        <description>每分钟恢复的Trion能量。</description>
        <category>RimTrion</category>
        <alwaysHide>true</alwaysHide>
        <defaultBaseValue>5</defaultBaseValue>
        <minValue>0</minValue>
        <maxValue>100</maxValue>
        <toStringStyle>FloatOne</toStringStyle>
        <toStringNumberSense>Absolute</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <displayPriorityInCategory>3</displayPriorityInCategory>
    </StatDef>
    
    <!-- ==================== 显示属性（供玩家查看） ==================== -->
    <StatDef>
        <defName>TrionAvailable</defName>
        <label>可用Trion</label>
        <description>当前可用于变身战斗体的能量。</description>
        <category>RimTrion</category>
        <defaultBaseValue>0</defaultBaseValue>
        <minValue>0</minValue>
        <toStringStyle>FloatOne</toStringStyle>
        <toStringNumberSense>Absolute</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <showOnAnimals>false</showOnAnimals>
        <displayPriorityInCategory>10</displayPriorityInCategory>
        <formatString>{0}点</formatString>
    </StatDef>
    
    <StatDef>
        <defName>TrionUsageRate</defName>
        <label>当前消耗率</label>
        <description>变身状态下每秒消耗的Trion能量。</description>
        <category>RimTrion</category>
        <defaultBaseValue>0</defaultBaseValue>
        <minValue>0</minValue>
        <toStringStyle>FloatOne</toStringStyle>
        <toStringNumberSense>Absolute</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <showOnAnimals>false</showOnAnimals>
        <displayPriorityInCategory>11</displayPriorityInCategory>
        <formatString>{0}/秒</formatString>
    </StatDef>
    
    <!-- ==================== 百分比倍率属性 ==================== -->
    <StatDef>
        <defName>TrionEfficiency</defName>
        <label>Trion效率</label>
        <description>Trion使用效率加成，影响所有Trion消耗。</description>
        <category>RimTrion</category>
        <defaultBaseValue>1</defaultBaseValue>
        <minValue>0.5</minValue>
        <maxValue>2</maxValue>
        <toStringStyle>PercentOne</toStringStyle>
        <toStringNumberSense>Factor</toStringNumberSense>
        <showOnPawns>true</showOnPawns>
        <displayPriorityInCategory>20</displayPriorityInCategory>
    </StatDef>
</Defs>
```

### 11.2 完整的 StatWorker 示例

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    /// <summary>
    /// Trion总量上限的计算器
    /// </summary>
    public class StatWorker_TrionMaxCap : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float baseValue = base.GetValueUnfinalized(req, applyPostProcess);
            
            // 获取基础值
            if (!req.HasThing)
            {
                return baseValue;
            }
            
            if (req.Thing is not Pawn pawn)
            {
                return baseValue;
            }
            
            float totalBonus = 0f;
            float totalFactor = 1f;
            
            // 1. Gene 修正
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene gene in pawn.genes.GenesListForReading)
                {
                    if (!gene.Active) continue;
                    
                    // Offset 修正
                    totalBonus += gene.def.statOffsets.GetStatOffsetFromList(stat);
                    
                    // Factor 修正
                    totalFactor *= gene.def.statFactors.GetStatFactorFromList(stat);
                }
            }
            
            // 2. Hediff 修正
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.CurStage == null) continue;
                
                totalBonus += HediffStatsUtility.GetStatOffsetForSeverity(
                    stat, hediff.CurStage, pawn, hediff.Severity);
                totalFactor *= HediffStatsUtility.GetStatFactorForSeverity(
                    stat, hediff.CurStage, pawn, hediff.Severity);
            }
            
            // 3. 特质修正
            if (pawn.story != null)
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (trait.Suppressed) continue;
                    totalBonus += trait.OffsetOfStat(stat);
                    totalFactor *= trait.MultiplierOfStat(stat);
                }
            }
            
            // 计算最终值
            float finalValue = (baseValue + totalBonus) * totalFactor;
            
            // 应用后处理曲线（如果有）
            if (stat.postProcessCurve != null && applyPostProcess)
            {
                finalValue = stat.postProcessCurve.Evaluate(finalValue);
            }
            
            return finalValue;
        }
        
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder sb = new StringBuilder();
            
            // 基础值说明
            float baseValue = GetBaseValueFor(req);
            sb.AppendLine($"StatsReport_BaseValue: {stat.ValueToString(baseValue, numberSense)}");
            
            // 各项修正说明
            GetOffsetsAndFactorsExplanation(req, sb, baseValue, "", numberSense);
            
            return sb.ToString();
        }
        
        private void GetOffsetsAndFactorsExplanation(
            StatRequest req, StringBuilder sb, float baseValue, 
            string whitespace, ToStringNumberSense numberSense)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                // Hediff 修正说明
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.CurStage != null)
                    {
                        float offset = HediffStatsUtility.GetStatOffsetForSeverity(
                            stat, hediff.CurStage, pawn, hediff.Severity);
                        if (offset != 0)
                        {
                            sb.AppendLine($"{whitespace}{hediff.Label}: {stat.ValueToString(offset, numberSense)}");
                        }
                    }
                }
                
                // ... 其他修正的说明
            }
        }
    }
}
```

### 11.3 C# 静态引用类

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    /// <summary>
    /// RimTrion 相关 StatDef 的静态引用
    /// </summary>
    [StaticConstructorOnStartup]
    public static class StatDefOf
    {
        public static readonly StatDef TrionMaxCap;
        public static readonly StatDef TrionOutput;
        public static readonly StatDef TrionRecoveryRate;
        public static readonly StatDef TrionAvailable;
        public static readonly StatDef TrionUsageRate;
        public static readonly StatDef TrionEfficiency;
        
        public static readonly StatCategoryDef RimTrion;
        
        static StatDefOf()
        {
            // 初始化 StatDef
            TrionMaxCap = DefDatabase<StatDef>.GetNamedSilentFail("TrionMaxCap");
            TrionOutput = DefDatabase<StatDef>.GetNamedSilentFail("TrionOutput");
            TrionRecoveryRate = DefDatabase<StatDef>.GetNamedSilentFail("TrionRecoveryRate");
            TrionAvailable = DefDatabase<StatDef>.GetNamedSilentFail("TrionAvailable");
            TrionUsageRate = DefDatabase<StatDef>.GetNamedSilentFail("TrionUsageRate");
            TrionEfficiency = DefDatabase<StatDef>.GetNamedSilentFail("TrionEfficiency");
            
            RimTrion = DefDatabase<StatCategoryDef>.GetNamedSilentFail("RimTrion");
        }
    }
}
```

### 11.4 便捷扩展方法

```csharp
using RimWorld;
using Verse;

namespace RimTrion
{
    public static class PawnStatExtensions
    {
        /// <summary>
        /// 获取角色的 Trion 总量上限
        /// </summary>
        public static float GetTrionMaxCap(this Pawn pawn)
        {
            if (StatDefOf.TrionMaxCap == null) return 0f;
            return pawn.GetStatValue(StatDefOf.TrionMaxCap);
        }
        
        /// <summary>
        /// 获取角色的 Trion 输出功率
        /// </summary>
        public static float GetTrionOutput(this Pawn pawn)
        {
            if (StatDefOf.TrionOutput == null) return 0f;
            return pawn.GetStatValue(StatDefOf.TrionOutput);
        }
        
        /// <summary>
        /// 获取角色的 Trion 恢复速度
        /// </summary>
        public static float GetTrionRecoveryRate(this Pawn pawn)
        {
            if (StatDefOf.TrionRecoveryRate == null) return 0f;
            return pawn.GetStatValue(StatDefOf.TrionRecoveryRate);
        }
        
        /// <summary>
        /// 检查角色是否有足够的输出功率来使用特定能力
        /// </summary>
        public static bool CanUseTrigger(this Pawn pawn, float requiredOutput)
        {
            return pawn.GetTrionOutput() >= requiredOutput;
        }
    }
}
```

---

## 12. 与其他系统的交互

### 12.1 与 Gene 系统交互

```xml
<!-- GeneDef 中定义 statOffsets 和 statFactors -->
<GeneDef>
    <defName>Gene_TrionCore</defName>
    <label>Trion核心</label>
    <description>此基因提供额外的Trion能量上限。</description>
    <statOffsets>
        <li>
            <stat>TrionMaxCap</stat>
            <offset>200</offset>   <!-- +200 Trion -->
        </li>
    </statOffsets>
    <statFactors>
        <li>
            <stat>TrionEfficiency</stat>
            <factor>1.1</factor>   <!-- +10% 效率 -->
        </li>
    </statFactors>
</GeneDef>
```

### 12.2 与 Hediff 系统交互

```csharp
// HediffDef 中定义 statOffsets 和 statFactors
public class Hediff_TrionEnhancement : Hediff
{
    public override void Notify_Added()
    {
        // 添加时应用临时修正
        Pawn.health.AddHediff(HediffDefOf.SomeTempBuff);
    }
}
```

```xml
<HediffDef>
    <defName>Hediff_TrionEnhancement</defName>
    <label>Trion强化</label>
    <statOffsets>
        <li>
            <stat>TrionMaxCap</stat>
            <offset>50</offset>
        </li>
    </statOffsets>
</HediffDef>
```

### 12.3 与 Apparel 系统交互

```xml
<!-- ThingDef (Apparel) 中定义 statOffsets 和 statFactors -->
<ThingDef ParentName="ApparelPowerArmorBase">
    <defName>Apparel_TrionSuit</defName>
    <label>Trion战衣</label>
    <statBases>
        <TrionMaxCap>100</TrionMaxCap>
    </statBases>
    <statOffsets>
        <TrionOutput>5</TrionOutput>
    </statOffsets>
    <statFactors>
        <TrionEfficiency>1.05</TrionEfficiency>
    </statFactors>
</ThingDef>
```

### 12.4 与 Trait 系统交互

```xml
<!-- TraitDef 中定义 statOffsets 和 statFactors -->
<TraitDef>
    <defName>Trait_TrionTalent</defName>
    <label>Trion天赋</label>
    <description>此角色天生Trion天赋异禀。</description>
    <commonality>0.1</commonality>
    <degreeDatas>
        <li>
            <label>Trion感知</label>
            <statOffsets>
                <TrionMaxCap>150</TrionMaxCap>
                <TrionOutput>3</TrionOutput>
            </statOffsets>
        </li>
    </degreeDatas>
</TraitDef>
```

### 12.5 与 ThingComp 系统交互

```csharp
// Comp 提供 statOffsets 和 statFactors
public class CompTrionEmitter : ThingComp
{
    public override void CompGetStatsExplanation(ref string explanation)
    {
        explanation += $"Trion发射器: +{Props.trionOutput} 输出功率\n";
    }
    
    public override IEnumerable<StatModifier> SpecialDisplayStats()
    {
        yield return new StatModifier
        {
            stat = StatDefOf.TrionOutput,
            value = Props.trionOutput
        };
    }
}
```

```xml
<ThingDef>
    <comps>
        <li Class="RimTrion.CompProperties_TrionEmitter">
            <trionOutput>20</trionOutput>
        </li>
    </comps>
</ThingDef>
```

---

## 13. 调试技巧

### 13.1 控制台命令

```csharp
// 在开发时使用
StatsManager.ReportStats(pawn);  // 输出所有属性报告
```

### 13.2 日志输出

```csharp
// 获取完整的属性说明
string explanation = StatUtility.GetOffsetsAndFactorsFor(StatDefOf.TrionMaxCap, pawn);
Log.Message($"TrionMaxCap 说明:\n{explanation}");
```

### 13.3 DevMode 工具

- 开启开发者模式后，可以在信息面板看到"Show debug stats"
- 使用 `ddebug stats` 命令查看详细数据

---

## 14. 常见问题

### Q1: 属性值不更新？
确保：
1. 检查是否需要手动刷新缓存
2. 检查 `cacheable` 设置
3. 检查 `alwaysHide` 设置

### Q2: 属性计算顺序？
计算顺序是：
1. `defaultBaseValue`
2. `skillNeedOffsets` / `capacityOffsets`
3. `traits` / `hediffs` / `genes` / `precepts` Offset
4. `gear` Offset
5. `traits` / `hediffs` / `genes` / `precepts` Factor
6. `stuff` Factor/Offset
7. `stat.statFactors`
8. `stat.statOffsets`
9. `postProcessCurve`
10. `minValue`/`maxValue` clamp

### Q3: 如何隐藏属性？
使用 `alwaysHide` 标志：
```xml
<alwaysHide>true</alwaysHide>
```

### Q4: 属性值显示错误？
检查 `toStringStyle` 和 `toStringNumberSense` 是否匹配你的数值含义。

---

## 15. 验证状态

✓ **RiMCP 已验证**：
- `StatDef` 类定义 [RimWorld.StatDef.cs:109-10616](file:///C:/NiwtGames/Tools/Rimworld/RiMCP/RiMCP_hybrid/RimWorldData/Source/RimWorld/StatDef.cs)
- `StatWorker` 计算逻辑 [RimWorld.StatWorker.cs:1-1374](file:///C:/NiwtGames/Tools/Rimworld/RiMCP/RiMCP_hybrid/RimWorldData/Source/RimWorld/StatWorker.cs)
- `StatRequest` 请求对象 [RimWorld.StatRequest.cs:1-214](file:///C:/NiwtGames/Tools/Rimworld/RiMCP/RiMCP_hybrid/RimWorldData/Source/RimWorld/StatRequest.cs)
- 官方示例 [BedHungerRateFactor](file:///C:/NiwtGames/Tools/Rimworld/RiMCP/RiMCP_hybrid/RimWorldData/Data/Core/Defs/Stats/Stats_Pawns_General.xml)
- 分类定义 [StatCategories.xml](file:///C:/NiwtGames/Tools/Rimworld/RiMCP/RiMCP_hybrid/RimWorldData/Data/Core/Defs/Stats/StatCategories.xml)

---

## 历史记录

| 版本 | 修改日期 | 修改内容 | 修改者 |
|------|----------|---------|--------|
| v1.0 | 2026-01-19 | 初始版本。从 RimWorld 源码和 RiMCP 查询结果整理的完整 StatDef 系统说明书，包含核心属性、分类系统、显示格式、计算部件、请求对象、计算逻辑、缓存机制、代码调用方式和完整示例。 | 知识提炼者 |
