---
标题：Need需求系统总览
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld Need系统完整总览，包含Need基类核心机制、Need_Seeker子系统、18个Need子类速查表（Core 12 + Biotech 4 + Royalty 1 + Ideology 1）、5个关键Need详解、NeedDef完整字段表、XML配置示例
---

# Need需求系统总览

**总览**：Need（需求）是RimWorld中Pawn的内在状态条——从饥饿到心情、从休息到娱乐、从舒适到权威，全部是Need。每个Need是一个0~MaxLevel的浮点数值，通过`NeedInterval()`每150 ticks更新一次，驱动Pawn的行为决策和心情变化。

## 1. Need基类核心机制

**类签名**：`RimWorld.Need : IExposable`（抽象类）

### 核心字段和属性

| 字段/属性 | 类型 | 说明 |
|----------|------|------|
| `def` | `NeedDef` | 需求定义 |
| `pawn` | `Pawn` | 所属Pawn（readonly） |
| `curLevelInt` | `float` | 当前等级内部值 |
| `threshPercents` | `List<float>` | GUI阈值线位置列表 |
| `CurLevel` | `float` | 当前等级（Clamp到0~MaxLevel） |
| `CurLevelPercentage` | `float` | 当前等级百分比（CurLevel/MaxLevel） |
| `MaxLevel` | `float` | 最大等级（默认1.0，可重写） |
| `CurInstantLevel` | `float` | 瞬时等级（默认-1，Seeker子类重写） |
| `IsFrozen` | `bool` | 是否冻结更新 |
| `ShowOnNeedList` | `bool` | 是否在需求面板显示 |

### NeedInterval更新机制

- **调用频率**：每 **150 ticks** 调用一次（约2.5秒游戏时间）
- **调用链**：`Pawn.Tick()` → `Pawn_NeedsTracker.NeedsTrackerTick()` → 每150 ticks → `Need.NeedInterval()`
- **抽象方法**：`NeedInterval()` 是抽象方法，每个Need子类必须实现自己的更新逻辑

### IsFrozen冻结条件（源码：`Need.IsFrozen`）

Need在以下任一条件满足时冻结（不更新）：

| # | 条件 | 对应代码 |
|---|------|---------|
| 1 | Pawn被暂停（冬眠舱等） | `pawn.Suspended` |
| 2 | 睡眠中且NeedDef设置了冻结 | `def.freezeWhileSleeping && !pawn.Awake()` |
| 3 | 精神状态中且NeedDef设置了冻结 | `def.freezeInMentalState && pawn.InMentalState` |
| 4 | 休眠组件冻结 | `CompCanBeDormant.freezeNeeds`包含此Need |
| 5 | Pawn不可交互且不可见 | 未生成、非远行队成员、非运输舱中 |

> **注意**：部分Need子类重写了`IsFrozen`添加额外条件（如Need_Food在死眠/平台拘束时冻结，Need_Authority在非玩家基地时冻结）。

## 2. Need_Seeker子系统

**类签名**：`RimWorld.Need_Seeker : Need`（抽象类）

### Seeker趋近机制

Need_Seeker不像普通Need那样持续下降，而是**追踪一个瞬时目标值（CurInstantLevel）并逐渐趋近**：

```csharp
// Need_Seeker.NeedInterval() 核心逻辑
if (curInstantLevel > CurLevel)
{
    CurLevel += def.seekerRisePerHour * 0.06f;  // 上升趋近
    CurLevel = Min(CurLevel, curInstantLevel);   // 不超过目标
}
if (curInstantLevel < CurLevel)
{
    CurLevel -= def.seekerFallPerHour * 0.06f;  // 下降趋近
    CurLevel = Max(CurLevel, curInstantLevel);   // 不低于目标
}
```

**关键参数**：
- `seekerRisePerHour`：每小时上升速率（NeedDef字段）
- `seekerFallPerHour`：每小时下降速率（NeedDef字段）
- `0.06f`：150 ticks / 2500 ticks per hour = 0.06（每次NeedInterval的小时占比）

**4个Seeker子类**：Need_Mood（心情）、Need_Beauty（美观）、Need_Comfort（舒适）、Need_RoomSize（房间大小）

## 3. 完整继承层次

