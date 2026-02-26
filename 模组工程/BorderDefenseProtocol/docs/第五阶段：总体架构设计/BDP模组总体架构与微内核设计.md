---
标题：BDP模组总体架构与微内核设计
版本号: v1.0
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: BDP模组的总体架构设计与Trion能量微内核详细设计。合并原5.1总体架构和6.1 Trion能量系统两份文档。覆盖模块划分、依赖关系、微内核API与数据模型、Pawn/Building适配、GUI、事件机制、数据流、模块间通信、开发路线图。概念层为主，图表丰富。
---

# BDP模组总体架构与微内核设计

## 1. 设计目标与约束

### 1.1 设计目标

| 目标 | 含义 |
|------|------|
| 独立开发与测试 | 各模块可独立开发、独立测试，不必等其他模块完成 |
| 可扩展性 | 新增子系统（新芯片类型、新触发器变体）无需修改核心代码 |
| 可维护性 | 清晰的代码组织，新人或AI能快速理解各部分职责 |
| 实用主义(YAGNI) | 架构复杂度与实际需求匹配，不为假想需求增加复杂度 |

### 1.2 约束条件

- RimWorld模组编译为**单个DLL**，"模块"是逻辑划分（命名空间+文件夹）
- 必须遵循RimWorld的Def/Comp/Worker/Tracker设计模式
- 模组依赖Biotech DLC作为前置
- Trion能量系统的技术选型已确定并实现：CompTrion(ThingComp)为核心

---

## 2. 架构全景

### 2.1 同心环图

从最直觉的角度理解整体结构——内核在中心，模块按依赖层级向外展开。
同一环内的模块之间互不依赖。

```
                        ┌─────────────────────────────────┐
                        │          第二层（外环）           │
                        │                                 │
                        │   ┌───────────────────────┐     │
                        │   │      第一层（内环）     │     │
                        │   │                       │     │
                        │   │   ┌───────────────┐   │     │
                        │   │   │               │   │     │
                        │   │   │   微 内 核     │   │     │
                        │   │   │               │   │     │
                        │   │   │  Trion能量系统 │   │     │
                        │   │   │  BDP.Core     │   │     │
                        │   │   │               │   │     │
                        │   │   └───────────────┘   │     │
                        │   │                       │     │
                        │   │  触发器  角色  设施    │     │
                        │   │  ✅     🔲    🔲      │     │
                        │   └───────────────────────┘     │
                        │                                 │
                        │       战斗 🔲      世界 🔲      │
                        │                                 │
                        └─────────────────────────────────┘

  ✅ = 已实现    🔲 = 规划中
```

### 2.2 模块职责一览

| 模块 | 命名空间 | 层级 | 一句话职责 | 状态 |
|------|---------|------|-----------|------|
| 微内核 | BDP.Core | 中心 | 通用Trion能量容器+恢复驱动+Stat聚合+GUI | ✅ 已实现 |
| 触发器 | BDP.Trigger | L1 | 触发体装备、芯片槽位管理、双武器合成、弹道系统 | ✅ 已实现 |
| 角色 | BDP.Character | L1 | Trion天赋基因变体、触发角植入、副作用特性/能力 | 🔲 规划中 |
| 设施 | BDP.Facility | L1 | 制造设施、Trion炮台、Trion防卫墙 | 🔲 规划中 |
| 战斗 | BDP.Combat | L2 | 战斗体状态切换、伤害隔离、弱点系统、紧急脱离 | 🔲 规划中 |
| 世界 | BDP.World | L2 | BORDER派系、敌对惑星国家、袭击事件、门传送 | 🔲 规划中 |

---

## 3. 微内核：Trion能量系统

微内核是所有模块的共同依赖，提供Trion能量的存储、消耗、恢复、占用等基础能力。

### 3.1 组件职责图

```
┌─────────────────────────────────────────────────────────┐
│                    微内核 (BDP.Core)                      │
│                                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │  核心层                                          │    │
│  │  CompTrion — 通用能量容器+恢复驱动+事件通知       │    │
│  │  CompProperties_Trion — XML可配置参数             │    │
│  └──────────────────────┬──────────────────────────┘    │
│                          │                               │
│  ┌───────────┬───────────┼───────────┬──────────────┐   │
│  │           │           │           │              │   │
│  ▼           ▼           ▼           ▼              ▼   │
│  Gene_       Gizmo_      StatDef ×3  BDPDef         适配 │
│  TrionGland  TrionBar    (聚合层)    Extension      约定 │
│  (配置器)    (资源条GUI)              (通用扩展)    (B/I)│
│                                                          │
│  └─────── Pawn适配层 ────────┘                           │
└─────────────────────────────────────────────────────────┘
```

