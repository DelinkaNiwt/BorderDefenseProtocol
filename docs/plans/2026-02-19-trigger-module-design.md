---
标题：6.2 触发器模块详细设计
版本号: v1.0
更新日期: 2026-02-19
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: BDP触发器模块的完整架构设计。覆盖触发体（Equipment）、芯片槽位系统（主副双侧）、IChipEffect抽象层（武器/护盾/隐身/辅助四类效果的RimWorld机制映射）、三种触发器体系统一方案（BORDER/近界/黑触发器共享底层）、消耗模型修正（按次/受击/持续三类）、微内核扩展（RegisterDrain聚合结算）。基于方案B（ChipEffect抽象层）设计，遵循SOLID/KISS/YAGNI/数据驱动原则。
---

# 6.2 触发器模块详细设计

## 前置文档

| 文档 | 关系 | 说明 |
|------|------|------|
| 1.3 触发器机制笔记 | 需求来源 | 三层架构（触发体/芯片/Trion表现）、8槽系统、主副双持、消耗类型 |
| 2.3 触发器三层架构过程分析 | 需求来源 | 触发器激活/切换/解除流程 |
| 2.6 产出物实体关系与游戏机制初步映射 | 实体映射 | 触发体→ThingDef(武器)、芯片→ThingDef(物品) |
| 5.1 模组总体架构设计 | 架构上下文 | 触发器模块为Layer 1，依赖BDP.Core |
| 6.1 Trion能量系统详细设计 | 微内核接口 | CompTrion API（Consume/Allocate/Release） |

## 设计原则

本设计遵循以下原则（按优先级排序）：

| 原则 | 在本设计中的体现 |
|------|----------------|
| **KISS** | IChipEffect接口仅4个方法；CompTriggerBody只管槽位和激活，不关心效果实现 |
| **SRP** | CompTriggerBody管槽位状态机、IChipEffect管效果、TriggerChipComp管芯片数据 |
| **OCP** | 新芯片类型只需实现IChipEffect接口，不修改任何现有代码 |
| **DIP** | CompTriggerBody依赖IChipEffect抽象接口，不依赖具体效果实现 |
| **数据驱动** | 槽位数量、冷却时间、消耗值全部通过XML CompProperties配置 |
| **YAGNI** | 不预设事件系统、不预设芯片组合逻辑，需要时再加 |
| **轮询优于事件** | CompTick驱动切换冷却和状态检查，符合RimWorld tick驱动架构 |

---

## 1. 设计总览

### 1.1 模块边界

```
┌─────────────────────────────────────────────────────────┐
│                触发器模块 (BDP.Trigger)                    │
│                                                           │
│  定位：微内核的第一层消费者                                  │
│  依赖：BDP.Core（CompTrion API）                           │
│  被依赖：BDP.Combat（检查Pawn是否有CompTriggerBody）         │
│                                                           │
│  职责边界：                                                 │
│    ✓ 触发体的物品定义和装备逻辑                              │
│    ✓ 芯片的物品定义、装载/卸载管理                           │
│    ✓ 主副侧槽位系统和激活状态机                              │
│    ✓ IChipEffect抽象层（定义接口+基础实现）                  │
│    ✗ 战斗体状态切换（BDP.Combat负责）                        │
│    ✗ Trion数据持有（BDP.Core的CompTrion负责）                │
│    ✗ 芯片注册/Trion占用（BDP.Combat的HComp_TrionAllocate）  │
└─────────────────────────────────────────────────────────┘
```

### 1.2 组件职责总览

```
┌──────────────────┬──────────────────────────────────────────┐
│  组件              │  职责（一句话）                             │
├──────────────────┼──────────────────────────────────────────┤
│  CompTriggerBody  │  触发体核心：管理槽位状态机和激活逻辑         │
│  CompChipSlot     │  单个槽位的数据容器（芯片引用+激活状态）       │
│  TriggerChipComp  │  芯片物品上的组件：持有IChipEffect实现        │
│  IChipEffect      │  抽象接口：定义芯片效果的激活/关闭/Tick协议   │
│                   │  ← 扩展点：新芯片类型只需实现此接口           │
├──────────────────┼──────────────────────────────────────────┤
│  WeaponChipEffect │  [示例实现] 武器类：向触发体注入Verb/Tool     │
│  ShieldChipEffect │  [示例实现] 护盾类：添加伤害拦截Hediff        │
│  StealthChipEffect│  [示例实现] 隐身类：添加/移除状态Hediff       │
│  UtilityChipEffect│  [示例实现] 辅助类：授予Ability               │
│  ...              │  [未来扩展] 投射物类、移动类、感知类...        │
└──────────────────┴──────────────────────────────────────────┘
```