```
Need (抽象基类, RimWorld)
├── Need_Seeker (追踪瞬时等级的抽象子类)
│   ├── Need_Mood (心情) ─── Core
│   ├── Need_Beauty (美观) ─── Core
│   ├── Need_Comfort (舒适) ─── Core
│   └── Need_RoomSize (房间大小) ─── Core
├── Need_Food (饥饿) ─── Core
├── Need_Rest (休息) ─── Core
├── Need_Joy (娱乐) ─── Core
├── Need_Outdoors (户外) ─── Core
├── Need_Indoors (室内, 洞穴人特质) ─── Core
├── Need_Chemical (化学品/单一成瘾) ─── Core
├── Need_Chemical_Any (化学品渴望/DrugDesire特质) ─── Core
├── Need_MechEnergy (机械能量) ─── Biotech
├── Need_Deathrest (死亡休息) ─── Biotech
├── Need_Learning (学习) ─── Biotech
├── Need_Play (玩耍) ─── Biotech
├── Need_KillThirst (杀戮渴望) ─── Biotech
├── Need_Authority (权威) ─── Royalty
└── Need_Suppression (压制) ─── Ideology
```

## 4. 全部Need子类速查表（18个，按DLC分组）

### Core（12个）

| Need | C#类 | 继承 | 下降机制 | 类别枚举 | 阈值 | 关键特性 |
|------|------|------|---------|---------|------|---------|
| **心情** | `Need_Mood` | Need_Seeker | Seeker趋近CurInstantLevel | MoodString（6级文本） | 精神崩溃阈值（Extreme/Major/Minor） | 思想系统驱动瞬时心情 |
| **美观** | `Need_Beauty` | Need_Seeker | Seeker趋近环境美观值 | `BeautyCategory`（7级） | 0.01/0.15/0.35/0.65/0.85/0.99 | 失明时固定0.5 |
| **舒适** | `Need_Comfort` | Need_Seeker | Seeker趋近当前家具舒适度 | `ComfortCategory`（6级） | 0.1/0.6/0.7/0.8/0.9 | 每15 ticks采样一次舒适度 |
| **房间大小** | `Need_RoomSize` | Need_Seeker | Seeker趋近当前空间感知 | `RoomSizeCategory`（4级） | 0.01/0.3/0.7 | 室外时固定1.0 |
| **饥饿** | `Need_Food` | Need | BaseFoodFallPerTick × 多重因子 | `HungerCategory`（4级） | Hungry/UrgentlyHungry（动态） | 饥饿→营养不良Hediff |
| **休息** | `Need_Rest` | Need | BaseRestFallPerTick × 类别因子 | `RestCategory`（4级） | 0.14/0.28 | 非自愿睡眠MTB机制 |
| **娱乐** | `Need_Joy` | Need | FallPerInterval按类别不同 | `JoyCategory`（6级） | 0.15/0.3/0.7/0.85 | 娱乐容忍度系统 |
| **户外** | `Need_Outdoors` | Need | 室内时下降 | — | — | 幽闭恐惧症思想 |
| **室内** | `Need_Indoors` | Need | 室外时下降 | — | — | 洞穴人特质专属 |
| **化学品** | `Need_Chemical` | Need | `fallPerDay`（NeedDef配置） | — | — | 单一成瘾物质需求 |
| **化学品渴望** | `Need_Chemical_Any` | Need | 按DrugDesire特质等级×曲线 | `MoodBuff`（6级） | Interest/Fascination两套阈值 | DrugDesire特质专属 |
| *(注：Outdoors和Indoors互斥，同一Pawn只会有其中一个)* | | | | | | |

### Biotech（5个）

| Need | C#类 | 继承 | 下降机制 | 关键特性 |
|------|------|------|---------|---------|
| **机械能量** | `Need_MechEnergy` | Need | 活跃10/天，空闲3/天 | 能量归零→自动关机（SelfShutdown Hediff） |
| **死亡休息** | `Need_Deathrest` | Need | 持续缓慢下降 | 吸血鬼基因专属，需定期死眠 |
| **学习** | `Need_Learning` | Need | 0.00045/间隔 | 儿童专属，影响成长点数获取 |
| **玩耍** | `Need_Play` | Need | 持续下降 | 婴儿专属 |
| **杀戮渴望** | `Need_KillThirst` | Need | 持续下降 | 嗜血基因专属 |

### Royalty（1个）

