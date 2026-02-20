---
标题：Trion能量系统技术选型分析报告
版本号: v1.2
更新日期: 2026-02-17
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 以"CompTrion（ThingComp）为通用能量容器"为核心思想，系统分析RimWorld五大框架系统（Comp/Gene/Need/Hediff/Stat）在Trion能量内核中的职责分工、协作关系与选型依据。涵盖问题定义、候选方案对比、最终架构设计、生命周期映射、原版先例参考、UML类图与数据流图。
---

# Trion能量系统技术选型分析报告

## 1. 问题定义

### 1.1 Trion能量的本质

Trion是一种具有能量-物质二象性的生物能量，是整个World Trigger世界观的基石资源。它具有以下核心特性（详见"2.4_Trion能量全生命周期过程分析"）：

- **双维度模型**：容量（总量）和输出功率（行为阈值）是两个独立维度
- **三个派生属性**：Trion天赋(1a)、Trion输出功率(1b)、Trion恢复速率(1c)
- **完整生命周期**：产生→储存→增强→分配→消耗→结算→恢复→再循环
- **需要媒介转化**：Trion本身无法自行转化，必须通过触发器等媒介"编程"

### 1.2 核心挑战：Trion不仅属于人

Trion能量的载体不限于人类Pawn，至少包括以下四类实体：

| 载体类型 | RimWorld基类 | 举例 |
|---------|-------------|------|
| **人类队员** | `Pawn : ThingWithComps` | BORDER队员、近界民 |
| **Trion兵** | `Pawn : ThingWithComps`（类似机械体） | 侦察型/战斗型/自律型Trion兵 |
| **建筑设施** | `Building : ThingWithComps` | 穿梭机、固定炮台、防卫墙、训练设施 |
| **物品/武器** | `ThingWithComps` | 触发器（装备）、Trion武器、Trion弹药 |

这意味着Trion能量系统必须是**跨实体类型的通用系统**，不能被锁定在任何Pawn专属框架内。

### 1.3 选型目标

设计一个Trion能量内核架构，满足以下要求：

1. **通用性**：同一套核心API适用于Pawn、Building、Item所有载体类型
2. **语义正确性**：每个RimWorld框架系统只承担符合其语义的职责
3. **最小侵入性**：尽量复用引擎现有机制，减少重复造轮子
4. **可扩展性**：未来新增载体类型时无需修改核心层
5. **数据一致性**：Trion值有且仅有一个数据源（Single Source of Truth）

---

## 2. 候选系统分析

### 2.1 RimWorld五大框架系统概览

| 系统 | 基类 | 挂载对象 | 生命周期 | 数据性质 | Pawn专属？ |
|------|------|---------|---------|---------|-----------|
| **ThingComp** | `Verse.ThingComp` | ThingWithComps（Pawn/Building/Item） | 随宿主存在，CompTick()驱动 | 行为+数据容器 | **否（通用）** |
| **Gene** | `Verse.Gene` | Pawn（GeneTracker） | 随Pawn永久存在 | 先天属性 | **是** |
| **Need** | `RimWorld.Need` | Pawn（NeedsTracker） | 每150 ticks自动NeedInterval() | 持续变化的状态条 | **是** |
| **Hediff** | `Verse.Hediff` | Pawn（HealthTracker） | 可动态添加/移除 | 临时/永久健康状态 | **是** |
| **Stat** | `RimWorld.StatDef` | 无（按需计算） | 查询时实时聚合 | 只读最终数值 | 否（但主要用于Pawn） |

**关键发现**：Gene、Need、Hediff三个系统全部是**Pawn专属**——它们的管理器（GeneTracker、NeedsTracker、HealthTracker）只存在于Pawn类上。Building和Item上不存在这些Tracker，因此无法使用这三个系统。

**唯一的跨类型系统是ThingComp**。原因：

```
RimWorld继承链：
  Entity → Thing → ThingWithComps → Pawn      ← 有AllComps
                                  → Building   ← 有AllComps
                                  → (物品)     ← 有AllComps

ThingComp通过ThingWithComps.AllComps挂载，
因此Pawn、Building、Item都可以拥有ThingComp。
```

### 2.2 三种候选核心方案对比

#### 方案A：Gene_Resource为核心（Pawn中心方案）

```
Gene_TrionGland (Gene_Resource)  ← 数据持有者
  ├── cur, max 字段
  ├── GUI资源条 (GeneGizmo_Resource)
  └── 所有消耗/恢复直接操作Gene_Resource.Value
```

| 优点 | 缺点 |
|------|------|
| 原版Hemogen的成熟模式 | Gene是Pawn专属，Building/Item无法使用 |
| Gene_Resource内置资源条GUI | 需要为非Pawn实体另造一套完全独立的系统 |
| 与GeneDef的statOffsets天然集成 | 两套系统之间无法共享API，代码重复 |

