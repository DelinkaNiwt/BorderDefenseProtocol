---
标题：触发器模块架构设计
版本号: v2.2
更新日期: 2026-02-26
最后修改者: user
标签: [文档][用户未确认][未完成][未锁定]
摘要: BDP触发器模块（BDP.Trigger）的架构设计文档。重写自旧版6.2(v1.5)。以CompTriggerBody为编排器，描述6个子系统的全局结构和概要。
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

  #### 3.3.1 数据结构与Verb创建流程

  **CompTriggerBody中的按侧Verb存储**：

  CompTriggerBody（Verb相关字段）
  │
  ├─ 按侧原始数据（由WeaponChipEffect.SetSideVerbs写入，序列化）
  │   ├── leftHandActiveVerbProps:  List   左手芯片的Verb定义
  │   ├── rightHandActiveVerbProps: List   右手芯片的Verb定义
  │   ├── leftHandActiveTools:      List             左手芯片的近战Tool
  │   └── rightHandActiveTools:     List             右手芯片的近战Tool
  │
  ├─ Verb实例缓存（不序列化，由CreateAndCacheChipVerbs创建）
  │   ├── leftHandAttackVerb:   Verb
  左手攻击Verb（Verb_BDPShoot或Verb_BDPMelee）
  │   ├── rightHandAttackVerb:  Verb    右手攻击Verb
  │   ├── dualAttackVerb:       Verb
  双手合成攻击Verb（Verb_BDPDualRanged或Verb_BDPMelee）
  │   ├── leftHandVolleyVerb:   Verb    左手齐射Verb（Verb_BDPVolley）
  │   ├── rightHandVolleyVerb:  Verb    右手齐射Verb
  │   └── dualVolleyVerb:       Verb    双手齐射Verb（Verb_BDPDualVolley）
  │
  └─ 读档复用（仅序列化阶段使用）
      └── savedChipVerbs: List
  存档时收集所有芯片Verb，读档时通过loadID匹配复用

  **数据流向**：

  XML芯片ThingDef
    └─ WeaponChipConfig (DefModExtension)
         ├── verbProperties: List
         └── tools: List
              │
              │ WeaponChipEffect.Activate()
              │ → SetSideVerbs(side, verbs, tools)
              ▼
  CompTriggerBody 按侧原始数据
    ├── leftHandActiveVerbProps / leftHandActiveTools
    └── rightHandActiveVerbProps / rightHandActiveTools
              │
              │ RebuildVerbs(pawn)
              │   └─ CreateAndCacheChipVerbs(pawn)
              │       └─ DualVerbCompositor.ComposeVerbs()
              ▼
  合成后的 List
              │
              │ Activator.CreateInstance(vp.verbClass)
              │ 或 FindSavedVerb(loadID) 复用
              ▼
  6个Verb实例缓存（leftHandAttackVerb ... dualVolleyVerb）
              │
              │ CompGetEquippedGizmosExtra()
              ▼
  Command_BDPChipAttack Gizmo（传入verb + volleyVerb）

  **RebuildVerbs完整流程**（6步编排）：

  RebuildVerbs(pawn) — 由WeaponChipEffect.Activate/Deactivate触发
  │
  ├─ ① VerbTracker.InitVerbsFromZero()
  │     重建VerbTracker，只包含触发体ThingDef上的占位Verb
  │     和Tool产生的"柄"近战Verb
  │
  ├─ ② 遍历VerbTracker.AllVerbs，重新绑定verb.caster = pawn
  │     原因：InitVerbsFromZero后caster被重置为null
  │
  ├─ ③ CreateAndCacheChipVerbs(pawn)
  │     芯片Verb合成 + 创建 + 缓存（见下方详细流程）
  │
  └─ ④ [DevMode] 输出诊断日志
        列出所有VerbTracker.AllVerbs和芯片Verb缓存

  **CreateAndCacheChipVerbs内部步骤**：

  CreateAndCacheChipVerbs(pawn)
  │
  ├─ Step 1: 清空6个Verb缓存
  │   leftHandAttackVerb = rightHandAttackVerb = dualAttackVerb = null
  │   leftHandVolleyVerb = rightHandVolleyVerb = dualVolleyVerb = null
  │
  ├─ Step 2: 调用DualVerbCompositor.ComposeVerbs()
  │   输入：leftHandActiveVerbProps, rightHandActiveVerbProps,
  │         GetActiveOrActivatingSlot(LeftHand),
  GetActiveOrActivatingSlot(RightHand)
  │   输出：List composedVerbs
  │
  ├─ Step 3: 遍历composedVerbs，逐个创建Verb实例
  │   │
  │   │  对每个VerbProperties vp：
  │   │
  │   ├─ 3a. 生成loadID = "BDP_Chip_{parent.ThingID}_{index}"
  │   │
  │   ├─ 3b. 尝试复用：verb = FindSavedVerb(loadID)
  │   │       命中 → 跳过创建，直接使用已反序列化的实例
  │   │       未命中 → verb = Activator.CreateInstance(vp.verbClass)
  │   │
  │   ├─ 3c. 初始化Verb字段：
  │   │       verb.loadID = loadID
  │   │       verb.verbProps = vp
  │   │       verb.caster = pawn
  │   │       verb.verbTracker = this.VerbTracker  ← 使EquipmentSource指向触发体
  │   │
  │   └─ 3d. 按类型+侧别分配到缓存：
  │           ┌─────────────────────────┬──────────────────────────┐
  │           │ Verb类型                │ 分配目标                  │
  │           ├─────────────────────────┼──────────────────────────┤
  │           │ Verb_BDPMelee           │                          │
  │           │   label含LeftHand       │ → leftHandAttackVerb     │
  │           │   label含RightHand      │ → rightHandAttackVerb    │
  │           │   isPrimary=true(双手)  │ → dualAttackVerb         │
  │           ├─────────────────────────┼──────────────────────────┤
  │           │ Verb_BDPShoot           │                          │
  │           │   label含LeftHand       │ → leftHandAttackVerb     │
  │           │   label含RightHand      │ → rightHandAttackVerb    │
  │           ├─────────────────────────┼──────────────────────────┤
  │           │ Verb_BDPDualRanged      │ → dualAttackVerb         │
  │           └─────────────────────────┴──────────────────────────┘
  │
  └─ Step 4: CreateVolleyVerbs(pawn)
      创建齐射Verb（见下方）

  **CreateVolleyVerbs流程**：

  CreateVolleyVerbs(pawn)
  │
  ├─ 获取槽位：GetActiveOrActivatingSlot(side)
  │   注意：用GetActiveOrActivating而非GetActive
  │   原因：DoActivate中effect.Activate()触发RebuildVerbs时，
  │         slot.isActive尚未设为true，GetActive会返回null
  │
  ├─ 左手齐射检查：
  │   leftSlot有芯片？
  │   → WeaponChipConfig.supportsVolley == true？
  │   → leftHandAttackVerb != null？
  │   → 全部满足：CreateSingleVolleyVerb(Verb_BDPVolley, 源verb)
  │   → 赋值给leftHandVolleyVerb
  │
  ├─ 右手齐射检查：（同上逻辑）
  │   → 赋值给rightHandVolleyVerb
  │
  └─ 双手齐射检查：
      左右都支持齐射？
      → CreateSingleVolleyVerb(Verb_BDPDualVolley, dualAttackVerb)
      → 赋值给dualVolleyVerb

  CreateSingleVolleyVerb(verbClass, sourceVerb):
    ① CopyVerbProps(sourceVerb.verbProps) — 浅拷贝VerbProperties
    ② 修改verbClass = volleyVerbClass
    ③ 修改burstShotCount = 1（引擎只触发一次TryCastShot）
    ④ 生成loadID，尝试FindSavedVerb复用
    ⑤ 否则Activator.CreateInstance创建
    ⑥ 绑定caster/verbTracker

  **读档Verb复用**（简要）：

  芯片Verb脱离VerbTracker.AllVerbs，但Job/Stance的verbToUse字段在读档时通过Resol
  vingCrossRefs按loadID查找Verb实例。savedChipVerbs在PostExposeData中收集所有芯
  片Verb并序列化，读档后CreateAndCacheChipVerbs优先通过FindSavedVerb(loadID)复用
  已反序列化的实例，保证Job/Stance引用不断裂。详细机制见§3.6。

  #### 3.3.2 DualVerbCompositor四种组合路径

  DualVerbCompositor是纯静态工具类（无状态纯函数），负责根据左右手芯片的Verb/Too
  l数据合成最终VerbProperties列表。所有合成结果统一设置hasStandardCommand=false
  ，使芯片Verb彻底脱离引擎标准Gizmo路径。

  **侧别标签编码**：

  合成时为每个VerbProperties.label写入侧别标识，供后续Verb实例通过ParseSideLabel
  ()定位自身所属侧别。

  | 常量 | 值 | 用途 |
  |------|----|------|
  | SideLabel_LeftHand | "BDP_LeftHand" |
  编码在VerbProperties.label中，标识左手侧 |
  | SideLabel_RightHand | "BDP_RightHand" | 标识右手侧 |

  ParseSideLabel(label)解析规则：label包含"BDP_LeftHand"→LeftHand，包含"BDP_Righ
  tHand"→RightHand，否则→null。

  **组合判定流程**：

  ComposeVerbs(leftVerbs, rightVerbs, leftSlot, rightSlot)
  │
  ├─ leftVerbs为空 且 rightVerbs为空？
  │   └─ 返回空列表（两侧都无武器芯片）
  │
  ├─ 仅一侧有值？
  │   └─ 路径A：单侧直通
  │
  ├─ 两侧都有值：
  │   ├─ IsMeleeOnly(left) 且 IsMeleeOnly(right)？
  │   │   └─ 路径B：近战+近战
  │   │
  │   ├─ !IsMeleeOnly(left) 且 !IsMeleeOnly(right)？
  │   │   └─ 路径C：远程+远程
  │   │
  │   └─ 一侧近战一侧远程？
  │       └─ 路径D：混合

  **四条路径详细规格**：

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 路径A：单侧直通                                                      │
  │                                                                      │
  │ 触发条件：仅左手或仅右手有VerbProperties                              │
  │                                                                      │
  │ 处理：                                                               │
  │   ① CopyVerbProps() 浅拷贝（Fix-7：避免修改原始DefModExtension数据）  │
  │   ② 设置 isPrimary = true                                           │
  │   ③ 写入侧别label（SideLabel_LeftHand 或 SideLabel_RightHand）       │
  │   ④ hasStandardCommand = false                                       │
  │                                                                      │
  │ 输出：[ 该侧Verb(isPrimary) ]                                        │
  │ verbClass：保持原始（Verb_BDPShoot 或 Verb_BDPMelee）                 │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 路径B：近战+近战 → ComposeDualMelee                                   │
  │                                                                      │
  │ 触发条件：IsMeleeOnly(left)==true 且 IsMeleeOnly(right)==true         │
  │                                                                      │
  │ 输出列表（2或3个Verb）：                                              │
  │                                                                      │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [0] 左手Verb                                            │        │
  │   │   · CopyVerbProps(left[0])                              │        │
  │   │   · label = SideLabel_LeftHand                          │        │
  │   │   · isPrimary = false（副Verb，可手动选择）              │        │
  │   │   · verbClass = Verb_BDPMelee                           │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [1] 右手Verb（仅当左右不同芯片时添加）                   │        │
  │   │   · AreSameChip(leftSlot, rightSlot) == true → 跳过     │        │
  │   │   · CopyVerbProps(right[0])                             │        │
  │   │   · label = SideLabel_RightHand                         │        │
  │   │   · isPrimary = false                                   │        │
  │   │   · verbClass = Verb_BDPMelee                           │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [末] 双手合成Verb（isPrimary=true，默认攻击）            │        │
  │   │   · CopyVerbProps(left[0]) 作为基底                     │        │
  │   │   · verbClass = Verb_BDPMelee                           │        │
  │   │   · isPrimary = true                                    │        │
  │   │   · burstShotCount = left.burst + right.burst           │        │
  │   │   · ticksBetweenBurstShots = 取两侧较短间隔             │        │
  │   │   · label = 无侧别标签（双手Verb通过isPrimary识别）      │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │                                                                      │
  │ 设计意图：                                                           │
  │   · 双手合成Verb的burstShotCount = 左burst + 右burst                │
  │     例：左3击+右2击 → 合成5击burst                                   │
  │   · hitIndex状态机（§3.3.3）根据hitIndex分配每击归属侧               │
  │   · 单侧Verb保留为副选项，玩家可手动选择只用一只手攻击               │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 路径C：远程+远程 → ComposeDualRanged                                  │
  │                                                                      │
  │ 触发条件：!IsMeleeOnly(left) 且 !IsMeleeOnly(right)                  │
  │                                                                      │
  │ 输出列表（2或3个Verb）：                                              │
  │                                                                      │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [0] 左手Verb                                            │        │
  │   │   · CopyVerbProps(left[0])                              │        │
  │   │   · label = SideLabel_LeftHand                          │        │
  │   │   · isPrimary = false                                   │        │
  │   │   · verbClass = 保持原始（Verb_BDPShoot）               │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [1] 右手Verb（仅当不同芯片时添加）                       │        │
  │   │   · 同路径B逻辑                                         │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [末] 双手合成Verb（isPrimary=true）                      │        │
  │   │   · CopyVerbProps(left[0]) 作为基底                     │        │
  │   │   · verbClass = Verb_BDPDualRanged                      │        │
  │   │   · isPrimary = true                                    │        │
  │   │   · range = min(left.range, right.range)                │        │
  │   │   · warmupTime = max(left.warmup, right.warmup)         │        │
  │   │   · burstShotCount = left.burst + right.burst           │        │
  │   │   · ticksBetweenBurstShots = 取两侧较短间隔             │        │
  │   │   · defaultProjectile = left.defaultProjectile          │        │
  │   │     （运行时由Verb_BDPDualRanged动态切换）               │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │                                                                      │
  │ 关键参数取值规则：                                                    │
  │   · range取min：保证双侧都能命中（短射程侧不会打空）                 │
  │   · warmupTime取max：两侧都需要准备完毕才能开火                      │
  │   · burstShotCount叠加：交替射击，总发数=左+右                       │
  │   · defaultProjectile只是占位：实际由DualRanged逐发切换               │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 路径D：近战+远程 → ComposeMixed                                       │
  │                                                                      │
  │ 触发条件：一侧IsMeleeOnly==true，另一侧==false                       │
  │                                                                      │
  │ 输出列表（2个Verb，无合成Verb）：                                     │
  │                                                                      │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [0] 近战侧Verb                                          │        │
  │   │   · isPrimary = false                                   │        │
  │   │   · verbClass = Verb_BDPMelee                           │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │   ┌─────────────────────────────────────────────────────────┐        │
  │   │ [1] 远程侧Verb                                          │        │
  │   │   · isPrimary = true（远程优先作为默认攻击）             │        │
  │   │   · verbClass = Verb_BDPShoot                           │        │
  │   └─────────────────────────────────────────────────────────┘        │
  │                                                                      │
  │ 设计意图：                                                           │
  │   · 不合成DualVerb——近战和远程的攻击距离/机制差异太大                │
  │   · 远程isPrimary=true：征召后默认远程攻击，近战作为副选项            │
  │   · 两个Verb各自独立Gizmo，玩家可自由切换                            │
  └──────────────────────────────────────────────────────────────────────┘

  **四路径对比总览**：

  | 路径 | 触发条件 | 输出Verb数 | 合成Verb类型 | isPrimary | range规则 |
  burst规则 |
  |------|---------|-----------|-------------|-----------|----------|----------|
  | A 单侧 | 仅一侧有值 | 1 | 无合成 | 该侧Verb | 保持原始 | 保持原始 |
  | B 近战+近战 | 双侧近战 | 2~3 | Verb_BDPMelee | 合成Verb | N/A(近战) |
  左burst+右burst |
  | C 远程+远程 | 双侧远程 | 2~3 | Verb_BDPDualRanged | 合成Verb | min(左,右) |
  左burst+右burst |
  | D 混合 | 一近一远 | 2 | 无合成 | 远程侧 | 各自独立 | 各自独立 |

  **CopyVerbProps实现说明**：

  通过反射调用MemberwiseClone()浅拷贝VerbProperties。原因（Fix-7）：VerbProperti
  es来自DefModExtension，是全局共享的Def数据。若直接修改label/isPrimary等字段，
  会污染所有使用该芯片的触发体。浅拷贝后修改副本，原始数据不受影响。

  **AreSameChip判定**：

  比较两个ChipSlot的loadedChip.def.defName是否相同。相同芯片时不重复添加右手Verb
  （避免Gizmo重复），合成Verb的burstShotCount仍然叠加（同一芯片双持=双倍连击）。

  #### 3.3.3 攻击执行链

  本节描述三条攻击执行路径：远程JobDriver驱动、近战hitIndex状态机、引导弹多步锚
  点瞄准。

  ##### 远程攻击：JobDriver_BDPChipRangedAttack

  **为什么需要自定义JobDriver**（设计决策D4）：

  原版AttackStatic的两个致命问题：

  ┌─ 问题①：Verb查找错误 ──────────────────────────────────────────────┐
  │                                                                      │
  │  原版 tickIntervalAction:                                            │
  │    pawn.TryStartAttack(target)                                       │
  │      └─ TryGetAttackVerb()                                          │
  │           └─ 搜索 VerbTracker.AllVerbs                              │
  │                └─ 返回触发体"柄"的近战Verb ✗                        │
  │                   （芯片Verb不在AllVerbs中，被跳过）                 │
  │                                                                      │
  │  BDP JobDriver:                                                      │
  │    job.verbToUse.TryStartCastOn(target)                              │
  │      └─ 直接使用创建Job时绑定的芯片Verb ✓                           │
  │                                                                      │
  └──────────────────────────────────────────────────────────────────────┘

  ┌─ 问题②：VerbTick不被调用 ──────────────────────────────────────────┐
  │                                                                      │
  │  原版VerbTick调用链：                                                │
  │    Pawn_EquipmentTracker.EquipmentTrackerTick()                      │
  │      └─ VerbTracker.VerbsTick()                                     │
  │           └─ 遍历AllVerbs → verb.VerbTick()                         │
  │              （芯片Verb不在AllVerbs中 → 永远不被调用）               │
  │                                                                      │
  │  后果：burstShotCount>1的Verb在第1发后state卡在Bursting              │
  │        ticksToNextBurstShot永远不递减，burst永远不推进               │
  │                                                                      │
  │  BDP JobDriver修复：                                                 │
  │    tickAction（每tick）:                                              │
  │      job.verbToUse.VerbTick()   ← 手动驱动burst计时器               │
  │                                                                      │
  └──────────────────────────────────────────────────────────────────────┘

  **JobDriver_BDPChipRangedAttack完整Toil流程**：

  MakeNewToils()
  │
  ├─ Toil 1: ThrowColonistAttackingMote
  │   · 一次性：在pawn头顶显示攻击动画提示
  │
  └─ Toil 2: attackToil（核心攻击循环）
      │
      ├─ initAction（首次进入）:
      │   · 记录 startedIncapacitated = target.Downed
      │   · pawn.pather.StopDead()  停止移动
      │
      ├─ tickAction（每tick执行）:
      │   · job.verbToUse.VerbTick()
      │     手动推进burst计时器（ticksToNextBurstShot递减）
      │     这是整个JobDriver存在的核心原因之一
      │
      ├─ tickIntervalAction（每tickInterval执行）:
      │   │
      │   ├─ 目标检查：
      │   │   · target已死亡？→ EndJobWith(Incompletable)
      │   │   · target倒地 且 初始未倒地 且 非强制攻击？→ 结束
      │   │   · numAttacksMade ≥ maxNumStaticAttacks？→ 结束
      │   │
      │   ├─ Stance等待：
      │   │   · pawn.stances.curStance is Stance_Busy？→ 跳过本次
      │   │     （上一次攻击的动画/冷却尚未结束）
      │   │
      │   └─ 发起攻击：
      │       · job.verbToUse.TryStartCastOn(target)  ← 核心区别
      │       · 成功 → numAttacksMade++
      │
      ├─ 失败条件 endIfCantShootTargetFromCurPos:
      │   · !verb.CanHitTarget(target) → 结束
      │     （目标移出射程或LOS被阻挡）
      │
      └─ 失败条件 endIfCantShootInMelee:
          · verb.verbProps.minRange > 0
            且 target与pawn相邻（距离<最小射程）→ 结束
            （修复旧代码AdjacentTo8WayOrInside在远程战斗中的误判）

  **与原版AttackStatic的差异对比**：

  | 维度 | 原版AttackStatic | BDP JobDriver |
  |------|-----------------|---------------|
  | Verb来源 | pawn.TryStartAttack()重新查找 | job.verbToUse直接使用 |
  | VerbTick驱动 | 依赖VerbTracker.VerbsTick() | tickAction手动调用 |
  | 最小射程检查 | AdjacentTo8WayOrInside | verb.minRange + 距离计算 |
  | 序列化 | 原版字段 | startedIncapacitated + numAttacksMade |
  | 适用Verb | VerbTracker中的标准Verb |
  任意Verb实例（含脱离VerbTracker的芯片Verb） |

  ##### 近战攻击：Verb_BDPMelee hitIndex状态机

  **为什么用hitIndex状态机而非for循环**（设计决策D10）：

  方案A（已否决）：for循环同步多击
    TryCastShot() {
      for (int i = 0; i < totalHits; i++)
        ApplyDamage(target);  // 同一tick内全部命中
    }
    问题：无动画、无音效、无Stance切换，视觉上只有1击

  方案B（采用）：引擎burst机制 + hitIndex状态机
    · burstShotCount = 左burst + 右burst（由DualVerbCompositor合成）
    · 引擎每隔ticksBetweenBurstShots调用一次TryCastShot()
    · 每次TryCastShot()只执行1击，通过hitIndex确定归属侧
    · 每击有独立的动画、音效、Stance

  **hitIndex状态机完整流程**：

  TryCastShot() — 引擎每burst间隔调用一次
  │
  ├─ hitIndex == 0？（首击）
  │   └─ InitBurst(triggerComp)
  │       ├─ 解析侧别label判断模式：
  │       │   · 有侧别标签 → 单侧模式
  │       │   │   cachedLeftBurst = burstShotCount（或0）
  │       │   │   cachedRightBurst = 0（或burstShotCount）
  │       │   └─ cachedLeftInterval = cachedRightInterval =
  ticksBetweenBurstShots
  │       │
  │       └─ 无侧别标签（isPrimary双手Verb）→ 双侧模式
  │           ├─ 从左手芯片配置读取 cachedLeftBurst, cachedLeftInterval
  │           └─ 从右手芯片配置读取 cachedRightBurst, cachedRightInterval
  │
  ├─ Bug6防御：target无效？→ SafeAbortBurst() + return false
  │
  ├─ 清除Stance_Cooldown
  │   原因：引擎时序 VerbsTick() → StanceTrackerTick()
  │   burst的下一击在VerbsTick中触发TryCastShot()，
  │   但上一击的Stance_Cooldown要到StanceTrackerTick才清除。
  │   若不手动清除，TryCastShot()中的动画会被Stance阻挡。
  │
  ├─ GetSideForHitIndex() → 确定当前击归属侧
  │   │
  │   │  分配规则（前N击=左手，之后=右手）：
  │   │
  │   │  hitIndex:     0   1   2   3   4
  │   │  cachedLeft=3: ├─L─┤─L─┤─L─┤
  │   │  cachedRight=2:              ├─R─┤─R─┤
  │   │
  │   └─ hitIndex < cachedLeftBurst → LeftHand
  │      否则 → RightHand
  │
  ├─ EnsureToolAndManeuver(triggerComp, side)
  │   │  Bug11修复：BDP Verb不经过VerbTracker.InitVerb()，
  │   │  tool和maneuver字段未被初始化。每击手动设置。
  │   │
  │   ├─ 从该侧芯片的WeaponChipConfig.tools[0]读取Tool
  │   ├─ 从Tool.capacities[0]查找ManeuverDef（缓存版本）
  │   ├─ 设置 this.tool = tool
  │   ├─ 设置 this.maneuver = maneuverDef
  │   └─ 设置 currentChipDef = 该侧芯片ThingDef
  │       （供ApplyMeleeDamageToTarget使用）
  │
  ├─ base.TryCastShot()
  │   └─ 内部调用ApplyMeleeDamageToTarget(target)
  │       │
  │       │  重写伤害应用（与原版的关键差异）：
  │       │
  │       │  原版：weapon = EquipmentSource.def（触发体ThingDef）
  │       │  BDP：  weapon = currentChipDef（芯片ThingDef）
  │       │
  │       ├─ 主伤害：
  │       │   DamageInfo(maneuver.verb.meleeDamageDef,
  │       │             tool.AdjustedBaseMeleeDamageAmount(pawn) × [0.8~1.2],
  │       │             weapon: currentChipDef)
  │       │
  │       └─ extraMeleeDamages（若有）：
  │           遍历tool.extraMeleeDamages，逐个Apply
  │
  ├─ Bug7修复：强制 return true
  │   原因：base.TryCastShot()在miss/dodge时返回false，
  │   引擎将false解读为"攻击失败，取消整个burst"。
  │   但miss/dodge是正常战斗结果，不应中断连击。
  │
  ├─ 计算下一击的pendingInterval：
  │   │
  │   │  当前侧和下一击侧不同？→ 使用下一侧的interval
  │   │  相同？→ 使用当前侧的interval
  │   │
  │   │  为什么不直接设置ticksToNextBurstShot？
  │   │  因为TicksBetweenBurstShots属性是non-virtual的，
  │   │  引擎在TryCastShot()返回后会用固定值覆盖。
  │   │  所以用pendingInterval暂存，由JobDriver在VerbTick()后覆盖。
  │   │
  │   └─ pendingInterval = nextSideInterval
  │
  ├─ hitIndex++
  │
  └─ hitIndex >= burstShotCount？（burst结束）
      └─ 重置：hitIndex=0, pendingInterval=-1,
         cachedLeftBurst=cachedRightBurst=0

  **ApplyPendingInterval机制**：

  时序图（单个burst间隔内）：

    tick N:  引擎调用 VerbTick()
               └─ ticksToNextBurstShot--
               └─ ticksToNextBurstShot == 0？
                    └─ 调用 TryCastShot()
                         └─ 设置 pendingInterval = X
             引擎设置 ticksToNextBurstShot = TicksBetweenBurstShots（固定值）

    tick N:  JobDriver.tickAction 调用 verb.VerbTick()（第二次）
               └─ 无操作（已在本tick触发过）

    tick N:  JobDriver.tickAction 之后：
               └─ verb.ApplyPendingInterval()
                    └─ pendingInterval != -1？
                         └─ ticksToNextBurstShot = pendingInterval
                         └─ pendingInterval = -1
                    （覆盖引擎刚设置的固定值为芯片自定义间隔）

    tick N+X: 下一击触发...

  **SafeAbortBurst流程**：

  SafeAbortBurst()
  ├─ hitIndex = 0
  ├─ pendingInterval = -1
  ├─ cachedLeftBurst = cachedRightBurst = 0
  ├─ cachedLeftInterval = cachedRightInterval = 0
  ├─ currentChipDef = null
  └─ base.Reset()
      └─ 重置引擎burst状态（state, burstShotsLeft, ticksToNextBurstShot等）

  ##### 引导弹多步锚点瞄准

  **GuidedVerbState数据结构**：

  GuidedVerbState（引导弹共享状态，持有在Verb_BDPRangedBase中）
  ├── guidedActive: bool              引导模式是否激活
  ├── anchors: List          已确认的锚点列表（折线路径中间点）
  ├── finalTarget: LocalTargetInfo    最终目标（最后一步确认）
  ├── maxAnchors: int                 最大锚点数（从BDPGuidedConfig读取）
  ├── isLeftGuided: bool              左侧是否为引导弹（双侧Verb用）
  ├── isRightGuided: bool             右侧是否为引导弹（双侧Verb用）
  └── pendingTargetOverride: LocalTargetInfo
  拦截用临时目标（InterceptCastTarget设置）

  序列化字段（ExposeData中保存）：
    guidedActive, anchors, finalTarget, isLeftGuided, isRightGuided
    原因：burst中途存档→读档后需恢复引导状态

  **多步锚点瞄准交互状态机**：

                  ┌──────────────────────────────────────────┐
                  │         Idle（未瞄准）                    │
                  │  · guidedActive = false                  │
                  │  · anchors 为空                          │
                  └──────────────┬───────────────────────────┘
                                 │
                  玩家左键点击引导弹Gizmo
                  （Command_BDPChipAttack.GizmoOnGUIInt拦截）
                  verb.StartGuidedTargeting()
                                 │
                                 ▼
                  ┌──────────────────────────────────────────┐
                  │      锚点选择阶段（循环）                 │
                  │  · GuidedTargetingHelper启动Targeter     │
                  │  · 允许地面瞄准（原版Targeter不允许）     │
                  │  · 显示已选锚点连线预览                   │
                  └──────────────┬───────────────────────────┘
                                 │
                  ┌──────────────┼──────────────┐
                  │              │              │
               左键确认       右键/ESC       达到maxAnchors
               选择锚点       取消瞄准       自动进入下一阶段
                  │              │              │
                  ▼              ▼              ▼
            锚点加入         ┌────────┐    ┌──────────────┐
            anchors列表      │ 回到   │    │ 最终目标选择 │
            继续循环         │ Idle   │    │              │
            （若未满）       └────────┘    └──────┬───────┘
                  │                               │
                  │  anchors.Count==maxAnchors     │
                  └───────────────────────────────→│
                                                   │
                                 ┌─────────────────┼──────────────┐
                                 │                 │              │
                              左键确认          右键/ESC
                              选择最终目标      取消全部
                                 │                 │
                                 ▼                 ▼
                  ┌──────────────────────┐   ┌────────┐
                  │   执行攻击           │   │ 回到   │
                  │                      │   │ Idle   │
                  │ · guidedActive=true  │   │ 清空   │
                  │ · finalTarget=目标   │   │ anchors│
                  │ · OrderForceTarget() │   └────────┘
                  │   → 创建Job          │
                  └──────────┬───────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────────┐
                  │   TryCastShot执行                         │
                  │                                          │
                  │ · TryStartCastOn中：                     │
                  │   InterceptCastTarget()                  │
                  │   用第一个锚点替代最终目标做LOS检查        │
                  │   （最终目标可能不在直视范围内）           │
                  │                                          │
                  │ · OnProjectileLaunched(proj)中：          │
                  │   GuidedModule.SetPath(anchors+final)    │
                  │   弹道沿折线路径飞行                      │
                  │                                          │
                  │ · burst结束后：                           │
                  │   guidedActive=false, anchors清空         │
                  └──────────────────────────────────────────┘

  **InterceptCastTarget / InterceptDualCastTarget机制**：

  单侧引导（Verb_BDPShoot / Verb_BDPVolley）：

    TryStartCastOn(target) 被引擎调用
      │
      └─ gs.InterceptCastTarget(target)
          ├─ guidedActive == false？→ 返回原target（不拦截）
          └─ guidedActive == true？
              └─ 返回 anchors[0]（第一个锚点）
                 原因：引擎用target做LOS检查，
                 最终目标可能在墙后，但第一个锚点在视线内

  双侧引导（Verb_BDPDualRanged / Verb_BDPDualVolley）：

    TryStartCastOn(target) 被引擎调用
      │
      └─ gs.InterceptDualCastTarget(target)
          ├─ 任一侧isGuided == true？
          │   └─ 返回 anchors[0]（引导侧的第一个锚点）
          └─ 两侧都非引导？→ 返回原target

    TryCastShot中逐发处理：
      │
      ├─ 当前发属于引导侧？
      │   └─ 正常发射，OnProjectileLaunched附加折线路径
      │
      └─ 当前发属于非引导侧？
          └─ 恢复Thing目标进行直射LOS检查
             无LOS → 跳过该发（不浪费弹药）
             有LOS → 正常直射

  **Verb_BDPRangedBase中的三级回退定位策略**：

  贯穿整个远程攻击系统，用于从Verb实例定位其所属芯片Thing和配置：

  GetCurrentChipThing(triggerComp) — 定位芯片Thing
  │
  ├─ 优先级1：侧别label精确定位
  │   ParseSideLabel(verbProps.label) → side
  │   triggerComp.GetActiveSlot(side)?.loadedChip
  │   （最精确：Verb创建时已编码侧别）
  │
  ├─ 优先级2：ActivatingSlot临时上下文
  │   triggerComp.ActivatingSlot?.loadedChip
  │   （DoActivate/DeactivateSlot执行期间有效）
  │
  └─ 优先级3：遍历所有激活槽位
      triggerComp.AllActiveSlots中第一个武器芯片
      （兜底：双手Verb无侧别label时使用）

  GetChipConfig() — 定位芯片配置（同样三级回退）
    侧别label → ActivatingSlot → 遍历AllActiveSlots
    读取 WeaponChipConfig (DefModExtension)

  #### 3.3.4 核心不变量与边界情况

  **Verb隔离不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ⑮ | 芯片Verb不在VerbTracker.AllVerbs中 | CreateAndCacheChipVerbs |
  被引擎近战选择池/Y按钮自动拾取 |
  | ⑯ | 所有芯片Verb的hasStandardCommand=false | DualVerbCompositor |
  引擎为其生成标准Command_VerbTarget |
  | ⑰ | 芯片Verb的verbTracker指向CompTriggerBody.VerbTracker |
  CreateAndCacheChipVerbs | EquipmentSource为null，战斗日志无武器来源 |
  | ⑱ | 芯片Verb的caster = 持有者Pawn | CreateAndCacheChipVerbs + RebuildVerbs |
   射击/近战时caster为null导致空引用 |

  **Burst状态不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ⑲ | 近战hitIndex在burst结束时重置为0 | TryCastShot末尾 |
  下次burst从错误位置开始 |
  | ⑳ | pendingInterval在非burst期间为-1 | SafeAbortBurst / burst结束重置 |
  ApplyPendingInterval误覆盖正常间隔 |
  | ㉑ | 远程DualRanged的dualBurstIndex在新burst开始时清零 |
  TryStartCastOn中Reset() | 残留索引导致侧别分配错误 |
  | ㉒ | Verb_BDPMelee.TryCastShot始终返回true | Bug7修复 |
  miss/dodge取消整个burst连击 |

  **引导弹不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㉓ | guidedActive=true时anchors非空 | StartGuidedTargeting |
  InterceptCastTarget用anchors[0]时空引用 |
  | ㉔ | 非引导Verb的guidedActive始终为false | StartGuidedTargeting守卫检查 |
  普通弹道被错误拦截 |
  | ㉕ | burst结束后guidedActive重置为false | TryCastShot末尾 |
  下次普通攻击被引导拦截 |

  **数据一致性不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㉖ | CopyVerbProps产生独立副本，不修改原始Def数据 |
  DualVerbCompositor.CopyVerbProps | 污染全局DefModExtension，影响所有触发体 |
  | ㉗ | 双手合成Verb的burstShotCount = 左burst + 右burst | ComposeDualMelee /
  ComposeDualRanged | 连击/连射数与实际侧别分配不匹配 |
  | ㉘ | savedChipVerbs的loadID与CreateAndCacheChipVerbs生成的loadID格式一致 |
  PostExposeData / CreateAndCacheChipVerbs |
  读档后FindSavedVerb匹配失败，Job引用断裂 |

  **边界情况**：

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：burst被外部中断（目标死亡/逃离射程）                            │
  │                                                                      │
  │ 远程：JobDriver检测到target无效 → EndJobWith(Incompletable)          │
  │       Verb的burst状态由引擎在Job结束时自动Reset                      │
  │                                                                      │
  │ 近战：TryCastShot中Bug6防御检测target无效                            │
  │       → SafeAbortBurst() 重置所有BDP状态字段 + base.Reset()          │
  │       原因：近战burst中途目标死亡时，引擎不一定调用Reset()            │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：双侧不同弹药类型（DualRanged交替射击）                         │
  │                                                                      │
  │ 机制：VerbProperties.defaultProjectile在合成时设为左侧弹药（占位）   │
  │       TryCastShot中逐发切换：                                        │
  │         try {                                                        │
  │           verbProps.defaultProjectile = currentSideProjectile;       │
  │           TryCastShotCore(chipEquipment);                            │
  │         } finally {                                                  │
  │           verbProps.defaultProjectile = originalProjectile;          │
  │         }                                                            │
  │       try/finally保证异常时也能恢复原值                               │
  │                                                                      │
  │ 风险：多线程环境下不安全。但RimWorld是单线程游戏，无此问题。          │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：引导弹的非引导侧LOS检查（DualRanged/DualVolley）               │
  │                                                                      │
  │ 问题：InterceptDualCastTarget用锚点替代目标做LOS检查，               │
  │       但非引导侧需要对真实目标直射，可能无LOS。                       │
  │                                                                      │
  │ 处理：TryCastShot中对非引导侧恢复Thing目标检查LOS：                  │
  │       · 有LOS → 正常直射                                            │
  │       · 无LOS → 跳过该发（dualBurstIndex仍推进，不卡住）            │
  │       · Trion不扣除（未实际发射）                                    │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：近战Verb的Stance_Cooldown时序冲突                               │
  │                                                                      │
  │ 引擎调用顺序（每tick）：                                              │
  │   ① VerbsTick() → 检查burst计时 → 触发TryCastShot()                │
  │   ② StanceTrackerTick() → 清除到期的Stance_Cooldown                 │
  │                                                                      │
  │ 问题：①中触发的TryCastShot()试图播放攻击动画，                       │
  │       但上一击的Stance_Cooldown在②中才清除，动画被阻挡。             │
  │                                                                      │
  │ 修复：TryCastShot()开头手动检查并清除Stance_Cooldown：               │
  │       if (pawn.stances.curStance is Stance_Cooldown)                 │
  │           pawn.stances.SetStance(new Stance_Mobile());               │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：同一芯片双持（AreSameChip=true）                                │
  │                                                                      │
  │ 合成行为：                                                           │
  │   · 不添加右手副Verb（避免Gizmo重复）                                │
  │   · 合成Verb的burstShotCount仍然叠加（左burst+右burst）             │
  │   · 近战：hitIndex前半=左手，后半=右手（虽然是同一芯片）             │
  │   · 远程：交替射击，两侧弹药类型相同                                 │
  │                                                                      │
  │ 视觉效果：双倍连击/连射，但只有1个独立Gizmo + 1个合成Gizmo           │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：齐射Verb的burstShotCount=1设计                                  │
  │                                                                      │
  │ 齐射（Volley）在VerbProperties层面设burstShotCount=1，               │
  │ 引擎只触发一次TryCastShot()。                                        │
  │ 实际发射数由芯片配置的burstShotCount决定，在TryCastShot内部循环。    │
  │                                                                      │
  │ 原因：引擎burst机制是逐发调用TryCastShot，每发之间有间隔。           │
  │       齐射需要同一tick内发射所有子弹（视觉上同时出膛）。              │
  │       若用引擎burst，子弹会一颗一颗飞出，失去齐射感。                │
  │                                                                      │
  │ Trion消耗：预检总消耗（volleyCount × costPerShot），                  │
  │           全部发射后一次性扣除。避免发射到一半Trion不足。              │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：Verb_BDPRangedBase.ExposeData占位VerbProperties                 │
  │                                                                      │
  │ 问题：读档时Verb实例先被反序列化（ResolvingCrossRefs阶段），          │
  │       此时verbProps可能为null。引擎在某些路径检查verbProps非null，     │
  │       null时标记verb为BuggedAfterLoading并跳过。                      │
  │                                                                      │
  │ 修复：ExposeData中若verbProps==null，创建最小化占位VerbProperties：   │
  │       verbProps = new VerbProperties { verbClass = GetType() }       │
  │       后续CreateAndCacheChipVerbs会用正确的verbProps覆盖。            │
  └──────────────────────────────────────────────────────────────────────┘

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
  #### 3.4.1 管线架构与模块接口

  PMS采用"薄壳宿主 + 模块组合 +
  管线分发"架构。Bullet_BDP继承原版Bullet，本身不含业务逻辑（薄壳），功能由实现I
  BDPProjectileModule的模块提供。模块按需实现管线接口参与不同阶段的处理。

  **7阶段管线执行顺序**：

  每tick执行（Bullet_BDP.TickInterval）：

    ┌─ 阶段1: PathResolver ──────────────────────────────────────────┐
    │  接口: IBDPPathResolver                                         │
    │  数据包: PathContext { Origin, Destination(可写), Modified }     │
    │  用途: 修改destination实现追踪/锁定/折线弹道                     │
    │  示例: GuidedModule（当前未在此阶段，而是用ArrivalHandler重定向） │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
    ┌─ 阶段2: SpeedModifier（预留）──────────────────────────────────┐
    │  接口: IBDPSpeedModifier                                        │
    │  数据包: SpeedContext { BaseSpeed, SpeedMultiplier(可写) }       │
    │  用途: 加速/减速弹道                                            │
    │  状态: 预留接口，当前引擎不支持动态速度                          │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
    ┌─ 阶段3: 引擎位置计算 ─────────────────────────────────────────┐
    │  base.TickInterval(delta)                                       │
    │  原版Bullet逻辑：推进飞行位置、检查到达                          │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
    ┌─ 阶段4: InterceptModifier（预留）─────────────────────────────┐
    │  接口: IBDPInterceptModifier                                    │
    │  数据包: InterceptContext { SkipIntercept(可写) }                │
    │  用途: 穿透/豁免拦截                                            │
    │  状态: 预留接口                                                 │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
    ┌─ 阶段5: PositionModifier ─────────────────────────────────────┐
    │  接口: IBDPPositionModifier                                     │
    │  数据包: PositionContext { LogicalPosition, DrawPosition(可写) } │
    │  用途: 抛物线弧度、抖动等视觉偏移（不影响逻辑位置）             │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
    ┌─ 阶段6: TickObserver ─────────────────────────────────────────┐
    │  接口: IBDPTickObserver                                         │
    │  方法: OnTick(host)（只读，无数据包）                            │
    │  用途: 拖尾/视觉/音效等只读观察                                 │
    │  示例: TrailModule                                              │
    └─────────────────────────────────────────────────────────────────┘

  到达时执行（Bullet_BDP.ImpactSomething → Impact）：

    ┌─ 到达决策: ArrivalHandler ────────────────────────────────────┐
    │  接口: IBDPArrivalHandler                                       │
    │  数据包: ArrivalContext { HitTarget, Continue(可写) }            │
    │  用途: 引导飞行重定向、穿透、分裂、延迟引爆                     │
    │  规则: 任一模块设Continue=true → 跳过Impact，弹道继续飞行       │
    │  示例: GuidedModule（到达中间锚点时重定向到下一路径点）          │
    └─────────────────────────────────────────────────────────────────┘
                                      │
                            Continue=false
                                      ▼
    ┌─ 命中效果: ImpactHandler ────────────────────────────────────┐
    │  接口: IBDPImpactHandler                                       │
    │  数据包: ImpactContext { HitThing, Handled(可写) }              │
    │  用途: 爆炸/分裂/伤害修饰                                      │
    │  规则: 所有模块依次执行（非first-handler-wins）                 │
    │        任一设Handled=true → 跳过base.Impact（原版伤害）         │
    │  示例: ExplosionModule                                          │
    └─────────────────────────────────────────────────────────────────┘

  **IBDPProjectileModule基础接口**：

  IBDPProjectileModule : IExposable
  ├── int Priority { get; }              执行优先级（越小越先）
  └── void OnSpawn(Bullet_BDP host)      SpawnSetup时初始化

  Priority约定：
    10  = 路径修改（GuidedModule）    — 最先执行，影响后续所有阶段
    50  = 伤害效果（ExplosionModule） — 中等优先级
    100 = 视觉效果（TrailModule）     — 最后执行，只读观察

  **管线接口与Context数据包总览**：

  | 阶段 | 接口 | Context数据包 | 可写字段 | 执行模式 |
  |------|------|-------------|---------|---------|
  | 1 路径解析 | IBDPPathResolver | PathContext | Destination, Modified |
  链式（多模块依次修改） |
  | 2 速度修饰 | IBDPSpeedModifier | SpeedContext | SpeedMultiplier | 链式 |
  | 4 拦截修饰 | IBDPInterceptModifier | InterceptContext | SkipIntercept | 链式
   |
  | 5 位置修饰 | IBDPPositionModifier | PositionContext | DrawPosition | 链式 |
  | 6 Tick观察 | IBDPTickObserver | 无（只读） | — | 广播 |
  | 到达决策 | IBDPArrivalHandler | ArrivalContext | Continue |
  短路（Continue=true时跳过Impact） |
  | 命中效果 | IBDPImpactHandler | ImpactContext | Handled |
  广播（全部执行，Handled控制base.Impact） |

  **Bullet_BDP薄壳宿主的管线分发**：

  Bullet_BDP（薄壳宿主，继承Bullet）
  │
  ├─ 字段：
  │   modules: List     所有模块（按Priority升序）
  │   pathResolvers: List   ┐
  │   speedModifiers: List │
  │   interceptModifiers: List<...>           │ 7个管线缓存列表
  │   positionModifiers: List<...>            │ （BuildPipelineCache构建）
  │   tickObservers: List   │
  │   arrivalHandlers: List<...>              │
  │   impactHandlers: List ┘
  │   modifiedDrawPos: Vector3                经修饰的显示位置
  │   hasPositionModifiers: bool              是否有位置修饰器
  │
  ├─ TickInterval(delta)：
  │   ① 遍历pathResolvers → ResolvePath(host, ref pathCtx)
  │   ② pathCtx.Modified？→ 更新destination
  │   ③ 遍历speedModifiers → ModifySpeed(host, ref speedCtx)（预留）
  │   ④ base.TickInterval(delta) — 引擎位置计算
  │   ⑤ 遍历interceptModifiers → ModifyIntercept(host, ref interceptCtx)（预留）
  │   ⑥ hasPositionModifiers？→ 遍历positionModifiers → ModifyPosition
  │      → modifiedDrawPos = posCtx.DrawPosition
  │   ⑦ 遍历tickObservers → OnTick(host)
  │
  ├─ ImpactSomething()：
  │   ① 遍历arrivalHandlers → HandleArrival(host, ref arrivalCtx)
  │   ② arrivalCtx.Continue == true？→ return（跳过Impact，弹道继续）
  │   ③ 否则 → 调用原版ImpactSomething逻辑
  │
  ├─ Impact(hitThing, blockedByShield)：
  │   ① 遍历impactHandlers → HandleImpact(host, ref impactCtx)
  │   ② impactCtx.Handled == true？→ return（跳过base.Impact）
  │   ③ 否则 → base.Impact(hitThing, blockedByShield)
  │
  ├─ DrawPos属性：
  │   hasPositionModifiers？→ 返回modifiedDrawPos
  │   否则 → base.DrawPos
  │
  ├─ GetModule()：按类型获取模块实例（供Verb层调用）
  │
  └─ RedirectFlight(newOrigin, newDestination)：
      重置origin/destination/ticksToImpact
      （由GuidedModule.HandleArrival调用）

  **零开销设计**：无参与者的管线阶段 = 空列表，遍历零次，零开销。例如一颗普通子
  弹没有任何模块时，7个缓存列表全为空，TickInterval只执行base.TickInterval。

   #### 3.4.2 模块工厂与生命周期

  **BDPModuleFactory注册表**：

  BDPModuleFactory（静态类）
  │
  ├─ registry: Dictionary<Type, Func<DefModExtension, IBDPProjectileModule>>
  │   键 = DefModExtension的具体类型
  │   值 = 工厂方法（接收配置实例，返回模块实例）
  │
  ├─ Register(factory)
  │   将 typeof(TConfig) → factory 写入registry
  │
  └─ CreateModules(ThingDef def) → List
      遍历 def.modExtensions，对每个extension：
        registry.TryGetValue(extension.GetType(), out factory)
        命中 → factory(extension) 创建模块实例加入列表
        未命中 → 跳过（非BDP配置的DefModExtension）
      返回未排序列表

  **注册时机**（BDPMod静态构造函数，游戏启动时执行一次）：

  static BDPMod()
  {
      BDPModuleFactory.Register(cfg => new TrailModule(cfg));
      BDPModuleFactory.Register(cfg => new GuidedModule(cfg));
      BDPModuleFactory.Register(cfg => new ExplosionModule(cfg));
  }

  **扩展新模块的步骤**：

  ① 创建模块类实现IBDPProjectileModule + 关心的管线接口
  ② 创建配置类继承DefModExtension
  ③ 在BDPMod静态构造函数中注册：
     BDPModuleFactory.Register(cfg => new NewModule(cfg));
  ④ XML中给投射物ThingDef挂上配置DefModExtension
  ⑤ 不修改Bullet_BDP或现有模块（OCP）

  **模块创建→排序→缓存的完整流程**：

  Bullet_BDP.SpawnSetup(map, respawningAfterLoad)
  │
  ├─ base.SpawnSetup(map, respawningAfterLoad)
  │
  ├─ respawningAfterLoad == true？
  │   └─ 跳过创建（模块已由ExposeData反序列化恢复）
  │      → 直接 BuildPipelineCache()
  │
  └─ respawningAfterLoad == false？（首次生成）
      │
      ├─ Step 1: 创建模块
      │   modules = BDPModuleFactory.CreateModules(this.def)
      │   遍历def.modExtensions，匹配注册表，创建模块实例
      │
      ├─ Step 2: 按Priority升序排序
      │   modules.Sort((a, b) => a.Priority.CompareTo(b.Priority))
      │   排序后：GuidedModule(10) → ExplosionModule(50) → TrailModule(100)
      │
      ├─ Step 3: 构建管线缓存
      │   BuildPipelineCache()
      │   遍历modules，按接口类型分组到7个缓存列表：
      │   ┌──────────────────────────────────────────────────┐
      │   │ 模块实现了IBDPPathResolver？    → pathResolvers   │
      │   │ 模块实现了IBDPSpeedModifier？   → speedModifiers  │
      │   │ 模块实现了IBDPInterceptModifier？→ interceptMods  │
      │   │ 模块实现了IBDPPositionModifier？ → positionMods   │
      │   │ 模块实现了IBDPTickObserver？    → tickObservers   │
      │   │ 模块实现了IBDPArrivalHandler？  → arrivalHandlers │
      │   │ 模块实现了IBDPImpactHandler？   → impactHandlers  │
      │   └──────────────────────────────────────────────────┘
      │   hasPositionModifiers = positionModifiers.Count > 0
      │
      └─ Step 4: 通知模块初始化
          遍历modules → module.OnSpawn(this)
          各模块在此获取host引用、记录初始位置等

  **模块序列化**：

  Bullet_BDP.ExposeData()
  │
  ├─ base.ExposeData()  — 原版Bullet字段序列化
  │
  └─ Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep)
      · LookMode.Deep：每个模块实现IExposable，独立序列化内部状态
      · Saving：遍历modules，逐个调用module.ExposeData()
      · Loading：反序列化恢复模块实例列表
      ·
  PostLoadInit：由SpawnSetup(respawningAfterLoad=true)触发BuildPipelineCache

  各模块序列化内容：
  ┌──────────────────┬──────────────────────────────────────────┐
  │ GuidedModule     │ controller (Scribe_Deep)                 │
  │                  │ └─ waypoints + currentIndex               │
  ├──────────────────┼──────────────────────────────────────────┤
  │ ExplosionModule  │ 无运行时状态（config来自Def，不序列化）   │
  ├──────────────────┼──────────────────────────────────────────┤
  │ TrailModule      │ 无运行时状态（prevPos在OnSpawn中重置）    │
  │                  │ Material延迟初始化，不序列化               │
  └──────────────────┴──────────────────────────────────────────┘

  **模块生命周期时序**：

  时间轴 ──────────────────────────────────────────────────────→

    Verb.TryCastShot()
    │  创建Bullet_BDP实例
    │  GenSpawn.Spawn(bullet)
    │
    ├─ SpawnSetup(false)
    │   ├─ CreateModules → 模块实例化
    │   ├─ Sort → 按Priority排序
    │   ├─ BuildPipelineCache → 管线缓存
    │   └─ OnSpawn → 各模块初始化
    │
    ├─ Verb层调用（可选）：
    │   bullet.GetModule()?.SetWaypoints(...)
    │   （为引导弹设置折线路径）
    │
    ├─ tick 1..N: TickInterval(delta)
    │   管线分发：PathResolver→base→PositionModifier→TickObserver
    │
    ├─ 到达: ImpactSomething()
    │   ├─ ArrivalHandler → Continue=true？→ RedirectFlight（继续飞行）
    │   └─ Continue=false → Impact()
    │       └─ ImpactHandler → 爆炸/分裂/base.Impact
    │
    └─ Destroy()
        模块随Bullet_BDP一起被GC回收
        TrailModule创建的线段由BDPEffectMapComponent独立管理生命周期

    ── 存档/读档 ──
    ExposeData(Saving) → 序列化modules列表
    ExposeData(Loading) → 反序列化恢复modules
    SpawnSetup(true) → BuildPipelineCache（不重新创建模块）
    OnSpawn → 各模块重新初始化运行时状态

   #### 3.4.3 已实现模块详细

  ##### GuidedModule — 引导飞行（Priority=10）

  GuidedModule
  ├── 实现接口: IBDPProjectileModule, IBDPArrivalHandler
  ├── Priority: 10（路径修改，最先执行）
  ├── 管线阶段: 到达决策（ArrivalHandler）
  │
  ├── 字段:
  │   └── controller: GuidedFlightController  引导飞行控制器
  │
  └── 配置: BDPGuidedConfig（DefModExtension）
      └── 纯标记类，无字段
          挂在投射物ThingDef上，存在即表示支持引导飞行
          实际引导参数（maxAnchors/anchorSpread）由Verb层的WeaponChipConfig管理

  **GuidedFlightController路径点管理**：

  GuidedFlightController（IExposable）
  ├── waypoints: List    所有路径点（不含起点，含最终目标）
  ├── currentIndex: int           当前目标路径点索引
  │
  ├── IsGuided → bool             有路径点且未到达最终目标
  ├── CurrentWaypoint → Vector3   当前目标路径点
  │
  └── TryAdvanceWaypoint() → bool
      ├── currentIndex < waypoints.Count - 1？
      │   └── currentIndex++ → return true（调用者应重定向）
      └── 已在最终目标 → return false（正常Impact）

  **SetWaypoints路径构建**：

  GuidedModule.SetWaypoints(host, anchors, finalTarget, anchorSpread)
  │
  ├─ BuildWaypoints(anchors, finalTarget, anchorSpread)
  │   │
  │   │  输入: anchors = [A1, A2, A3], finalTarget = T, spread = 2.0
  │   │
  │   │  对每个锚点应用递增散布偏移：
  │   │    waypoint[i] = anchor[i].ToVector3Shifted()
  │   │                  + Random.insideUnitCircle
  │   │                  × spread × (i+1) / totalAnchors
  │   │
  │   │  散布递增示意（spread=2.0, 3个锚点）：
  │   │    A1: ±0.67格散布（2.0 × 1/3）
  │   │    A2: ±1.33格散布（2.0 × 2/3）
  │   │    A3: ±2.00格散布（2.0 × 3/3）
  │   │
  │   │  最终目标散布上限0.45格（保证基本命中精度）：
  │   │    T: ±min(spread×(n+1)/(n+1), 0.45)格散布
  │   │
  │   └─ 返回 List waypoints
  │
  └─ controller.Init(waypoints)
     host.RedirectFlight(host.Position, waypoints[0])
     弹道立即转向第一个路径点

  **HandleArrival重定向流程**：

  HandleArrival(host, ref ArrivalContext ctx)
  │
  ├─ controller.IsGuided == false？→ return（非引导弹，不干预）
  │
  ├─ controller.TryAdvanceWaypoint()
  │   ├─ true（还有下一路径点）：
  │   │   host.RedirectFlight(host.Position, controller.CurrentWaypoint)
  │   │   ctx.Continue = true  ← 跳过Impact，弹道继续飞行
  │   │
  │   └─ false（已到达最终目标）：
  │       不设Continue → 正常进入Impact流程
  │
  └─ 折线弹道示意：

  发射点 ──→ A1(散布) ──→ A2(散布) ──→ A3(散布) ──→ 最终目标
       段1          段2          段3          段4

  每段到达时：ArrivalHandler → RedirectFlight → 下一段
  最终段到达：ArrivalHandler不干预 → Impact

  ##### ExplosionModule — 爆炸效果（Priority=50）

  ExplosionModule
  ├── 实现接口: IBDPProjectileModule, IBDPImpactHandler
  ├── Priority: 50（伤害效果）
  ├── 管线阶段: 命中效果（ImpactHandler）
  │
  ├── 字段:
  │   └── config: BDPExplosionConfig（构造时传入）
  │
  └── 配置: BDPExplosionConfig（DefModExtension）
      ├── explosionRadius: float = 1.0    爆炸半径（格）
      └── explosionDamageDef: DamageDef   爆炸伤害类型（null时回退）

  **HandleImpact爆炸流程**：

  HandleImpact(host, ref ImpactContext ctx)
  │
  ├─ 参数来源：
  │   ┌──────────────────┬──────────────────────────────────────┐
  │   │ 参数             │ 来源                                  │
  │   ├──────────────────┼──────────────────────────────────────┤
  │   │ radius           │ config.explosionRadius                │
  │   │ damageDef        │ config.explosionDamageDef             │
  │   │                  │ ?? host.def.projectile.damageDef（回退）│
  │   │ damageAmount     │ host.DamageAmount（Bullet公开属性）    │
  │   │ armorPenetration │ host.ArmorPenetration                 │
  │   │ instigator       │ host.launcher                         │
  │   │ weapon           │ host.equipmentDef                     │
  │   └──────────────────┴──────────────────────────────────────┘
  │
  ├─ GenExplosion.DoExplosion(
  │     center: host.Position,
  │     map: host.Map,
  │     radius, damageDef, instigator,
  │     damageAmount, armorPenetration,
  │     weapon: weapon)
  │
  ├─ host.Destroy()  销毁弹体
  │
  └─ ctx.Handled = true  跳过base.Impact（避免重复伤害）

  ##### TrailModule — 拖尾效果（Priority=100）

  TrailModule
  ├── 实现接口: IBDPProjectileModule, IBDPTickObserver
  ├── Priority: 100（视觉效果，最后执行）
  ├── 管线阶段: Tick观察（TickObserver）
  │
  ├── 字段:
  │   ├── config: BeamTrailConfig     拖尾配置
  │   ├── trailMat: Material          缓存的Material（延迟初始化）
  │   ├── matResolved: bool           是否已尝试初始化
  │   └── prevPos: Vector3            上一tick位置
  │
  └── 配置: BeamTrailConfig（DefModExtension）
      ├── enabled: bool = true              总开关
      ├── trailWidth: float = 0.15          拖尾宽度（世界单位）
      ├── trailColor: Color = white         拖尾颜色
      ├── trailTexPath: string              拖尾贴图路径
      ├── segmentDuration: int = 8          每段线段存活tick数
      ├── startOpacity: float = 0.9         初始不透明度
      ├── decayTime: float = 1.0            衰减时间比例
      └── decaySharpness: float = 1.0       衰减锐度

  **OnTick线段创建**：

  OnTick(host)
  │
  ├─ config.enabled == false？→ return
  │
  ├─ EnsureMaterial(host)
  │   matResolved == false？
  │   └─ trailMat = MaterialPool.MatFrom(config.trailTexPath,
  ShaderDatabase.MoteGlow)
  │      matResolved = true
  │      延迟初始化原因：避免读档时跨线程加载MaterialPool.MatFrom
  │
  ├─ trailMat == null？→ return（贴图路径无效）
  │
  ├─ currentPos = host.DrawPos
  │
  ├─ BDPEffectMapComponent.GetInstance(host.Map)
  │   .CreateSegment(
  │       origin: prevPos,
  │       destination: currentPos,
  │       material: trailMat,
  │       baseColor: config.trailColor,
  │       width: config.trailWidth,
  │       duration: config.segmentDuration,
  │       startOpacity: config.startOpacity,
  │       decayTime: config.decayTime,
  │       decaySharpness: config.decaySharpness)
  │
  └─ prevPos = currentPos  更新位置

  ##### BDPEffectMapComponent — 拖尾渲染管理器

  BDPEffectMapComponent（MapComponent）
  │
  ├── 字段:
  │   ├── segments: List              活跃线段列表
  │   ├── pool: Stack                 对象池（回收复用）
  │   └── cache: Dictionary<int, BDPEffectMapComponent> 静态缓存（MapID→实例）
  │
  ├── GetInstance(map) → BDPEffectMapComponent
  │   先查cache[map.uniqueID]，miss时查map.GetComponent<>()并写入cache
  │
  ├── CreateSegment(origin, dest, material, color, width, duration, ...)
  │   ├─ pool.Count > 0？→ pool.Pop() + Reset(...)  复用
  │   └─ 否则 → new BDPTrailSegment(...)             新建
  │   → segments.Add(segment)
  │
  ├── MapComponentTick()（每tick）
  │   遍历segments（倒序，安全删除）：
  │   ├─ segment.Tick() 返回false（已过期）？
  │   │   └─ 回收到pool + 用末尾元素覆盖当前位置（O(1)删除）
  │   └─ 返回true → 继续存活
  │
  ├── MapComponentUpdate()（每帧渲染）
  │   ├─ 获取当前视口 CellRect.ExpandedBy(2)（视口裁剪）
  │   └─ 遍历segments：
  │       segment在视口内？→ segment.Draw()
  │       否则 → 跳过（不渲染视口外线段）
  │
  └── MapRemoved()
      cache.Remove(map.uniqueID)  清理静态缓存

  **BDPTrailSegment衰减公式**：

  Opacity = startOpacity × (1 - pow(min(1, ticksAlive / (duration × decayTime)),
   decaySharpness))

  参数效果：
  ┌──────────────┬──────────────────────────────────────────────┐
  │ decaySharpness │ 视觉效果                                    │
  ├──────────────┼──────────────────────────────────────────────┤
  │ 1.0          │ 线性衰减 — 烟雾感，缓慢消散                  │
  │ 2.0          │ 二次衰减 — 先慢后快                           │
  │ 3.0          │ 三次衰减 — 光束硬边感，突然消失               │
  └──────────────┴──────────────────────────────────────────────┘

  衰减曲线示意（duration=8, decayTime=1.0, startOpacity=0.9）：

    不透明度
    0.9 ┤██
        │  ██
        │    ██          decaySharpness=1.0（线性）
        │      ██
        │        ██
    0.0 ┤──────────██──→ tick
        0  2  4  6  8

    0.9 ┤████
        │    ████
        │        ██      decaySharpness=2.0（二次）
        │          ██
        │           █
    0.0 ┤────────────█─→ tick

    0.9 ┤██████
        │      ████
        │          ██    decaySharpness=3.0（三次）
        │           ██
        │            █
    0.0 ┤─────────────█→ tick

  **渲染方式**：

  BDPTrailSegment.Draw()
  │
  ├─ 计算中点: midPoint = (origin + destination) / 2
  ├─ 计算长度: length = Vector3.Distance(origin, destination)
  ├─ 计算旋转: rotation = Quaternion.LookRotation(destination - origin)
  ├─ 计算缩放: scale = (width, 1, length)
  │
  ├─ Matrix4x4.TRS(midPoint, rotation, scale)
  │
  ├─ MaterialPropertyBlock.SetColor("_Color", baseColor × Opacity)
  │   使用静态共享propBlock，避免每帧GC分配
  │
  └─ Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0,
  propBlock)

  #### 3.4.4 核心不变量与边界情况

  **管线不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㉙ | modules列表按Priority升序排列 | SpawnSetup中Sort |
  管线执行顺序错误（如TrailModule先于GuidedModule） |
  | ㉚ | 7个管线缓存列表与modules列表一致 | BuildPipelineCache |
  模块实现了接口但不被分发 |
  | ㉛ | ArrivalHandler中Continue=true时不进入Impact | ImpactSomething |
  中间锚点被当作最终命中处理 |
  | ㉜ | ImpactHandler中Handled=true时不调用base.Impact | Impact |
  爆炸+原版伤害双重生效 |
  | ㉝ | 模块实现IExposable，序列化内部状态 | 各模块ExposeData |
  读档后模块状态丢失 |

  **模块生命周期不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㉞ | respawningAfterLoad=true时不重新创建模块 | SpawnSetup守卫 |
  覆盖已反序列化的模块状态 |
  | ㉟ | OnSpawn在BuildPipelineCache之后调用 | SpawnSetup顺序 |
  模块初始化时管线缓存未就绪 |
  | ㊱ | TrailModule的Material延迟初始化 | EnsureMaterial |
  读档时跨线程加载MaterialPool崩溃 |

  **渲染不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㊲ | BDPEffectMapComponent静态缓存在MapRemoved时清理 | MapRemoved |
  地图卸载后缓存指向已销毁对象 |
  | ㊳ | BDPTrailSegment使用静态共享MaterialPropertyBlock | Draw方法 | 每帧new
  PropertyBlock导致GC压力 |
  | ㊴ | 对象池复用线段时Reset所有字段 | CreateSegment |
  残留上一次的颜色/位置数据 |

  **边界情况**：

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：投射物无任何BDP模块（普通子弹使用Bullet_BDP基类）               │
  │                                                                      │
  │ 行为：CreateModules返回空列表，7个管线缓存全为空列表                  │
  │       TickInterval只执行base.TickInterval                            │
  │       ImpactSomething/Impact直接走原版逻辑                           │
  │       零开销：空列表遍历零次                                         │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：引导弹到达中间锚点                                              │
  │                                                                      │
  │ 流程：                                                               │
  │   ImpactSomething()                                                  │
  │     → GuidedModule.HandleArrival()                                   │
  │       → controller.TryAdvanceWaypoint() = true                      │
  │       → host.RedirectFlight(currentPos, nextWaypoint)               │
  │       → ctx.Continue = true                                         │
  │     → return（跳过Impact）                                          │
  │                                                                      │
  │ RedirectFlight内部：                                                 │
  │   origin = newOrigin                                                 │
  │   destination = newDestination                                       │
  │   ticksToImpact = 重新计算（距离/速度）                              │
  │   弹道从当前位置开始飞向下一路径点                                    │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：引导弹+爆炸弹组合（同一投射物挂两个模块）                       │
  │                                                                      │
  │ 中间锚点：GuidedModule(P=10)先执行，Continue=true → 跳过Impact      │
  │           ExplosionModule(P=50)不被调用 ✓（中间点不爆炸）            │
  │                                                                      │
  │ 最终目标：GuidedModule不设Continue → 进入Impact                     │
  │           ExplosionModule.HandleImpact → 爆炸 + Handled=true        │
  │           base.Impact被跳过 ✓（不重复伤害）                          │
  │                                                                      │
  │ 管线顺序保证了正确行为：路径模块优先于伤害模块                        │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：拖尾线段的视口裁剪                                              │
  │                                                                      │
  │ MapComponentUpdate中：                                               │
  │   viewRect = Find.CameraDriver.CurrentViewRect.ExpandedBy(2)        │
  │   线段中点不在viewRect内 → 跳过Draw()                                │
  │                                                                      │
  │ ExpandedBy(2)的原因：                                                │
  │   线段可能跨越视口边界，中点在视口外但端点在视口内。                   │
  │   扩展2格作为安全边距，避免视口边缘线段突然消失。                     │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：对象池复用与O(1)删除                                            │
  │                                                                      │
  │ MapComponentTick中过期线段的删除策略：                                │
  │   倒序遍历segments列表：                                             │
  │     segment.Tick() == false（过期）？                                │
  │       segments[i] = segments[segments.Count - 1]  用末尾覆盖         │
  │       segments.RemoveAt(segments.Count - 1)       移除末尾           │
  │       pool.Push(segment)                          回收到对象池       │
  │                                                                      │
  │   O(1)删除：不触发列表元素移动（List.RemoveAt(i)是O(n)）            │
  │   倒序遍历：覆盖后不会跳过元素                                       │
  │   对象池：避免频繁new/GC，线段对象通过Reset()重置后复用               │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：读档后模块恢复                                                  │
  │                                                                      │
  │ ExposeData(Loading)：                                                │
  │   Scribe_Collections反序列化modules列表                              │
  │   各模块的ExposeData恢复内部状态                                     │
  │                                                                      │
  │ SpawnSetup(respawningAfterLoad=true)：                               │
  │   不重新CreateModules（守卫跳过）                                    │
  │   BuildPipelineCache() — 重建管线缓存                                │
  │   OnSpawn() — 各模块重新初始化运行时状态                             │
  │                                                                      │
  │ TrailModule特殊处理：                                                │
  │   matResolved = false（Material不序列化）                            │
  │   OnSpawn中记录prevPos                                               │
  │   首次OnTick时EnsureMaterial延迟加载Material                         │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：散布公式的递增设计                                              │
  │                                                                      │
  │ 散布随锚点序号递增：spread × (i+1) / totalAnchors                   │
  │                                                                      │
  │ 设计意图：                                                           │
  │   · 第一个锚点散布最小 — 起飞阶段精确                                │
  │   · 后续锚点散布递增 — 模拟弹道不稳定性                              │
  │   · 最终目标散布上限0.45格 — 保证基本命中精度                        │
  │                                                                      │
  │ 示例（3锚点, spread=3.0）：                                         │
  │   A1: ±1.0格   A2: ±2.0格   A3: ±3.0格   Target: ±0.45格          │
  └──────────────────────────────────────────────────────────────────────┘


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
  #### 3.5.1 Gizmo_TriggerBodyStatus

  Gizmo_TriggerBodyStatus是常驻内联状态条（75×75像素），严格遵循SRP——只负责状态
  显示和窗口入口，不持有任何业务逻辑。所有数据通过CompTriggerBody的公开属性读取
  。

  **整体布局**：

  ┌─────────────────────────────── 75px ────────────────────────────────┐
  │ y+0   ┌─────────────────────────────────────────────────────────┐  │
  │        │                   窗口背景                              │  │
  │ y+4   │  [左手标签14px] [芯片图标24×24] [芯片图标] [芯片图标]   │  │
  │        │  "左手"          槽位0          槽位1       槽位2       │  │
  │ y+30  │  ████████████░░░░░░░░  左手切换进度条（3px高）          │  │
  │        │  （仅切换中显示，WindingDown=橙红，WarmingUp=青蓝）      │  │
  │ y+34  │                                                         │  │
  │ y+38  │  [右手标签14px] [芯片图标24×24] [芯片图标] [芯片图标]   │  │
  │        │  "右手"          槽位0          槽位1       槽位2       │  │
  │ y+64  │  ████████████░░░░░░░░  右手切换进度条（3px高）          │  │
  │        │  （仅切换中显示）                                       │  │
  │ y+68  │                                                         │  │
  │ y+75  └─────────────────────────────────────────────────────────┘  │
  │         Tooltip: "点击查看/操作所有槽位"                            │
  │         点击 → 打开Window_TriggerBodySlots（防重复检查）            │
  └─────────────────────────────────────────────────────────────────────┘

  **四态视觉系统**：

  每个槽位图标根据芯片状态显示不同视觉效果。判定按优先级从高到低：

                      ┌─────────────┐
                      │ slot.isActive│
                      │  == true?   │
                      └──────┬──────┘
                        是 ╱    ╲ 否
                         ╱      ╲
              ┌─────────┐    ┌──────────────┐
              │ ① 激活  │    │ loadedChip   │
              │         │    │  != null?    │
              │ 芯片图标│    └──────┬───────┘
              │ 绿色边框│      是 ╱    ╲ 否
              │ (0.4,   │       ╱      ╲
              │  0.8,   │  ┌────────┐  ┌──────────┐
              │  0.4)   │  │Combat  │  │ ④ 空     │
              └─────────┘  │Body    │  │          │
                           │Active? │  │ 灰色实心 │
                           └───┬────┘  │ (0.25,   │
                         是 ╱    ╲ 否  │  0.25,   │
                          ╱      ╲     │  0.25)   │
               ┌──────────┐ ┌────────┐ └──────────┘
               │②注册    │ │③挂载  │
               │ 未激活   │ │ 未注册 │
               │          │ │        │
               │ 芯片图标 │ │ 50%透明│
               │ 蓝色边框 │ │ 芯片图标│
               │ (0.3,    │ │ 暗黄边框│
               │  0.5,    │ │ (0.6,  │
               │  0.8)    │ │  0.55, │
               └──────────┘ │  0.3)  │
                            └────────┘

  **四态含义对照**：

  | 状态 | 条件组合 | 图标处理 | 边框颜色 | 含义 |
  |------|---------|---------|---------|------|
  | ① 激活 | isActive == true | 正常显示 | 绿色(0.4, 0.8, 0.4) |
  芯片正在输出效果 |
  | ② 注册未激活 | 有芯片 + 战斗体已激活 + !isActive | 正常显示 | 蓝色(0.3, 0.5,
   0.8) | 芯片已装载，战斗体在线，但未选中激活 |
  | ③ 挂载未注册 | 有芯片 + 战斗体未激活 | 50%透明 | 暗黄(0.6, 0.55, 0.3) |
  芯片已装载，但战斗体离线 |
  | ④ 空 | loadedChip == null | 无图标 | 灰色实心填充(0.25, 0.25, 0.25) | 空槽位
   |

  **GetBestDisplaySlot优先级**：

  每侧只显示一个"代表"槽位的图标。选择逻辑：

  GetBestDisplaySlot(side) → ChipSlot
  │
  ├─ 优先级1：该侧的激活槽位
  │   GetActiveSlot(side)?.loadedChip != null → 返回
  │
  └─ 优先级2：该侧第一个有芯片的槽位
      遍历slots，找到loadedChip != null的第一个 → 返回
      全部为空 → 返回null（显示④空状态）

  **切换进度条**：

  绘制条件：triggerBody.IsSideSwitching(side) == true

  进度值：triggerBody.GetSideSwitchProgress(side)
          返回 0.0~1.0（已经过时间 / 总时长）

  颜色选择：
    triggerBody.GetSideSwitchPhase(side) 返回值：
    ┌──────────────┬────────────────────────────────────┐
    │ WindingDown   │ WindingDownBarTex — 橙红色          │
    │              │ 含义：旧芯片正在后摇/关闭中          │
    ├──────────────┼────────────────────────────────────┤
    │ WarmingUp    │ WarmingUpBarTex — 青蓝色            │
    │              │ 含义：新芯片正在前摇/预热中          │
    └──────────────┴────────────────────────────────────┘

  绘制方式：
    Widgets.FillableBar(barRect, progress, barTex)
    barRect: 宽度=Gizmo宽度-边距, 高度=3px
    位置：紧贴对应侧图标行下方

  **点击交互**：

  Widgets.ButtonInvisible(gizmoRect)
  │
  ├─ 检查Window_TriggerBodySlots是否已打开
  │   Find.WindowStack.IsOpen()
  │   └─ 已打开 → 不重复创建
  │
  └─ 未打开 → Find.WindowStack.Add(new Window_TriggerBodySlots(triggerBody))

  #### 3.5.2 Window_TriggerBodySlots

  Window_TriggerBodySlots是浮动窗口，展示触发体所有槽位的详细状态，支持点击激活/
  关闭芯片。严格遵循SRP——交互操作全部委托CompTriggerBody公开API（ActivateChip/De
  activateChip），不绕过状态机。

  **窗口属性**：

  | 属性 | 值 | 说明 |
  |------|----|------|
  | doCloseX | true | 右上角关闭按钮 |
  | absorbInputAroundWindow | false | 不阻挡窗口外的游戏交互 |
  | forcePause | false | 不暂停游戏 |
  | draggable | true | 可拖拽移动 |
  | closeOnClickedOutside | false | 点击外部不关闭 |

  **自适应布局**：

  HasSpecialSlots = triggerBody.Props.specialSlotCount > 0

  ┌─ specialSlotCount > 0 → 三列布局（560×260px）──────────────────────┐
  │                                                                      │
  │  标题: "槽位状态 — {触发体名称}"                                     │
  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐             │
  │  │ 左手槽        │  │ 右手槽        │  │ 特殊槽          │             │
  │  │ (Left Hand)  │  │ (Right Hand) │  │ (Special)      │             │
  │  │              │  │              │  │                │             │
  │  │ 宽度:        │  │ 宽度:        │  │ 宽度: 160px    │             │
  │  │ (560-160     │  │ (同左)       │  │                │             │
  │  │  -8×2)/2     │  │              │  │ editable=false │             │
  │  │ =192px       │  │              │  │ （只读预览）    │             │
  │  │              │  │              │  │                │             │
  │  │ editable     │  │ editable     │  │                │             │
  │  │ =true        │  │ =true        │  │                │             │
  │  └──────────────┘  └──────────────┘  └────────────────┘             │
  │  ├── gap=8px ──┤  ├── gap=8px ──┤                                   │
  └──────────────────────────────────────────────────────────────────────┘

  ┌─ specialSlotCount == 0 → 两列布局（380×260px）─────────────────────┐
  │                                                                      │
  │  标题: "槽位状态 — {触发体名称}"                                     │
  │  ┌──────────────────┐  ┌──────────────────┐                         │
  │  │ 左手槽            │  │ 右手槽            │                         │
  │  │ (Left Hand)      │  │ (Right Hand)     │                         │
  │  │                  │  │                  │                         │
  │  │ 宽度:            │  │ 宽度:            │                         │
  │  │ (380-8)/2=186px  │  │ (同左)           │                         │
  │  │                  │  │                  │                         │
  │  │ editable=true    │  │ editable=true    │                         │
  │  └──────────────────┘  └──────────────────┘                         │
  │  ├──── gap=8px ────┤                                                │
  └──────────────────────────────────────────────────────────────────────┘

  hasRightHand==false时右手列显示"（无右手槽）"提示文字。

  **单行槽位绘制（DrawSlotRow）**：

  每行高度: 34px（含2px间距）

  ┌─ 单行布局（32px有效高度）──────────────────────────────────────────┐
  │                                                                      │
  │  ┌────┐ ┌──────┐ ┌──────────────────────────┐ ┌──┐                 │
  │  │[0] │ │ icon │ │ "芯片名称"                │ │● │                 │
  │  │    │ │24×24 │ │                          │ │  │                 │
  │  └────┘ └──────┘ └──────────────────────────┘ └──┘                 │
  │  x+2    x+22     x+50                        xMax-18               │
  │  18px   24px     (width-70)px                 16px                  │
  │  索引    图标     芯片标签                     激活指示器             │
  │                                               （绿色●，仅激活时）   │
  │                                                                      │
  │  整行背景色 = 四态行颜色（见下方）                                    │
  │  整行可点击（ButtonInvisible），切换中/空槽/只读时禁用               │
  └──────────────────────────────────────────────────────────────────────┘

  空槽位时：
  ┌────┐ ┌──────────────────────────────────────────┐
  │[0] │ │ "空"（灰色半透明文字）                     │
  └────┘ └──────────────────────────────────────────┘

  **四态行颜色判定**：

  与Gizmo_TriggerBodyStatus的四态视觉系统对应，但用于行背景色：

  判定流程（与Gizmo相同优先级）：

    slot.isActive == true？
    ├─ 是 → 特殊槽？SpecialRowColor(0.2,0.4,0.6,0.3)蓝
    │                 否则 ActiveRowColor(0.3,0.6,0.3,0.3)绿
    │
    └─ 否 → loadedChip != null？
         ├─ 是 → IsCombatBodyActive？
         │       ├─ 是 → RegisteredInactiveColor(0.2,0.3,0.5,0.3)暗蓝
         │       └─ 否 → LoadedUnregisteredColor(0.5,0.45,0.2,0.3)暗黄
         │               图标额外设50%透明
         │
         └─ 否 → EmptyRowColor(0.15,0.15,0.15,0.3)深灰

  | 状态 | 行背景色 | 图标处理 | 可点击 |
  |------|---------|---------|--------|
  | 激活（左右手） | 绿色(0.3,0.6,0.3) | 正常 + 绿色●指示器 | 是（点击关闭） |
  | 激活（特殊槽） | 蓝色(0.2,0.4,0.6) | 正常 + 绿色●指示器 |
  否（editable=false） |
  | 注册未激活 | 暗蓝(0.2,0.3,0.5) | 正常 | 是（点击激活） |
  | 挂载未注册 | 暗黄(0.5,0.45,0.2) | 50%透明 | 否（战斗体未激活） |
  | 空 | 深灰(0.15,0.15,0.15) | 无图标 | 否（无芯片） |

  **交互流程**：

  玩家点击槽位行
  │
  ├─ 前置守卫（canClick判定）：
  │   editable == true？                    （特殊槽只读 → 拒绝）
  │   slot.loadedChip != null？             （空槽 → 拒绝）
  │   !triggerBody.IsSideSwitching(side)？  （该侧切换中 → 拒绝）
  │   全部通过 → canClick = true
  │
  ├─ canClick == false → 行显示DisabledColor(0.5,0.5,0.5,0.5)，无响应
  │
  └─ canClick == true → Widgets.ButtonInvisible(rect)
      │
      ├─ slot.isActive == true？
      │   └─ triggerBody.DeactivateChip(side)    关闭当前芯片
      │
      └─ slot.isActive == false？
          └─ triggerBody.ActivateChip(side, slot.index)    激活该芯片
              └─ 进入§3.1的激活流程（状态机、前置检查等）

  注意：IsSideSwitching(side)是按侧独立检查。
        v6.0修复：旧代码用IsSwitching检查全局，
        导致左手切换时右手槽位也被禁用。

  #### 3.5.3 Command_BDPChipAttack

  Command_BDPChipAttack继承Command_VerbTarget，是芯片攻击的Gizmo入口。解决原版按
  ownerThing.def合并导致同一触发体所有Verb只显示1个Gizmo的问题（设计决策D5）。仅
  征召时显示。

  **attackId合并规则**：

  原版合并逻辑（被替代）：
    Command_VerbTarget.GroupsWith(other)
      └─ verb.EquipmentSource.def == other.verb.EquipmentSource.def
         所有芯片Verb的EquipmentSource都是同一个触发体 → 全部合并为1个Gizmo ✗

  BDP合并逻辑：
    Command_BDPChipAttack.GroupsWith(other)
      └─ other is Command_BDPChipAttack bdp
         && this.attackId == bdp.attackId
         不同芯片有不同attackId → 各自独立Gizmo ✓
         同芯片跨Pawn有相同attackId → 正确合并 ✓

  **attackId生成规则**（在CompGetEquippedGizmosExtra中）：

  | 场景 | attackId格式 | 示例 | 说明 |
  |------|-------------|------|------|
  | 独立左手芯片 | `chipDef.defName` | `"BDP_ArcMoonBlade"` |
  芯片defName直接作为ID |
  | 独立右手芯片 | `chipDef.defName` | `"BDP_ScorpionGun"` | 同上 |
  | 双手合成攻击 | `"dual:" + Sort(A,B).Join("+")` |
  `"dual:BDP_ArcMoonBlade+BDP_ScorpionGun"` | 字典序排序保证对称性 |
  | 组合技能 | `"combo:" + comboAbilityDef.defName` |
  `"combo:BDP_ArcScorpionCombo"` | 组合能力defName |

  **排序对称性保证**：

  双手attackId排序示例：

    左手=弧月刃, 右手=蝎子枪:
      Sort("BDP_ArcMoonBlade", "BDP_ScorpionGun")
      → "dual:BDP_ArcMoonBlade+BDP_ScorpionGun"

    左手=蝎子枪, 右手=弧月刃（交换位置）:
      Sort("BDP_ScorpionGun", "BDP_ArcMoonBlade")
      → "dual:BDP_ArcMoonBlade+BDP_ScorpionGun"  ← 相同！

    保证：无论芯片装在哪只手，同一组合的attackId一致，
          多Pawn选中时Gizmo正确合并。

  **Gizmo生成编排**（CompGetEquippedGizmosExtra中的征召分支）：

  CompGetEquippedGizmosExtra()
  │
  ├─ yield return 基类Gizmo
  │
  ├─ OwnerHasTrionGland() == false？→ 跳过所有芯片Gizmo
  │
  ├─ pawn.Drafted == true？（征召时生成攻击Gizmo）
  │   │
  │   ├─ leftHandAttackVerb != null？
  │   │   └─ yield return new Command_BDPChipAttack {
  │   │         verb = leftHandAttackVerb,
  │   │         volleyVerb = leftHandVolleyVerb,    ← 可能为null
  │   │         attackId = leftChipDef.defName
  │   │       }
  │   │
  │   ├─ rightHandAttackVerb != null？
  │   │   └─ yield return new Command_BDPChipAttack {
  │   │         verb = rightHandAttackVerb,
  │   │         volleyVerb = rightHandVolleyVerb,
  │   │         attackId = rightChipDef.defName
  │   │       }
  │   │
  │   └─ dualAttackVerb != null 且 两侧都有芯片？
  │       └─ yield return new Command_BDPChipAttack {
  │             verb = dualAttackVerb,
  │             volleyVerb = dualVolleyVerb,
  │             attackId = "dual:" + Sort(leftDef, rightDef) + "+"
  │           }
  │
  ├─ allowChipManagement == true？
  │   └─ yield return new Gizmo_TriggerBodyStatus(this)
  │
  └─ [godMode] yield return 调试Gizmo

  **Gizmo显示示例**（征召状态下）：

  场景：左手=弧月刃（近战），右手=蝎子枪（远程）→ 路径D混合

    ┌──────────┐  ┌──────────┐
    │ 弧月刃   │  │ 蝎子枪   │
    │ (近战)   │  │ (远程)   │
    │          │  │ ★primary │
    └──────────┘  └──────────┘
    attackId:     attackId:
    "BDP_Arc..."  "BDP_Sco..."

  场景：左手=蝎子枪（远程），右手=蝎子枪（远程）→ 路径C远程+远程

    ┌──────────┐  ┌──────────┐
    │ 蝎子枪   │  │ 双持蝎子 │
    │ (单发)   │  │ (交替连射)│
    │          │  │ ★primary │
    └──────────┘  └──────────┘
    attackId:     attackId:
    "BDP_Sco..."  "dual:BDP_Sco...+BDP_Sco..."

    注意：同芯片双持时AreSameChip=true，
    不生成右手独立Gizmo，只有左手单发+双手合成。

  场景：左手=弧月刃（远程+齐射），右手=蝎子枪（远程+齐射）

    ┌──────────┐  ┌──────────┐  ┌──────────┐
    │ 弧月刃   │  │ 蝎子枪   │  │ 双持连射 │
    │ (单发)   │  │ (单发)   │  │ (交替)   │
    │ 右键:齐射│  │ 右键:齐射│  │ 右键:齐射│
    └──────────┘  └──────────┘  └──────────┘
    volleyVerb:   volleyVerb:   volleyVerb:
    leftVolley    rightVolley   dualVolley

  **GizmoOnGUIInt三种拦截路径**：

  GizmoOnGUIInt(rect, parms)
  │
  ├─ 检测鼠标在Gizmo区域内 + 鼠标按下事件
  │
  ├─ 路径①：左键 + 单侧引导弹
  │   │
  │   │  条件：verb is Verb_BDPRangedBase ranged
  │   │         && ranged.SupportsGuided == true
  │   │
  │   │  为什么拦截：
  │   │    原版Command_VerbTarget左键 → Find.Targeter.BeginTargeting()
  │   │    原版Targeter不允许地面瞄准（只能选Thing目标）
  │   │    引导弹需要选择地面锚点作为折线路径中间点
  │   │
  │   │  处理：
  │   │    ranged.StartGuidedTargeting()
  │   │    → 进入§3.3.3的多步锚点瞄准状态机
  │   │    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera()  音效反馈
  │   │    return GizmoState.Clear  阻止引擎继续处理
  │   │
  │   └─ 结果：玩家进入锚点选择→最终目标选择→发射的完整流程
  │
  ├─ 路径②：左键 + 双手引导弹
  │   │
  │   │  条件：verb is Verb_BDPDualRanged dual
  │   │         && dual.HasGuidedSide == true
  │   │         （任一侧支持引导即触发）
  │   │
  │   │  处理：同路径①，调用dual.StartGuidedTargeting()
  │   │
  │   │  注意：Verb_BDPDualVolley同理检查
  │   │
  │   └─ 结果：双侧引导瞄准，引导侧走折线，非引导侧直射
  │
  ├─ 路径③：右键 + 有齐射Verb
  │   │
  │   │  条件：volleyVerb != null
  │   │         && 右键点击事件
  │   │
  │   │  为什么拦截：
  │   │    原版右键无特殊行为（或打开verb选择菜单）
  │   │    BDP用右键触发齐射模式（同一tick发射所有子弹）
  │   │
  │   │  处理（按volleyVerb类型分三条子路径）：
  │   │
  │   │  ┌─ volleyVerb is Verb_BDPRangedBase vr && vr.SupportsGuided
  │   │  │   └─ vr.StartGuidedTargeting()  引导齐射
  │   │  │
  │   │  ├─ volleyVerb is Verb_BDPDualVolley dv && dv.HasGuidedSide
  │   │  │   └─ dv.StartGuidedTargeting()  双侧引导齐射
  │   │  │
  │   │  └─ 其他（普通齐射）
  │   │      └─ Find.Targeter.BeginTargeting(volleyVerb)
  │   │         标准瞄准→选目标→发射齐射
  │   │
  │   └─ 结果：右键触发齐射版本的攻击
  │
  └─ 无拦截命中 → 返回base.GizmoOnGUIInt()
      引擎标准处理（普通左键瞄准→选目标→单发攻击）

  **交互总览矩阵**：

  | 鼠标操作 | verb类型 | 条件 | 行为 |
  |---------|---------|------|------|
  | 左键 | 普通远程 | !SupportsGuided | 原版Targeter瞄准→单发攻击 |
  | 左键 | 引导远程 | SupportsGuided | 多步锚点瞄准→引导弹攻击 |
  | 左键 | 双手引导 | HasGuidedSide | 多步锚点瞄准→双侧引导攻击 |
  | 左键 | 近战 | — | 原版近战瞄准→近战攻击 |
  | 右键 | 任意 | volleyVerb==null | 无特殊行为 |
  | 右键 | 普通远程 | volleyVerb!=null, !Guided | 标准瞄准→齐射 |
  | 右键 | 引导远程 | volleyVerb!=null, Guided | 多步锚点瞄准→引导齐射 |
  | 右键 | 双手引导 | volleyVerb!=null, HasGuided | 多步锚点瞄准→双侧引导齐射 |

  **Desc属性增强**：

  volleyVerb != null时：
    原始描述 + "\n右键：齐射"

  示例：
    "弧月刃远程攻击。射程24，伤害12。
     右键：齐射"

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

  #### 3.6.1 序列化策略

  **完整字段分类**：

  CompTriggerBody 序列化字段总览
  │
  ├─ 序列化字段（PostExposeData中显式处理）
  │   │
  │   ├─ 槽位数据
  │   │   ├── leftHandSlots    Scribe_Collections  LookMode.Deep
  │   │   ├── rightHandSlots   Scribe_Collections  LookMode.Deep
  │   │   └── specialSlots     Scribe_Collections  LookMode.Deep
  │   │
  │   ├─ 切换状态机
  │   │   ├── leftSwitchCtx    Scribe_Deep         （null=Idle）
  │   │   └── rightSwitchCtx   Scribe_Deep         （null=Idle）
  │   │
  │   ├─ 战斗体状态
  │   │   └── isCombatBodyActive  Scribe_Values    bool
  │   │
  │   └─ 芯片Verb（v8.0 PMS重构）
  │       └── savedChipVerbs   Scribe_Collections  LookMode.Deep
  │
  └─ 不序列化字段（运行时重建）
      │
      ├─ Verb实例缓存
      │   ├── leftHandAttackVerb     ┐
      │   ├── rightHandAttackVerb    │ 由PostLoadInit
      │   ├── dualAttackVerb         │ → effect.Activate
      │   ├── leftHandVolleyVerb     │ → RebuildVerbs
      │   ├── rightHandVolleyVerb    │ → CreateAndCacheChipVerbs
      │   └── dualVolleyVerb         ┘ 重建
      │
      ├─ 按侧Verb原始数据
      │   ├── leftHandActiveVerbProps   ┐ 由PostLoadInit
      │   ├── rightHandActiveVerbProps  │ → WeaponChipEffect.Activate
      │   ├── leftHandActiveTools       │ → SetSideVerbs
      │   └── rightHandActiveTools      ┘ 恢复
      │
      ├─ 引用缓存
      │   ├── dualHandLockSlot          由PostLoadInit遍历激活槽位重建
      │   └── grantedCombos             由PostLoadInit →
  TryGrantComboAbility重建
      │
      └─ 临时上下文
          ├── ActivatingSide            仅DoActivate/DeactivateSlot期间有值
          └── ActivatingSlot            同上

  **为什么这些字段不序列化**：

  | 字段类别 | 不序列化原因 |
  |---------|------------|
  | Verb实例缓存 | IChipEffect无状态设计，Activate()可幂等重建所有Verb |
  | 按侧Verb原始数据 | WeaponChipEffect.Activate()会重新调用SetSideVerbs写入 |
  | dualHandLockSlot | 遍历激活槽位即可重建（isDualHand标记） |
  | grantedCombos | Ability本身由Pawn.abilities序列化，grantedCombos只是查询缓存
   |
  | 临时上下文 | 仅在方法调用栈内有效，不跨tick |

  **savedChipVerbs完整机制**：

  ┌─ 问题 ──────────────────────────────────────────────────────────────┐
  │                                                                      │
  │  Job/Stance的verbToUse字段在读档时通过ResolvingCrossRefs阶段         │
  │  按loadID查找Verb实例。                                              │
  │                                                                      │
  │  芯片Verb脱离VerbTracker.AllVerbs → 不在引擎的标准Verb注册表中       │
  │  → ResolvingCrossRefs找不到 → Job/Stance的verbToUse变成null          │
  │  → 攻击Job在读档后立即失败                                           │
  │                                                                      │
  └──────────────────────────────────────────────────────────────────────┘

  ┌─ 解决方案（v8.0 PMS重构）────────────────────────────────────────────┐
  │                                                                      │
  │  存档时：收集芯片Verb → 序列化（带loadID）                           │
  │  读档时：反序列化恢复Verb实例 → CreateAndCacheChipVerbs优先复用      │
  │  效果：Job/Stance的verbToUse引用指向同一实例，不断裂                 │
  │                                                                      │
  └──────────────────────────────────────────────────────────────────────┘

  **loadID生成规则**：

  格式: "BDP_Chip_{parent.ThingID}_{index}"

    parent.ThingID = 触发体的唯一ID（如"BDP_TriggerBody_12345"）
    index = 该Verb在composedVerbs列表中的索引（0-based）

  示例:
    "BDP_Chip_BDP_TriggerBody_12345_0"  → leftHandAttackVerb
    "BDP_Chip_BDP_TriggerBody_12345_1"  → rightHandAttackVerb
    "BDP_Chip_BDP_TriggerBody_12345_2"  → dualAttackVerb

  齐射Verb的loadID:
    "BDP_ChipVolley_{parent.ThingID}_{index}"
    与攻击Verb区分前缀，避免冲突

  **存档→读档完整时序**：

  ═══ 存档阶段 ═══

  PostExposeData() [Scribe.mode == Saving]
  │
  ├─ 收集savedChipVerbs：
  │   savedChipVerbs = new List()
  │   if (leftHandAttackVerb != null)  savedChipVerbs.Add(leftHandAttackVerb)
  │   if (rightHandAttackVerb != null) savedChipVerbs.Add(rightHandAttackVerb)
  │   if (dualAttackVerb != null)      savedChipVerbs.Add(dualAttackVerb)
  │   if (leftHandVolleyVerb != null)  savedChipVerbs.Add(leftHandVolleyVerb)
  │   if (rightHandVolleyVerb != null) savedChipVerbs.Add(rightHandVolleyVerb)
  │   if (dualVolleyVerb != null)      savedChipVerbs.Add(dualVolleyVerb)
  │   （最多6个Verb实例）
  │
  ├─ Scribe_Collections.Look(ref savedChipVerbs, "chipVerbs", LookMode.Deep)
  │   每个Verb的ExposeData()序列化：loadID, verbProps等
  │
  └─ 其他字段正常序列化...

  ═══ 读档阶段 ═══

  PostExposeData() [Scribe.mode == Loading]
  │
  ├─ Scribe_Collections反序列化savedChipVerbs
  │   恢复Verb实例列表（带loadID）
  │
  └─ 其他字段正常反序列化...

  ResolvingCrossRefs阶段（引擎自动）
  │
  └─ Job/Stance的verbToUse通过loadID查找Verb实例
      savedChipVerbs中的Verb已注册到引擎的loadID查找表
      → 引用正确解析 ✓

  PostExposeData() [Scribe.mode == PostLoadInit]
  │
  ├─ 恢复激活效果（见§3.6.2）
  │   → WeaponChipEffect.Activate → SetSideVerbs → RebuildVerbs
  │     → CreateAndCacheChipVerbs
  │
  └─ CreateAndCacheChipVerbs中的FindSavedVerb：
      对每个composedVerb生成expectedLoadID
      → FindSavedVerb(expectedLoadID)
        ├─ 命中：复用已反序列化的Verb实例（与Job/Stance引用同一对象）
        └─ 未命中：Activator.CreateInstance创建新实例

  **FindSavedVerb匹配逻辑**：

  FindSavedVerb(string loadID) → Verb
  │
  ├─ savedChipVerbs == null？→ return null
  │
  ├─ 遍历savedChipVerbs：
  │   verb.loadID == loadID？
  │   ├─ 命中：
  │   │   保存引用
  │   │   RemoveAt(i)  ← 从列表移除，避免重复匹配
  │   │   return verb
  │   └─ 未命中：继续
  │
  └─ 遍历完毕未找到 → return null

  设计要点：
    · RemoveAt防止同一Verb被多次匹配
    · 遍历完成后savedChipVerbs应为空（所有Verb都被复用）
    · 若有残留说明loadID格式变更或芯片配置变化，残留Verb被GC回收

  #### 3.6.2 读档恢复流程

  **PostLoadInit阶段完整步骤**：

  PostExposeData() [Scribe.mode == PostLoadInit]
  │
  ├─ Step 1: 槽位空值修复
  │   │
  │   │  旧存档可能缺失某些槽位列表（版本升级新增字段）
  │   │
  │   ├─ leftHandSlots == null？→ InitSlots(LeftHand, Props.leftHandSlotCount)
  │   ├─ rightHandSlots == null 且 Props.hasRightHand？→ InitSlots(RightHand,
  ...)
  │   └─ specialSlots == null 且 Props.specialSlotCount > 0？→
  InitSlots(Special, ...)
  │
  ├─ Step 2: 获取OwnerPawn
  │   │
  │   │  OwnerPawn通过属性链获取：
  │   │    CompEquippable.parent（触发体Thing）
  │   │    → Holder（Pawn_EquipmentTracker）
  │   │    → Pawn
  │   │
  │   ├─ OwnerPawn == null？
  │   │   └─ 跳过后续所有恢复（触发体掉在地上，无持有者）
  │   │      芯片效果在下次Notify_Equipped时恢复
  │   │
  │   └─ OwnerPawn != null → 继续
  │
  ├─ Step 3: 注册Trion耗尽事件
  │   │
  │   │  trion = OwnerPawn.GetComp()
  │   │  trion.OnAvailableDepleted += OnTrionDepleted
  │   │
  │   │  原因：事件委托不序列化，读档后必须重新注册
  │   │  时机：在效果恢复之前注册，保证恢复过程中Trion耗尽能被捕获
  │   │
  │
  ├─ Step 4: 遍历所有槽位恢复激活效果
  │   │
  │   │  AllSlots() = leftHandSlots ∪ rightHandSlots ∪ specialSlots
  │   │
  │   │  对每个slot：
  │   │
  │   │  ┌─ slot.isActive == false 或 slot.loadedChip == null？
  │   │  │   └─ 跳过
  │   │  │
  │   │  └─ slot.isActive == true 且 slot.loadedChip != null：
  │   │      │
  │   │      ├─ chipComp = slot.loadedChip.GetComp()
  │   │      ├─ effect = chipComp.GetEffect()  → IChipEffect实例
  │   │      │
  │   │      ├─ 设置临时上下文：
  │   │      │   ActivatingSide = slot.side
  │   │      │   ActivatingSlot = slot
  │   │      │
  │   │      ├─ try:
  │   │      │   effect.Activate(pawn, parent)
  │   │      │   │
  │   │      │   │  各效果类的恢复行为：
  │   │      │   │
  │   │      │   │  WeaponChipEffect.Activate:
  │   │      │   │    SetSideVerbs(side, verbs, tools)  写入按侧数据
  │   │      │   │    RebuildVerbs(pawn)                重建VerbTracker
  │   │      │   │    → CreateAndCacheChipVerbs          合成+创建Verb
  │   │      │   │      → FindSavedVerb(loadID)          复用已反序列化实例
  │   │      │   │
  │   │      │   │  ShieldChipEffect.Activate:
  │   │      │   │    pawn.health.AddHediff(shieldHediffDef)
  │   │      │   │    （幂等：若Hediff已存在则不重复添加）
  │   │      │   │
  │   │      │   │  UtilityChipEffect.Activate:
  │   │      │   │    pawn.abilities.GainAbility(abilityDef)
  │   │      │   │    （幂等：若Ability已存在则不重复添加）
  │   │      │   │
  │   │      ├─ finally:
  │   │      │   ActivatingSide = null    ← C3修复：保证异常时也清除
  │   │      │   ActivatingSlot = null
  │   │      │
  │   │      └─ chipComp.Props.isDualHand == true？
  │   │          └─ dualHandLockSlot = slot  重建双手锁定引用
  │   │
  │
  └─ Step 5: 组合能力隐式恢复
      │
      │  不需要显式步骤。
      │  Step 4中WeaponChipEffect.Activate → RebuildVerbs结束后，
      │  DoActivate的后续逻辑（TryGrantComboAbility）不在此路径中。
      │
      │  但Ability本身由Pawn.abilities序列化/反序列化恢复，
      │  grantedCombos列表在首次需要时由TryRevokeComboAbilities重建。
      │
      │  实际上：读档后组合Ability已在Pawn.abilities中（引擎序列化），
      │  grantedCombos在下次芯片切换时通过TryGrant/TryRevoke自然重建。

  **IChipEffect无状态设计如何使幂等恢复成为可能**：

  ┌─ 传统有状态设计（未采用）─────────────────────────────────────────┐
  │                                                                    │
  │  class WeaponChipEffect {                                          │
  │    List createdVerbs;    // 运行时状态                       │
  │    SlotSide activeSide;       // 运行时状态                       │
  │  }                                                                 │
  │                                                                    │
  │  问题：                                                            │
  │    · 需要序列化Effect内部状态                                      │
  │    · Activate()不可重复调用（会重复创建Verb）                      │
  │    · 读档恢复需要区分"首次激活"和"恢复激活"两条路径               │
  │    · 版本升级时Effect状态格式变化导致存档不兼容                    │
  │                                                                    │
  └────────────────────────────────────────────────────────────────────┘

  ┌─ BDP无状态设计（采用）───────────────────────────────────────────┐
  │                                                                    │
  │  class WeaponChipEffect : IChipEffect {                            │
  │    // 无实例字段                                                   │
  │    void Activate(pawn, triggerBody) {                               │
  │      var side = triggerBody.ActivatingSide;   // 从上下文读取       │
  │      var cfg = triggerBody.GetChipExtension(); // 从Def读取         │
  │      triggerBody.SetSideVerbs(side, cfg.verbs, cfg.tools);         │
  │      triggerBody.RebuildVerbs(pawn);                               │
  │    }                                                               │
  │  }                                                                 │
  │                                                                    │
  │  优势：                                                            │
  │    · 无需序列化Effect状态（零序列化负担）                          │
  │    · Activate()天然幂等（SetSideVerbs覆盖写入，RebuildVerbs全量重建）│
  │    · 读档恢复 = 正常激活（同一条代码路径）                         │
  │    · 版本升级只需保证Def数据兼容                                   │
  │                                                                    │
  │  代价：                                                            │
  │    · 需要临时上下文机制（ActivatingSide/ActivatingSlot）           │
  │    · 需要三级回退定位策略（§3.3.3）                                │
  │                                                                    │
  └────────────────────────────────────────────────────────────────────┘

  **恢复顺序的重要性**：

  正确顺序（当前实现）：

    ① 注册Trion事件        ← 先注册，保证后续操作中Trion耗尽能被捕获
    ② 恢复左手芯片效果      ← WeaponChipEffect.Activate → SetSideVerbs
    ③ 恢复右手芯片效果      ← 同上，此时两侧数据都已写入
    ④ RebuildVerbs          ← 最后一次Activate触发，合成双手Verb

    关键：最后一个WeaponChipEffect.Activate触发的RebuildVerbs
    会看到两侧的VerbProps数据，正确合成双手Verb。

  错误顺序（假设先恢复效果再注册事件）：

    ① 恢复芯片效果          ← Trion消耗但事件未注册
    ② Trion耗尽             ← OnTrionDepleted未被调用！
    ③ 注册Trion事件          ← 为时已晚
    → 战斗体应该被解除但没有，状态不一致

  #### 3.6.3 生命周期事件

  ##### PostSpawnSetup — 初始化与预装

  PostSpawnSetup(bool respawningAfterLoad)
  │
  ├─ base.PostSpawnSetup(respawningAfterLoad)
  │
  ├─ 守卫条件：respawningAfterLoad == true？→ return
  │   原因：读档恢复由PostExposeData.PostLoadInit处理
  │
  ├─ 守卫条件：leftHandSlots != null？→ return
  │   原因（B5修复）：触发体从装备卸下掉到地面时
  │     GenSpawn.Spawn → PostSpawnSetup(false)
  │     若无此守卫，会清空已装载的芯片数据
  │
  ├─ 初始化槽位：
  │   leftHandSlots = InitSlots(LeftHand, Props.leftHandSlotCount)
  │   Props.hasRightHand？
  │     → rightHandSlots = InitSlots(RightHand, Props.rightHandSlotCount)
  │   Props.specialSlotCount > 0？
  │     → specialSlots = InitSlots(Special, Props.specialSlotCount)
  │
  └─ 预装芯片（Props.preloadedChips）：
      遍历每个PreloadedChipConfig：
      ├─ cfg.chipDef == null？→ 跳过
      ├─ chip = ThingMaker.MakeThing(cfg.chipDef)
      └─ LoadChipInternal(cfg.side, cfg.slotIndex, chip)
          不检查allowChipManagement（预装绕过权限）

  ##### Notify_Equipped / Notify_Unequipped

  Notify_Equipped(Pawn pawn)              Notify_Unequipped(Pawn pawn)
  │                                        │
  ├─ base.Notify_Equipped(pawn)           ├─ trion.OnAvailableDepleted
  │                                        │   -= OnTrionDepleted
  ├─ EnsureSlotsInitialized()             │   注意：用参数pawn获取trion
  │   兼容CharacterEditor等外部工具       │   而非OwnerPawn（此时可能已null）
  │   跳过PostSpawnSetup直接装备的场景    │
  │                                        ├─ DeactivateAll(pawn)
  ├─ trion.OnAvailableDepleted            │   显式传入pawn引用
  │   += OnTrionDepleted                  │
  │                                        └─ base.Notify_Unequipped(pawn)
  └─ Props.autoActivateOnEquip？
      遍历所有已装载未激活槽位
      → ActivateChip(slot.side, slot.index)

  **DeactivateAll完整流程**：

  DeactivateAll(Pawn pawn = null)
  │
  ├─ pawn ?? OwnerPawn  回退获取pawn引用
  │
  ├─ 遍历AllSlots()：
  │   slot.isActive == true？
  │   └─ try:
  │       DeactivateSlot(slot, pawn)
  │       ├─ UnregisterDrain("chip_{side}_{index}")
  │       ├─ 设置ActivatingSide/ActivatingSlot上下文
  │       ├─ effect.Deactivate(pawn, parent)
  │       ├─ slot.isActive = false
  │       ├─ dualHandLockSlot == slot？→ dualHandLockSlot = null
  │       └─ TryRevokeComboAbilities(pawn)
  │     catch:
  │       slot.isActive = false  ← 异常时强制关闭，不影响其他槽位
  │
  ├─ 清除按侧Verb数据：
  │   leftHandActiveVerbProps = null
  │   rightHandActiveVerbProps = null
  │   leftHandActiveTools = null
  │   rightHandActiveTools = null
  │
  ├─ 清除切换上下文：
  │   leftSwitchCtx = null
  │   rightSwitchCtx = null
  │
  └─ 清除双手锁定：
      dualHandLockSlot = null

  ##### PostDestroy

  PostDestroy(DestroyMode mode, Map previousMap)
  │
  ├─ DeactivateAll()  无显式pawn参数，使用OwnerPawn
  │
  └─ base.PostDestroy(mode, previousMap)

  与Notify_Unequipped的区别：
  ┌──────────────────────┬──────────────────────┐
  │ Notify_Unequipped    │ PostDestroy          │
  ├──────────────────────┼──────────────────────┤
  │ 注销Trion事件        │ 不注销（GC回收）     │
  │ 显式传入pawn         │ 使用OwnerPawn        │
  │ 触发体仍存在         │ 触发体即将销毁       │
  └──────────────────────┴──────────────────────┘

  ##### 战斗体激活与解除

  DismissCombatBody()
  │
  ├─ DeactivateAll()
  │   关闭所有激活芯片（效果、Verb、持续消耗全部清除）
  │
  ├─ trion.Release(trion.Allocated)
  │   释放全部已占用的Trion回到可用池
  │   关键：基于trion.Allocated（Single Source of Truth）
  │   不依赖芯片是否仍在槽位中，避免数据不一致
  │
  └─ IsCombatBodyActive = false

  ActivateCombatBody（当前仅作为调试按钮存在，未提取为正式方法）
  │
  ├─ CanGenerateCombatBody()？
  │   Trion是否足够支撑战斗体
  │
  ├─ IsCombatBodyActive = true
  │
  ├─ 遍历所有已装载芯片：
  │   trion.Allocate(chipComp.Props.allocationCost)
  │   占用Trion（降低可用上限）
  │
  └─ ActivateAllSpecial()
      激活所有特殊槽芯片（立即生效，无前摇）

  ##### OnTrionDepleted安全阀

  OnTrionDepleted()  ← CompTrion.OnAvailableDepleted事件触发
  │
  ├─ IsCombatBodyActive == false？→ return
  │   已经没有战斗体，无需处理
  │
  └─ DismissCombatBody()
      Trion耗尽 = 无法维持战斗体 → 自动解除

  触发链路：
    CompTrion.CompTick()
      └─ 持续消耗结算 → Available降为0
          └─ OnAvailableDepleted?.Invoke()
              └─ CompTriggerBody.OnTrionDepleted()
                  └─ DismissCombatBody()
                      └─ DeactivateAll() + Release + false

  ##### 完整生命周期时序图

  ═══ 触发体创建 ═══

    ThingMaker.MakeThing(triggerBodyDef)
      └─ PostSpawnSetup(false)
          ├─ InitSlots（创建空槽位列表）
          └─ PreloadChips（近界/黑触发器预装芯片）

  ═══ 装备到Pawn ═══

    Pawn.equipment.AddEquipment(triggerBody)
      └─ Notify_Equipped(pawn)
          ├─ EnsureSlotsInitialized
          ├─ trion.OnAvailableDepleted += OnTrionDepleted
          └─ autoActivateOnEquip？→ ActivateChip(...)

  ═══ 正常使用 ═══

    玩家操作：
      ActivateCombatBody → IsCombatBodyActive=true + Allocate
      ActivateChip → 状态机 → DoActivate → effect.Activate
      DeactivateChip → DeactivateSlot → effect.Deactivate
      切换芯片 → WindingDown → WarmingUp → DoActivate

    自动事件：
      Trion耗尽 → OnTrionDepleted → DismissCombatBody

  ═══ 存档 ═══

    PostExposeData(Saving)
      ├─ 序列化槽位/状态机/战斗体状态
      └─ 收集savedChipVerbs → 序列化

  ═══ 读档 ═══

    PostExposeData(Loading)
      └─ 反序列化所有字段

    ResolvingCrossRefs
      └─ Job/Stance的verbToUse通过loadID找到savedChipVerbs中的Verb

    PostExposeData(PostLoadInit)
      ├─ 修复null槽位
      ├─ 注册Trion事件
      ├─ 遍历激活槽位 → effect.Activate（幂等恢复）
      │   └─ WeaponChipEffect → SetSideVerbs → RebuildVerbs
      │       → CreateAndCacheChipVerbs → FindSavedVerb复用
      └─ 重建dualHandLockSlot

    PostSpawnSetup(true)
      └─ 仅base处理（守卫跳过初始化）

  ═══ 卸下装备 ═══

    Pawn.equipment.Remove(triggerBody)
      └─ Notify_Unequipped(pawn)
          ├─ trion.OnAvailableDepleted -= OnTrionDepleted
          └─ DeactivateAll(pawn)
              └─ 所有效果关闭 + Verb清除 + 状态机重置

  ═══ 触发体销毁 ═══

    triggerBody.Destroy()
      └─ PostDestroy
          └─ DeactivateAll()

  ═══ 触发体掉落地面（非销毁）═══

    GenSpawn.Spawn(triggerBody, cell, map)
      └─ PostSpawnSetup(false)
          └─ leftHandSlots != null → return（B5守卫，不覆盖数据）