| Need | C#类 | 继承 | 下降机制 | 关键特性 |
|------|------|------|---------|---------|
| **权威** | `Need_Authority` | Need | 按派系人数曲线下降 | 演讲+2/天，统治+3/天；非玩家基地时冻结为1.0 |

### Ideology（1个）

| Need | C#类 | 继承 | 下降机制 | 关键特性 |
|------|------|------|---------|---------|
| **压制** | `Need_Suppression` | Need | 0.0025 × SlaveSuppressionFallRate | 奴隶专属，低于阈值可能叛乱 |

## 5. 关键Need详解

### 5.1 Need_Food（饥饿）

**饥饿计算公式**（源码：`Need_Food.FoodFallPerTickAssumingCategory`）：

```
FoodFallPerTick = BaseHungerRate × HungerMultiplier × HediffHungerRateFactor
                  × TraitHungerRateFactor × BedHungerRateFactor × MetabolismFactor
```

其中：
- `BaseHungerRate` = `lifeStage.hungerRateFactor × race.baseHungerRate × 2.6666667E-05f`
- `HungerMultiplier` = 按HungerCategory不同的倍率
- `MetabolismFactor` = Biotech基因代谢效率曲线

**HungerCategory阈值**（动态，基于`FoodLevelPercentageWantEat`）：

| 类别 | 条件 | 说明 |
|------|------|------|
| Fed | ≥ WantEat×0.8 | 吃饱 |
| Hungry | ≥ WantEat×0.4 | 饥饿 |
| UrgentlyHungry | > 0 | 极度饥饿 |
| Starving | ≤ 0 | 饥饿中（触发营养不良） |

**饥饿→营养不良**：Starving时每间隔增加`0.0011325f`的Malnutrition Severity（约0.453/天）。

### 5.2 Need_Rest（休息）

**RestCategory阈值**（固定）：

| 类别 | 阈值 | 下降速率倍率 |
|------|------|------------|
| Rested | ≥ 0.28 | 1.0× |
| Tired | ≥ 0.14 | 0.7× |
| VeryTired | ≥ 0.01 | 0.3× |
| Exhausted | < 0.01 | 0.6× |

**关键常量**：
- `BaseRestFallPerTick` = 1.5833333E-05f
- `BaseRestGainPerTick` = 3.809524E-05f（约10.5小时睡满）
- 恢复速率受`RestRateMultiplier`和床铺效率影响

**非自愿睡眠MTB**：ticksAtZero超过1000后开始概率触发，随时间递增（0.25天→0.125天→1/12天→0.0625天）。

### 5.3 Need_Joy（娱乐）

**JoyCategory阈值和下降速率**：

| 类别 | 阈值 | FallPerInterval |
|------|------|----------------|
| Empty | < 0.01 | 0.0015 |
| VeryLow | < 0.15 | 0.0006 |
| Low | < 0.30 | 0.00105 |
| Satisfied | < 0.70 | 0.0015 |
| High | < 0.85 | 0.0015 |
| Extreme | ≥ 0.85 | 0.0015 |

**娱乐容忍度系统**：
- 每种JoyKind有独立的容忍度值
- 重复使用同一种娱乐→容忍度上升→获取效率下降
- 容忍度下降速率受期望等级（Expectations）影响
- `GainJoy(amount, joyKind)`：实际增益 = amount × `JoyFactorFromTolerance(joyKind)`

### 5.4 Need_Mood（心情）

**瞬时心情计算**（源码：`Need_Mood.CurInstantLevel`）：

```
instantMood = Clamp01(baseMoodLevel + TotalMoodOffset / 100 + colonistMoodOffset)
```

- `baseMoodLevel`：默认0.5（可被Hediff的`OverrideMoodBase`覆盖）
- `TotalMoodOffset`：所有思想（Thought）的心情偏移总和
- `colonistMoodOffset`：叙事者难度的殖民者心情偏移

**精神崩溃阈值**（由`MentalBreaker`管理）：
- Extreme（极端）：默认0.05
- Major（严重）：默认0.15
- Minor（轻微）：默认0.25

**心情等级文本**：AboutToBreak → OnEdge → Stressed → Neutral(0.65) → Content(0.9) → Happy

### 5.5 Need_MechEnergy（机械能量）

**能量消耗**（源码：`Need_MechEnergy`）：