### 1.3 三种触发器体系的统一方式

```
BORDER触发器：
  ThingDef(触发体) + CompTriggerBody(allowChipManagement=true)
  → 玩家可见芯片槽位UI，可自由装载/卸载芯片

近界触发器：
  ThingDef(触发体) + CompTriggerBody(allowChipManagement=false)
  → 芯片预装且锁定，玩家看到的是"一体化武器"
  → 底层复用完全相同的CompTriggerBody + IChipEffect

黑触发器：
  ThingDef(触发体) + CompTriggerBody(allowChipManagement=false)
                  + equippedStatOffsets[BDP_TrionCapacity: +N]
  → 同近界，额外Trion容量通过Stat系统自动聚合
  → 无需自定义Comp，复用微内核已有机制（6.1 §5.2）
```

### 1.4 OCP验证

```
新增一种芯片效果类型（如投射物类）：
  → 新建 ProjectileChipEffect : IChipEffect
  → 不修改 CompTriggerBody、CompChipSlot、任何现有IChipEffect实现 ✓

新增一种触发器变体（如特殊近界触发器）：
  → 新建 ThingDef XML + 预装芯片配置
  → 不修改任何C#代码 ✓

黑触发器新增其他机制（未来）：
  → 纯数值类 → equippedStatOffsets/statOffsets（零C#）
  → 特殊行为类 → 新增专用Comp，不影响触发器核心系统 ✓
```

---

## 2. 核心数据模型

### 2.1 CompProperties_TriggerBody（XML可配置参数）

```xml
<li Class="BDP.Trigger.CompProperties_TriggerBody">

  <!-- ── 槽位配置 ── -->
  <mainSlotCount>4</mainSlotCount>
    <!-- 主侧槽位数量，默认4（原作设定） -->

  <subSlotCount>4</subSlotCount>
    <!-- 副侧槽位数量，默认4（原作设定） -->

  <!-- ── 切换配置 ── -->
  <switchCooldownTicks>30</switchCooldownTicks>
    <!-- 切换空窗期长度（ticks），默认30≈0.5秒 -->

  <!-- ── 权限配置 ── -->
  <allowChipManagement>true</allowChipManagement>
    <!-- 玩家是否可以装载/卸载芯片 -->
    <!-- BORDER触发器=true，近界/黑触发器=false -->

  <!-- ── 槽位侧配置 ── -->
  <hasSub>true</hasSub>
    <!-- 是否有副侧。某些近界触发器可能只有单侧 -->

  <!-- ── 预装芯片（近界/黑触发器用） ── -->
  <preloadedChips>
    <!-- 首次生成时自动装载的芯片列表 -->
    <!-- BORDER触发器不配置此项 -->
  </preloadedChips>

  <!-- ── 自动激活 ── -->
  <autoActivateOnEquip>false</autoActivateOnEquip>
    <!-- 装备时是否自动激活预装芯片 -->
    <!-- 近界/黑触发器可设为true -->

</li>
```

### 2.2 数据结构层次

```
CompTriggerBody（触发体上的核心Comp）
  ├── ChipSlot[] mainSlots     主侧槽位（数量由Props.mainSlotCount决定）
  ├── ChipSlot[] subSlots      副侧槽位（数量由Props.subSlotCount决定）
  ├── SwitchState switchState   当前切换状态（Idle / Switching）
  └── SwitchContext? pending    待切换的目标（切换中时有值）

ChipSlot（槽位数据类，非ThingComp）
  ├── int index                 槽位编号
  ├── SlotSide side             Main / Sub
  ├── Thing? loadedChip         已装载的芯片物品（null=空槽）
  └── bool isActive             该槽位的芯片是否当前激活

TriggerChipComp（芯片物品上的ThingComp）
  └── CompProperties_TriggerChip
        ├── SlotSide preferredSide      建议槽位侧（Main/Sub/Either）
        ├── float activationCost        激活时一次性Trion消耗
        ├── Type chipEffectClass        IChipEffect实现类（XML指定）
        └── ...                         效果类型专用配置（通过DefModExtension）
```