**致命缺陷**：无法满足"通用性"要求。Building上的Trion炮台和Pawn上的Trion能量会是两套完全不同的代码，无法用统一的`thing.GetComp<CompTrion>()`访问。

#### 方案B：自定义Tracker为核心（完全自建方案）

```
TrionTracker (自定义类)  ← 数据持有者
  ├── 挂载到Pawn: 通过MapComponent或GameComponent管理
  ├── 挂载到Building: 同上
  └── 挂载到Item: 同上
```

| 优点 | 缺点 |
|------|------|
| 完全自由的设计空间 | 完全脱离引擎框架，需要自建全部基础设施 |
| 不受任何引擎限制 | 无法利用ThingComp的Tick/Gizmo/Stat集成 |
| | 存档/读档需要自行实现ExposeData |
| | 与引擎的Stat计算管线无法自然集成 |

**致命缺陷**：重复造轮子。ThingComp已经提供了Tick、Gizmo、Stat集成、ExposeData等全部基础设施，自建Tracker等于放弃这些。

#### 方案C：CompTrion（ThingComp）为核心（通用Comp方案）✓ 推荐

```
CompTrion (ThingComp)  ← 数据持有者
  ├── 可挂载到任何ThingWithComps
  ├── 利用ThingComp的全部基础设施
  └── Pawn专属系统(Gene/Need/Hediff)作为适配层读写CompTrion
```

| 优点 | 缺点 |
|------|------|
| 天然支持Pawn/Building/Item所有载体 | Gene_Resource的内置GUI不能直接复用（需自建） |
| 统一API：`thing.GetComp<CompTrion>()` | Pawn上需要额外的Gene/Need适配层 |
| 利用ThingComp的Tick/Gizmo/Stat/ExposeData | 比纯Gene_Resource方案多一层间接 |
| 与StatWorker天然集成（GetStatOffset/GetStatFactor） | |
| 原版CompRefuelable是成熟的建筑资源管理先例 | |

### 2.3 选型结论

**选择方案C：CompTrion（ThingComp）为核心**。

核心理由：**这是唯一能同时满足"通用性"和"最小侵入性"的方案**。ThingComp是RimWorld引擎中唯一跨越Pawn/Building/Item的组件系统，且自带完整的生命周期管理基础设施。

Gene/Need/Hediff不是被抛弃，而是**降级为Pawn专属的适配层**——它们各自承担符合自身语义的职责，但数据统一存储在CompTrion中。

---

## 3. 最终架构设计

### 3.1 分层架构

整个Trion能量系统分为三层：

```
┌─────────────────────────────────────────────────────────┐
│  核心层（Core Layer）                                     │
│  CompTrion : ThingComp                                   │
│  ─ 通用Trion容器，挂载到任何ThingWithComps                │
│  ─ 唯一数据源：cur / max / allocated                     │
│  ─ 统一API：Consume / Recover / Allocate / Release       │
│  ─ 不依赖任何Pawn专属系统                                 │
├─────────────────────────────────────────────────────────┤
│  适配层（Adapter Layer）                                  │
│  ─ Pawn适配：Gene_TrionGland + Need_Trion + Hediff系列   │
│  ─ Building适配：CompTrionTurret / CompTrionFueled 等     │
│  ─ Item适配：CompTrionWeapon 等                           │
│  ─ 各适配层读写核心层CompTrion，不直接持有Trion数据        │
├─────────────────────────────────────────────────────────┤
│  聚合层（Aggregation Layer）                              │
│  ─ StatDef：TrionCapacity / TrionOutputPower / ...       │
│  ─ 从Gene/Hediff/Comp多源聚合，提供只读最终值             │
│  ─ CompTrion在初始化和状态变化时读取Stat更新自身max        │
└─────────────────────────────────────────────────────────┘
```

### 3.2 核心层：CompTrion 详细设计

```csharp
// === 核心数据容器 ===
public class CompTrion : ThingComp
{
    // ── 核心数据字段 ──
    private float cur;          // 当前Trion值
    private float max;          // 当前最大容量
    private float allocated;    // 被占用量（芯片注册锁定）

    // ── 只读属性 ──
    public float Cur => cur;
    public float Max => max;
    public float Allocated => allocated;
    public float Available => cur - allocated;       // 可用量
    public float Percent => max > 0 ? cur / max : 0; // 百分比

    // ── 核心操作（所有消耗/恢复的统一入口）──
    public bool Consume(float amount);    // 消耗（从Available扣减）
    public void Recover(float amount);    // 恢复（增加cur，不超过max）
    public bool Allocate(float amount);   // 锁定占用量
    public void Release(float amount);    // 释放占用量（主动解除时）
    public void ForceDeplete();           // 强制耗尽（被动破裂时）

    // ── ThingComp生命周期 ──
    public override void CompTick();              // 可选的持续逻辑
    public override void PostExposeData();        // 存档/读档
    public override float GetStatOffset(StatDef); // 向Stat系统提供修改
    public override IEnumerable<Gizmo> CompGetGizmosExtra(); // GUI资源条
}
```