| 状态 | 消耗/天 | 说明 |
|------|--------|------|
| 活跃（非空闲） | 10 × MechEnergyUsageFactor | 执行任务时 |
| 空闲 | 3 × MechEnergyUsageFactor | 待命时 |
| 自动关机 | -1（恢复） | 每天恢复1点 |
| 倒地/睡眠/充电/远行 | 0 | 不消耗 |

**自动关机机制**：
1. 能量降至0 → `selfShutdown = true` → 添加`SelfShutdown` Hediff
2. 机械体自动寻找关机位置并执行`JobDefOf.SelfShutdown`
3. 关机期间每天恢复1点能量
4. 能量恢复到15或开始充电 → `selfShutdown = false` → 移除Hediff

**MaxLevel**：由`pawn.RaceProps.maxMechEnergy`决定（不同机械体不同）

## 6. NeedDef完整字段表

**类签名**：`RimWorld.NeedDef : Def`

| 字段 | 类型 | 默认值 | 说明 |
|------|------|-------|------|
| `needClass` | `Type` | — | Need的C#类类型（必填） |
| `baseLevel` | `float` | 0.5 | 基础等级（Seeker的中心点） |
| `fallPerDay` | `float` | 0.5 | 每天下降速率 |
| `seekerRisePerHour` | `float` | 0 | Seeker每小时上升速率 |
| `seekerFallPerHour` | `float` | 0 | Seeker每小时下降速率 |
| `freezeWhileSleeping` | `bool` | false | 睡眠时冻结 |
| `freezeInMentalState` | `bool` | false | 精神状态时冻结 |
| `major` | `bool` | false | 是否为主要需求 |
| `showOnNeedList` | `bool` | true | 是否在需求面板显示 |
| `showForCaravanMembers` | `bool` | false | 远行队成员是否显示 |
| `scaleBar` | `bool` | false | MaxLevel<1时是否缩放进度条 |
| `showUnitTicks` | `bool` | false | 是否显示单位刻度线 |
| `listPriority` | `int` | 0 | 需求列表排序优先级 |
| `tutorHighlightTag` | `string` | null | 教程高亮标签 |

### 条件过滤器字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `minIntelligence` | `Intelligence` | 最低智力要求 |
| `colonistAndPrisonersOnly` | `bool` | 仅殖民者和囚犯 |
| `playerMechsOnly` | `bool` | 仅玩家机械体 |
| `colonistsOnly` | `bool` | 仅殖民者 |
| `slavesOnly` | `bool` | 仅奴隶 |
| `neverOnPrisoner` | `bool` | 囚犯排除 |
| `neverOnSlave` | `bool` | 奴隶排除 |
| `onlyIfCausedByHediff` | `bool` | 仅当由Hediff触发时 |
| `onlyIfCausedByGene` | `bool` | 仅当由基因触发时 |
| `onlyIfCausedByTrait` | `bool` | 仅当由特质触发时 |
| `onlyIfCausedByIdeo` | `bool` | 仅当由意识形态触发时 |
| `developmentalStageFilter` | `DevelopmentalStage` | 发育阶段过滤（默认Child\|Adult） |
| `titleRequiredAny` | `List<RoyalTitleDef>` | 需要任一皇家头衔 |
| `hediffRequiredAny` | `List<HediffDef>` | 需要任一Hediff |
| `nullifyingPrecepts` | `List<PreceptDef>` | 使Need无效的教条 |
| `requiredComps` | `List<CompProperties>` | 需要的ThingComp |

## 7. XML配置示例

### 示例1：基础Need（Food）

```xml
<NeedDef>
  <defName>Food</defName>
  <needClass>RimWorld.Need_Food</needClass>
  <label>food</label>
  <description>...</description>
  <baseLevel>0.5</baseLevel>
  <major>true</major>
  <listPriority>100</listPriority>
  <showForCaravanMembers>true</showForCaravanMembers>
  <tutorHighlightTag>NeedFood</tutorHighlightTag>
</NeedDef>
```

### 示例2：Seeker类Need（Mood）

```xml
<NeedDef>
  <defName>Mood</defName>
  <needClass>RimWorld.Need_Mood</needClass>
  <label>mood</label>
  <description>...</description>
  <baseLevel>0.5</baseLevel>
  <seekerRisePerHour>0.4</seekerRisePerHour>
  <seekerFallPerHour>0.4</seekerFallPerHour>
  <major>true</major>
  <listPriority>200</listPriority>
  <tutorHighlightTag>NeedMood</tutorHighlightTag>
</NeedDef>
```

