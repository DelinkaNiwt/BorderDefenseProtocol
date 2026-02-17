---
标题：Hediff互相影响与触发机制
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未完成][未锁定]
摘要: 详解RimWorld中Hediff之间互相影响的5大类机制——HediffGiver系统、HediffComp交互、生命周期回调、Stage层级触发、典型交互模式，含源码验证的字段表和XML配置示例
---

## 总览

RimWorld中Hediff之间的互相影响是健康系统的核心动态——出血导致失血、疾病触发并发症、环境引发体温异常、器官衰竭导致死亡。这些交互通过**5大类机制**实现：

1. **HediffGiver系统**：定期检查条件并给予新Hediff（最常用的触发机制）
2. **HediffComp交互系统**：通过组件实现Hediff间的给予、替换、移除、反应
3. **生命周期回调**：Hediff添加/移除时的即时回调
4. **HediffStage层级触发**：特定严重度阶段激活的HediffGiver和免疫授予
5. **典型交互模式**：上述机制组合形成的8种常见游戏模式

---

## 1. HediffGiver系统

### 1.1 基类核心方法

`HediffGiver`是所有Hediff给予器的抽象基类，定义了三个核心虚方法：

| 方法 | 调用时机 | 用途 |
|------|---------|------|
| `OnIntervalPassed(Pawn, Hediff cause)` | 每60 ticks（1秒）由`Pawn_HealthTracker`调用 | 定期检查条件并触发效果 |
| `TryApply(Pawn, outAddedHediffs)` | 由子类主动调用 | 尝试将hediff应用到pawn，含多重过滤 |
| `OnHediffAdded(Pawn, Hediff)` | Hediff被添加时回调 | 响应新Hediff的添加事件 |

**TryApply的过滤链**（源码验证）：

```
TryApply(pawn)
  ├── 任务Pawn过滤（Lodger/QuestReward/QuestReserved/Beggar）
  ├── 婴儿健康保护（babiesAreHealthy难度设置）
  ├── 基因免疫检查（genes.HediffGiversCanGive）
  ├── 变异体免疫检查（mutant.HediffGiversCanGive）
  └── HediffGiverUtility.TryApply()（实际添加Hediff）
```

### 1.2 常用子类速查表

| 子类 | 命名空间 | 触发条件 | 效果 | 关键字段 | 典型实例 |
|------|---------|---------|------|---------|---------|
| **HediffGiver_Bleeding** | Verse | `BleedRateTotal ≥ 0.1` | 增加BloodLoss严重度（`bleedRate × 0.001`/tick） | — | 出血→失血 |
| **HediffGiver_Birthday** | Verse | 生日时按年龄曲线概率触发 | 给予慢性病并模拟已有严重度 | `ageFractionChanceCurve`, `averageSeverityPerDayBeforeGeneration` | 年龄→白内障/痴呆 |
| **HediffGiver_Random** | Verse | MTB概率触发 | 给予指定Hediff | `mtbDays` | 随机疾病 |
| **HediffGiver_RandomAgeCurved** | Verse | 按年龄曲线MTB触发 | 给予指定Hediff | `ageFractionMtbDaysCurve`, `minPlayerPopulation` | 年龄相关随机疾病 |
| **HediffGiver_RandomDrugEffect** | Verse | 药物严重度达阈值后MTB触发 | 给予药物副作用 | `severityToMtbDaysCurve`, `baseMtbDays`, `minSeverity` | 成瘾→肝损伤 |
| **HediffGiver_Hypothermia** | Verse | 环境温度 < 安全最低温 | 增加低温症严重度；严重时触发冻伤 | `hediffInsectoid`（虫族变体） | 低温→低温症→冻伤 |
| **HediffGiver_Heat** | Verse | 环境温度 > 安全最高温 | 增加中暑严重度；极端时直接烧伤 | `TemperatureOverageAdjustmentCurve` | 高温→中暑→烧伤 |
| **HediffGiver_AddSeverity** | Verse | MTB概率触发 | 增加已有Hediff的严重度 | `severityAmount`, `mtbHours` | 恶化现有状态 |