| 组件 | 职责（一句话） |
|------|--------------|
| CompTrion | 通用Trion数据容器+恢复驱动+聚合消耗结算+事件通知，唯一数据源 |
| CompProperties_Trion | XML可配置参数（容量、恢复间隔、消耗间隔、颜色等） |
| Gene_TrionGland | 向Stat贡献天赋基础值（statOffsets），初始化时刷新max。零运行时开销 |
| Gizmo_TrionBar | 底部Gizmo栏三段色资源条（占用→可用→空），继承Gizmo_Slider |
| StatDef ×3 | TrionCapacity / TrionOutputPower / TrionRecoveryRate，多源聚合 |
| BDPDefExtension | 通用DefModExtension，为任意Def类型提供BDP扩展字段 |

### 3.2 核心数据模型

CompTrion持有3个核心字段，构成Trion能量的Single Source of Truth：

```
核心字段：
  float cur          当前Trion总值
  float max          当前最大容量
  float allocated    被占用量（芯片注册锁定）

状态标志：
  bool frozen        恢复是否被冻结（由外部系统设置）

聚合消耗：
  Dictionary<string, float> drainRegistry    持续消耗源注册表

事件：
  Action OnAvailableDepleted    Available从>0降至≤0时触发
```

**不变量**（任何操作后必须成立）：

```
① 0 ≤ cur ≤ max
② 0 ≤ allocated ≤ cur
③ max ≥ 0

推论：Available = cur - allocated ≥ 0
```

**状态空间图**：

```
日常状态（frozen=false, allocated=0）：
┌─────────────────────────────────────────┐
│  ████████████████████████  │  空闲      │
│  Available = cur            │  max-cur  │
│  全部可用，CompTick()驱动恢复│           │
└─────────────────────────────────────────┘
0                            cur         max

战斗体状态（frozen=true, allocated>0）：
┌─────────────────────────────────────────┐
│  ██████████│  ████████████│  ░░░░░░░  │
│  占用(锁定) │  可用(可消耗) │  已消耗    │
└─────────────────────────────────────────┘
0          alloc          cur          max
```

### 3.3 API概览

CompTrion对外暴露的核心操作，所有模块通过这些接口与Trion交互：

```
┌─ 消耗/恢复 ──────────────────────────────────────────────┐
│                                                           │
│  Consume(amount) → bool                                   │
│    从可用量消耗。Available不足时返回false。                  │
│    调用者：触发器（芯片使用）、战斗（维持消耗）、设施（运作） │
│                                                           │
│  Recover(amount)                                          │
│    恢复Trion。frozen时静默忽略（不抛异常）。                │
│    调用者：CompTick()自驱动恢复、Building补给              │
│                                                           │
├─ 占用/释放 ──────────────────────────────────────────────┤
│                                                           │
│  Allocate(amount) → bool                                  │
│    锁定占用量。Available不足时返回false。                   │
│    调用者：战斗模块（战斗体激活时锁定）                     │
│                                                           │
│  Release(amount)                                          │
│    释放占用量。防御性钳位（不超过allocated）。               │
│    调用者：战斗模块（主动解除时释放）                       │
│                                                           │
├─ 强制操作 ───────────────────────────────────────────────┤
│                                                           │
│  ForceDeplete()                                           │
│    强制归零：cur=0, allocated=0。                          │
│    调用者：战斗模块（被动破裂路径）                         │
│                                                           │
├─ 状态控制 ───────────────────────────────────────────────┤
│                                                           │
│  SetFrozen(bool)     设置恢复冻结状态                     │
│  RefreshMax()        从Stat系统重新读取max值               │
│                                                           │
├─ 聚合消耗 ───────────────────────────────────────────────┤
│                                                           │
│  RegisterDrain(key, drainPerDay)   注册持续消耗源          │
│  UnregisterDrain(key)              移除持续消耗源          │
│  TotalDrainPerDay { get }          所有源的聚合消耗速率    │
│                                                           │
├─ 事件 ───────────────────────────────────────────────────┤
│                                                           │
│  Action OnAvailableDepleted                               │
│    Available从>0降至≤0时触发。                             │
│    触发点：Consume()、聚合消耗结算、RefreshMax()           │
│    订阅者：CompTriggerBody（Trion耗尽时自动解除战斗体）    │
│                                                           │
├─ 只读属性 ───────────────────────────────────────────────┤
│                                                           │
│  Cur / Max / Allocated / Available / Percent              │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

### 3.4 恢复驱动与聚合消耗

CompTrion.CompTick()承担三项职责：定期刷新max、聚合消耗结算、Pawn恢复驱动。

```
CompTick() 每tick执行：
    │
    ├─ 定期刷新max（每statRefreshInterval ticks，默认250≈4秒）
    │   └─ RefreshMax() → 从Stat系统读取最新max
    │
    ├─ 聚合消耗结算（每drainSettleInterval ticks，默认60≈1秒）
    │   └─ 有注册源时：totalDrain = Sum(drainRegistry)
    │      Consume(totalDrain × interval / 60000)
    │
    └─ Pawn恢复驱动（每recoveryInterval ticks，默认150）
        └─ 条件：parent是Pawn && max>0 && !frozen
           recoveryRate = pawn.GetStatValue(TrionRecoveryRate)
           Recover(recoveryRate × interval / 60000)