#### 3.6.4 核心不变量与边界情况

  **序列化不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㊵ | savedChipVerbs的loadID与CreateAndCacheChipVerbs生成格式一致 |
  PostExposeData / CreateAndCacheChipVerbs |
  读档后FindSavedVerb匹配失败，Job引用断裂 |
  | ㊶ | PostLoadInit中先注册Trion事件再恢复效果 | PostExposeData顺序 |
  恢复期间Trion耗尽不被捕获 |
  | ㊷ | isActive==true的槽位loadedChip!=null | ChipSlot.ExposeData防御校验 |
  空引用（不变量⑥的序列化层守卫） |
  | ㊸ | IChipEffect无状态，Activate()幂等 | 各Effect实现 |
  读档恢复重复调用导致副作用叠加 |
  | ㊹ | 事件委托不序列化，读档后必须重新注册 | PostLoadInit / Notify_Equipped |
   Trion耗尽事件丢失，战斗体不自动解除 |

  **生命周期不变量**：

  | # | 不变量 | 维护者 | 违反后果 |
  |---|--------|--------|---------|
  | ㊺ | PostSpawnSetup(false)中leftHandSlots!=null时跳过初始化 |
  PostSpawnSetup守卫 | 触发体掉落地面时已装载芯片被清空 |
  | ㊻ | Notify_Unequipped用参数pawn而非OwnerPawn | Notify_Unequipped |
  卸下时OwnerPawn已null，无法获取trion注销事件 |
  | ㊼ | DeactivateAll中单slot异常不影响其他slot | try/catch保护 |
  一个芯片Deactivate失败导致其余芯片永远激活 |
  | ㊽ | DismissCombatBody基于trion.Allocated释放 | DismissCombatBody |
  基于芯片计算释放量可能与实际占用不一致 |

  **边界情况**：

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：触发体从装备卸下掉到地面再被捡起                                │
  │                                                                      │
  │ 时序：                                                               │
  │   ① Notify_Unequipped(pawnA) → DeactivateAll → 效果全部关闭         │
  │   ② GenSpawn.Spawn(triggerBody, cell) → PostSpawnSetup(false)       │
  │      leftHandSlots != null → return（B5守卫，数据保留）              │
  │   ③ pawnB.equipment.AddEquipment → Notify_Equipped(pawnB)           │
  │      → 注册新Trion事件 → autoActivate？→ 重新激活                   │
  │                                                                      │
  │ 关键：芯片装载数据在整个过程中保持不变，只有激活状态被重置            │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：读档时OwnerPawn为null（触发体掉在地上）                         │
  │                                                                      │
  │ PostLoadInit中：                                                     │
  │   OwnerPawn == null → 跳过所有恢复逻辑                               │
  │   槽位数据已由Scribe反序列化恢复（isActive/loadedChip等）            │
  │   但效果未恢复（无Verb、无Hediff、无Ability）                        │
  │                                                                      │
  │ 后续：pawn捡起触发体 → Notify_Equipped                              │
  │   autoActivateOnEquip == true？→ 遍历激活槽位重新ActivateChip       │
  │   autoActivateOnEquip == false？→ 槽位数据保留，等待玩家手动激活     │
  │                                                                      │
  │ 注意：isActive标记在地面期间保持为true（序列化值），                  │
  │       但实际效果未生效。Notify_Equipped的autoActivate逻辑             │
  │       检查的是"已装载未激活"，不会重复激活已标记active的槽位。        │
  │       这意味着掉落地面的触发体被捡起后，                              │
  │       需要先DeactivateAll再重新激活，或在Notify_Equipped中            │
  │       对isActive==true的槽位也执行effect.Activate恢复。              │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：旧存档兼容（缺失新增字段）                                      │
  │                                                                      │
  │ Scribe反序列化时，不存在的XML节点返回默认值：                        │
  │   · specialSlots → null（Scribe_Collections返回null）               │
  │   · leftSwitchCtx → null（Scribe_Deep返回null = Idle状态）          │
  │   · savedChipVerbs → null（无芯片Verb可复用）                       │
  │                                                                      │
  │ PostLoadInit中的空值修复：                                           │
  │   specialSlots == null 且 Props.specialSlotCount > 0？               │
  │   → InitSlots创建空槽位列表                                         │
  │                                                                      │
  │ savedChipVerbs == null时：                                           │
  │   FindSavedVerb始终返回null                                         │
  │   → CreateAndCacheChipVerbs全部用Activator.CreateInstance新建       │
  │   → 旧存档的Job/Stance引用可能断裂（已知限制，旧存档升级代价）      │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：DeactivateAll中某个芯片Deactivate抛异常                         │
  │                                                                      │
  │ 处理：                                                               │
  │   try {                                                              │
  │     DeactivateSlot(slot, pawn);                                      │
  │   } catch (Exception ex) {                                           │
  │     Log.Error("...");                                                │
  │     slot.isActive = false;  ← 强制关闭，维护数据一致性              │
  │   }                                                                  │
  │   // 继续处理下一个slot                                              │
  │                                                                      │
  │ 设计意图：                                                           │
  │   一个芯片的Effect.Deactivate失败不应阻止其他芯片的关闭。            │
  │   强制isActive=false保证不变量⑥不被永久违反。                        │
  │   可能的副作用残留（如Hediff未移除）由玩家手动处理或下次激活时覆盖。 │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：Trion事件重复注册                                               │
  │                                                                      │
  │ 可能路径：                                                           │
  │   PostLoadInit注册一次 → Notify_Equipped再注册一次                   │
  │   （读档后触发体已在Pawn身上，两个路径都会执行）                      │
  │                                                                      │
  │ 防御：C#委托的 += 对同一方法多次注册会导致多次调用。                  │
  │       OnTrionDepleted内部有IsCombatBodyActive守卫，                   │
  │       第一次调用DismissCombatBody后IsCombatBodyActive=false，         │
  │       第二次调用直接return。                                         │
  │       功能正确但有微小性能浪费（事件触发两次）。                      │
  └──────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────┐
  │ 场景：EnsureSlotsInitialized的防御作用                                │
  │                                                                      │
  │ CharacterEditor等外部工具可能：                                      │
  │   直接调用Pawn.equipment.AddEquipment(triggerBody)                   │
  │   跳过GenSpawn.Spawn → 跳过PostSpawnSetup                           │
  │   → leftHandSlots为null                                             │
  │                                                                      │
  │ Notify_Equipped中EnsureSlotsInitialized()：                          │
  │   leftHandSlots == null？→ 执行InitSlots初始化                      │
  │   leftHandSlots != null？→ 跳过（正常路径）                         │
  │                                                                      │
  │ 保证：无论通过何种路径装备触发体，槽位列表在首次使用前一定已初始化   │
  └──────────────────────────────────────────────────────────────────────┘



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


## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v2.0 | 2026-02-25 | 从旧版6.2(v1.5)全面重写。概念层重写：去除C#代码片段，改用架构图和概念描述。新增PMS弹道模块系统（§3.4）。新增齐射Verb和引导飞行概要。重组为6个子系统结构，预留详细设计插入点。精选10条核心设计决策。 | Claude Opus 4.6 |
| v2.1 | 2026-02-25 | 填充§3.1槽位与状态机系统详细设计（数据结构定义、完整激活流程、14条核心不变量、7个边界情况）。填充§3.2芯片效果系统详细设计（IChipEffect完整协议、三种策略实现细节、两层Trion消耗模型、组合能力系统）。更新填充计划状态。 | Claude Opus 4.6 |
| v2.2 | 2026-02-26 | 与AI讨论并设计好了剩余未填充的章节，AI工具出错未正确更新文档，用户手动粘贴填充 | user |
