---
标题：Gizmo命令按钮系统
版本号: v1.0
更新日期: 2026-02-14
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld Gizmo命令按钮系统完整源码分析，覆盖Gizmo三层类继承体系（Gizmo→Command→6核心子类+Designator、Gizmo→Gizmo_Slider→3子类、13+状态显示Gizmo）、GizmoGridDrawer收集/分组/渲染机制（ISelectable.GetGizmos+反向设计器+groupKey内容匹配/groupKeyIgnoreContent忽略内容匹配+MergeWith合并）、Pawn.GetGizmos()完整28个提供者来源（drafter/attack/equipment/carry/needs/psychicEntropy/mechanitor/mech/child/mutant/creepjoiner/abilities/playerSettings/health/apparel/inventory/mindState/royalty permits/quest/connections/genes/training/lord）、4种模组扩展Gizmo模式（ThingComp.CompGetGizmosExtra/CompGetWornGizmosExtra/HediffComp.CompGetGizmos/Gene.GetGizmos）、25+自定义Gizmo类型清单、关键源码引用表
---

# Gizmo命令按钮系统

**总览**：Gizmo是RimWorld中选中物体后屏幕底部显示的命令按钮/状态条的统称。整个系统由三层架构组成——**类继承体系**（定义Gizmo的类型和行为）→ **收集机制**（GizmoGridDrawer从选中物体收集所有Gizmo）→ **分组与渲染**（相同Gizmo合并显示，支持多选操作）。模组通过4个扩展点（ThingComp/HediffComp/Gene/自定义Gizmo子类）添加自定义按钮。

## 1. Gizmo类继承体系

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Gizmo 类继承体系                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Gizmo（抽象基类）                                                   │
│  ├── Command（带图标/标签/热键的标准按钮）                            │
│  │   ├── Command_Action ← 点击执行Action委托                        │
│  │   │   ├── Command_ActionWithCooldown ← 带冷却条                  │
│  │   │   └── Command_ActionWithLimitedUseCount ← 限次使用           │
│  │   ├── Command_Toggle ← 开关切换（isActive+toggleAction）         │
│  │   ├── Command_Target ← 选择目标后执行                            │
│  │   ├── Command_VerbTarget ← 绑定Verb的攻击目标选择                │
│  │   ├── Command_Ability ← 能力按钮（绑定Ability实例）              │
│  │   │   ├── Command_AbilitySpeech ← 演讲能力                      │
│  │   │   └── Command_AbilityTrial ← 审判能力                       │
│  │   ├── Designator ← 建造/指定工具（地图操作）                     │
│  │   └── 专用Command ×6                                             │
│  │       ├── Command_Ritual ← 仪式                                  │
│  │       ├── Command_BestowerCeremony ← 授爵仪式                    │
│  │       ├── Command_LoadToTransporter ← 装载运输舱                 │
│  │       ├── Command_SetPlantToGrow ← 设置种植作物                  │
│  │       ├── Command_SetBedOwnerType ← 设置床位归属                 │
│  │       └── Command_SetTargetFuelLevel ← 设置目标燃料              │
│  │                                                                   │
│  ├── Gizmo_Slider（抽象，带拖拽条的滑块）                            │
│  │   ├── ActivityGizmo ← 活动进度条                                  │
│  │   ├── Gizmo_SetFuelLevel ← 燃料目标滑块                          │
│  │   └── GeneGizmo_Resource ← 基因资源条                            │
│  │       └── GeneGizmo_ResourceHemogen ← 血源条（特殊阈值）         │
│  │                                                                   │
│  └── 状态显示Gizmo ×13+（非Command，纯显示/信息）                    │
│      ├── Gizmo_EnergyShieldStatus ← 护盾能量条                      │
│      ├── Gizmo_ProjectileInterceptorHitPoints ← 拦截器HP            │
│      ├── PsychicEntropyGizmo ← 灵能熵/灵能聚焦                     │
│      ├── MechanitorBandwidthGizmo ← 机械师带宽                      │
│      ├── MechanitorControlGroupGizmo ← 机械师控制组                 │
│      ├── MechCarrierGizmo ← 机械载体                                │
│      ├── MechPowerCellGizmo ← 机械电池                              │
│      ├── Gizmo_MechResurrectionCharges ← 机械复活次数               │
│      ├── GeneGizmo_DeathrestCapacity ← 死眠容量                     │
│      ├── Gizmo_GrowthTier ← 儿童成长等级                            │
│      ├── Gizmo_PruningConfig ← 树木修剪配置                         │
│      ├── GuardianShipGizmo ← 守护者飞船                             │
│      ├── Gizmo_RoomStats ← 房间属性                                 │
│      └── Gizmo_CaravanInfo ← 远行队信息                             │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.1 Gizmo基类核心字段与方法