### 2.3 激活状态机

```
                    ActivateChip(slot)
                    （目标侧无激活芯片）
                         │
    ┌────────────────────▼────────────────────┐
    │              Idle（稳定）                 │
    │  mainActive = N 或 -1                    │◄──────────────┐
    │  subActive  = M 或 -1                    │               │
    └────────────────────┬────────────────────┘               │
                         │ ActivateChip(slot)                  │
                         │ （目标侧已有激活芯片）                │
                         ▼                                     │
    ┌────────────────────────────────────────┐                │
    │           Switching（切换空窗）          │                │
    │  旧芯片 IChipEffect.Deactivate() 已调用  │                │
    │  新芯片等待 cooldownTick                 │                │
    └────────────────────┬───────────────────┘                │
                         │ tick >= cooldownTick                │
                         │ 调用新芯片 IChipEffect.Activate()   │
                         └────────────────────────────────────┘

  注：从空侧激活芯片（无旧芯片）→ 直接调用Activate()，不经过Switching状态
```

### 2.4 不变量

```
任何操作后必须成立：

  ① 每侧激活芯片数 ≤ 1
  ② 已装载芯片数 ≤ 该侧槽位数（由Props决定）
  ③ hasSub == false 时，subSlots 为空，不参与任何逻辑
  ④ switchState == Switching 时，pending != null
  ⑤ switchState == Idle 时，pending == null
  ⑥ isActive == true 的槽位，loadedChip != null
  ⑦ allowChipManagement == false 时，loadedChip 不可被玩家修改
```

---

## 3. API规范：CompTriggerBody

### 3.1 方法总览

```
┌──────────────────────┬──────────────────────────────────────────┐
│  方法                  │  职责（一句话）                             │
├──────────────────────┼──────────────────────────────────────────┤
│  LoadChip()           │  将芯片物品装入指定槽位                      │
│  UnloadChip()         │  从槽位取出芯片物品                          │
│  ActivateChip()       │  激活指定槽位（含切换逻辑）                   │
│  DeactivateChip()     │  关闭指定侧的当前激活芯片                    │
│  DeactivateAll()      │  关闭所有激活芯片（卸下触发体时调用）          │
│  CanActivateChip()    │  前置条件检查（供UI灰显判断）                 │
│  CompTick()           │  切换冷却计时 + Trion耗尽检查                │
│  PostSpawnSetup()     │  初始化槽位数组 + 预装芯片                   │
│  PostUnequip()        │  卸下时关闭所有效果                          │
└──────────────────────┴──────────────────────────────────────────┘
```

### 3.2 LoadChip / UnloadChip

```
bool LoadChip(SlotSide side, int slotIndex, Thing chip)

前置条件：
  Props.allowChipManagement == true
  GetSlot(side, slotIndex).loadedChip == null
  chip.TryGetComp<TriggerChipComp>() != null

行为：
  slot.loadedChip = chip
  chip从持有者库存移除（被触发体"吸收"）
  return true

后置条件：
  slot.loadedChip == chip
  slot.isActive == false（装载不等于激活）

─────────────────────────────────────────────────────────────

bool UnloadChip(SlotSide side, int slotIndex)

前置条件：
  Props.allowChipManagement == true
  slot.loadedChip != null

行为：
  if slot.isActive:
    DeactivateChip(side)
  chip = slot.loadedChip
  slot.loadedChip = null
  chip生成到Pawn库存（或地面）
  return true
```

### 3.3 ActivateChip（核心方法）