**CompProperties_Trion 配置字段**（XML可配置）：

| 字段 | 类型 | 说明 | 适用载体 |
|------|------|------|---------|
| `baseMax` | float | 基础最大容量（无Gene时的默认值） | 全部 |
| `startPercent` | float | 初始百分比（默认1.0=满） | 全部 |
| `passiveDrainPerDay` | float | 被动消耗/天（如建筑维持消耗） | Building |
| `canRecover` | bool | 是否可恢复（默认true） | 全部 |
| `recoveryPerDay` | float | 基础恢复速率/天（无Need时的默认值） | Building/Item |
| `showGizmo` | bool | 是否显示GUI资源条 | 全部 |
| `barColor` | Color | 资源条颜色 | 全部 |
| `depletedEffect` | EffecterDef | 耗尽时的视觉效果 | 全部 |

> **设计原则**：CompProperties_Trion提供所有载体类型的通用默认值。对于Pawn，Gene_TrionGland会在PostAdd()时覆盖这些默认值（如用Stat聚合值替换baseMax）。对于Building/Item，直接使用XML配置值。

### 3.3 Pawn适配层：五系统职责分工

#### 3.3.1 Gene_TrionGland（基因 → 配置器）

**语义**：Trion腺体是天生器官，Trion天赋是先天个体差异——这是Gene的语义域。

**职责**：配置CompTrion的初始参数 + 向Stat系统提供基础属性值。**不持有Trion数据**。

```
Gene_TrionGland : Gene
├── PostAdd()
│   └── 找到Pawn上的CompTrion → 设置max为Stat:TrionCapacity的值
├── GeneDef.statOffsets
│   ├── TrionCapacity: +N      （天赋对容量的基础贡献）
│   ├── TrionOutputPower: +N   （天赋对输出功率的基础贡献）
│   └── TrionRecoveryRate: +N  （天赋对恢复速率的基础贡献）
└── 不重写Tick()——无运行时逻辑
```

**为什么不继承Gene_Resource？**

Gene_Resource的设计假设是"Gene自身持有资源数据（cur/max）"。但在CompTrion架构中，数据在CompTrion里，Gene只是配置器。继承Gene_Resource会导致两份数据（Gene里一份、Comp里一份），需要额外的同步逻辑，违反Single Source of Truth原则。直接继承Gene基类更干净。

> **备选方案**：如果后续发现Gene_Resource的GeneGizmo_Resource UI复用价值很高，可以考虑继承Gene_Resource但将其cur/max代理到CompTrion。这是一个实现细节，不影响架构层面的决策。

#### 3.3.2 Need_Trion（需求 → 恢复驱动器 + GUI显示）

**语义**：Trion的日常恢复是一个"持续变化的内在状态"——这是Need的语义域。Need的自动NeedInterval()机制天然适合驱动恢复逻辑。

**职责**：驱动日常状态下的Trion恢复 + 在需求面板显示Trion状态条。**不持有Trion数据**。

```
Need_Trion : Need
├── CurLevel (属性重写)
│   └── get → 读取CompTrion.Percent（代理模式）
│   └── set → 写入CompTrion（如果需要外部设置）
├── MaxLevel (属性重写)
│   └── get → 1.0（百分比制）或 CompTrion.Max（绝对值制）
├── NeedInterval()
│   ├── if IsFrozen → return（战斗体状态下不恢复）
│   └── CompTrion.Recover(Stat:TrionRecoveryRate × 时间增量)
├── IsFrozen (属性重写)
│   └── base.IsFrozen || Pawn有Hediff_CombatBody
└── threshPercents → [0.1, 0.3]（危险线、临界线）
```

**为什么Need而不是在CompTrion.CompTick()里直接恢复？**

两个原因：
1. **语义正确性**：Need系统有完整的冻结机制（IsFrozen），天然支持"战斗体状态下停止恢复"的需求。在CompTick里实现需要自己写冻结逻辑。
2. **GUI集成**：Need自动出现在Pawn的需求面板中，玩家可以直观看到Trion状态。CompTrion的Gizmo是选中时才显示的按钮栏，不如Need面板直观。

**Need和CompTrion的Gizmo是否冲突？**

不冲突，互补。Need面板显示在Pawn信息栏的需求列表中（始终可见），CompTrion的Gizmo显示在选中Pawn时的底部按钮栏（可放置额外操作按钮）。两者显示位置不同，可以共存。对于Building/Item，没有Need面板，只有CompTrion的Gizmo。

#### 3.3.3 Hediff系列（健康差异 → 状态管理）

**语义**：战斗体是一个可激活/解除的临时状态，触发角是手术植入的器官——这些都是Hediff的语义域。