```

**惰性激活机制**：CompTrion通过XPath Patch预挂载到所有Human Pawn。没有Trion腺体基因时max=0，系统完全静默（不恢复、不显示Gizmo）。添加Gene后statOffsets使max>0，系统自动激活。

```
惰性激活链路：
  1. Patch预挂载 → 所有Human Pawn都有CompTrion（max=0，静默）
  2. 添加Gene_TrionGland → statOffsets使TrionCapacity>0
  3. CompTrion.max>0 → CompTick()开始恢复 + Gizmo显示
  4. 移除Gene → max回到0 → 系统回到静默
```

### 3.5 Pawn适配：Gene与Stat聚合

Gene_TrionGland是纯配置器——通过statOffsets向Stat系统贡献基础值，不持有任何Trion数据，零运行时开销。

**为什么用statOffsets而不是直接设置max？**

多个来源（基因、触发角植入、未来的训练系统）都可能贡献Trion容量。statOffsets让引擎自动聚合，无冲突。

```
Stat聚合流程（以TrionCapacity为例）：

  Gene_TrionGland.statOffsets    ──→ +80（天赋基础值）
  Hediff_TriggerHorn.statOffsets ──→ +20（植入增强）
  ──────────────────────────────────────
  引擎自动聚合                    = 100

  CompTrion.RefreshMax()
  → pawn.GetStatValue(TrionCapacity)
  → max = 100
```

三个自定义StatDef：

| StatDef | 用途 | 来源 | 消费者 |
|---------|------|------|--------|
| TrionCapacity | Trion最大容量 | Gene + Hediff | CompTrion.RefreshMax() |
| TrionOutputPower | Trion输出功率 | Gene + Hediff | 芯片激活前置检查 |
| TrionRecoveryRate | Trion恢复速率 | Gene | CompTrion.CompTick()恢复驱动 |

所有Stat通过RimWorld原生StatWorker计算，无需自定义StatWorker。

### 3.6 GUI：Gizmo_TrionBar

选中Pawn/Building时，在底部Gizmo栏显示Trion三段色资源条。继承Gizmo_Slider框架。

```
三段色资源条（从左到右）：

  ┌──────────────────────────────────────────────┐
  │  ▓▓▓▓▓▓▓▓████████████░░░░░░░░░░░░░░░░░░░░  │
  │  [占用段]  [可用段]    [空段]                  │
  │  暗青      明亮青绿    深色背景                │
  └──────────────────────────────────────────────┘

  日常状态（allocated=0）：占用段宽度=0，退化为双色条
  战斗体状态（allocated>0）：三段清晰可辨