### 1.3 三层配置位置

HediffGiver可以在三个层级配置，作用范围不同：

| 配置位置 | XML路径 | 作用范围 | 说明 |
|---------|---------|---------|------|
| **HediffDef层级** | `HediffDef/hediffGivers` | 拥有该Hediff的Pawn | 该Hediff存在时持续检查 |
| **HediffStage层级** | `HediffDef/stages/li/hediffGivers` | 处于特定阶段的Pawn | 仅在该Stage激活时检查 |
| **HediffGiverSetDef（种族层级）** | `RaceProperties/hediffGiverSets` | 该种族的所有Pawn | 与Hediff无关，种族固有 |

> **开发者要点**：种族层级的HediffGiverSet是最常用的配置方式——出血→失血、年龄→慢性病、温度→体温异常都配置在`HediffGiverSetDef`中，而非某个具体Hediff上。

---

## 2. HediffComp交互系统

HediffComp是Hediff的组件系统，通过`CompPostTickInterval`等回调实现Hediff间的交互。以下按功能分为4类共9个关键Comp。

### 2.1 直接给予类

#### HediffComp_GiveHediff（达到严重度时给予）

**命名空间**：`Verse`

**核心逻辑**：每个Tick间隔检查，当`parent.Severity ≥ atSeverity`时给予目标Hediff。

| Properties字段 | 类型 | 默认值 | 说明 |
|---------------|------|-------|------|
| `hediffDef` | HediffDef | — | 要给予的Hediff |
| `atSeverity` | float | 1.0 | 触发严重度阈值 |
| `skipIfAlreadyExists` | bool | false | 已存在时跳过 |
| `disappearsAfterGiving` | bool | false | 给予后移除自身 |
| `letterLabel/letterText` | string | null | 通知信件 |
| `letterDef` | LetterDef | NegativeEvent | 信件类型 |

**典型用途**：疾病恶化到一定程度触发并发症。

#### HediffComp_GiveHediffsInRange（范围光环效果）

**命名空间**：`Verse`

**核心逻辑**：每Tick扫描范围内的人形Pawn，给予带`HediffComp_Disappears`的短时Hediff（每5 ticks刷新，离开范围自动消失）。

**关键行为**：
- 仅影响人形Pawn（`Humanlike`）
- Pawn必须清醒、未疼痛休克、已生成
- 可选仅同阵营（`onlyPawnsInSameFaction`）
- 给予的Hediff附加到脑部（`GetBrain()`）
- 支持`HediffComp_Link`连线显示

**典型用途**：心灵连接光环、领袖鼓舞光环。

#### HediffComp_GiveHediffLungRot（腐烂气体→肺腐病）

**命名空间**：`Verse`

**核心逻辑**：当Pawn处于腐臭气体中且`parent.Severity ≥ minSeverity`时，按MTB概率给予LungRot到所有肺部位。

**特殊点**：
- 检查基因免疫（`AnyGeneMakesFullyImmuneTo`）
- 使用`mtbOverRotGasExposureCurve`按严重度调整概率
- 影响所有肺部位（`GetLungRotAffectedBodyParts`）

### 2.2 替换/移除类

#### HediffComp_ReplaceHediff（疾病转化）

**命名空间**：`Verse`

**核心逻辑**：当`parent.Severity ≥ severity`时，移除自身并添加一组新Hediff。

| Properties字段 | 类型 | 默认值 | 说明 |
|---------------|------|-------|------|
| `severity` | float | 1.0 | 触发严重度阈值 |
| `manuallyTriggered` | bool | false | 是否仅手动触发（不自动检查） |
| `hediffs` | List\<TriggeredHediff\> | — | 要添加的Hediff列表 |
| `message/letterLabel/letterDesc` | string | null | 通知消息/信件 |