| Hediff | 类型 | 职责 | 与CompTrion的交互 |
|--------|------|------|------------------|
| **Hediff_CombatBody** | HediffWithComps | 战斗体状态本体 | 添加时触发CompTrion进入战斗模式 |
| **HComp_TrionAllocate** | HediffComp | 芯片注册、占用量管理 | CompTrion.Allocate() / Release() |
| **HComp_TrionDrain** | HediffComp | 维持消耗（存在税） | CompTrion.Consume() per tick |
| **HComp_TrionLeak** | HediffComp | 受伤流失消耗 | Notify_PawnPostApplyDamage → CompTrion.Consume() |
| **Hediff_TriggerHorn** | Hediff_Implant | 触发角植入体 | HediffStage.statOffsets修改TrionCapacity/OutputPower |
| **Hediff_TrionDepletion** | HediffWithComps | Trion耗尽的负面效果 | CompTrion.Percent低于阈值时自动添加 |

**战斗体Hediff的生命周期与CompTrion的交互**：

```
激活战斗体:
  添加Hediff_CombatBody
  → HComp_TrionAllocate.CompPostPostAdd()
    → 计算所有已装载芯片的占用量总和
    → CompTrion.Allocate(totalOccupied)
  → Need_Trion.IsFrozen = true（停止恢复）

战斗中:
  → HComp_TrionDrain.CompPostTick() → CompTrion.Consume(维持消耗/tick)
  → 芯片激活/使用 → CompTrion.Consume(激活消耗/使用消耗)
  → 受伤 → HComp_TrionLeak → CompTrion.Consume(流失消耗)

主动解除(路径A):
  移除Hediff_CombatBody
  → HComp_TrionAllocate.CompPostPostRemoved()
    → CompTrion.Release(allocated)  // 占用量返还
  → Need_Trion.IsFrozen = false（恢复开始）

被动破裂(路径B):
  Trion耗尽或弱点被毁
  → CompTrion.ForceDeplete()  // 强制耗尽，占用量不返还
  → 移除Hediff_CombatBody
  → Need_Trion.IsFrozen = false（从枯竭状态开始恢复）
```

#### 3.3.4 Stat聚合层（属性 → 多源聚合的只读接口）

**语义**：Trion的容量、输出功率、恢复速率都会被多个来源修改——这是Stat的语义域。

**自定义StatDef列表**：

| StatDef | 默认基础值 | 来源（Offsets/Factors） | 消费者 |
|---------|-----------|----------------------|--------|
| `TrionCapacity` | 0 | Gene_TrionGland(+基础) + Hediff_TriggerHorn(+植入) + 训练效果 | CompTrion.max |
| `TrionOutputPower` | 0 | Gene_TrionGland(+基础) + Hediff_TriggerHorn(+植入) | Ability系统前置条件检查（`abilityDef.minOutputPower`） |
| `TrionRecoveryRate` | 0 | Gene_TrionGland(+基础) + Hediff状态修改 | Need_Trion.NeedInterval() |

**Stat计算管线**（源码验证，StatWorker.GetValueUnfinalized；以下仅列出与Trion系统相关的主要来源，完整管线还包含skillNeedOffsets、capacityOffsets、Ideo/Precept、LifeStage、Stuff、Inspiration等来源）：

```
最终值 = 基础值
       + Σ Trait.statOffsets            // 特质加值
       + Σ Hediff.CurStage.statOffsets  // Hediff阶段加值（触发角在这里）
       + Σ Gene.def.statOffsets         // 基因加值（Trion天赋在这里）
       + Σ Apparel.statOffsets          // 装备加值（via StatOffsetFromGear）
       + Σ Equipment.statOffsets        // 武器加值（via StatOffsetFromGear）
       × Π Trait.statFactors            // 特质倍率
       × Π Hediff.CurStage.statFactors  // Hediff阶段倍率
       × Π Gene.def.statFactors         // 基因倍率
       // ── 以下在Stuff处理之后 ──
       + Σ Comp.GetStatOffset()         // Comp加值（黑触发器在这里）
       × Π Comp.GetStatFactor()         // Comp倍率
       → StatPart后处理
```

**注意**：Comp.GetStatOffset/GetStatFactor的调用位置在Trait、Hediff、Gene、Apparel、Equipment等Pawn专属源全部处理之后。这不影响功能——关键结论是Gene、Hediff、Comp都可以通过引擎原生机制向Stat贡献修改值，无需任何Harmony Patch。

### 3.4 Building/Item适配层

对于非Pawn载体，CompTrion直接作为数据持有者和行为驱动器，不需要Gene/Need/Hediff适配层。

#### Building适配

```
Building_TrionTurret : Building
├── CompTrion（核心能量池）
│   └── CompProperties_Trion: baseMax=500, passiveDrainPerDay=10
├── CompTrionTurret（射击消耗逻辑）
│   └── 射击时 → CompTrion.Consume(perShotCost)
└── 补给方式：
    ├── 方案1: 类似CompRefuelable，由Pawn搬运Trion燃料补给
    ├── 方案2: 连接到Trion供能网络（类似电力网络）
    └── 方案3: 由高Trion Pawn手动充能（Ability/Job）
```

