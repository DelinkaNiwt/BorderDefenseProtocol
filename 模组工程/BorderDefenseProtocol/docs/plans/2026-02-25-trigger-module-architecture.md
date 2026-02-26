---
标题：触发器模块架构设计
版本号: v2.1
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][未完成][未锁定]
摘要: BDP触发器模块（BDP.Trigger）的架构设计文档。重写自旧版6.2(v1.5)。以CompTriggerBody为编排器，描述6个子系统的全局结构和概要。v2.1填充了§3.1槽位与状态机系统、§3.2芯片效果系统的详细设计。
---

# 触发器模块架构设计

## 前置文档

- BDP模组总体架构与微内核设计（2026-02-25版）

## 设计原则（按优先级）

1. **KISS** — 能简单就不复杂
2. **SRP** — 每个类只干一件事
3. **OCP** — 新增芯片类型不改现有代码
4. **DIP** — 依赖抽象（IChipEffect），不依赖具体效果类
5. **数据驱动** — 变体通过XML Def配置，不硬编码
6. **YAGNI** — 不为假想需求增加复杂度
7. **零Harmony Patch** — 完全通过继承和接口实现，最大化模组兼容性

---

## 1. 模块定位

触发器模块是BDP总体架构（微内核+多模块）中的**第一层消费者模块**，仅依赖微内核（CompTrion能量系统），与角色模块、设施模块之间零依赖。

```
                    ┌───────────────┐
                    │   微 内 核     │
                    │  BDP.Core     │
                    │  CompTrion    │
                    └───────┬───────┘
                            │ 唯一依赖
                            ▼
              ┌─────────────────────────────┐
              │     触发器模块 BDP.Trigger    │
              │                             │
              │  "第一层消费者，零跨模块依赖"  │
              └─────────────────────────────┘
```

三种触发器（BORDER / 近界 / 黑触发器）共享同一套CompTriggerBody代码，仅通过XML Props配置产生完全不同的玩家体验。

---

## 2. 架构总览

模块内部以**CompTriggerBody为中心编排器**，围绕6个子系统组织：

```
┌─ BDP.Trigger 模块 ──────────────────────────────────────────────┐
│                                                                  │
│  ┌──────── CompTriggerBody (编排器) ──────────────────────────┐ │
│  │  · 管理槽位列表（左手/右手/特殊）                           │ │
│  │  · 按侧独立状态机（Idle/WindingDown/WarmingUp）             │ │
│  │  · IVerbOwner实现 + Verb缓存                               │ │
│  │  · Gizmo生成 + 事件注册                                    │ │
│  └──────────────────────┬────────────────────────────────────┘ │
│                          │ 委托/协作                             │
│         ┌────────────────┼────────────────┐                     │
│         ▼                ▼                ▼                     │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐          │
│  │ ① 槽位与    │ │ ② 芯片效果  │ │ ③ 双武器与      │          │
│  │   状态机    │ │   系统      │ │   攻击系统      │          │
│  │             │ │             │ │                 │          │
│  │ ChipSlot    │ │ IChipEffect │ │ DualVerb        │          │
│  │ SwitchCtx   │ │ Weapon/     │ │ Compositor      │          │
│  │ 激活流程    │ │ Shield/     │ │ Verb_BDP*       │          │
│  │             │ │ Utility     │ │ JobDriver_BDP*  │          │
│  └─────────────┘ └─────────────┘ └────────┬────────┘          │
│                                            │ 发射               │
│                                            ▼                    │
│                                   ┌─────────────────┐          │
│                                   │ ④ PMS弹道       │          │
│                                   │   模块系统      │          │
│                                   │                 │          │
│                                   │ Bullet_BDP(薄壳)│          │
│                                   │ 7阶段管线       │          │
│                                   │ Guided/Explosion│          │
│                                   │ /Trail模块      │          │
│                                   └─────────────────┘          │
│                                                                  │
│  ┌─────────────┐                  ┌─────────────────┐          │
│  │ ⑤ UI系统    │                  │ ⑥ 存档与        │          │
│  │             │                  │   生命周期      │          │
│  │ Gizmo状态条 │                  │                 │          │
│  │ 槽位窗口    │                  │ 序列化策略      │          │
│  │ 攻击Command │                  │ 读档恢复        │          │
│  └─────────────┘                  │ 生命周期事件    │          │
│                                   └─────────────────┘          │
│                                                                  │
└──────────────────────────┬─────────────────────────────────────┘
                            │ 依赖
                            ▼
                 BDP.Core — CompTrion (微内核/能量系统)
```

### 子系统职责一览

