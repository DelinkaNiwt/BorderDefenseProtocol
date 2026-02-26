---
标题：BDP Trigger模块架构评估报告
版本号: v1.0
更新日期: 2026-02-24
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 对BDP模组Trigger模块进行子系统拆解、架构设计评估、游戏体验评估，并给出结构性问题的改进建议。供后续重构参考。
---

# BDP Trigger模块架构评估报告

## 一、子系统概览

Trigger模块可拆解为6个子系统：

| 子系统 | 核心职责 | 关键类 |
|--------|---------|--------|
| 触发体(TriggerBody) | 宿主载体，芯片容器与调度器 | CompTriggerBody, CompProperties_TriggerBody |
| 芯片(Chip) | 可插拔的能力单元 | TriggerChipComp, CompProperties_TriggerChip |
| 芯片效果(ChipEffect) | 策略层，三种效果路线 | IChipEffect, WeaponChipEffect, UtilityChipEffect, ShieldChipEffect |
| 双武器合成(DualWeapon) | 组合两侧Verb数据 | DualVerbCompositor, Verb族, JobDriver族 |
| 槽位与切换(Slot & Switch) | 状态管理 | ChipSlot, SwitchContext |
| UI层 | 交互表面 | Gizmo_TriggerBodyStatus, Window_TriggerBodySlots, Command_BDPChipAttack |

### 数据流路径

```
Pawn → equipment.Primary (触发体Thing)
  → CompTriggerBody → GetActiveSlot(side)
    → ChipSlot.loadedChip (芯片Thing)
      → TriggerChipComp.GetEffect() (IChipEffect实例)
      → loadedChip.def.GetModExtension<WeaponChipConfig>() (配置数据)
```

### 三种芯片效果策略的预设逻辑

**武器型(WeaponChipEffect)** — 作用于触发体本身
- 激活 = 向触发体注入Verb/Tool数据 + RebuildVerbs；关闭 = 清除 + RebuildVerbs
- 芯片武器数据藏在DefModExtension中（避免IsWeapon=true），由效果类在激活时"搬运"
- 唯一需要感知"侧别"的效果

**辅助型(UtilityChipEffect)** — 作用于Pawn的Ability系统
- 激活 = GainAbility；关闭 = RemoveAbility
- Ability的执行完全由原版系统接管，芯片只管"给"和"收"

**护盾型(ShieldChipEffect)** — 作用于Pawn的Health系统
- 激活 = AddHediff；关闭 = RemoveHediff
- 防御逻辑委托给Hediff自身实现

三者共同特征：无状态、Tick()空实现、CanActivate()直接返回true。

### 双武器合成系统细分

**4.1 组合决策(DualVerbCompositor)**
纯函数路由器，决策矩阵：

| 左手 | 右手 | 路径 | 产出 |
|------|------|------|------|
| 有 | 无 | 单侧直通 | 该侧Verb(primary) |
| 无 | 有 | 单侧直通 | 该侧Verb(primary) |
| 近战 | 近战 | 双近战合成 | 左独立 + 右独立 + 双手合成Verb |
| 远程 | 远程 | 双远程合成 | 左独立 + 右独立 + 双手合成Verb |
| 近战 | 远程 | 混合 | 近战独立 + 远程独立(primary) |

同芯片时独立Verb去重。

**4.2 攻击模式(Verb族)**
- 逐发模式(Burst-driven)：BDPShoot、BDPDualRanged、BDPMelee — 引擎burst驱动节奏
- 齐射模式(Volley)：BDPVolley、BDPDualVolley — 单tick内全部倾泻

逐发=左键默认，齐射=右键可选。

**4.3 侧别标识(Side Labeling)**
通过VerbProperties.label携带侧别（BDP_LeftHand/BDP_RightHand），无label=双侧合成Verb。
贯穿合成→执行→UI的隐式协议。

**4.4 执行桥接(JobDriver层)**
芯片Verb脱离VerbTracker，引擎不调用VerbTick()。JobDriver手动驱动burst计时器。

---

## 二、架构设计评估

### 做得好的地方

- IChipEffect策略模式抽象层次恰当，三种实现各自清晰对接RimWorld不同子系统
- DualVerbCompositor无状态纯函数设计，避免双武器状态与触发体状态耦合
- 槽位-芯片数据模型简洁（ChipSlot四字段，SwitchContext按侧独立）

### 结构性问题

#### 问题1：Verb脱离VerbTracker — 最大技术债务

**严重程度：高 | 改动成本：高 | 建议优先级：低（暂不动）**

原因可理解（芯片Verb需动态创建/销毁），但引发连锁问题：
- 引擎不调用VerbTick() → 需自定义JobDriver手动驱动
- 引擎不调用InitVerb() → tool/maneuver为null → 需EnsureToolAndManeuver（Bug11）
- ShotsPerBurst基类硬编码为1 → 需手动override（Bug9）
- Stance_Cooldown时序不对 → 需手动清除
- burst间miss/dodge返回false → 需强制返回true绕过（Bug7）

从Bug1到Bug11，此决策贡献了大部分bug。

#### 问题2：配置读取路径重复且脆弱 ⭐重点

**严重程度：中 | 改动成本：低 | 建议优先级：高**

"ActivatingSlot → 回退遍历AllActiveSlots"模式在以下位置各写了一遍：
- WeaponChipEffect.GetConfig()
- UtilityChipEffect.GetConfig()
- ShieldChipEffect.GetConfig()
- Verb_BDPShoot.GetCurrentChipThing() + GetTrionCostPerShot()
- Verb_BDPDualRanged.GetSideTrionCost() + GetSideChipThing()
- Verb_BDPVolley.GetCurrentChipThing()