#### Item/Weapon适配

```
Trion武器（ThingWithComps）
├── CompTrion（武器自身的能量池，如果有的话）
│   └── CompProperties_Trion: baseMax=50, canRecover=false
├── CompTrionWeapon（使用逻辑）
│   ├── 模式A: 消耗武器自身CompTrion（独立能量池）
│   └── 模式B: 消耗持有者的CompTrion（从Pawn身上扣）
│       └── parent.ParentHolder → Pawn → GetComp<CompTrion>()
└── 弹药耗尽时武器失效或降级
```

**模式A vs 模式B的选择**：取决于世界观设定。触发器芯片消耗的是队员自身的Trion（模式B），而某些独立Trion武器可能有自己的能量池（模式A）。CompTrionWeapon可以通过配置字段切换模式。

---

## 4. UML类图

### 4.1 核心层类图

```
┌─────────────────────────────────────────────────────────────────┐
│                     «RimWorld Engine»                            │
│                                                                  │
│  ┌──────────────┐         ┌──────────────────┐                  │
│  │ ThingWithComps│◄────────│ ThingComp        │                  │
│  │               │ 1    * │                  │                  │
│  │ +AllComps     │         │ +parent          │                  │
│  │ +GetComp<T>() │         │ +CompTick()      │                  │
│  └──┬───┬───┬───┘         │ +GetStatOffset() │                  │
│     │   │   │              │ +PostExposeData()│                  │
│     │   │   │              │ +CompGetGizmos() │                  │
│  Pawn Building Item        └────────┬─────────┘                  │
│                                     │ 继承                       │
└─────────────────────────────────────┼───────────────────────────┘
                                      │
┌─────────────────────────────────────┼───────────────────────────┐
│                     «Trion Mod»     │                            │
│                                     ▼                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ CompTrion : ThingComp                                     │   │
│  │ «核心层 - 通用Trion容器»                                   │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ - cur : float                                             │   │
│  │ - max : float                                             │   │
│  │ - allocated : float                                       │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ + Cur : float «get»                                       │   │
│  │ + Max : float «get»                                       │   │
│  │ + Allocated : float «get»                                 │   │
│  │ + Available : float «get» {cur - allocated}               │   │
│  │ + Percent : float «get» {cur / max}                       │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ + Consume(amount : float) : bool                          │   │
│  │ + Recover(amount : float) : void                          │   │
│  │ + Allocate(amount : float) : bool                         │   │
│  │ + Release(amount : float) : void                          │   │
│  │ + ForceDeplete() : void                                   │   │
│  │ + SetMax(newMax : float) : void                           │   │
│  │ + CompTick() : void «override»                            │   │
│  │ + PostExposeData() : void «override»                      │   │
│  │ + GetStatOffset(stat) : float «override»                  │   │
│  │ + CompGetGizmosExtra() : IEnumerable<Gizmo> «override»   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ CompProperties_Trion : CompProperties                     │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ + baseMax : float                                         │   │
│  │ + startPercent : float = 1.0                              │   │
│  │ + passiveDrainPerDay : float = 0                          │   │
│  │ + canRecover : bool = true                                │   │
│  │ + recoveryPerDay : float = 0                              │   │
│  │ + showGizmo : bool = true                                 │   │
│  │ + barColor : Color                                        │   │
│  │ + depletedEffect : EffecterDef                            │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 Pawn适配层类图

```
┌──────────────────────────────────────────────────────────────────────┐
│  «Pawn适配层»                                                        │
│                                                                      │
│  ┌─────────────────────┐    读写     ┌──────────────────────┐       │
│  │ Gene_TrionGland     │───────────→│ CompTrion            │       │
│  │ : Gene              │             │ (在同一个Pawn上)      │       │
│  ├─────────────────────┤             └──────────────────────┘       │
│  │ + PostAdd()         │                    ▲  ▲  ▲                 │
│  │   → 初始化max       │                    │  │  │                 │
│  │ + GeneDef:          │                    │  │  │                 │
│  │   statOffsets →     │    驱动恢复         │  │  │ 消耗            │
│  │   TrionCapacity     │  ┌─────────────────┘  │  └──────────┐     │
│  │   TrionOutputPower  │  │                    │             │     │
│  │   TrionRecoveryRate │  │                    │             │     │
│  └─────────────────────┘  │                    │             │     │
│                            │                    │             │     │
│  ┌─────────────────────┐  │  ┌─────────────────┴──────┐     │     │
│  │ Need_Trion          │──┘  │ Hediff_CombatBody      │     │     │
│  │ : Need              │     │ : HediffWithComps       │─────┘     │
│  ├─────────────────────┤     ├────────────────────────┤           │
│  │ + CurLevel          │     │ HediffComps:           │           │
│  │   → CompTrion.Pct   │     │ ┌────────────────────┐ │           │
│  │ + NeedInterval()    │     │ │HComp_TrionAllocate │ │           │
│  │   → CompTrion       │     │ │· Allocate/Release  │ │           │
│  │     .Recover()      │     │ └────────────────────┘ │           │
│  │ + IsFrozen          │     │ ┌────────────────────┐ │           │
│  │   → 战斗体时true    │     │ │HComp_TrionDrain   │ │           │
│  └─────────────────────┘     │ │· 维持消耗/tick     │ │           │
│                               │ └────────────────────┘ │           │
│  ┌─────────────────────┐     │ ┌────────────────────┐ │           │
│  │ Hediff_TriggerHorn  │     │ │HComp_TrionLeak    │ │           │
│  │ : Hediff_Implant    │     │ │· 受伤流失消耗      │ │           │
│  ├─────────────────────┤     │ └────────────────────┘ │           │
│  │ HediffStage:        │     └────────────────────────┘           │
│  │  statOffsets →      │                                          │
│  │  TrionCapacity      │     ┌────────────────────────┐           │
│  │  TrionOutputPower   │     │ Hediff_TrionDepletion  │           │
│  └─────────────────────┘     │ : HediffWithComps      │           │
│                               ├────────────────────────┤           │
│                               │ 当CompTrion.Percent    │           │
│                               │ 低于阈值时自动添加     │           │
│                               │ · 阶段性负面效果       │           │
│                               └────────────────────────┘           │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ «Stat聚合层»                                                  │   │
│  │                                                                │   │
│  │  StatDef:TrionCapacity ← Gene(+基础) + Hediff(+植入) + Comp  │   │
│  │  StatDef:TrionOutputPower ← Gene(+基础) + Hediff(+植入)      │   │
│  │  StatDef:TrionRecoveryRate ← Gene(+基础) + Hediff(+状态)     │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### 4.3 跨载体统一访问模式