```

**显示条件**：max > 0 且 Props.showGizmo = true。由CompTrion.CompGetGizmosExtra()提供（而非Gene），原因是Building/Item也有CompTrion，可复用同一套Gizmo。

**上帝模式调试按钮**（godMode + 拥有Trion腺体基因时显示）：Trion→0/50%/满、Trion±10、切换冻结、强制耗尽、刷新Max、输出注册表。

### 3.7 事件机制：OnAvailableDepleted

CompTrion新增`Action OnAvailableDepleted`事件，在Available从>0降至≤0时触发。

```
事件触发链路：

  CompTrion.Consume() / 聚合消耗结算 / RefreshMax()
      │
      ▼
  Available ≤ 0 且 之前 > 0？
      │ 是
      ▼
  OnAvailableDepleted?.Invoke()
      │
      ▼
  CompTriggerBody.OnTrionDepleted()
      └─ 若IsCombatBodyActive → DismissCombatBody()
```

**为什么用事件而非轮询？** 装备的CompTick()不被引擎调用，CompTriggerBody无法主动轮询。事件只在Available实际变化时触发，比每tick回调高效。事件字段不需要序列化（Notify_Equipped时重新注册）。

### 3.8 Building/Item适配约定

微内核只提供CompTrion和CompProperties_Trion。具体的适配器Comp由各功能模块实现，通过`parent.GetComp<CompTrion>()`获取能量池。

```
Building适配示例（Trion炮台，规划中）：

  Building_TurretGun
  ├── CompTrion              ← 微内核提供
  │   Props: baseMax=500, passiveDrainPerDay=10
  │
  └── CompTrionTurret        ← 设施模块提供
      射击时: GetComp<CompTrion>().Consume(perShotCost)
```

Building的Trion补给方式待设施模块详细设计时确定（类似CompRefuelable / Pawn手动充能 / Trion供能网络）。

---

## 4. 依赖关系

### 4.1 依赖矩阵

区分两种本质不同的耦合：

- **C#代码依赖**（紧耦合）：代码A直接import并调用代码B的类/方法。改B可能导致A编译失败。
- **XML Def引用**（松耦合）：Def A通过defName字符串引用Def B。仅改B的defName才影响A。

```
┌──────────┬──────────┬──────────┬──────────┬──────────┬──────┐
│ 依赖方→  │ 微内核   │ 触发器   │ 角色     │ 设施     │      │
│ ↓被依赖  │          │          │          │          │      │
├──────────┼──────────┼──────────┼──────────┼──────────┼──────┤
│ 触发器   │ C#       │    —     │    —     │    —     │ L1   │
├──────────┼──────────┼──────────┼──────────┼──────────┼──────┤
│ 角色     │ C#       │    —     │    —     │    —     │ L1   │
├──────────┼──────────┼──────────┼──────────┼──────────┼──────┤
│ 设施     │ C#       │    —     │    —     │    —     │ L1   │
├──────────┼──────────┼──────────┼──────────┼──────────┼──────┤
│ 战斗     │ C#       │ C# ★    │    —     │    —     │ L2   │
├──────────┼──────────┼──────────┼──────────┼──────────┼──────┤
│ 世界     │ C#       │    —     │ XML      │ XML      │ L2   │
└──────────┴──────────┴──────────┴──────────┴──────────┴──────┘

★ = 整个架构中唯一的跨模块C#硬依赖
    战斗体激活时需检查Pawn是否装备了触发体（CompTriggerBody）