各处回退逻辑微妙不同（有的走ActivatingSlot，有的走侧别label，有的两者都走），增加出错概率。

#### 问题3：侧别标识依赖字符串约定

**严重程度：低 | 改动成本：中 | 建议优先级：低**

用VerbProperties.label携带侧别信息是隐式协议，无编译期保障。Bug1和Bug2都因label缺失/未正确设置导致。

#### 问题4：Verb_BDPMelee职责过多

**严重程度：中 | 改动成本：中 | 建议优先级：低**

同时处理：单侧/双侧近战、burst状态机、hitIndex调度、Stance清除、tool/maneuver注入、伤害计算重写、burst中止。是系统中最复杂的单一类。

#### 问题5：ComboAbilityDef未完成

**严重程度：低 | 改动成本：低 | 建议优先级：低**

数据结构存在（chipA + chipB → abilityDef），attackId预留了"combo:"前缀，但无实际激活逻辑。悬空设计。

---

## 三、游戏体验评估

### 体验优势

- **可组合性**是核心卖点，左右手不同芯片自动产生不同攻击组合，设计空间大
- **齐射作为右键替代模式**，逐发(稳定)vs齐射(爆发高消耗)的选择有战术意义

### 体验隐患

| 问题 | 描述 |
|------|------|
| Gizmo数量爆炸 | 最坏情况（两个不同远程芯片+齐射）可产生3-6个攻击按钮，认知负担高 |
| 右键齐射是隐藏机制 | RimWorld玩家几乎没有"右键Gizmo"的习惯，功能易被忽略 |
| 切换前摇/后摇 | WindingDown→WarmingUp若总时间>1秒，操作感会变"黏"，依赖数值调优 |
| 近战burst视觉反馈存疑 | 大量引擎时序workaround暗示连击间动画过渡可能不流畅 |
| 同芯片去重困惑 | 两侧装同芯片时独立Gizmo只显示一个，玩家可能疑惑 |

### 缺失要素

- **视觉/音效反馈系统**：芯片激活、切换、齐射等关键时刻几乎无反馈（仅齐射按钮有Tick_Tiny音效）
- **芯片协同提示**：ComboAbilityDef暗示组合效果设计意图，但无UI提示引导玩家发现

---

## 四、改进建议

### 建议1：缓解Verb脱离VerbTracker的复杂度（优先级：低）

不建议现在动根本架构。可将workaround（清除Stance、覆盖interval、强制返回true）抽到`BDPVerbHelper`静态类，让Verb_BDPMelee不用自己处理所有引擎兼容逻辑。

### 建议2：统一配置读取路径 ⭐重点（优先级：高）

在CompTriggerBody上集中提供两个通用方法：

```csharp
/// <summary>
/// 通用配置解析：ActivatingSlot → AllActiveSlots回退链。
/// 供Effect类使用（不需要侧别信息）。
/// </summary>
public T ResolveChipConfig<T>() where T : DefModExtension
{
    if (ActivatingSlot?.loadedChip != null)
    {
        var cfg = ActivatingSlot.loadedChip.def.GetModExtension<T>();
        if (cfg != null) return cfg;
    }
    foreach (var slot in AllActiveSlots())
    {
        var ext = slot.loadedChip?.def?.GetModExtension<T>();
        if (ext != null) return ext;
    }
    return null;
}

/// <summary>
/// 按侧解析芯片Thing：侧别精确定位 → ActivatingSlot → AllActiveSlots回退链。
/// 供Verb类使用（需要知道自己属于哪一侧）。
/// </summary>
public Thing ResolveChipThing(SlotSide? side)
{
    if (side.HasValue)
    {
        var slot = GetActiveSlot(side.Value);
        if (slot?.loadedChip != null) return slot.loadedChip;
    }
    if (ActivatingSlot?.loadedChip != null)
        return ActivatingSlot.loadedChip;
    foreach (var slot in AllActiveSlots())
    {
        if (slot.loadedChip != null) return slot.loadedChip;
    }
    return null;
}
```

改造后各调用点简化为：

```csharp
// Effect类（三个全部统一为一行）
var cfg = triggerBody.TryGetComp<CompTriggerBody>()?.ResolveChipConfig<ShieldChipConfig>();

// Verb类
Thing chip = triggerComp.ResolveChipThing(DualVerbCompositor.ParseSideLabel(verbProps?.label));
```

对于Verb_BDPShoot等需要额外过滤"有WeaponChipConfig的"槽位的场景，可提供泛型版本：

```csharp
public Thing ResolveChipThing<T>(SlotSide? side) where T : DefModExtension
{
    // 同上逻辑，最终回退时加 GetModExtension<T>() != null 过滤
}
```

### 建议3：侧别标识改用实例字段（优先级：低）

在Verb实例上直接挂`SlotSide? assignedSide`字段（CreateAndCacheChipVerbs时设置），替代通过label间接传递。消除ParseSideLabel，获得编译期类型安全。改动面涉及Verb创建流程，不急。

### 建议4：拆分Verb_BDPMelee的burst状态机（优先级：低）

将hitIndex、cachedLeftBurst/Right、pendingInterval及相关方法抽为`MeleeBurstState`值对象，Verb_BDPMelee持有它而非直接持有字段。锦上添花。

### 建议5：ComboAbilityDef要么实现要么删除（优先级：低）

悬空设计会误导后续开发。

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-24 | 基于源码分析生成Trigger模块架构评估报告 | Claude Opus 4.6 |