```
┌──────────────────────────────────────────────────────────────────┐
│  «统一访问模式：thing.GetComp<CompTrion>()»                       │
│                                                                   │
│  调用方（任意代码）                                                │
│       │                                                           │
│       │  thing.GetComp<CompTrion>()                               │
│       ▼                                                           │
│  ┌─────────┐    ┌─────────────┐    ┌──────────┐    ┌──────────┐ │
│  │  Pawn   │    │  Building   │    │  Weapon  │    │ TrionSol │ │
│  │ (队员)  │    │ (炮台/穿梭机)│    │ (Trion枪)│    │ (Trion兵)│ │
│  ├─────────┤    ├─────────────┤    ├──────────┤    ├──────────┤ │
│  │CompTrion│    │CompTrion    │    │CompTrion │    │CompTrion │ │
│  │ cur=70  │    │ cur=450     │    │ cur=30   │    │ cur=200  │ │
│  │ max=100 │    │ max=500     │    │ max=50   │    │ max=300  │ │
│  │ alloc=30│    │ alloc=0     │    │ alloc=0  │    │ alloc=50 │ │
│  ├─────────┤    ├─────────────┤    ├──────────┤    ├──────────┤ │
│  │额外适配: │    │额外适配:     │    │额外适配:  │    │额外适配:  │ │
│  │Gene     │    │CompTrion    │    │CompTrion │    │(简化版   │ │
│  │Need     │    │  Turret     │    │  Weapon  │    │ Pawn适配)│ │
│  │Hediff   │    │CompTrion    │    │          │    │          │ │
│  │Stat     │    │  Fueled     │    │          │    │          │ │
│  └─────────┘    └─────────────┘    └──────────┘    └──────────┘ │
│                                                                   │
│  所有载体类型返回相同的CompTrion接口                               │
│  调用方无需知道载体是什么类型                                      │
└──────────────────────────────────────────────────────────────────┘
```

---

## 5. 原版先例参考

### 5.1 Hemogen（血源素）系统 — Pawn资源管理先例

| 组件 | 类 | 职责 | Trion对应 |
|------|-----|------|----------|
| 资源基因 | `Gene_Hemogen : Gene_Resource` | 持有cur/max，GUI资源条 | Gene_TrionGland（但降级为配置器） |
| 消耗源 | `Gene_HemogenDrain : IGeneResourceDrain` | 每日消耗 | HComp_TrionDrain |
| 低值效果 | `Hediff_HemogenCraving` | 血源素过低时的负面Hediff | Hediff_TrionDepletion |
| 补充方式 | `Gene_Bloodfeeder` + 进食回调 | 吸血/进食补充 | Need_Trion的恢复逻辑 |

**与Trion的关键差异**：Hemogen只存在于Pawn上，因此Gene_Resource作为数据持有者是合理的。Trion需要跨载体，所以数据持有者必须下沉到CompTrion。