**TriggeredHediff内部类字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `hediff` | HediffDef | 要添加的Hediff |
| `countRange` | IntRange | 添加数量范围（默认1-1） |
| `partsToAffect` | List\<BodyPartDef\> | 目标部位 |
| `severityRange` | FloatRange | 初始严重度范围 |

**典型用途**：器官腐烂（OrganDecay）达到阈值后转化为器官摧毁。

#### HediffComp_RemoveIfOtherHediff（Hediff互斥）

**命名空间**：`Verse`（继承自`HediffComp_MessageBase`）

**核心逻辑**：当Pawn拥有指定列表中的任一Hediff时，移除自身。

| Properties字段 | 类型 | 说明 |
|---------------|------|------|
| `hediffs` | List\<HediffDef\> | 触发移除的Hediff列表 |
| `stages` | IntRange? | 可选：仅当目标Hediff处于指定Stage范围时触发 |
| `mtbHours` | int | 可选：MTB概率移除（0=立即移除） |

**典型用途**：治愈状态与疾病互斥、免疫状态移除感染。

### 2.3 反应类

#### HediffComp_ReactOnDamage（受伤反应）

**命名空间**：`Verse`

**核心逻辑**：通过`Notify_PawnPostApplyDamage`回调，当Pawn受到指定类型伤害时触发反应。

| Properties字段 | 类型 | 说明 |
|---------------|------|------|
| `damageDefIncoming` | DamageDef | 触发反应的伤害类型 |
| `createHediff` | HediffDef | 反应时创建的Hediff |
| `createHediffOn` | BodyPartDef | 创建Hediff的目标部位（null=同部位） |
| `vomit` | bool | 是否触发呕吐Job |

**典型用途**：仿生植入体受EMP伤害时短路（EMP→BionicShortCircuit）。

#### HediffComp_DestroyOrgan（器官摧毁）

**命名空间**：`RimWorld`

**核心逻辑**：当`parent.Severity ≥ maxSeverity`时，移除自身并对部位施加99999点Rotting伤害（摧毁器官）。死亡时若死因是同部位OrganDecay也会触发。

**关键行为**：
- 活着时：施加99999伤害摧毁部位
- 已死时：添加Decayed标记
- `SetAllowDamagePropagation(false)`防止伤害扩散

**典型用途**：器官腐烂（OrganDecay）达到最大严重度→摧毁器官→可能致死。

### 2.4 免疫竞赛

#### HediffComp_Immunizable（免疫系统）

**命名空间**：`Verse`

**核心逻辑**：疾病的严重度与免疫值进行竞赛——严重度先到1.0则致死，免疫值先到1.0则开始康复。详见[Severity严重程度机制](Severity严重程度机制.md)。

**关键交互**：
- 免疫值通过`ImmunityHandler`独立追踪
- `severityPerDayNotImmune`：未免疫时的恶化速度
- `severityPerDayImmune`：免疫后的康复速度（负值）
- 与`HediffComp_TendDuration`协作：治疗加速康复

---

## 3. 生命周期回调

Hediff和HediffComp提供即时回调，在Hediff添加/移除时触发交互：

| 回调方法 | 所属层级 | 调用时机 | 典型用途 |
|---------|---------|---------|---------|
| `Hediff.PostAdd(dinfo)` | Hediff | Hediff被添加到Pawn后 | `Hediff_AddedPart`：调用`RestorePart()`恢复部位 |
| `Hediff.PostRemoved()` | Hediff | Hediff从Pawn移除后 | 清理关联状态 |
| `HediffComp.CompPostPostAdd(dinfo)` | HediffComp | Comp所属Hediff被添加后 | 初始化Comp状态 |
| `HediffComp.CompPostPostRemoved()` | HediffComp | Comp所属Hediff被移除后 | 清理Comp状态 |
| `HediffComp.Notify_PawnPostApplyDamage(dinfo, totalDamage)` | HediffComp | Pawn受到伤害后 | `ReactOnDamage`：EMP反应 |
| `HediffComp.Notify_PawnDied(dinfo, culprit)` | HediffComp | Pawn死亡时 | `DestroyOrgan`：死亡时器官腐烂效果 |
| `HediffComp.Notify_PawnKilled()` | HediffComp | Pawn被杀死时 | `DissolveGearOnDeath`：死亡时摧毁装备 |