```
bool ActivateChip(SlotSide side, int slotIndex)

前置条件检查（任一不满足则return false）：
  slot.loadedChip != null
  CanActivateChip(side, slotIndex) == true
  switchState == Idle（切换中不允许再次切换）

行为流程：

  ┌── 目标侧有激活芯片？ ──────────────────────────────────────┐
  │                                                            │
  │  YES（切换路径）：                                          │
  │    oldEffect.Deactivate(pawn, triggerBody)                 │
  │    oldSlot.isActive = false                                │
  │    switchState = Switching                                 │
  │    pending = { side, slotIndex, tick+Props.switchCooldown }│
  │    return true（激活将在CompTick中完成）                    │
  │                                                            │
  │  NO（直接激活路径）：                                       │
  │    CompTrion.Consume(chip.Props.activationCost)            │
  │    newEffect.Activate(pawn, triggerBody)                   │
  │    slot.isActive = true                                    │
  │    return true                                             │
  └────────────────────────────────────────────────────────────┘

设计决策：
  切换路径不立即消耗Trion，等空窗期结束后激活时再消耗。
  原因：如果空窗期内Trion耗尽，新芯片不应激活。
```

### 3.4 CompTick

```
CompTick()

// ── 1. 切换冷却计时 ──
if switchState == Switching:
  if TicksGame >= pending.cooldownTick:
    newSlot = GetSlot(pending.side, pending.slotIndex)
    if CanActivateChip(pending.side, pending.slotIndex):
      CompTrion.Consume(newSlot.chip.Props.activationCost)
      newSlot.effect.Activate(pawn, triggerBody)
      newSlot.isActive = true
    // CanActivate失败（如Trion不足）→ 静默取消
    switchState = Idle
    pending = null

// ── 2. Trion耗尽检查 ──
if CompTrion.Available <= 0 && HasAnyActiveChip():
  DeactivateAll()

// ── 3. 效果Tick ──
foreach activeSlot in AllActiveSlots():
  activeSlot.effect.Tick(pawn, triggerBody)

注：维持消耗不在此处理。
    持续消耗类芯片（如隐身）通过CompTrion.RegisterDrain()注册，
    由微内核统一聚合结算（见§7）。
```

### 3.5 数据流总览

```
玩家点击Gizmo
  → ActivateChip(side, slot)
      → CanActivateChip() 检查
      → 切换路径 or 直接激活路径
      → IChipEffect.Deactivate() / Activate()
      → CompTrion.Consume(activationCost)

每tick（CompTick）：
  → 切换冷却计时 → 到期时激活新芯片
  → Trion耗尽 → DeactivateAll()
  → IChipEffect.Tick()

触发体卸下（PostUnequip）：
  → DeactivateAll()
  → 所有IChipEffect.Deactivate()
```

---

## 4. IChipEffect实现策略

### 4.1 接口定义

```csharp
// 扩展点：所有芯片效果的统一协议
// 新增芯片类型只需实现此接口，不修改任何现有代码
interface IChipEffect
{
    // 激活：芯片开始输出效果
    void Activate(Pawn pawn, Thing triggerBody);

    // 关闭：芯片停止输出效果
    void Deactivate(Pawn pawn, Thing triggerBody);

    // 每tick逻辑（可选，默认空实现）
    void Tick(Pawn pawn, Thing triggerBody);

    // 前置条件检查：当前是否可以激活
    bool CanActivate(Pawn pawn, Thing triggerBody);
}
```

### 4.2 效果类型与RimWorld机制映射

```
┌──────────────────┬──────────────────────┬──────────────────────────┐
│  效果类型          │  RimWorld对接机制      │  代表芯片                  │
├──────────────────┼──────────────────────┼──────────────────────────┤
│  WeaponChipEffect │  Harmony Patch +     │  弧月、蝎子、枪械、狙击     │
│                   │  VerbTracker重建      │                           │
│  ShieldChipEffect │  Hediff + 伤害拦截    │  护盾                      │
│  StealthChipEffect│  Hediff + Stat修改 +  │  蓑衣虫、变色龙             │
│                   │  RegisterDrain        │                           │
│  UtilityChipEffect│  Ability授予          │  蜘蛛、蚱蜢                │
│  ...              │  视需求扩展           │  未来芯片                   │
└──────────────────┴──────────────────────┴──────────────────────────┘

注：这是当前已知的效果类型，不是封闭枚举。
    新增类型只需实现IChipEffect接口。
```