### 5.2 CompRefuelable — Building资源管理先例

| 组件 | 类 | 职责 | Trion对应 |
|------|-----|------|----------|
| 燃料管理 | `CompRefuelable : ThingComp`（经ThingComp_VacuumAware） | fuel字段，消耗/补给逻辑 | CompTrion（核心层） |
| 配置 | `CompProperties_Refuelable` | fuelCapacity, fuelConsumptionRate等 | CompProperties_Trion |
| 补给Job | `JobDriver_Refuel` | Pawn搬运燃料补给 | 未来的Trion补给Job |
| GUI | `CompRefuelable.PostDraw()` | 燃料条绘制 | CompTrion.CompGetGizmosExtra() |

**借鉴价值**：CompRefuelable证明了ThingComp完全可以胜任资源管理职责——它管理fuel字段、处理消耗/补给、绘制GUI、集成存档系统，与CompTrion的需求高度一致。

### 5.3 Need_MechEnergy — 机械体能量先例

| 组件 | 类 | 职责 | Trion对应 |
|------|-----|------|----------|
| 能量Need | `Need_MechEnergy : Need` | 机械体能量条，自动消耗/恢复 | Need_Trion |
| 自动关机 | `SelfShutdown` Hediff | 能量归零时添加 | Hediff_TrionDepletion |
| 充电 | `Building_MechCharger` | 外部充电设施 | Trion补给设施 |

**借鉴价值**：Need_MechEnergy展示了Need如何驱动能量消耗/恢复循环，以及能量耗尽时如何通过Hediff触发状态变化。Need_Trion可以参考其IsFrozen和阈值触发机制。

### 5.4 CompPower — 建筑电力网络先例

| 组件 | 类 | 职责 | Trion对应 |
|------|-----|------|----------|
| 电力组件 | `CompPower / CompPowerTrader` | 建筑的电力消耗/产出 | 未来的Trion供能网络 |
| 电网 | `PowerNet` | 电力网络管理 | 未来的TrionNet（如果需要） |

**借鉴价值**：如果未来需要实现"Trion供能网络"（建筑之间共享Trion），CompPower的PowerNet架构是直接参考。但这属于远期扩展，当前阶段不需要。

---

## 6. 数据流图

### 6.1 Pawn日常状态数据流

```
每150 ticks:
  Need_Trion.NeedInterval()
    │
    ├── if IsFrozen → return
    │
    ├── recoveryRate = Pawn.GetStatValue(TrionRecoveryRate)
    │                    │
    │                    ├── Gene_TrionGland.def.statOffsets[TrionRecoveryRate]
    │                    ├── Hediff状态修改
    │                    └── 聚合为最终值
    │
    ├── amount = recoveryRate × (150 ticks / 2500 ticks per hour)
    │
    └── CompTrion.Recover(amount)
          │
          └── cur = Min(cur + amount, max)
```

### 6.2 Pawn战斗状态数据流

```
激活战斗体:
  AddHediff(Hediff_CombatBody)
    │
    ├── HComp_TrionAllocate.CompPostPostAdd()
    │     │
    │     ├── 遍历已装载芯片 → 计算总占用量
    │     └── CompTrion.Allocate(totalOccupied)
    │           └── allocated += totalOccupied
    │
    └── Need_Trion.IsFrozen → true（停止恢复）

每tick战斗中:
  HComp_TrionDrain.CompPostTick()
    │
    └── CompTrion.Consume(维持消耗率 × dt)
          │
          └── cur -= amount (从Available扣减)

芯片使用:
  Ability.Activate()
    │
    └── CompTrion.Consume(使用消耗)

受伤:
  HComp_TrionLeak.Notify_PawnPostApplyDamage()
    │
    ├── 计算流失量（基于伤害大小）
    └── 添加临时流失Hediff → 持续约1分钟 → CompTrion.Consume(流失/tick)

Trion耗尽检测:
  CompTrion.Consume() 返回后
    │
    ├── if Available <= 0
    │     └── 触发被动破裂流程（路径B）
    │
    └── if Percent < 阈值
          └── 添加/更新 Hediff_TrionDepletion
```

### 6.3 Building数据流

```
Building_TrionTurret:

每tick:
  CompTrion.CompTick()
    │
    └── if passiveDrainPerDay > 0
          └── CompTrion.Consume(passiveDrainPerDay / 60000)

射击时:
  CompTrionTurret → CompTrion.Consume(perShotCost)
    │
    └── if !HasEnough → 停止射击

补给时:
  Pawn执行补给Job → CompTrion.Recover(fuelAmount)
```

---

## 7. 五系统职责总结矩阵