```csharp
// Verse.Gizmo — 所有Gizmo的抽象基类
public abstract class Gizmo
{
    public bool disabled;              // 是否禁用（灰显）
    public string disabledReason;      // 禁用原因提示
    public bool alsoClickIfOtherInGroupClicked; // 组内其他Gizmo被点击时是否也触发
    public float order;                // 排序权重（越小越靠左）
    public float Height;               // 高度（默认75f）

    // 核心虚方法
    public abstract GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms);
    public virtual void ProcessInput(Event ev);           // 单个Gizmo被点击
    public virtual void ProcessGroupInput(Event ev, List<Gizmo> group); // 组被点击
    public virtual bool GroupsWith(Gizmo other);          // 是否与另一个Gizmo分为同组
    public virtual void MergeWith(Gizmo other);           // 合并另一个Gizmo的数据
}
```

### 1.2 Command核心字段

```csharp
// Verse.Command : Gizmo — 带图标/标签/热键的标准按钮
public abstract class Command : Gizmo
{
    public string defaultLabel;        // 按钮文字
    public string defaultDesc;         // 鼠标悬停描述
    public Texture icon;               // 图标纹理
    public KeyBindingDef hotKey;       // 快捷键
    public int groupKey;               // 分组键（内容匹配）
    public int groupKeyIgnoreContent;  // 分组键（忽略内容匹配）
    public bool shrinkable;            // 空间不足时是否可缩小
    public bool groupable = true;      // 是否参与分组
}
```

### 1.3 6个核心Command子类对比

| # | 类 | 命名空间 | 核心字段 | 用途 | 典型实例 |
|---|---|---------|---------|------|---------|
| 1 | `Command_Action` | Verse | `Action action` | 点击执行一次性动作 | 征召按钮、开门、引爆 |
| 2 | `Command_Toggle` | Verse | `Func<bool> isActive` + `Action toggleAction` | 开关切换 | 自由射击、保持征召、自动重装 |
| 3 | `Command_Target` | Verse | `Action<LocalTargetInfo> action` + `TargetingParameters` | 选择目标后执行 | 指定攻击目标 |
| 4 | `Command_VerbTarget` | Verse | `Verb verb` + `List<Verb> groupedVerbs` | 绑定Verb的攻击命令 | 远程武器射击、炮塔攻击 |
| 5 | `Command_Ability` | RimWorld | `Ability ability` + 冷却条渲染 | 能力按钮 | 灵能力、基因能力、皇室能力 |
| 6 | `Designator` | Verse | `DesignationDef` + 地图操作方法 | 建造/指定工具 | 建造墙壁、砍伐、采矿 |

## 2. Gizmo收集与显示机制

### 2.1 GizmoGridDrawer收集流程

`GizmoGridDrawer.DrawGizmoGridFor()` 是Gizmo显示的入口，每帧调用：