### 示例3：条件激活Need（Suppression，仅奴隶）

```xml
<NeedDef>
  <defName>Suppression</defName>
  <needClass>RimWorld.Need_Suppression</needClass>
  <label>suppression</label>
  <description>...</description>
  <slavesOnly>true</slavesOnly>
  <showOnNeedList>false</showOnNeedList>
</NeedDef>
```

### 示例4：基因触发Need（KillThirst）

```xml
<NeedDef>
  <defName>KillThirst</defName>
  <needClass>RimWorld.Need_KillThirst</needClass>
  <label>kill thirst</label>
  <description>...</description>
  <onlyIfCausedByGene>true</onlyIfCausedByGene>
  <baseLevel>0.5</baseLevel>
  <fallPerDay>0.4</fallPerDay>
</NeedDef>
```

## 8. 开发者要点

1. **Need是被动更新的**：每150 ticks由`Pawn_NeedsTracker`统一调度`NeedInterval()`，不需要手动触发。模组新增Need只需定义NeedDef + C#类，系统自动管理生命周期
2. **优先用现有Need子类**：大多数"资源条"需求可以直接继承`Need`并重写`NeedInterval()`。如果需求是"追踪环境值并趋近"的模式，继承`Need_Seeker`更合适
3. **条件激活是NeedDef的职责**：通过`onlyIfCausedByGene`/`onlyIfCausedByTrait`/`onlyIfCausedByHediff`等字段控制Need是否出现，无需在C#中手动检查
4. **IsFrozen可重写**：如果自定义Need有特殊的冻结条件（如Need_Food在死眠时冻结），重写`IsFrozen`属性即可
5. **threshPercents控制GUI阈值线**：在构造函数中设置`threshPercents`列表，GUI会自动在进度条上绘制阈值线
6. **MaxLevel可重写**：Need_Food的MaxLevel由Stat决定（体型×生命阶段），Need_MechEnergy由种族属性决定。自定义Need可按需重写
7. **Category枚举是可选的**：不是所有Need都需要Category枚举，简单的Need可以直接用CurLevel百分比判断状态

## 9. 关键源码引用表

| 类 | 命名空间 | 关键内容 |
|----|---------|---------|
| `Need` | RimWorld | 基类：IsFrozen、CurLevel、NeedInterval抽象方法 |
| `Need_Seeker` | RimWorld | Seeker趋近机制：seekerRisePerHour/seekerFallPerHour |
| `NeedDef` | RimWorld | 完整字段定义、条件过滤器 |
| `Pawn_NeedsTracker` | RimWorld | Need生命周期管理、NeedInterval调度（每150 ticks） |
| `Need_Food` | RimWorld | 饥饿计算、HungerCategory、营养不良触发 |
| `Need_Rest` | RimWorld | 休息恢复/消耗、RestCategory、非自愿睡眠MTB |
| `Need_Joy` | RimWorld | 娱乐容忍度、JoyCategory、JoyKind系统 |
| `Need_Mood` | RimWorld | 瞬时心情计算、ThoughtHandler、精神崩溃阈值 |
| `Need_MechEnergy` | RimWorld | 机械能量消耗、自动关机机制 |
| `Need_Beauty` | RimWorld | 环境美观感知、BeautyCategory |
| `Need_Comfort` | RimWorld | 家具舒适度采样、ComfortCategory |
| `Need_RoomSize` | RimWorld | 空间感知、RoomSizeCategory |
| `Need_Authority` | RimWorld | 权威下降曲线、演讲/统治增益 |
| `Need_Suppression` | RimWorld | 奴隶压制、SlaveSuppressionFallRate |
| `Need_Chemical` | RimWorld | 单一成瘾物质需求 |
| `Need_Chemical_Any` | RimWorld | DrugDesire特质、Interest/Fascination两套阈值 |
| `Need_Deathrest` | RimWorld | 吸血鬼死眠需求 |
| `Need_Learning` | RimWorld | 儿童学习需求、成长点数 |
| `Need_Play` | RimWorld | 婴儿玩耍需求 |
| `Need_KillThirst` | RimWorld | 嗜血基因杀戮渴望 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初始版本：Need基类机制、Seeker子系统、18个Need子类速查表、5个关键Need详解、NeedDef字段表、XML示例、开发者要点 | Claude Opus 4.6 |