| 系统 | 对Trion的职责 | 数据关系 | 适用载体 | 原版先例 |
|------|-------------|---------|---------|---------|
| **CompTrion** | 唯一数据源，通用容器 | **持有** cur/max/allocated | 全部 | CompRefuelable |
| **Gene** | 天生属性配置器 | **写入** CompTrion.max（初始化时） | Pawn | Gene_Hemogen |
| **Need** | 恢复驱动器 + GUI | **读写** CompTrion（代理模式） | Pawn | Need_MechEnergy |
| **Hediff** | 状态管理（战斗体/植入体/耗尽） | **读写** CompTrion（事件驱动） | Pawn | Hediff_HemogenCraving |
| **HediffComp** | 具体行为（占用/消耗/流失） | **写入** CompTrion（Tick/事件） | Pawn | HediffComp_SeverityPerDay |
| **Stat** | 多源聚合的只读属性 | **被读取**（CompTrion/Need读取Stat值） | Pawn | StatDef系统 |
| **ThingComp（其他）** | 载体专属行为（炮台/武器/燃料） | **读写** CompTrion | Building/Item | CompPowerTrader |

---

## 8. 风险与待决事项

### 8.1 已识别风险

| # | 风险 | 影响 | 缓解措施 |
|---|------|------|---------|
| 1 | Gene_Resource的GeneGizmo_Resource无法直接复用 | 需要自建Pawn上的Trion资源条Gizmo | 可参考GeneGizmo_Resource源码实现，或通过Need面板替代 |
| 2 | Need代理模式可能与某些原版Need逻辑冲突 | CurLevel的set行为可能被意外调用 | 重写CurLevel的set为空操作或转发到CompTrion |
| 3 | CompTrion在Pawn上与Gene_Resource并存可能造成混淆 | 开发者可能误用Gene_Resource的API | 文档明确说明：Gene_TrionGland不继承Gene_Resource |
| 4 | 战斗体Hediff的添加/移除时序需要精确控制 | 占用量分配/释放的时序错误可能导致数据不一致 | 在CompTrion中添加状态断言和日志 |

### 8.2 待决事项

| # | 事项 | 决策点 | 影响范围 |
|---|------|--------|---------|
| 1 | Gene_TrionGland是否继承Gene_Resource | 如果复用GeneGizmo_Resource的价值足够高，可以考虑继承并代理 | Pawn适配层 |
| 2 | Trion供能网络是否需要（类似PowerNet） | 取决于建筑之间是否需要共享Trion | Building适配层 |
| 3 | CompTrion的Gizmo样式设计 | 资源条的视觉设计、位置、交互方式 | 全部载体的UI |
| 4 | Trion兵（机械体类Pawn）是否需要完整的Gene/Need适配 | 取决于Trion兵的复杂度需求 | Pawn适配层 |

### 8.3 已决事项

| # | 事项 | 结论 | 理由 |
|---|------|------|------|
| 1 | 黑触发器的"持有期间永久扩容"如何与CompTrion.max交互 | **不需要多池模型**。黑触发器自身是Trion容器（拥有CompTrion），装备时将其Max/Cur合并到持有者的CompTrion中（`pawn.Max += bt.Max, pawn.Cur += bt.Cur`）；卸下时结算分离（`bt.Cur = Max(0, pawn.Cur - pawn自身Max)`）。扩容期间不区分Trion来源，只在卸下时做一次结算。原作"优先消耗扩容部分"的效果被自然实现——卸下时超出pawn自身Max的部分归还黑触发器。 | 方案简洁，完全在CompTrion框架内解决，不需要额外字段或多池消耗优先级逻辑。具体的装备/卸下同步机制（如Notify_Equipped/Notify_Unequipped）留待详细设计阶段。 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.2 | 2026-02-17 | 评审修正2项：(1)§3.3.4 TrionOutputPower消费者从"能力施展阈值判断"明确为"Ability系统前置条件检查"，TrionCapacity来源移除CompBlackTrigger（改由装备时直接合并CompTrion数值）；(2)§8.2待决事项#5（黑触发器扩容机制）移至新增§8.3已决事项，确认方案：黑触发器自身为Trion容器，装备时合并Max/Cur到持有者，卸下时结算分离，不需要多池模型 | Claude Opus 4.6 |
| v1.1 | 2026-02-15 | 精确性修正2项：(1)Stat计算管线标注从纯"源码验证"改为注明"仅列出与Trion相关的主要来源"，补充Comp.GetStatOffset/GetStatFactor的实际调用位置（在Pawn专属源之后），补充遗漏的来源类别；(2)CompRefuelable继承链修正为经ThingComp_VacuumAware继承ThingComp | Claude Opus 4.6 |
| v1.0 | 2026-02-15 | 初始版本：问题定义、三方案对比、CompTrion核心架构、五系统职责分工、Pawn/Building/Item适配层设计、UML类图（核心层+Pawn适配层+跨载体统一访问）、数据流图（日常/战斗/建筑）、原版先例参考（Hemogen/CompRefuelable/MechEnergy/CompPower）、风险与待决事项 | Claude Opus 4.6 |