```

**关键结论**：
- 第一层三个模块之间：零C#依赖，完全可并行开发
- 世界模块对角色/设施的依赖仅在XML层面
- 战斗→触发器是唯一需要跨模块C# import的依赖

### 4.2 Trion数据流图

从Trion能量的视角，追踪它在整个系统中的流转：

```
┌─────────────────────────────────────────────────────────────┐
│                     Trion能量数据流                            │
│                                                              │
│  ┌─────────┐                                                 │
│  │ 角色模块 │  Gene_TrionGland ──statOffsets──→ StatDef       │
│  │ (产生源) │  Hediff_TriggerHorn ──statOffsets──→ StatDef   │
│  └─────────┘                                    │            │
│                                                  ▼            │
│                              ┌──────────────────────────┐    │
│                              │        微内核             │    │
│                              │  Stat聚合 → CompTrion.max │    │
│                              │  CompTick(): 恢复驱动     │    │
│                              │  drainRegistry: 聚合消耗  │    │
│                              └─────────┬────────────────┘    │
│                                         │                     │
│            ┌────────────────────────────┼──────────────┐     │
│            ▼                            ▼              ▼     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  触发器模块   │  │  战斗模块     │  │  设施模块     │      │
│  │  Consume()   │  │  Allocate()  │  │  Consume()   │      │
│  │  RegisterDrain│  │  Consume()   │  │              │      │
│  │  cur ↓       │  │  allocated ↑ │  │  cur ↓       │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  图例: ↑ = 增加    ↓ = 减少    → = 数据流向                  │
└─────────────────────────────────────────────────────────────┘
```

所有对Trion的操作都通过CompTrion的统一API，没有任何模块绕过内核直接修改Trion值。

---

## 5. 模块间通信

模块之间不直接互相调用（除战斗→触发器外），而是通过微内核间接协作。

### 5.1 间接协作：芯片使用导致Trion耗尽→战斗体解除

```
  触发器模块              微内核(CompTrion)          战斗模块(规划中)
  ──────────            ────────────────          ──────────
       │                      │                       │
  芯片使用                    │                       │
       │──── Consume(30) ───→│                       │
       │                      │ cur: 35→5             │
       │                      │                       │
  芯片使用                    │                       │
       │──── Consume(10) ───→│                       │
       │                      │ cur: 5→0              │
       │                      │ Available ≤ 0         │
       │                      │                       │
       │                      │── OnAvailableDepleted →│
       │                      │                       │
       │                      │              检测到Trion耗尽
       │                      │              触发战斗体解除
       │                      │                       │

  关键：触发器模块不知道战斗模块的存在
       战斗模块不知道是谁消耗了Trion
       它们通过CompTrion的状态变化+事件间接协作
```

### 5.2 事件驱动：CompTriggerBody自动响应Trion耗尽

```
  CompTriggerBody                CompTrion
  ──────────────                ──────────
       │                            │
  Notify_Equipped()                 │
       │── 注册OnAvailableDepleted ─→│
       │                            │
       │    ... 正常使用 ...         │
       │                            │
       │                     Available降至≤0
       │←── OnTrionDepleted() ──────│
       │                            │
  DismissCombatBody()               │
       │── Release(allocated) ─────→│
       │                            │
  Notify_Unequipped()               │
       │── 注销OnAvailableDepleted ─→│
       │                            │
```

---

## 6. 未实现模块概要

以下模块处于规划阶段，概要描述其预期职责和依赖关系。具体的RimWorld框架映射（用哪个Def/Comp/Worker）需要后续逐模块深入分析后确定。

### 6.1 角色模块（BDP.Character）— L1，规划中

**职责**：定义Trion使用者的个体差异。

- Trion天赋基因变体（多个GeneDef，不同statOffsets值）
- 触发角植入体（HediffDef，贡献TrionCapacity）
- 副作用特性（TraitDef）和副作用能力（AbilityDef）

**依赖**：仅依赖微内核（C#）。与触发器/设施零依赖。

### 6.2 设施模块（BDP.Facility）— L1，规划中

**职责**：提供Trion相关的建筑和制造设施。

- 制造设施（工作台，消耗Trion运作）
- 母触发器（建筑）
- Trion炮台、Trion防卫墙

**依赖**：仅依赖微内核（C#）。

### 6.3 战斗模块（BDP.Combat）— L2，规划中

**职责**：管理战斗体的完整生命周期。

- 战斗体状态切换（Hediff_CombatBody）
- 伤害隔离逻辑（战斗体受伤不回传真身）
- 弱点系统
- 紧急脱离

**依赖**：微内核（C#）+ 触发器模块（C#，检查CompTriggerBody）。这是架构中唯一的跨模块C#硬依赖。

### 6.4 世界模块（BDP.World）— L2，规划中

**职责**：构建BDP的世界观和敌对势力。

- BORDER派系、敌对惑星国家
- 近界民、Trion兵（PawnKindDef）
- 袭击事件（IncidentDef）
- 门(Gate)传送

**依赖**：微内核（C#）+ 角色/设施模块（仅XML引用）。

---

## 7. 开发路线图

```
Phase 1 ─── 微内核（BDP.Core）                              ✅ 已完成
│            产出: CompTrion + Gene + Gizmo + Stat聚合
│            验证: Pawn有Trion能量条，恢复/消耗/占用正常
│
Phase 2 ─── 第一层（三个模块可并行，建议按此顺序）
│            │
│            ├─ 2a: 触发器模块                               ✅ 已完成
│            │      产出: 触发体装备、芯片槽位、双武器、PMS弹道
│            │      验证: 装备→芯片→攻击→弹道效果→Trion消耗
│            │
│            ├─ 2b: 角色模块                                 🔲 规划中
│            │      产出: Trion天赋差异、触发角植入、副作用
│            │
│            └─ 2c: 设施模块                                 🔲 规划中
│                   产出: 制造设施、Trion建筑
│
Phase 3 ─── 第二层（两个模块可并行）
             │
             ├─ 3a: 战斗模块                                 🔲 规划中
             │      前置: 触发器模块（C#依赖）
             │
             └─ 3b: 世界模块                                 🔲 规划中
                    前置: 角色/设施模块的Def已定义（XML引用）