> **开发者要点**：`PostAdd`是即时回调，与`CompPostTickInterval`的定期检查不同。需要"添加时立即生效"的交互应使用回调，需要"持续检查条件"的交互应使用Tick。

---

## 4. HediffStage层级触发

`HediffStage`提供两个与Hediff交互相关的字段：

### 4.1 hediffGivers（阶段性HediffGiver）

```xml
<stages>
  <li>
    <minSeverity>0.5</minSeverity>
    <hediffGivers>
      <li Class="Verse.HediffGiver_Random">
        <hediff>SomeComplication</hediff>
        <mtbDays>10</mtbDays>
      </li>
    </hediffGivers>
  </li>
</stages>
```

**行为**：仅当Hediff处于该Stage时，其中的HediffGiver才会被调用。离开该Stage后停止。

### 4.2 makeImmuneTo（阶段性免疫授予）

```xml
<stages>
  <li>
    <minSeverity>0.8</minSeverity>
    <makeImmuneTo>
      <li>Flu</li>
      <li>Plague</li>
    </makeImmuneTo>
  </li>
</stages>
```

**行为**：当Hediff处于该Stage时，Pawn对列表中的疾病免疫。这是一种条件性免疫——离开该Stage后免疫消失。

**源码字段**（`HediffStage`类，已验证）：
- `hediffGivers`：`List<HediffGiver>` — 该阶段激活的HediffGiver列表
- `makeImmuneTo`：`List<HediffDef>` — 该阶段授予免疫的Hediff列表

---

## 5. 典型交互模式速查表

| # | 模式 | 触发机制 | 链条 | 关键类 |
|---|------|---------|------|-------|
| 1 | **出血→失血** | HediffGiver_Bleeding | 伤口BleedRate ≥ 0.1 → BloodLoss严重度增加 → 致死 | `HediffGiver_Bleeding` |
| 2 | **疾病→免疫竞赛** | HediffComp_Immunizable | 感染 → Severity vs 免疫值竞赛 → 致死或康复 | `HediffComp_Immunizable` |
| 3 | **年龄→慢性病** | HediffGiver_Birthday | 生日 → 按年龄曲线概率 → 白内障/痴呆/心脏病 | `HediffGiver_Birthday` |
| 4 | **环境→体温异常** | HediffGiver_Hypothermia/Heat | 温度超出安全范围 → 低温症/中暑 → 冻伤/烧伤 | `HediffGiver_Hypothermia`, `HediffGiver_Heat` |
| 5 | **疾病→并发症** | HediffComp_GiveHediff / Stage.hediffGivers | 疾病严重度达阈值 → 触发并发症Hediff | `HediffComp_GiveHediff` |
| 6 | **器官衰竭→死亡** | HediffComp_DestroyOrgan | OrganDecay达maxSeverity → 99999伤害摧毁器官 → 致命能力归零 | `HediffComp_DestroyOrgan` |
| 7 | **范围光环** | HediffComp_GiveHediffsInRange | 持有者Hediff → 范围内Pawn获得短时Hediff（5 ticks刷新） | `HediffComp_GiveHediffsInRange` |
| 8 | **Hediff互斥** | HediffComp_RemoveIfOtherHediff | Pawn获得Hediff A → 自动移除互斥的Hediff B | `HediffComp_RemoveIfOtherHediff` |

---

## 6. 开发者要点