```
┌─────────────────────────────────────────────────────────────────┐
│              GizmoGridDrawer 收集流程                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 遍历所有选中对象（selectedObjects）                          │
│     ├── 对象实现 ISelectable? → 调用 GetGizmos() 收集           │
│     └── 对象本身是 Gizmo? → 直接加入列表                        │
│                                                                 │
│  2. 遍历所有选中的 Thing                                        │
│     └── 对每个Thing → 遍历 ReverseDesignatorDatabase            │
│         └── 调用 Designator.CreateReverseDesignationGizmo(t)    │
│             → 生成反向设计器按钮（如"砍伐"、"拆除"）            │
│                                                                 │
│  3. 调用 DrawGizmoGrid() 渲染所有收集到的Gizmo                  │
│     ├── 按 order 排序                                           │
│     ├── 分组（GroupsWith）                                      │
│     ├── 缩小（shrinkable，空间不足时）                          │
│     └── 逐个渲染 GizmoOnGUI()                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

> **关键点**：反向设计器Gizmo（如选中树木时出现的"砍伐"按钮）不是由Thing自身提供的，而是GizmoGridDrawer在收集阶段从`ReverseDesignatorDatabase`查询并注入的`Command_Action`。

### 2.2 ISelectable接口

所有可选中并显示Gizmo的对象都实现`ISelectable`接口：

```csharp
public interface ISelectable
{
    IEnumerable<Gizmo> GetGizmos();
    // ...
}
```

继承链：`Thing` → `ThingWithComps`（遍历所有Comp的`CompGetGizmosExtra()`）→ `Pawn`（28个提供者）→ 各Building子类。

## 3. Gizmo分组与合并机制

### 3.1 Command.GroupsWith() — 分组判定逻辑

多选物体时，相同功能的Gizmo会合并为一个按钮。`Command.GroupsWith()` 的完整判定逻辑：

```csharp
// Verse.Command.GroupsWith() — 两种匹配模式
public override bool GroupsWith(Gizmo other)
{
    if (!groupable) return false;
    if (other is not Command { groupable: not false } command) return false;

    // 模式1：内容匹配（hotKey + Label + icon + groupKey 全部相同）
    if (hotKey == command.hotKey && Label == command.Label
        && icon == command.icon && groupKey == command.groupKey)
        return true;

    // 模式2：忽略内容匹配（仅 groupKeyIgnoreContent 相同）
    if (groupKeyIgnoreContent == -1 || command.groupKeyIgnoreContent == -1)
        return false;
    if (groupKeyIgnoreContent == command.groupKeyIgnoreContent)
        return true;

    return false;
}
```

### 3.2 两种分组模式对比

| 模式 | 字段 | 匹配条件 | 合并后行为 | 典型场景 |
|------|------|---------|-----------|---------|
| **内容匹配** | `groupKey` | hotKey + Label + icon + groupKey 全部相同 | 点击时所有组内Gizmo都执行 | 多选Pawn的征召按钮（标签/图标完全一致） |
| **忽略内容匹配** | `groupKeyIgnoreContent` | 仅此字段相同（-1表示不参与） | 合并显示但可能内容不同 | 多选不同作物的种植区（合并为一个"设置作物"按钮） |

### 3.3 MergeWith() — 合并数据

`Command_VerbTarget` 重写了 `MergeWith()`，将多个Pawn的同类型Verb合并到 `groupedVerbs` 列表中，使多选Pawn时点击一次攻击按钮可以让所有Pawn同时攻击。

## 4. Pawn的Gizmo提供者全表

`Pawn.GetGizmos()` 从28个来源收集Gizmo，按代码顺序排列：

| # | 提供者 | 方法/字段 | 提供的Gizmo | 显示条件 | DLC |
|---|--------|---------|------------|---------|-----|
| 1 | **ThingWithComps基类** | `base.GetGizmos()` → 遍历`CompGetGizmosExtra()` | 所有ThingComp的Gizmo | 始终 | Core |
| 2 | **征召控制器** | `drafter.GetGizmos()` | 征召/解除征召 + 自由射击 | 殖民者/机械体/亚人类 | Core |
| 3 | **攻击命令** | `PawnAttackGizmoUtility.GetAttackGizmos()` | 攻击目标按钮 | 殖民者/机械体/亚人类 | Core |
| 4 | **装备** | `equipment.GetGizmos()` | 装备自带命令 | equipment != null | Core |
| 5 | **搬运物** | `carryTracker.GetGizmos()` | 搬运相关命令 | carryTracker != null | Core |
| 6 | **需求** | `needs.GetGizmos()` | 需求相关Gizmo | needs != null | Core |
| 7 | **灵能熵** | `psychicEntropy.GetGizmo()` | 灵能聚焦/神经热量条 | 单选 + NeedToShowGizmo | Royalty |
| 8 | **机械师** | `mechanitor.GetGizmos()` | 带宽/控制组/孵化 | Biotech + IsMechanitor | Biotech |
| 9 | **机械体** | `MechanitorUtility.GetMechGizmos()` | 机械体专属命令 | Biotech + IsMechanoid | Biotech |
| 10 | **儿童成长** | `new Gizmo_GrowthTier(this)` | 成长等级条 | Biotech + 年龄<13 + 未征召 + 单选 | Biotech |
| 11 | **变异体** | `mutant.GetGizmos()` | 变异体命令 | IsMutant | Anomaly |
| 12 | **诡异加入者** | `creepjoiner.GetGizmos()` | 诡异加入者命令 | Anomaly + IsCreepJoiner | Anomaly |
| 13 | **能力** | `abilities.GetGizmos()` | 所有能力按钮 | abilities != null | Core |
| 14 | **玩家设置** | `playerSettings.GetGizmos()` | 区域限制等 | 殖民者/机械体/囚犯 | Core |
| 15 | **健康** | `health.GetGizmos()` | 健康相关Gizmo | 殖民者/机械体/囚犯 | Core |
| 16 | **死后Hediff** | `health.GetGizmos()` | 死后显示的Gizmo | Dead + HasShowGizmosOnCorpseHediff | Core |
| 17 | **服装** | `apparel.GetGizmos()` → 遍历`CompGetWornGizmosExtra()` | 穿戴装备的Gizmo | apparel != null | Core |
| 18 | **物品栏** | `inventory.GetGizmos()` | 物品栏命令 | inventory != null | Core |
| 19 | **心智状态** | `mindState.GetGizmos()` | 心智相关命令 | mindState != null | Core |
| 20 | **皇室许可** | `FactionPermit.Permit.Worker.GetPawnGizmos()` | 各派系许可按钮 | Royalty + 殖民者 | Royalty |
| 21 | **皇室援助** | `royalty.RoyalAidGizmo()` | 皇室援助按钮 | HasAidPermit | Royalty |
| 22 | **头衔许可** | `RoyalTitlePermitDef.Worker.GetPawnGizmos()` | 头衔附带许可 | Royalty + 殖民者 | Royalty |
| 23 | **任务** | `QuestUtility.GetQuestRelatedGizmos()` | 任务相关按钮 | 始终 | Core |
| 24 | **皇室系统** | `royalty.GetGizmos()` | 皇室系统Gizmo | Royalty active | Royalty |
| 25 | **连接** | `connections.GetGizmos()` | 意识形态连接 | Ideology active | Ideology |
| 26 | **基因** | `genes.GetGizmos()` | 基因Gizmo（资源条等） | genes != null | Biotech |
| 27 | **训练** | `training.GetGizmos()` | 动物训练命令 | training != null | Core |
| 28 | **领主** | `lord.LordJob/CurLordToil.GetPawnGizmos()` | 领主任务命令 | 有Lord | Core |

> **要点**：
> 1. 提供者#1（ThingWithComps基类）是模组最常用的扩展入口——自定义ThingComp的`CompGetGizmosExtra()`会被自动收集
> 2. 提供者#17（服装）调用的是`CompGetWornGizmosExtra()`而非`CompGetGizmosExtra()`——这是护盾腰带等穿戴物显示Gizmo的专用通道
> 3. Lord系统（#28）可以为参与特定LordJob的Pawn注入临时Gizmo——如仪式参与者的专属按钮

## 5. ThingComp/HediffComp/Gene的Gizmo扩展点

### 5.1 四个扩展点对比

| # | 扩展点 | 所在类 | 调用时机 | 适用场景 |
|---|--------|-------|---------|---------|
| 1 | `CompGetGizmosExtra()` | `ThingComp` | `ThingWithComps.GetGizmos()` 遍历所有Comp | 最通用——建筑/武器/物品/Pawn身上的Comp |
| 2 | `CompGetWornGizmosExtra()` | `ThingComp` | `Apparel.GetWornGizmos()` 遍历穿戴物的Comp | 穿戴装备专用——护盾腰带、动力装甲Verb |
| 3 | `CompGetGizmos()` | `HediffComp` | `HediffWithComps.GetGizmos()` 遍历Hediff的Comp | Hediff附带的按钮——金属恐怖、怀孕、死亡拒绝 |
| 4 | `GetGizmos()` | `Gene` | `Pawn_GeneTracker.GetGizmos()` 遍历所有Gene | 基因附带的按钮——血源条、死眠、灵能连接 |

### 5.2 ThingComp.CompGetGizmosExtra() — 最常用扩展点

```csharp
// Verse.ThingWithComps.GetGizmos() — 自动收集所有Comp的Gizmo
public override IEnumerable<Gizmo> GetGizmos()
{
    foreach (Gizmo gizmo in base.GetGizmos())
        yield return gizmo;
    if (comps == null) yield break;
    for (int i = 0; i < comps.Count; i++)
    {
        foreach (Gizmo item in comps[i].CompGetGizmosExtra())
            yield return item;
    }
}
```

模组只需在自定义ThingComp中重写`CompGetGizmosExtra()`，返回的Gizmo会自动出现在选中该Thing时的按钮栏中。原版典型实例：`CompShield`返回`Gizmo_EnergyShieldStatus`护盾能量条。

### 5.3 CompGetWornGizmosExtra() — 穿戴装备专用

```csharp
// RimWorld.Apparel.GetWornGizmos() — 穿戴物的Gizmo收集
public IEnumerable<Gizmo> GetWornGizmos()
{
    foreach (ThingComp thingComp in AllComps)
    {
        foreach (Gizmo item in thingComp.CompGetWornGizmosExtra())
            yield return item;
    }
}
```

与`CompGetGizmosExtra()`的区别：`CompGetWornGizmosExtra()`仅在装备被穿戴时调用（通过`Pawn_ApparelTracker.GetGizmos()`），而`CompGetGizmosExtra()`在物品被选中时调用。护盾腰带的`CompShield`同时重写了两者——选中腰带物品时显示能量条，穿戴时也显示能量条。

### 5.4 HediffComp.CompGetGizmos()

```csharp
// Verse.HediffWithComps.GetGizmos() — 遍历Hediff的所有Comp
public override IEnumerable<Gizmo> GetGizmos()
{
    for (int i = 0; i < comps.Count; i++)
    {
        IEnumerable<Gizmo> enumerable = comps[i].CompGetGizmos();
        if (enumerable == null) continue;
        foreach (Gizmo item in enumerable)
            yield return item;
    }
}
```

原版使用实例：`Hediff_MetalhorrorImplant`、`Hediff_Pregnant`、`Hediff_DeathRefusal`等通过重写Hediff自身的`GetGizmos()`（而非HediffComp）提供按钮。HediffComp级别的`CompGetGizmos()`在原版中使用较少，但为模组提供了无侵入的扩展点。

## 6. 自定义Gizmo类型清单

### 6.1 状态显示类（直接继承Gizmo）

| # | 类 | 用途 | 显示内容 | DLC |
|---|---|------|---------|-----|
| 1 | `Gizmo_EnergyShieldStatus` | 护盾能量条 | 当前能量/最大能量 | Core |
| 2 | `Gizmo_ProjectileInterceptorHitPoints` | 拦截器HP | 当前HP/最大HP | Core |
| 3 | `PsychicEntropyGizmo` | 灵能熵 | 神经热量 + 灵能聚焦 | Royalty |
| 4 | `MechanitorBandwidthGizmo` | 机械师带宽 | 已用/总带宽 | Biotech |
| 5 | `MechanitorControlGroupGizmo` | 控制组 | 机械体分组管理 | Biotech |
| 6 | `MechCarrierGizmo` | 机械载体 | 载体内机械体 | Biotech |
| 7 | `MechPowerCellGizmo` | 机械电池 | 电量条 | Biotech |
| 8 | `Gizmo_MechResurrectionCharges` | 复活次数 | 剩余复活次数 | Biotech |
| 9 | `GeneGizmo_DeathrestCapacity` | 死眠容量 | 死眠建筑连接数 | Biotech |
| 10 | `Gizmo_GrowthTier` | 成长等级 | 儿童成长进度 | Biotech |
| 11 | `Gizmo_PruningConfig` | 修剪配置 | 树木修剪设置 | Ideology |
| 12 | `GuardianShipGizmo` | 守护者飞船 | 飞船状态 | Anomaly |
| 13 | `Gizmo_RoomStats` | 房间属性 | 房间各项数值 | Core |
| 14 | `Gizmo_CaravanInfo` | 远行队信息 | 远行队状态 | Core |

### 6.2 滑块类（继承Gizmo_Slider）

| # | 类 | 用途 | 可拖拽 | DLC |
|---|---|------|-------|-----|
| 1 | `Gizmo_SetFuelLevel` | 燃料目标 | 是 | Core |
| 2 | `ActivityGizmo` | 活动进度 | 否 | Biotech |
| 3 | `GeneGizmo_Resource` | 基因资源条 | 是（设置目标值） | Biotech |
| 4 | `GeneGizmo_ResourceHemogen` | 血源条 | 是（继承Resource） | Biotech |

### 6.3 专用Command（继承Command的特殊子类）

| # | 类 | 用途 | 特殊行为 | DLC |
|---|---|------|---------|-----|
| 1 | `Command_Ritual` | 仪式 | 打开仪式选择窗口 | Ideology |
| 2 | `Command_BestowerCeremony` | 授爵仪式 | 召唤授爵者 | Royalty |
| 3 | `Command_LoadToTransporter` | 装载运输舱 | 打开装载界面 | Core |
| 4 | `Command_SetPlantToGrow` | 设置种植 | 打开作物选择 | Core |
| 5 | `Command_SetBedOwnerType` | 床位归属 | 切换医疗/殖民者/囚犯 | Core |
| 6 | `Command_SetTargetFuelLevel` | 燃料目标 | 打开燃料设置 | Core |
| 7 | `Command_SetNeuralSuperchargerAutoUse` | 神经超充自动使用 | 切换自动使用 | Ideology |
| 8 | `Command_CallBossgroup` | 召唤Boss群 | 召唤机械Boss | Biotech |

## 7. 模组扩展Gizmo的4种模式

### 7.1 模式对比

| # | 模式 | 扩展点 | 侵入性 | 适用场景 | 实现方式 |
|---|------|-------|--------|---------|---------|
| 1 | **ThingComp.CompGetGizmosExtra** | `ThingComp` | 无 | 建筑/武器/物品/Pawn的Comp | 重写虚方法，yield return Gizmo |
| 2 | **ThingComp.CompGetWornGizmosExtra** | `ThingComp` | 无 | 穿戴装备的Comp | 同上，仅穿戴时显示 |
| 3 | **HediffComp.CompGetGizmos** | `HediffComp` | 无 | Hediff附带的按钮 | 重写虚方法 |
| 4 | **Gene.GetGizmos** | `Gene` | 无 | 基因附带的按钮 | 重写虚方法 |
| 5 | **自定义Gizmo子类** | 继承`Gizmo` | 低 | 需要自定义渲染的状态条/滑块 | 继承Gizmo/Gizmo_Slider，重写GizmoOnGUI |

### 7.2 最常用模式：ThingComp.CompGetGizmosExtra

```csharp
// 模组示例：自定义ThingComp添加Gizmo
public class CompMyCustomButton : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
            yield return g;

        yield return new Command_Action
        {
            defaultLabel = "我的按钮",
            defaultDesc = "点击执行自定义操作",
            icon = MyTextures.MyIcon,
            action = delegate { DoMyAction(); },
        };
    }
}
```

## 7. 关键源码引用表

| 类 | 方法/字段 | 命名空间 | 机制 |
|----|---------|---------|------|
| `Gizmo` | 基类（disabled/order/Height） | Verse | Gizmo抽象基类，定义分组/合并/渲染接口 |
| `Command` | `GroupsWith()` | Verse | 两种分组模式：groupKey内容匹配 + groupKeyIgnoreContent |
| `Command` | `groupKey` / `groupKeyIgnoreContent` | Verse | 分组键字段 |
| `Command_Action` | `action` | Verse | 点击执行的Action委托 |
| `Command_Toggle` | `isActive` + `toggleAction` | Verse | 开关状态查询 + 切换动作 |
| `Command_Target` | `action` + `targetingParams` | Verse | 目标选择后执行 |
| `Command_VerbTarget` | `verb` + `groupedVerbs` + `MergeWith()` | Verse | Verb绑定 + 多选合并 |
| `Command_Ability` | `ability` + `GroupsWith()` | RimWorld | 能力绑定 + 冷却条渲染 |
| `Designator` | `CanDesignateThing/Cell()` + `DesignateThing/Cell()` | Verse | 地图操作设计器 |
| `GizmoGridDrawer` | `DrawGizmoGridFor()` | Verse | Gizmo收集入口：ISelectable + 反向设计器 |
| `GizmoGridDrawer` | `DrawGizmoGrid()` | Verse | Gizmo排序/分组/缩小/渲染 |
| `ThingWithComps` | `GetGizmos()` | Verse | 遍历所有Comp的CompGetGizmosExtra() |
| `ThingComp` | `CompGetGizmosExtra()` | Verse | 模组最常用扩展点 |
| `ThingComp` | `CompGetWornGizmosExtra()` | Verse | 穿戴装备Gizmo扩展点 |
| `HediffComp` | `CompGetGizmos()` | Verse | Hediff Gizmo扩展点 |
| `Gene` | `GetGizmos()` | Verse | 基因Gizmo扩展点 |
| `Pawn` | `GetGizmos()` | Verse | 28个提供者的完整收集链 |
| `Gizmo_Slider` | 抽象基类（ValuePercent/Target/DragRange） | Verse | 可拖拽滑块Gizmo |
| `GeneGizmo_Resource` | 继承Gizmo_Slider | RimWorld | 基因资源条（血源等） |
| `Gizmo_EnergyShieldStatus` | 继承Gizmo | RimWorld | 护盾能量状态条 |
| `CompShield` | `CompGetWornGizmosExtra()` | RimWorld | 返回Gizmo_EnergyShieldStatus |
| `Apparel` | `GetWornGizmos()` | RimWorld | 遍历穿戴物Comp的CompGetWornGizmosExtra |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-14 | 创建文档：Gizmo命令按钮系统完整源码分析（三层类继承体系Gizmo→Command→6核心子类+Designator+专用Command×8、Gizmo→Gizmo_Slider→3子类、13+状态显示Gizmo；GizmoGridDrawer收集流程ISelectable+反向设计器；Command.GroupsWith两种分组模式groupKey/groupKeyIgnoreContent+MergeWith合并；Pawn.GetGizmos()完整28个提供者来源按代码顺序；4种模组扩展点CompGetGizmosExtra/CompGetWornGizmosExtra/HediffComp.CompGetGizmos/Gene.GetGizmos；25+自定义Gizmo类型清单按状态显示/滑块/专用Command分类），含类继承体系图、收集流程图、源码引用表22项 | Claude Opus 4.6 |