| # | 子系统 | 核心类 | 职责 |
|---|--------|--------|------|
| ① | 槽位与状态机 | ChipSlot, SwitchContext | 管理芯片的装载/卸载/激活/关闭，按侧独立的三态切换 |
| ② | 芯片效果 | IChipEffect, Weapon/Shield/UtilityChipEffect | 策略模式实现芯片行为：武器注入Verb、护盾添加Hediff、辅助授予Ability |
| ③ | 双武器与攻击 | DualVerbCompositor, Verb_BDP*, JobDriver_BDP* | 合成双武器Verb配置，自定义近战/远程攻击执行链 |
| ④ | PMS弹道模块 | Bullet_BDP, IBDPProjectileModule, Pipeline/* | 管线化弹道系统：薄壳宿主+可组合模块（引导/爆炸/拖尾） |
| ⑤ | UI | Gizmo_TriggerBodyStatus, Window_TriggerBodySlots, Command_BDPChipAttack | 状态显示、槽位管理窗口、攻击Gizmo |
| ⑥ | 存档与生命周期 | CompTriggerBody (PostExposeData/Notify_*) | 序列化、读档恢复、装备/卸下事件处理 |

### 三种触发器的Props差异

| 配置项 | BORDER触发器 | 近界触发器 | 黑触发器 |
|--------|-------------|-----------|---------|
| leftHandSlotCount | ≥1 | 0 | 0 |
| rightHandSlotCount | ≥1 | 0 | 0 |
| specialSlotCount | ≥0 | ≥1 | ≥1 |
| allowChipManagement | true | false | false |
| preloadedChips | 无 | 有（出厂预装） | 有（出厂预装） |
| switchCooldownTicks | 可配置 | N/A（无手槽） | N/A（无手槽） |

---

## 3. 子系统概要

> 以下各节为概要级描述。每个子系统的详细设计（数据结构、完整流程、边界情况）将在后续会话中逐层填充。填充时在对应章节内扩展即可，不影响其他章节。

### 3.1 槽位与状态机系统

**核心概念**：CompTriggerBody持有三组槽位列表（左手/右手/特殊），每个槽位是一个ChipSlot数据容器。左右手各侧同时只能激活1个芯片，切换时经过三态状态机。

```
数据模型：

CompTriggerBody
├── leftHandSlots: List<ChipSlot>     左手槽位列表
├── rightHandSlots: List<ChipSlot>    右手槽位列表
├── specialSlots: List<ChipSlot>      特殊槽位列表
├── leftSwitchCtx: SwitchContext      左手切换上下文（null=Idle）
├── rightSwitchCtx: SwitchContext     右手切换上下文（null=Idle）
└── dualHandLockSlot: ChipSlot       双手武器锁定（非null时另一侧不可激活）
```

**三态状态机**（每侧独立）：

```
  ┌───────┐  有旧芯片且deactivationDelay>0  ┌──────────────┐
  │ Idle  │ ────────────────────────────────→│ WindingDown  │
  │(null) │                                   │ (旧芯片后摇) │
  └───────┘                                   └──────┬───────┘
      ↑                                              │ 后摇到期
      │  前摇到期                                     ▼
      │  TryResolveSideSwitch()                ┌──────────────┐
      └────────────────────────────────────────│  WarmingUp   │
                                               │ (新芯片前摇) │
                                               └──────────────┘

  无旧芯片或deactivationDelay=0 → 直接进入WarmingUp
  两侧互不阻塞：左手切换时右手可同时操作
```

**关键设计决策**：切换冷却采用**懒求值**（在IsSwitching等属性访问时检查并结算），而非CompTick主动轮询。原因：RimWorld引擎中装备后的武器CompTick()不会被调用。

**特殊槽**：全量操作（ActivateAllSpecial / DeactivateAllSpecial），不参与切换状态机。

#### 3.1.1 数据结构定义

```
ChipSlot（数据容器，实现IExposable）
├── index: int          槽位在本侧列表中的索引（0-based）
├── side: SlotSide      所属侧别
├── loadedChip: Thing   已装载的芯片物品（null=空槽）
└── isActive: bool      该芯片是否当前激活

SlotSide枚举
├── LeftHand    左手槽（参与切换状态机）
├── RightHand   右手槽（参与切换状态机）
└── Special     特殊槽（全量操作，不参与状态机）

SwitchContext（按侧独立的切换上下文，实现IExposable）
├── phase: SwitchPhase          当前阶段（WindingDown / WarmingUp）
├── phaseEndTick: int           阶段结束的游戏tick
├── targetSlotIndex: int        待激活目标槽位索引（-1=纯关闭，无后续前摇）
├── windingDownSlotIndex: int   正在后摇的旧槽位索引（仅WindingDown有效）
├── warmupDuration: int         前摇总时长tick（供UI进度条计算）
└── winddownDuration: int       后摇总时长tick（供UI进度条计算）
```

**CompTriggerBody中的槽位相关字段**：

```
CompTriggerBody
├── leftHandSlots: List<ChipSlot>       左手槽位列表
├── rightHandSlots: List<ChipSlot>      右手槽位列表（hasRightHand=false时为null）
├── specialSlots: List<ChipSlot>        特殊槽位列表（specialSlotCount=0时为null）
├── leftSwitchCtx: SwitchContext        左手切换上下文（null=Idle）
├── rightSwitchCtx: SwitchContext       右手切换上下文（null=Idle）
└── dualHandLockSlot: ChipSlot         双手武器锁定引用（非null=另一侧被锁定）
```

**懒初始化**：槽位列表在首次访问时才创建（EnsureSlotsInitialized），兼容CharacterEditor等外部工具在PostSpawnSetup之前访问属性的场景。

#### 3.1.2 完整激活流程

```
ActivateChip(side, slotIndex) — 外部入口
│
├─ 前置检查：CanActivateChip()
│   ├─ ① IsCombatBodyActive == true？（不变量⑬）
│   ├─ ② 槽位有芯片？（loadedChip != null）
│   ├─ ③ Trion Available ≥ activationCost？
│   ├─ ④ OutputPower ≥ minOutputPower？
│   ├─ ⑤ 双手锁定检查（dualHandLockSlot != null 且不是本槽位 → 拒绝）
│   ├─ ⑥ 互斥标签检查（exclusionTags交集非空 → 拒绝）
│   └─ ⑦ effect.CanActivate()？
│
├─ Special侧？→ 委托ActivateAllSpecial()（遍历所有特殊槽，逐个DoActivate）
│
├─ 本侧正在切换中？→ 拒绝（IsSideSwitching == true）
│
├─ 双手芯片？→ 特殊路径：
│   ├─ 两侧都不在切换中？
│   ├─ 关闭对侧激活芯片
│   ├─ 关闭本侧激活芯片（若不同）
│   └─ 直接DoActivate（跳过状态机，无前摇）
│
├─ 有旧芯片（existingActive != null 且 != slot）？
│   │
│   ├─ deactivationDelay > 0
│   │   └─ 进入WindingDown（旧芯片仍isActive=true，不变量⑭）
│   │      phaseEndTick = now + winddown
│   │
│   └─ deactivationDelay == 0
│       └─ 立即DeactivateSlot(旧) → 进入WarmingUp
│          cooldown = max(switchCooldownTicks, activationWarmup)
│          phaseEndTick = now + cooldown
│
└─ 无旧芯片
    └─ 直接进入WarmingUp
       cooldown = max(switchCooldownTicks, activationWarmup)
       cooldown == 0 → 立即DoActivate + ctx=null
```

**懒求值结算**（TryResolveSideSwitch）：

```
UI每帧访问IsSwitching / IsSideSwitching / GetSideSwitchProgress
    │
    └─ TryResolveSideSwitch(ref ctx, side)
        │
        ├─ ctx == null → 返回（Idle）
        ├─ now < phaseEndTick → 返回（未到期）
        │
        ├─ WindingDown到期：
        │   ├─ DeactivateSlot(旧芯片)
        │   ├─ targetSlotIndex < 0？→ ctx=null（纯关闭，回到Idle）
        │   └─ targetSlotIndex ≥ 0？→ 进入WarmingUp
        │       cooldown = max(switchCooldownTicks, newChip.warmup)
        │       cooldown == 0 → 立即DoActivate + ctx=null
        │
        └─ WarmingUp到期：
            ├─ CanActivateChip？→ DoActivate(目标槽位)
            └─ ctx = null（回到Idle）
```

**DoActivate内部流程**：

```
DoActivate(slot)
    ├─ Consume(activationCost)           一次性Trion消耗
    ├─ RegisterDrain(drainPerDay)        注册持续消耗
    ├─ 设置临时上下文：ActivatingSide = slot.side, ActivatingSlot = slot
    ├─ try: effect.Activate(pawn, parent)
    │   └─ WeaponChipEffect → SetSideVerbs → RebuildVerbs
    │      ShieldChipEffect → AddHediff
    │      UtilityChipEffect → GainAbility
    ├─ finally: 清除临时上下文
    ├─ slot.isActive = true
    ├─ isDualHand？→ dualHandLockSlot = slot
    └─ TryGrantComboAbility()            检查组合能力
```

#### 3.1.3 核心不变量

| # | 不变量 | 维护者 | 违反后果 |
|---|--------|--------|---------|
| ① | 每侧激活芯片数 ≤ 1（左右手槽） | ActivateChip | 双Verb冲突 |
| ② | 已装载芯片数 ≤ 该侧槽位数 | LoadChip | 数据溢出 |
| ③ | hasRightHand==false时rightHandSlots为null | PostSpawnSetup | 空引用 |
| ④ | SwitchCtx非null时phase为WindingDown或WarmingUp | ActivateChip/TryResolve | 状态机卡死 |
| ⑤ | SwitchCtx为null时该侧处于Idle | TryResolveSideSwitch | 逻辑一致性 |
| ⑥ | isActive==true的槽位loadedChip!=null | ChipSlot.ExposeData | 空引用 |
| ⑦ | allowChipManagement==false时loadedChip不可被玩家修改 | LoadChip/UnloadChip | 近界/黑触发器被篡改 |
| ⑧ | dualHandLockSlot!=null时另一侧不可激活 | CanActivateChip | 双手武器语义破坏 |
| ⑨ | specialSlots全部同时激活/关闭 | ActivateAllSpecial/DeactivateAllSpecial | 部分激活不一致 |
| ⑩ | specialSlotCount==0时specialSlots为null | PostSpawnSetup | 空列表浪费 |
| ⑪ | 特殊槽不可单独操作 | ActivateChip路由 | 绕过全量约束 |
| ⑫ | activationWarmup对特殊槽无效 | ActivateAllSpecial | 战斗体生成时立即可用 |
| ⑬ | IsCombatBodyActive==false时不可激活任何芯片 | CanActivateChip | 无战斗体时芯片不应输出 |
| ⑭ | WindingDown阶段旧芯片仍isActive=true | ActivateChip | 后摇期间效果持续 |

#### 3.1.4 边界情况

| 场景 | 行为 | 原因 |
|------|------|------|
| 装备后CompTick不被调用 | 切换冷却采用懒求值：IsSwitching等属性访问时检查phaseEndTick并结算。UI每帧重绘自然触发。 | RimWorld引擎限制：Pawn_EquipmentTracker.EquipmentTrackerTick()只调用VerbsTick()，不调用CompTick() |
| 读档后槽位恢复 | PostLoadInit中：①重建缺失槽位列表 ②遍历所有isActive槽位重新调用effect.Activate() ③重建dualHandLockSlot ④重新注册OnAvailableDepleted事件 | IChipEffect无状态设计使幂等恢复成为可能 |
| 双手芯片激活 | 跳过状态机，先关闭对侧和本侧旧芯片，直接DoActivate。设置dualHandLockSlot锁定双侧。 | 双手武器语义：两侧必须同步，不允许异步切换 |
| 切换中途Trion耗尽 | CompTrion.OnAvailableDepleted事件 → OnTrionDepleted() → DismissCombatBody() → DeactivateAll() | 事件驱动，无需轮询 |
| 触发体从装备卸下掉到地面 | PostSpawnSetup(respawningAfterLoad: false)被调用，但leftHandSlots != null守卫防止重新初始化覆盖已装载芯片 | GenSpawn.Spawn触发PostSpawnSetup |
| ChipSlot读档时loadedChip为null但isActive为true | ExposeData.PostLoadInit中自动修正isActive=false | 防御性校验，维护不变量⑥ |
| cooldown为0的芯片 | WarmingUp阶段立即结算：DoActivate + ctx=null，无可见延迟 | 支持"瞬间切换"芯片设计 |

### 3.2 芯片效果系统（IChipEffect）

**核心概念**：策略模式。CompTriggerBody通过IChipEffect接口委托芯片行为，新增芯片类型只需实现接口+写XML，不改现有代码。

```
策略模式结构：

  CompTriggerBody (编排器)
       │ 委托
       ▼
  IChipEffect (策略接口)
  ├── Activate(pawn, triggerBody)
  ├── Deactivate(pawn, triggerBody)
  ├── Tick(pawn, triggerBody)
  └── CanActivate(pawn, triggerBody)
       │
       ├── WeaponChipEffect    → 注入Verb/Tool配置，触发RebuildVerbs
       ├── ShieldChipEffect    → 添加/移除护盾Hediff
       └── UtilityChipEffect   → 授予/移除Ability
```

**IChipEffect实现约定**：
- 无参构造函数（通过Activator.CreateInstance实例化）
- 无状态（不持有实例字段，所有上下文通过参数传入）
- 幂等（Activate可安全重复调用，用于读档恢复）

**三种策略与RimWorld机制映射**：

| 策略类 | RimWorld机制 | Activate | Deactivate |
|--------|-------------|----------|------------|
| WeaponChipEffect | IVerbOwner + VerbTracker | 写入侧别Verb/Tool → RebuildVerbs | 清除侧别配置 → RebuildVerbs |
| ShieldChipEffect | HediffDef | AddHediff(shieldHediffDef) | RemoveHediff |
| UtilityChipEffect | AbilityDef | GainAbility(abilityDef) | RemoveAbility |

**Trion消耗两层模型**：

```
┌─ 共有维度（CompProperties_TriggerChip，编排器统一处理）──────┐
│  activationCost   — 激活时一次性消耗                         │
│  allocationCost   — 激活期间占用量（降低Trion可用上限）       │
│  drainPerDay      — 激活期间持续消耗/天                      │
└──────────────────────────────────────────────────────────────┘

┌─ 效果专属维度（DefModExtension，各效果类自行处理）───────────┐
│  trionCostPerShot         — 武器：每次射击消耗               │
│  trionCostPerDamageFactor — 护盾：受击Trion消耗倍率          │
│  trionCostPerUse          — 辅助：每次使用消耗               │
└──────────────────────────────────────────────────────────────┘
```

**组合能力系统**：左右手同时激活特定芯片组合时，自动授予额外Ability。通过ComboAbilityDef（继承Def）在XML中配置组合规则，匹配对称（AB=BA）。

#### 3.2.1 IChipEffect完整协议

```
IChipEffect接口（扩展点）
├── Activate(pawn, triggerBody)       激活：芯片开始输出效果
├── Deactivate(pawn, triggerBody)     关闭：停止效果，清理所有副作用
├── Tick(pawn, triggerBody)           每tick逻辑（可选，默认空实现）
└── CanActivate(pawn, triggerBody)    前置条件检查（供UI灰显）
```

**三大设计约定**：

| 约定 | 含义 | 原因 |
|------|------|------|
| 无参构造 | 通过Activator.CreateInstance实例化 | XML配置chipEffectClass类名，运行时反射创建 |
| 无状态 | 不持有实例字段，所有上下文通过参数传入 | 同一IChipEffect实例可服务多个芯片；简化序列化 |
| 幂等 | Activate()可安全重复调用 | 读档恢复时对所有isActive槽位重新调用Activate() |

**实例化链路**：

```
XML定义芯片ThingDef
    └─ <li Class="BDP.Trigger.CompProperties_TriggerChip">
           <chipEffectClass>BDP.Trigger.WeaponChipEffect</chipEffectClass>
       </li>
            │
            ▼
TriggerChipComp.GetEffect()（懒加载，缓存到effectInstance字段）
    └─ Activator.CreateInstance(Props.chipEffectClass)
            │
            ▼
IChipEffect实例（全局唯一，被该芯片的TriggerChipComp持有）
```

**上下文传递机制**：

IChipEffect的Activate/Deactivate只接收pawn和triggerBody两个参数。效果类需要知道"自己在哪一侧"和"自己的芯片是哪个"时，通过CompTriggerBody的临时上下文获取：

```
CompTriggerBody
├── ActivatingSide: SlotSide?    当前操作侧别（DoActivate/DeactivateSlot中设置）
├── ActivatingSlot: ChipSlot     当前操作槽位（同上）
└── GetChipExtension<T>()        从当前槽位读取DefModExtension，回退遍历所有激活槽位

调用时序：
  ActivatingSide = slot.side
  ActivatingSlot = slot
  try { effect.Activate(pawn, parent); }
  finally { ActivatingSide = null; ActivatingSlot = null; }
```

#### 3.2.2 三种策略实现细节

**WeaponChipEffect**（最复杂）：

```
Activate流程：
  ① 读取WeaponChipConfig（DefModExtension，挂在芯片ThingDef上）
  ② verbProperties为null且tools非空？
     └─ 是：SynthesizeMeleeVerbProps()
        · 从tools[0].capacities查找ManeuverDef → DamageDef
        · 创建最小化VerbProperties（verbClass=Verb_BDPMelee）
        · 设置burstShotCount和ticksBetweenBurstShots（近战连击参数）
        · 原因：DualVerbCompositor需要VerbProperties来识别此侧为近战
  ③ SetSideVerbs(side, verbs, tools) — 写入CompTriggerBody的按侧存储
  ④ RebuildVerbs(pawn) — 触发VerbTracker重建 + 芯片Verb手动创建

Deactivate流程：
  ① ClearSideVerbs(side) — 清除本侧Verb/Tool数据
  ② RebuildVerbs(pawn) — 重建（此时该侧无数据，芯片Verb缓存清空）

关键设计决策：
  · 为什么用Verb_BDPMelee而非Verb_MeleeAttackDamage？
    标准类的DamageInfosToApply用EquipmentSource.def（触发体）作为weapon，
    Verb_BDPMelee用currentChipDef（芯片）作为weapon。
    若用标准类，伤害日志永远显示"触发体"而非芯片名称。
```

**ShieldChipEffect**：

```
Activate: pawn.health.AddHediff(cfg.shieldHediffDef)
Deactivate: pawn.health.RemoveHediff(找到的第一个匹配Hediff)

配置类：ShieldChipConfig（DefModExtension）
├── shieldHediffDef: HediffDef    护盾Hediff定义
└── trionCostPerDamageFactor: float  每点伤害的Trion消耗倍率（默认1）

Trion消耗由Hediff内部处理（效果专属层），不在ShieldChipEffect中。
```

**UtilityChipEffect**：

```
Activate: pawn.abilities.GainAbility(cfg.abilityDef)
Deactivate: pawn.abilities.RemoveAbility(cfg.abilityDef)

配置类：UtilityChipConfig（DefModExtension）
└── abilityDef: AbilityDef    授予的能力定义

适用场景：蜘蛛丝、蚱蜢跳跃等"点击→选目标→执行"的一次性动作。
Ability自带Gizmo（RimWorld原版机制），无需额外UI处理。
```

**扩展新芯片类型的步骤**：

```
① 创建新类实现IChipEffect（无参构造、无状态、幂等）
② 创建配置DefModExtension（如有专属参数）
③ XML中定义芯片ThingDef：
   · CompProperties_TriggerChip.chipEffectClass = 新类全限定名
   · 挂载配置DefModExtension
④ 无需修改CompTriggerBody或任何现有代码（OCP）
```

#### 3.2.3 Trion消耗模型

```
┌─ 共有层（CompProperties_TriggerChip定义，编排器统一处理）─────────────┐
│                                                                        │
│  activationCost    一次性消耗                                          │
│  ├─ 时机：DoActivate()开头                                            │
│  ├─ 方式：CompTrion.Consume(activationCost)                           │
│  └─ 前置：CanActivateChip中检查Available ≥ activationCost             │
│                                                                        │
│  allocationCost    占用/锁定量                                         │
│  ├─ 时机：战斗体激活时（由战斗模块调用CompTrion.Allocate）             │
│  ├─ 效果：降低Available上限（cur不变，但可用量减少）                   │
│  └─ 释放：DismissCombatBody时Release(allocated)                       │
│                                                                        │
│  drainPerDay       持续消耗/天                                         │
│  ├─ 注册：DoActivate中RegisterDrain("chip_{side}_{index}", drainPerDay)│
│  ├─ 结算：CompTrion.CompTick()每drainSettleInterval聚合结算            │
│  └─ 注销：DeactivateSlot中UnregisterDrain("chip_{side}_{index}")      │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘

┌─ 效果专属层（各DefModExtension定义，各效果类/Hediff/Ability自行处理）──┐
│                                                                        │
│  武器：WeaponChipConfig.trionCostPerShot                               │
│  ├─ 时机：每次射击（Verb_BDPRangedBase.TryCastShot内）                │
│  └─ 方式：CompTrion.Consume(trionCostPerShot)                         │
│                                                                        │
│  护盾：ShieldChipConfig.trionCostPerDamageFactor                       │
│  ├─ 时机：受击时（护盾Hediff内部处理）                                │
│  └─ 方式：CompTrion.Consume(damage × factor)                          │
│                                                                        │
│  辅助：trionCostPerUse（预留，当前未实现）                             │
│  ├─ 时机：每次使用Ability时                                           │
│  └─ 方式：CompTrion.Consume(trionCostPerUse)                          │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

**消耗时序全景**：

```
  时间轴 ──────────────────────────────────────────────────→

  战斗体激活
  │  Allocate(Σ allocationCost)     ← 锁定Trion
  │
  ├─ 芯片A激活
  │  │  Consume(activationCost_A)   ← 一次性
  │  │  RegisterDrain(drain_A)      ← 持续开始
  │  │
  │  ├─ 射击 → Consume(perShot)    ← 效果专属
  │  ├─ 射击 → Consume(perShot)
  │  │
  │  └─ 芯片A关闭
  │     UnregisterDrain(drain_A)    ← 持续停止
  │
  ├─ 芯片B激活 ...
  │
  └─ 战斗体解除
     DeactivateAll()
     Release(allocated)             ← 释放锁定
```

#### 3.2.4 组合能力系统

```
ComboAbilityDef（继承Def，XML数据驱动）
├── chipA: ThingDef       组合所需芯片A
├── chipB: ThingDef       组合所需芯片B
└── abilityDef: AbilityDef  组合成功后授予的Ability

匹配规则：
  · 对称：Matches(A,B) == Matches(B,A)
  · 实现：(a==chipA && b==chipB) || (a==chipB && b==chipA)
  · 同一对芯片只匹配一个ComboAbilityDef
```

**授予/撤销流程**：

```
DoActivate(slot)
    └─ TryGrantComboAbility(pawn)
        ├─ 左右手都有激活芯片？
        ├─ 遍历DefDatabase<ComboAbilityDef>.AllDefs
        ├─ 匹配且未授予？→ GainAbility + 加入grantedCombos列表
        └─ 不匹配 → 跳过

DeactivateSlot(slot)
    └─ TryRevokeComboAbilities(pawn)
        ├─ 遍历grantedCombos（倒序，安全删除）
        ├─ 左右手芯片仍满足匹配？→ 保留
        └─ 不满足？→ RemoveAbility + 从列表移除

运行时跟踪：
  grantedCombos: List<ComboAbilityDef>
  · 不序列化（读档时由激活恢复逻辑重建：所有isActive槽位重新Activate → TryGrant）
  · 原因：Ability本身由Pawn.abilities管理和序列化，grantedCombos只是查询缓存
```

**Gizmo显示**：组合Ability使用RimWorld原版Ability Gizmo机制，自动出现在Pawn的Gizmo栏中。Command_BDPChipAttack中通过"combo:defName"格式的attackId为组合技能生成独立Gizmo（若需要自定义外观）。

### 3.3 双武器与攻击系统

这是触发器模块中最复杂的子系统。武器类芯片需要在RimWorld的Verb/Tool体系中注入自定义攻击行为，同时避免被引擎的多条自动路径错误拾取。

**Verb隔离方案**：芯片Verb不在VerbTracker.AllVerbs中，而是由CompTriggerBody手动创建和缓存。原因：AllVerbs中的Verb会被引擎两条路径自动拾取（近战选择池、Y按钮绑定），hasStandardCommand=false无法阻止。

**Verb继承体系**：

```
Verb (RimWorld)
├── Verb_LaunchProjectile
│   └── Verb_Shoot
│       └── Verb_BDPRangedBase (abstract)     BDP远程基类
│           ├── Verb_BDPShoot                  单发远程
│           ├── Verb_BDPVolley                 齐射（同tick多发）
│           ├── Verb_BDPDualRanged             双侧交替连射
│           └── Verb_BDPDualVolley             双侧齐射
│
└── Verb_MeleeAttackDamage
    └── Verb_BDPMelee                         近战（hitIndex状态机+burst）
```

**DualVerbCompositor组合规则**（纯静态工具类）：

```
输入：左侧Verb/Tool + 右侧Verb/Tool

                 ┌─ 仅一侧有值 ──→ 该侧Verb(isPrimary)
                 │
ComposeVerbs() ──┼─ 近战+近战 ───→ [左Verb, 右Verb*, DualMeleeVerb(isPrimary)]
                 │
                 ├─ 远程+远程 ───→ [左Verb, 右Verb*, DualRangedVerb(isPrimary)]
                 │
                 └─ 近战+远程 ───→ [近战Verb, 远程Verb(isPrimary)]

                 * 相同芯片时不重复添加
                 所有Verb统一设 hasStandardCommand=false
```

**自定义JobDriver**：远程和近战各有自定义JobDriver，直接使用job.verbToUse（不重新查找），并在tickAction中手动调用VerbTick()推进burst计时器。

**Gizmo生成**：Command_BDPChipAttack继承Command_VerbTarget，重写GroupsWith()基于attackId控制合并（而非原版的ownerThing.def），使每个芯片攻击模式有独立Gizmo。

<!-- 详细设计插入点：双武器与攻击系统 -->

### 3.4 PMS弹道模块系统

PMS（Projectile Module System）是触发器模块的弹道子系统，采用**管线化架构**。投射物宿主（Bullet_BDP）是薄壳，不含业务逻辑，功能由可组合的模块提供。

**管线架构图**：

```
┌─ Bullet_BDP (薄壳宿主) ─────────────────────────────────────┐
│                                                               │
│  modules: List<IBDPProjectileModule>                          │
│  （由BDPModuleFactory根据ThingDef上的DefModExtension自动创建） │
│                                                               │
│  每tick按管线顺序分发：                                        │
│                                                               │
│  ┌──────────────┐                                             │
│  │ PathResolver  │ → 修改destination（追踪/锁定/折线弹道）     │
│  │ Priority: 10  │                                             │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  ┌──────────────┐                                             │
│  │SpeedModifier │ → 修改速度（预留）                           │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  ┌──────────────┐                                             │
│  │ 引擎位置计算  │ → base.TickInterval()                       │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  ┌──────────────┐                                             │
│  │Intercept     │ → 修饰拦截判定（预留）                       │
│  │Modifier      │                                             │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  ┌──────────────┐                                             │
│  │Position      │ → 修饰显示位置                               │
│  │Modifier      │                                             │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  ┌──────────────┐                                             │
│  │TickObserver  │ → 只读观察（拖尾/音效）                      │
│  │ Priority: 100│                                             │
│  └──────┬───────┘                                             │
│         ▼                                                     │
│  到达检查 → ArrivalHandler → ImpactHandler                    │
│             (重定向/穿透)    (爆炸/分裂)                       │
│                                                               │
│  无管线参与者的阶段 = 零开销（空列表跳过）                      │
└───────────────────────────────────────────────────────────────┘
```

**模块工厂**：BDPModuleFactory在BDPMod静态构造函数中注册 DefModExtension→模块实例 的映射。Bullet_BDP.Launch()时自动创建所需模块。

**已实现模块**：

| 模块 | 优先级 | 管线接口 | 功能 |
|------|--------|---------|------|
| GuidedModule | 10 | IBDPArrivalHandler | 引导飞行（折线弹道），多段锚点+最终目标 |
| ExplosionModule | 50 | IBDPImpactHandler | 命中时执行爆炸效果 |
| TrailModule | 100 | IBDPTickObserver | 每tick创建拖尾线段，由BDPEffectMapComponent统一渲染 |

**配置类**（均为DefModExtension，挂在投射物ThingDef上）：

| 配置类 | 对应模块 | 关键字段 |
|--------|---------|---------|
| BDPGuidedConfig | GuidedModule | 锚点数、散布、飞行速度 |
| BDPExplosionConfig | ExplosionModule | 爆炸半径、伤害类型、伤害量 |
| BeamTrailConfig | TrailModule | 拖尾颜色、宽度、持续时间 |

**扩展方式**：新增弹道行为只需——①实现IBDPProjectileModule+关心的管线接口、②创建配置DefModExtension、③在BDPMod中注册工厂映射、④XML中给投射物ThingDef挂上配置。不修改Bullet_BDP或现有模块。

<!-- 详细设计插入点：PMS弹道模块系统 -->

### 3.5 UI系统

触发器模块的UI由三个组件构成：

**Gizmo_TriggerBodyStatus**（内联状态条，75×75像素）：

```
┌──────────────────────────────┐
│  [左芯片图标]                 │  ← 四态显示
│  ████████░░  左手切换进度条   │  ← 切换中时显示
│  [右芯片图标]                 │
│  ████████░░  右手切换进度条   │
│  点击 → 打开槽位管理窗口      │
└──────────────────────────────┘
```

**四态视觉系统**：

| 状态 | 条件 | 视觉 |
|------|------|------|
| 激活 | isActive == true | 芯片图标 + 绿色边框 |
| 注册未激活 | 有芯片 && 战斗体激活 && !isActive | 芯片图标 + 蓝色边框 |
| 挂载未注册 | 有芯片 && !战斗体激活 | 50%透明图标 + 暗黄边框 |
| 空 | 无芯片 | 灰色占位框 |

**Window_TriggerBodySlots**（浮动窗口）：展示所有槽位详细状态，支持点击激活/关闭。布局根据是否有特殊槽自适应（两列/三列）。

**Command_BDPChipAttack**（攻击Gizmo）：仅征召时显示。按attackId控制合并（独立芯片=chipDef.defName，双手触发="dual:A+B"，组合技能="combo:defName"）。支持齐射模式（右键拦截）和引导弹多步锚点瞄准（左键/右键拦截）。

<!-- 详细设计插入点：UI系统 -->

### 3.6 存档与生命周期

**序列化策略**：

| 数据类别 | 序列化方式 | 说明 |
|---------|-----------|------|
| 槽位列表 | Scribe_Collections (LookMode.Deep) | ChipSlot实现IExposable |
| 切换上下文 | Scribe_Deep | SwitchContext (IExposable)，null=Idle |
| 战斗体激活状态 | Scribe_Values | 跨存档保持 |
| Verb缓存/Verb配置 | 不序列化 | 读档后从激活状态重建 |
| 组合能力/双手锁定 | 不序列化 | 读档后从激活状态重建 |

**读档恢复**：IChipEffect是无状态的，读档后对所有isActive==true的槽位重新调用Activate()幂等重建。

**生命周期事件**：

| 事件 | 行为 |
|------|------|
| PostSpawnSetup | 初始化槽位数组；处理preloadedChips预装 |
| Notify_Equipped | 恢复已激活芯片效果；注册OnAvailableDepleted事件 |
| Notify_Unequipped | 注销事件；关闭所有激活芯片效果 |
| PostDestroy | 清理所有激活效果 |

<!-- 详细设计插入点：存档与生命周期 -->

---

## 4. 关键设计决策记录

| # | 决策 | 选择 | 理由 |
|---|------|------|------|
| D1 | 芯片Verb隔离 | 脱离VerbTracker，手动创建缓存 | AllVerbs中的Verb被引擎两条路径自动拾取，hasStandardCommand无法阻止 |
| D2 | 切换冷却结算 | 懒求值（属性访问时结算） | 装备后CompTick()不被调用；UI每帧重绘自然触发 |
| D3 | 武器数据存储 | DefModExtension（WeaponChipConfig） | 避免ThingDef.IsWeapon误判（检查verbs/tools非空） |
| D4 | 攻击执行 | 自定义JobDriver | 原版JobDriver会重新查找verb（返回触发体"柄"）且不调用VerbTick() |
| D5 | Gizmo合并 | Command_BDPChipAttack按attackId | 原版按ownerThing.def合并，同一触发体所有Verb只显示1个 |
| D6 | 零Harmony Patch | 继承CompEquippable + IVerbOwner + 自定义Verb/JobDriver | 最大化模组兼容性 |
| D7 | 三种触发器统一 | 共享CompTriggerBody，Props差异化 | 核心逻辑相同，差异仅在槽位数量和权限 |
| D8 | PMS架构 | 管线化薄壳+可组合模块 | 新增弹道行为不改Bullet_BDP；无参与者的阶段零开销 |
| D9 | 模块工厂 | DefModExtension→模块实例映射 | 数据驱动，XML配置决定弹道行为组合 |
| D10 | 近战burst | hitIndex状态机+引擎burst驱动 | 替代for循环同步多击，利用引擎原生burst机制 |

---

## 后续会话填充计划

本文档v2.0为全局结构层，v2.1填充了§3.1和§3.2。剩余子系统将在后续会话中逐层填充：

| 会话 | 填充内容 | 插入位置 |
|------|---------|---------|
| 会话2 | 槽位系统详细 + 芯片效果系统详细 | §3.1 / §3.2 | ✅ v2.1已填充 |
| 会话3 | 双武器系统详细（Verb创建、burst机制、JobDriver流程） + UI系统详细 | §3.3 / §3.5 插入点 |
| 会话4 | PMS弹道系统详细（管线分发、各模块实现） + 存档详细 | §3.4 / §3.6 插入点 |

每次填充时，在对应的`<!-- 详细设计插入点 -->`处扩展内容即可。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v2.0 | 2026-02-25 | 从旧版6.2(v1.5)全面重写。概念层重写：去除C#代码片段，改用架构图和概念描述。新增PMS弹道模块系统（§3.4）。新增齐射Verb和引导飞行概要。重组为6个子系统结构，预留详细设计插入点。精选10条核心设计决策。 | Claude Opus 4.6 |
| v2.1 | 2026-02-25 | 填充§3.1槽位与状态机系统详细设计（数据结构定义、完整激活流程、14条核心不变量、7个边界情况）。填充§3.2芯片效果系统详细设计（IChipEffect完整协议、三种策略实现细节、两层Trion消耗模型、组合能力系统）。更新填充计划状态。 | Claude Opus 4.6 |