1. **优先使用现有Comp**：大多数Hediff交互需求可通过XML配置现有HediffComp实现，无需写C#代码。`HediffComp_GiveHediff`（并发症）、`HediffComp_ReplaceHediff`（转化）、`HediffComp_RemoveIfOtherHediff`（互斥）覆盖了最常见的场景
2. **HediffGiver vs HediffComp选择**：HediffGiver适合"外部条件触发新Hediff"（温度、年龄、出血率），HediffComp适合"Hediff自身状态触发交互"（严重度达阈值、受伤反应）
3. **三层配置灵活组合**：HediffDef层级（该Hediff固有）、Stage层级（阶段性）、种族层级（种族固有）可以灵活组合，实现从简单到复杂的交互
4. **注意Tick间隔**：`CompPostTickInterval`默认每200 ticks调用一次（`HediffDef.checkInterval`），而`CompPostTick`每Tick调用。范围光环（GiveHediffsInRange）使用`CompPostTick`因为需要实时刷新
5. **ReplaceHediff支持手动触发**：`manuallyTriggered=true`时不自动检查严重度，需要外部代码调用`Trigger()`——这为模组提供了程序化控制Hediff转化的接口

---

## 7. 关键源码引用表

| 类名 | 命名空间 | 源码路径 | 行数 |
|------|---------|---------|------|
| `HediffGiver` | Verse | `Source/Verse/HediffGiver.cs` | 93 |
| `HediffGiver_Bleeding` | Verse | `Source/Verse/HediffGiver_Bleeding.cs` | 15 |
| `HediffGiver_Birthday` | Verse | `Source/Verse/HediffGiver_Birthday.cs` | 83 |
| `HediffGiver_Random` | Verse | `Source/Verse/HediffGiver_Random.cs` | 14 |
| `HediffGiver_RandomAgeCurved` | Verse | `Source/Verse/HediffGiver_RandomAgeCurved.cs` | 24 |
| `HediffGiver_RandomDrugEffect` | Verse | `Source/Verse/HediffGiver_RandomDrugEffect.cs` | 21 |
| `HediffGiver_Hypothermia` | Verse | `Source/Verse/HediffGiver_Hypothermia.cs` | 44 |
| `HediffGiver_Heat` | Verse | `Source/Verse/HediffGiver_Heat.cs` | 63 |
| `HediffGiver_AddSeverity` | Verse | `Source/Verse/HediffGiver_AddSeverity.cs` | 36 |
| `HediffComp_GiveHediff` | Verse | `Source/Verse/HediffComp_GiveHediff.cs` | 28 |
| `HediffComp_GiveHediffsInRange` | Verse | `Source/Verse/HediffComp_GiveHediffsInRange.cs` | 54 |
| `HediffComp_GiveHediffLungRot` | Verse | `Source/Verse/HediffComp_GiveHediffLungRot.cs` | 32 |
| `HediffComp_ReplaceHediff` | Verse | `Source/Verse/HediffComp_ReplaceHediff.cs` | 43 |
| `HediffComp_RemoveIfOtherHediff` | Verse | `Source/Verse/HediffComp_RemoveIfOtherHediff.cs` | 32 |
| `HediffComp_ReactOnDamage` | Verse | `Source/Verse/HediffComp_ReactOnDamage.cs` | 29 |
| `HediffComp_DestroyOrgan` | RimWorld | `Source/RimWorld/HediffComp_DestroyOrgan.cs` | 39 |
| `HediffComp_Immunizable` | Verse | `Source/Verse/HediffComp_Immunizable.cs` | — |
| `HediffStage` | Verse | `Source/Verse/HediffStage.cs` | 148 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初始版本：5大类Hediff交互机制详解（HediffGiver系统8子类、HediffComp交互9个Comp、生命周期回调、Stage层级触发、8种典型交互模式），全部经RAG源码验证 | Claude Opus 4.6 |