### 4.3 消耗模型（审查修正后）

```
芯片的Trion消耗不是统一的"维持消耗"，而是按芯片类型不同：

  零消耗（大多数芯片）：
    近战武器（弧月、蝎子）→ 普通攻击不消耗Trion

  使用时消耗（按次）：
    远程武器 → 每次射击消耗（由Verb执行时调用CompTrion.Consume()）
    能力组件（旋空等）→ 释放时消耗（由Ability执行时调用）

  受击时消耗（事件驱动）：
    护盾 → 吸收伤害时消耗（由ShieldChipEffect的拦截逻辑调用）

  持续消耗（少数）：
    蓑衣虫（隐身）→ 通过CompTrion.RegisterDrain()注册
    → 由微内核统一聚合结算（见§7）

  CompTriggerBody不做统一维持消耗结算。
  每种消耗由产生消耗的具体逻辑自行处理。
```

### 4.4 WeaponChipEffect——武器类

```
核心思路：
  芯片激活时，通过Harmony Patch让触发体的CompEquippable
  返回芯片的Verb/Tool配置，然后重建VerbTracker。

技术方案（源码验证后确定）：

  问题：CompEquippable.VerbProperties直接返回parent.def.Verbs（共享ThingDef）
        修改ThingDef.Verbs会污染所有同类型触发体实例

  方案：Harmony Patch CompEquippable.VerbProperties和Tools的getter
        让触发体返回当前激活芯片的Verb/Tool配置

  原版流程：
    VerbTracker.InitVerbs()
      → directOwner.VerbProperties
      → CompEquippable.VerbProperties
      → parent.def.Verbs          ← 共享ThingDef

  Patch后流程：
    VerbTracker.InitVerbs()
      → directOwner.VerbProperties
      → [Harmony Prefix] 检测到是触发体
      → CompTriggerBody.GetActiveVerbProperties()  ← 每实例独立
      → 返回当前激活芯片的Verb/Tool配置

Activate(pawn, triggerBody):
  将芯片的VerbProperties/Tools存入CompTriggerBody
  调用 triggerBody.GetComp<CompEquippable>().VerbTracker.InitVerbsFromZero()
  → Harmony Patch使VerbTracker读取芯片的Verb配置
  → 触发体获得芯片定义的战斗能力

Deactivate(pawn, triggerBody):
  清除CompTriggerBody中的芯片Verb/Tool数据
  调用 VerbTracker.InitVerbsFromZero()
  → 恢复触发体默认状态（仅占位Tool）

触发体ThingDef必须定义占位Tool：
  原因：ThingDef.IsWeapon检查verbs/tools是否非空（L958）
        无Verb/Tool的物品无法作为Equipment装备
  方案：定义一个低伤害近战Tool作为占位
        芯片激活后被Harmony Patch替换

⚠️ 兼容性注意：
  Harmony Prefix Patch CompEquippable.VerbProperties/Tools getter
  可能与其他修改此属性的模组冲突。需记录为已知兼容性关注点。
```

### 4.5 ShieldChipEffect——护盾类

```
Activate(pawn, triggerBody):
  pawn.health.AddHediff(shieldHediffDef)
  → Hediff的HediffComp负责伤害拦截逻辑

Deactivate(pawn, triggerBody):
  pawn.health.RemoveHediff(shieldHediff)

消耗模型：受击时消耗
  HediffComp拦截伤害时调用CompTrion.Consume(absorbedDamage × costFactor)
  不使用RegisterDrain（非持续消耗）

参考：原版CompShield（护盾腰带）的伤害拦截模式
```

### 4.6 StealthChipEffect——隐身类

```
Activate(pawn, triggerBody):
  pawn.health.AddHediff(stealthHediffDef)
  CompTrion.RegisterDrain("stealth_" + side, drainPerDay)
  → 持续消耗由微内核聚合结算

Deactivate(pawn, triggerBody):
  pawn.health.RemoveHediff(stealthHediff)
  CompTrion.UnregisterDrain("stealth_" + side)

蓑衣虫（雷达隐身）：
  Hediff的statOffsets修改自定义Stat（如BDP_RadarVisibility）

变色龙（光学隐身）：
  Hediff降低被发现概率
  激活时可能禁用其他芯片（通过CanActivate检查）
```