```

---

## 8. 命名空间与文件结构

```
BDP/
├── Source/BDP/                      ← C#源码（编译为单个DLL）
│   ├── BDPMod.cs                   ← 模组入口（Harmony + PMS注册 + 引擎校验）
│   ├── Core/                       ← 微内核 (BDP.Core)
│   │   ├── Comps/                  CompTrion, CompProperties_Trion
│   │   ├── Genes/                  Gene_TrionGland
│   │   ├── Gizmos/                 Gizmo_TrionBar
│   │   ├── Defs/                   BDP_DefOf
│   │   └── BDPDefExtension.cs
│   │
│   └── Trigger/                    ← 触发器模块 (BDP.Trigger)
│       ├── Comps/                  CompTriggerBody, TriggerChipComp
│       ├── Data/                   ChipSlot, SwitchContext, WeaponChipConfig
│       ├── Interfaces/             IChipEffect
│       ├── Effects/                WeaponChipEffect, ShieldChipEffect, UtilityChipEffect
│       ├── DualWeapon/             DualVerbCompositor, Verb_BDP*, JobDriver_BDP*
│       ├── Projectiles/            Bullet_BDP, 模块, 配置, 管线接口
│       │   └── Pipeline/           IBDPPathResolver, IBDPImpactHandler, ...
│       └── UI/                     Command_BDPChipAttack, Gizmo_*, Window_*
│
├── 1.6/Defs/                       ← XML定义（镜像C#模块结构）
│   ├── Core/                       GeneDefs, StatDefs, HediffDefs
│   └── Trigger/                    ThingDefs(芯片/弹道/触发体), JobDefs, AbilityDefs
│
├── 1.6/Patches/                    ← XPath补丁
│   └── Patch_HumanlikePawn.xml     CompTrion注入
│
├── 1.6/Textures/                   ← 贴图资源
├── About/                          ← 模组元信息
└── docs/plans/                     ← 设计文档
```

---

## 9. 关键设计决策记录

| # | 决策 | 选择 | 理由 |
|---|------|------|------|
| D1 | 架构模式 | 微内核+多模块 | Trion是所有系统的共同依赖，天然适合作为内核 |
| D2 | 模块划分 | 5模块2层 | 依赖层级清晰，开发顺序自然 |
| D3 | 物理形态 | 命名空间+文件夹（单DLL） | RimWorld模组惯例，管理成本最低 |
| D4 | 跨模块通信 | 通过CompTrion间接协作+事件 | 解耦模块，触发器不需要知道战斗模块的存在 |
| D5 | CompTrion注入 | XPath Patch预挂载+惰性激活 | Comp必须在ThingDef.comps中；max>0判断统一 |
| D6 | Gene继承 | 继承Gene，不继承Gene_Resource | 避免两份数据源；Gene只做配置器 |
| D7 | 通知机制 | 轮询为主+OnAvailableDepleted事件 | 轮询符合RimWorld惯例；事件解决装备CompTick不调用的问题 |
| D8 | GUI方案 | Gizmo_TrionBar（三段色） | Trion是战斗资源非生理需求，应在Gizmo栏显示 |
| D9 | 持续消耗 | RegisterDrain聚合机制 | 多源统一结算避免浮点误差；可统一展示总消耗速率 |
| D10 | Stat聚合 | 原生StatWorker + showIfUndefined=false | 零额外C#代码；无基因时自动隐藏 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-25 | 合并原5.1总体架构(v1.3)和6.1 Trion能量系统(v1.7)两份文档。概念层重写：去除C#代码片段，改用伪代码和流程图。新增OnAvailableDepleted事件机制（§3.7/§5.2）。更新开发路线图标记已完成Phase。精简为10条核心设计决策。 | Claude Opus 4.6 |