### 4.7 UtilityChipEffect——辅助类

```
Activate(pawn, triggerBody):
  pawn.abilities.GainAbility(utilityAbilityDef)

Deactivate(pawn, triggerBody):
  pawn.abilities.RemoveAbility(utilityAbilityDef)

适用场景：
  蜘蛛（放置丝线）→ 目标点放置Ability
  蚱蜢（跳跃移动）→ 目标点传送Ability

为什么辅助类用Ability而武器类不用：
  辅助类是"点击→选目标→执行"的一次性动作 = Ability语义
  武器类是"持续持有，随时攻击"的状态 = Verb语义
```

---

## 5. 存档与生命周期

### 5.1 存档字段

```
CompTriggerBody.PostExposeData() 存档的字段：

  ChipSlot[] mainSlots / subSlots
    每个槽位存档：
      Thing loadedChip    Scribe_Deep（芯片物品完整序列化）
      bool  isActive      Scribe_Values

  切换状态：
    SwitchState switchState    Scribe_Values
    SlotSide    pendingSide    Scribe_Values（切换中时）
    int         pendingSlot    Scribe_Values（切换中时）
    int         cooldownTick   Scribe_Values（切换中时）

TriggerChipComp：无额外存档字段
IChipEffect：无状态（纯行为接口）
```

### 5.2 读档后恢复

```
PostExposeData (LoadSaveMode.PostLoadInit)：

  // 1. 校验槽位不变量
  foreach slot in allSlots:
    if slot.loadedChip == null:
      slot.isActive = false

  // 2. 恢复激活效果
  foreach slot in allSlots:
    if slot.isActive && slot.loadedChip != null:
      effect = slot.loadedChip.GetComp<TriggerChipComp>().GetEffect()
      effect.Activate(pawn, triggerBody)

  // 3. 切换状态保持（CompTick会继续计时）

设计决策：
  读档时重新调用Activate()而非恢复效果内部状态。
  原因：IChipEffect无状态，Activate()是幂等的重建操作。
```

### 5.3 生命周期事件

```
PostSpawnSetup(bool respawningAfterLoad)：
  if !respawningAfterLoad:
    mainSlots = new ChipSlot[Props.mainSlotCount]
    if Props.hasSub:
      subSlots = new ChipSlot[Props.subSlotCount]
    if Props.preloadedChips != null:
      foreach config in Props.preloadedChips:
        LoadChipInternal(config.side, config.slot, config.chipDef)

PostEquip(Pawn pawn)：
  if Props.autoActivateOnEquip:
    自动激活预装芯片

PostUnequip(Pawn pawn)：
  DeactivateAll()

PostDestroy()：
  DeactivateAll()
  已装载芯片随触发体一起销毁
```

### 5.4 版本升级兼容

```
新增槽位字段：Scribe_Values.Look的defaultValue保证旧存档兼容
槽位数量变化：PostLoadInit中检测并扩展/截断数组
新增IChipEffect类型：不影响已有存档
```

---

## 6. 关键设计决策与调试支持

### 6.1 开发者调试Gizmo

```
CompTriggerBody在DevMode下提供以下调试Gizmo：

┌──────────────────┬──────────────────────────────────────────┐
│  调试按钮          │  功能                                     │
├──────────────────┼──────────────────────────────────────────┤
│  [Dev] 填充所有槽位│  用随机芯片填满所有空槽位                    │
│  [Dev] 清空所有槽位│  卸载所有芯片（含强制关闭激活中的）           │
│  [Dev] 强制激活    │  无视Trion消耗直接激活指定槽位               │
│  [Dev] 强制关闭    │  立即关闭所有激活效果（跳过空窗期）           │
│  [Dev] 切换锁定    │  切换allowChipManagement（测试近界模式）     │
│  [Dev] 状态转储    │  Log输出所有槽位状态、激活状态、Trion数据     │
│  [Dev] 重建Verb   │  强制调用VerbTracker.InitVerbsFromZero()   │
│  [Dev] 切换空窗=0  │  将switchCooldownTicks临时设为0（快速切换）  │
└──────────────────┴──────────────────────────────────────────┘

实现：CompGetGizmosExtra()中检查Prefs.DevMode，false时不生成
```

### 6.2 关键设计决策记录

| # | 决策 | 选择 | 理由 | 否决方案 |
|---|------|------|------|---------|
| T1 | 触发体框架定位 | 主武器（Equipment） | 自然融入装备系统 | Apparel、Hediff |
| T2 | 芯片物品形态 | 独立ThingDef物品 | 完整游戏循环：制造→存储→装载→使用 | 配置数据 |
| T3 | 效果抽象层 | IChipEffect接口 | 每种效果用最合适的RimWorld机制；OCP | 纯Verb、纯Ability、纯Hediff |
| T4 | 武器Verb注入 | Harmony Patch CompEquippable.VerbProperties/Tools getter | per-instance行为，不修改共享ThingDef | 直接修改ThingDef.Verbs |
| T5 | 双持机制 | 完整主+副侧 | 最贴合原作 | 单激活、双激活无侧别 |
| T6 | 三种触发器统一 | 同一CompTriggerBody，Props控制差异 | 一套代码三种体验 | 三套独立实现 |
| T7 | 黑触发器扩容 | equippedStatOffsets走Stat聚合 | 零额外C#，复用微内核 | 自定义Comp |
| T8 | 槽位数量 | XML可配置 | 数据驱动原则 | 硬编码4+4 |
| T9 | 芯片注册 | 战斗模块负责 | 注册是战斗体激活时的行为 | 触发器模块自行Allocate |
| T10 | 读档恢复 | 重新调用Activate() | IChipEffect无状态，幂等重建 | 序列化效果内部状态 |
| T11 | 消耗模型 | 按芯片类型分别处理 | 大多数芯片无维持消耗 | 统一maintenanceCostPerDay |
| T12 | 持续消耗结算 | CompTrion.RegisterDrain聚合 | 多源统一结算，避免浮点误差 | 各模块独立Consume |

### 6.3 与其他模块的接口约定

```
触发器模块对外暴露（供其他模块使用）：

  CompTriggerBody:
    bool HasTriggerBody(Pawn)        → 战斗模块检查
    bool HasActiveChip(SlotSide)     → 战斗模块检查
    void DeactivateAll()             → 战斗模块在战斗体解除时调用
    ChipSlot GetActiveSlot(SlotSide) → 战斗模块读取当前武器信息

触发器模块消费（从其他模块使用）：

  BDP.Core:
    CompTrion.Consume()              → 激活消耗
    CompTrion.Available              → CanActivate检查
    CompTrion.RegisterDrain()        → 持续消耗注册（§7）
    CompTrion.UnregisterDrain()      → 持续消耗注销
```

---

## 7. 微内核扩展：RegisterDrain聚合结算

> 本节内容已迁移至 6.1 Trion能量系统详细设计 v1.4。
> 具体见 6.1 §2.1（drainRegistry字段）、§3.6（RegisterDrain/UnregisterDrain/TotalDrainPerDay API）、§3.7 CompTick（聚合结算逻辑）、§8（drainSettleInterval配置）。
>
> 触发器模块作为消费者，通过以下方式使用：
> - 隐身芯片激活时：CompTrion.RegisterDrain("stealth_" + side, drainPerDay)
> - 隐身芯片关闭时：CompTrion.UnregisterDrain("stealth_" + side)

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-19 | 初版完成：触发器模块完整架构设计。覆盖模块总览（组件清单、三种触发器统一方案、OCP验证）、核心数据模型（CompProperties_TriggerBody XML配置、ChipSlot数据结构、激活状态机、7条不变量）、API规范（CompTriggerBody 9个核心方法含前置/后置条件）、IChipEffect实现策略（4种效果类型的RimWorld机制映射、消耗模型修正、WeaponChipEffect的Harmony Patch方案含源码验证）、存档与生命周期（存档字段、读档恢复、版本兼容）、12项设计决策记录、开发者调试Gizmo、模块接口约定、微内核扩展（RegisterDrain聚合结算）。设计原则：KISS/SRP/OCP/DIP/数据驱动/YAGNI/轮询优于事件。 | Claude Opus 4.6 |
