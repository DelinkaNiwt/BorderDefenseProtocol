---
标题：BDP芯片形态系统架构设计
版本号: v1.0
更新日期: 2026-03-11
最后修改者: Claude Sonnet 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: BDP模组芯片系统架构升级设计，引入形态系统支持效果组合、形态切换和效果关系
---

# BDP芯片形态系统架构设计

## 1. 概述

本设计文档描述BDP模组芯片系统的架构升级方案，目标是解决当前架构"边界太死"的问题，支持：
1. **效果组合**：一个芯片能同时激活多种效果
2. **形态切换**：芯片内部有多个形态，每个形态有自己的效果集合
3. **效果关系**：效果之间能有条件/依赖关系

## 2. 当前架构问题

### 2.1 核心约束

当前芯片模型：
```
芯片 ──绑定──> 1个 chipEffectClass
                    ↓
              Activate / Deactivate
```

三个根本性约束：
1. **效果是原子的**：一个芯片只能有一种效果，无法组合
2. **效果是无状态的**：设计约定禁止效果类持有运行时数据，芯片没有"形态"概念
3. **效果是孤立的**：效果之间没有任何关系，不能互相感知、依赖、触发

### 2.2 具体痛点

- 无法实现"护盾+武器+能力"的复合芯片
- 无法实现形态切换（护盾形态 vs 近战形态）
- 无法实现形态专属属性修正（护盾形态降低移速）
- 无法实现条件效果（血量低时自动切换形态）

## 3. 设计目标

### 3.1 功能目标

1. 支持一个芯片同时具备多种效果（效果组合）
2. 支持芯片内部形态切换（形态系统）
3. 支持形态专属属性修正（StatModifiers）
4. 支持条件自动切换（可选高级功能）

### 3.2 非功能目标

1. 保持 `IChipEffect` 接口不变（现有效果类不需要修改）
2. 破坏性变更：不向后兼容，一改就改干净
3. XML配置直观易懂
4. 性能无明显下降

## 4. 架构方案

### 4.1 方案选择

采用**方案B：形态系统**

核心思路：引入 `ChipMode` 概念，芯片有多个形态，每个形态有自己的效果列表。

### 4.2 架构关系图

#### 当前架构（简化）

```
芯片物品定义 (ThingDef)
  └─ CompProperties_TriggerChip
       └─ chipEffectClass: Type ──────┐
                                      │ (1:1绑定)
芯片物品实例 (Thing)                    │
  └─ TriggerChipComp                  │
       └─ effectInstance ◄────────────┘
            ↓ 实现
       IChipEffect (接口)
         ├─ HediffChipEffect
         ├─ VerbChipEffect
         └─ AbilityChipEffect

槽位 (ChipSlot)
  ├─ loadedChip: Thing
  └─ isActive: bool
```

#### 升级后架构

```
芯片物品定义 (ThingDef)
  └─ CompProperties_TriggerChip
       └─ modes: List<ChipMode> ──────┐
                                      │ (1:N)
            ChipMode (新增)            │
              ├─ label: string        │
              ├─ icon: Texture2D      │
              ├─ effectClasses: List<Type> ──┐
              ├─ statModifiers: List<StatModifier>
              └─ switchCondition: IChipModeCondition (可选)
                                      │
芯片物品实例 (Thing)                    │
  └─ TriggerChipComp                  │
       ├─ GetCurrentMode(slot) ───────┘
       ├─ SwitchMode(slot, modeIndex)
       └─ modeEffectCache: Dict<int, List<IChipEffect>>
                                      │
                                      │ (N:M)
                                      ↓
            IChipEffect (接口，不变)
              ├─ HediffChipEffect
              ├─ VerbChipEffect
              ├─ AbilityChipEffect
              └─ PassiveChipEffect

槽位 (ChipSlot)
  ├─ loadedChip: Thing
  ├─ isActive: bool
  └─ currentModeIndex: int (新增)
```

## 5. 核心类设计

### 5.1 ChipMode（新增）

```csharp
/// <summary>
/// 芯片形态定义（DefModExtension的一部分）
/// 每个形态包含：效果列表、属性修正、切换条件
/// </summary>
public class ChipMode : IExposable
{
    // 形态标识
    public string label;              // 形态名称（UI显示）
    public string icon;               // 形态图标路径（可选）
    public string description;        // 形态描述

    // 效果组合
    public List<Type> effectClasses;  // 该形态的效果类型列表

    // 形态专属属性修正
    public List<StatModifier> statOffsets;  // 加算修正
    public List<StatModifier> statFactors;  // 乘算修正

    // 自动切换条件（可选）
    public ChipModeSwitchCondition switchCondition;

    // 切换成本（可选）
    public float switchCost;          // 切换到此形态的Trion消耗
    public int switchWarmup;          // 切换预热时间（ticks）
}
```

### 5.2 ChipModeSwitchCondition（新增）

```csharp
/// <summary>
/// 形态自动切换条件（可选功能）
/// </summary>
public class ChipModeSwitchCondition
{
    public ChipModeSwitchTrigger trigger;  // 触发类型
    public float threshold;                // 阈值
    public int targetModeIndex;            // 目标形态索引
}

public enum ChipModeSwitchTrigger
{
    None,              // 无自动切换
    HealthBelow,       // 血量低于阈值
    TrionBelow,        // Trion低于阈值
    EnemyNear,         // 敌人靠近（距离阈值）
    // 可扩展...
}
```

### 5.3 CompProperties_TriggerChip（修改）

```csharp
public class CompProperties_TriggerChip : CompProperties
{
    // ── 删除旧字段 ──
    // public Type chipEffectClass;  // 删除

    // ── 新字段 ──
    public List<ChipMode> modes;  // 形态列表（替代chipEffectClass）

    // ── 其他字段保持不变 ──
    public float activationCost;
    public float drainPerDay;
    // ...
}
```

### 5.4 ChipSlot（修改）

```csharp
public class ChipSlot : IExposable
{
    public int index;
    public SlotSide side;
    public Thing loadedChip;
    public bool isActive;
    public bool isDisabled;

    // ── 新增字段 ──
    public int currentModeIndex;  // 当前形态索引（默认0）

    public void ExposeData()
    {
        // ... 现有序列化代码 ...
        Scribe_Values.Look(ref currentModeIndex, "currentModeIndex", 0);
    }
}
```

### 5.5 TriggerChipComp（修改）

```csharp
public class TriggerChipComp : ThingComp
{
    // ── 删除旧字段 ──
    // private IChipEffect effectInstance;  // 删除

    // ── 新字段 ──
    // 效果实例缓存：modeIndex → 效果列表
    private Dictionary<int, List<IChipEffect>> modeEffectCache;

    // ── 新方法 ──

    /// <summary>
    /// 获取指定槽位的当前形态
    /// </summary>
    public ChipMode GetCurrentMode(ChipSlot slot)
    {
        if (Props.modes == null || Props.modes.Count == 0)
            return null;

        int idx = Mathf.Clamp(slot.currentModeIndex, 0, Props.modes.Count - 1);
        return Props.modes[idx];
    }

    /// <summary>
    /// 获取指定形态的所有效果实例（懒加载）
    /// </summary>
    public List<IChipEffect> GetModeEffects(int modeIndex)
    {
        if (modeEffectCache == null)
            modeEffectCache = new Dictionary<int, List<IChipEffect>>();

        if (modeEffectCache.TryGetValue(modeIndex, out var cached))
            return cached;

        var mode = Props.modes[modeIndex];
        var effects = new List<IChipEffect>();

        foreach (var effectClass in mode.effectClasses)
        {
            try
            {
                var effect = (IChipEffect)Activator.CreateInstance(effectClass);
                effects.Add(effect);
            }
            catch (Exception e)
            {
                Log.Error($"[BDP] 无法实例化效果 {effectClass}: {e}");
            }
        }

        modeEffectCache[modeIndex] = effects;
        return effects;
    }

    /// <summary>
    /// 检查是否可以切换形态
    /// </summary>
    public bool CanSwitchMode(ChipSlot slot, int targetModeIndex)
    {
        if (targetModeIndex < 0 || targetModeIndex >= Props.modes.Count)
            return false;

        if (slot.currentModeIndex == targetModeIndex)
            return false;  // 已经是目标形态

        // 检查切换成本
        var targetMode = Props.modes[targetModeIndex];
        if (targetMode.switchCost > 0f)
        {
            var pawn = slot.loadedChip?.ParentHolder?.ParentHolder as Pawn;
            var trionComp = pawn?.GetComp<CompTrion>();
            if (trionComp == null || trionComp.Available < targetMode.switchCost)
                return false;
        }

        return true;
    }
}
```

### 5.6 CompTriggerBody（修改）

关键修改点：

1. **DoActivate**：遍历当前形态的所有效果并激活
2. **DeactivateSlot**：遍历当前形态的所有效果并关闭
3. **SwitchChipMode**（新增）：切换形态逻辑
4. **ApplyModeStatModifiers**（新增）：应用形态属性修正
5. **RemoveModeStatModifiers**（新增）：移除形态属性修正

```csharp
public partial class CompTriggerBody
{
    // ── 修改：DoActivate ──
    private void DoActivate(ChipSlot slot)
    {
        var pawn = OwnerPawn;
        if (pawn == null) return;

        var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
        if (chipComp == null) return;

        // 获取当前形态
        var currentMode = chipComp.GetCurrentMode(slot);
        if (currentMode == null) return;

        // 激活该形态的所有效果
        var effects = chipComp.GetModeEffects(slot.currentModeIndex);
        WithActivatingContext(slot, () =>
        {
            foreach (var effect in effects)
            {
                effect.Activate(pawn, parent);
            }
        });

        // 应用形态专属属性修正
        ApplyModeStatModifiers(pawn, currentMode);

        slot.isActive = true;

        // ... 其他逻辑 ...
    }

    // ── 修改：DeactivateSlot ──
    private void DeactivateSlot(ChipSlot slot, Pawn pawnOverride = null)
    {
        if (!slot.isActive || slot.loadedChip == null)
            return;

        var pawn = pawnOverride ?? OwnerPawn;
        var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();

        // 获取当前形态
        var currentMode = chipComp?.GetCurrentMode(slot);

        // 关闭该形态的所有效果
        var effects = chipComp?.GetModeEffects(slot.currentModeIndex);
        if (effects != null)
        {
            WithActivatingContext(slot, () =>
            {
                foreach (var effect in effects)
                {
                    effect.Deactivate(pawn, parent);
                }
            });
        }

        // 移除形态专属属性修正
        RemoveModeStatModifiers(pawn, currentMode);

        slot.isActive = false;

        // ... 其他逻辑 ...
    }

    // ── 新增：切换形态 ──
    public bool SwitchChipMode(SlotSide side, int slotIndex, int targetModeIndex)
    {
        var slot = GetSlot(side, slotIndex);
        if (slot?.loadedChip == null) return false;

        var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
        if (!chipComp.CanSwitchMode(slot, targetModeIndex))
            return false;

        // 如果槽位激活，先关闭当前形态的效果
        bool wasActive = slot.isActive;
        if (wasActive)
        {
            DeactivateSlot(slot);
        }

        // 切换形态索引
        slot.currentModeIndex = targetModeIndex;

        // 消耗切换成本
        var targetMode = chipComp.Props.modes[targetModeIndex];
        if (targetMode.switchCost > 0f)
        {
            TrionComp?.Consume(targetMode.switchCost);
        }

        // 重新激活（如果之前是激活状态）
        if (wasActive)
        {
            DoActivate(slot);
        }

        return true;
    }

    // ── 新增：应用形态属性修正 ──
    private void ApplyModeStatModifiers(Pawn pawn, ChipMode mode)
    {
        // TODO: 通过Hediff或其他机制应用StatModifiers
        // 可以复用现有的ChipStatConfig机制
    }

    private void RemoveModeStatModifiers(Pawn pawn, ChipMode mode)
    {
        // TODO: 移除StatModifiers
    }
}
```

## 6. 数据流

### 6.1 激活芯片（多效果）

```
用户点击激活Gizmo
  ↓
CompTriggerBody.ActivateChip(side, slotIndex)
  ↓
DoActivate(slot)
  ↓
TriggerChipComp.GetCurrentMode(slot) → ChipMode
  ↓
TriggerChipComp.GetModeEffects(slot.currentModeIndex) → List<IChipEffect>
  ↓
遍历效果列表：
  effect1.Activate(pawn, triggerBody)  // 例如：HediffChipEffect（护盾）
  effect2.Activate(pawn, triggerBody)  // 例如：AbilityChipEffect（蚱蜢跳跃）
  effect3.Activate(pawn, triggerBody)  // 例如：PassiveChipEffect（被动标记）
  ↓
ApplyModeStatModifiers(pawn, mode)  // 应用形态专属属性修正（移速-0.3）
  ↓
slot.isActive = true
```

### 6.2 切换形态

```
用户点击形态切换Gizmo
  ↓
CompTriggerBody.SwitchChipMode(side, slotIndex, targetModeIndex)
  ↓
检查：CanSwitchMode() → 切换成本、形态有效性
  ↓
关闭当前形态：
  DeactivateSlot(slot)
    → 关闭旧Mode的所有效果
    → 移除旧Mode的StatModifiers
  ↓
slot.currentModeIndex = targetModeIndex
  ↓
激活新形态：
  DoActivate(slot)
    → 激活新Mode的所有效果
    → 应用新Mode的StatModifiers
```

## 7. XML配置示例

### 7.1 单形态芯片

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_BasicShield</defName>
  <label>基础护盾芯片</label>
  <comps>
    <li Class="BDP.Trigger.CompProperties_TriggerChip">
      <!-- 单形态模式：只有一个Mode -->
      <modes>
        <li>
          <label>护盾</label>
          <effectClasses>
            <li>BDP.Trigger.HediffChipEffect</li>
          </effectClasses>
          <statOffsets>
            <MoveSpeed>-0.2</MoveSpeed>
          </statOffsets>
        </li>
      </modes>
      <activationCost>10</activationCost>
      <drainPerDay>5</drainPerDay>
    </li>
  </comps>
  <modExtensions>
    <li Class="BDP.Trigger.HediffChipConfig">
      <hediffDef>BDP_Hediff_FrontShield</hediffDef>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.2 多形态芯片（复合功能）

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_HybridShieldWeapon</defName>
  <label>混合护盾武器芯片</label>
  <comps>
    <li Class="BDP.Trigger.CompProperties_TriggerChip">
      <modes>
        <!-- 形态1：护盾模式 -->
        <li>
          <label>护盾形态</label>
          <icon>UI/Chips/Mode_Shield</icon>
          <description>前方大盾，降低移速但提供强力防护</description>
          <effectClasses>
            <li>BDP.Trigger.HediffChipEffect</li>      <!-- 护盾Hediff -->
            <li>BDP.Trigger.AbilityChipEffect</li>     <!-- 蚱蜢跳跃 -->
          </effectClasses>
          <statOffsets>
            <MoveSpeed>-0.3</MoveSpeed>                <!-- 移速降低 -->
            <ArmorRating_Sharp>0.15</ArmorRating_Sharp>
          </statOffsets>
          <switchCost>5</switchCost>                   <!-- 切换到此形态消耗5 Trion -->
          <switchWarmup>60</switchWarmup>              <!-- 1秒预热 -->
        </li>

        <!-- 形态2：近战模式 -->
        <li>
          <label>近战形态</label>
          <icon>UI/Chips/Mode_Melee</icon>
          <description>收起护盾，展开近战武器</description>
          <effectClasses>
            <li>BDP.Trigger.VerbChipEffect</li>        <!-- 近战武器 -->
            <li>BDP.Trigger.AbilityChipEffect</li>     <!-- 蚱蜢跳跃（保留） -->
          </effectClasses>
          <statOffsets>
            <MeleeHitChance>0.1</MeleeHitChance>       <!-- 近战命中+10% -->
          </statOffsets>
          <switchCost>5</switchCost>
          <switchWarmup>60</switchWarmup>
        </li>
      </modes>
      <activationCost>15</activationCost>
      <drainPerDay>8</drainPerDay>
    </li>
  </comps>

  <!-- 形态1的配置：护盾Hediff -->
  <modExtensions>
    <li Class="BDP.Trigger.HediffChipConfig">
      <hediffDef>BDP_Hediff_FrontShield</hediffDef>
    </li>

    <!-- 形态1和2共享的配置：蚱蜢跳跃Ability -->
    <li Class="BDP.Trigger.AbilityChipConfig">
      <abilityDef>BDP_Ability_GrasshopperJump</abilityDef>
    </li>

    <!-- 形态2的配置：近战武器 -->
    <li Class="BDP.Trigger.VerbChipConfig">
      <primaryVerbProps>
        <verbClass>Verse.Verb_MeleeAttack</verbClass>
        <hasStandardCommand>false</hasStandardCommand>
      </primaryVerbProps>
      <melee>
        <tools>
          <li>
            <label>能量刃</label>
            <capacities><li>Cut</li></capacities>
            <power>25</power>
            <cooldownTime>1.2</cooldownTime>
          </li>
        </tools>
      </melee>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.3 条件自动切换（高级功能）

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_AdaptiveShield</defName>
  <label>自适应护盾芯片</label>
  <comps>
    <li Class="BDP.Trigger.CompProperties_TriggerChip">
      <modes>
        <!-- 形态1：全功率护盾 -->
        <li>
          <label>全功率</label>
          <effectClasses>
            <li>BDP.Trigger.HediffChipEffect</li>
          </effectClasses>
          <statOffsets>
            <MoveSpeed>-0.4</MoveSpeed>
          </statOffsets>
          <!-- 条件：血量低于30%时自动切换到形态2 -->
          <switchCondition>
            <trigger>HealthBelow</trigger>
            <threshold>0.3</threshold>
            <targetModeIndex>1</targetModeIndex>
          </switchCondition>
        </li>

        <!-- 形态2：紧急模式（低功耗） -->
        <li>
          <label>紧急模式</label>
          <effectClasses>
            <li>BDP.Trigger.HediffChipEffect</li>
          </effectClasses>
          <statOffsets>
            <MoveSpeed>-0.1</MoveSpeed>  <!-- 移速惩罚减少 -->
          </statOffsets>
        </li>
      </modes>
      <activationCost>20</activationCost>
      <drainPerDay>10</drainPerDay>
    </li>
  </comps>
  <modExtensions>
    <li Class="BDP.Trigger.HediffChipConfig">
      <hediffDef>BDP_Hediff_AdaptiveShield</hediffDef>
    </li>
  </modExtensions>
</ThingDef>
```

## 8. 破坏性变更清单

### 8.1 C# 层

- `CompProperties_TriggerChip`：删除 `chipEffectClass`，只保留 `modes: List<ChipMode>`
- `TriggerChipComp`：删除 `effectInstance`，改为 `modeEffectCache`，`GetEffect()` 改为 `GetModeEffects(int)`
- `CompTriggerBody.Activation.cs`：`DoActivate` / `DeactivateSlot` 改为遍历效果列表
- `CompTriggerBody.GizmoGeneration.cs`：新增形态切换Gizmo（多形态时才显示）
- `ChipSlot`：新增 `currentModeIndex` 字段

### 8.2 XML 层

- 所有现有芯片 ThingDef：`chipEffectClass` → `modes` 结构
- 现有芯片的 `modExtensions` 配置保持不变（HediffChipConfig / VerbChipConfig / AbilityChipConfig 不动）

## 9. 实施步骤（高层）

```
阶段1：数据层
  ├─ 新增 ChipMode 类
  ├─ 新增 ChipModeSwitchCondition 类
  ├─ 修改 CompProperties_TriggerChip（删chipEffectClass，加modes）
  └─ 修改 ChipSlot（加currentModeIndex）

阶段2：效果执行层
  ├─ 修改 TriggerChipComp（GetEffect → GetModeEffects）
  ├─ 修改 DoActivate（遍历效果列表）
  ├─ 修改 DeactivateSlot（遍历效果列表）
  └─ 新增 ApplyModeStatModifiers / RemoveModeStatModifiers

阶段3：形态切换层
  ├─ 新增 SwitchChipMode 方法
  ├─ 新增形态切换Gizmo
  └─ 新增条件自动切换检测（Tick逻辑）

阶段4：XML迁移
  └─ 更新所有现有芯片定义
```

## 10. 潜在风险与缓解

### 10.1 风险

1. **存档兼容性**：`ChipSlot` 新增字段，旧存档读取时 `currentModeIndex` 默认为0
2. **性能影响**：遍历效果列表可能比单效果慢（但影响极小）
3. **XML配置复杂度**：多形态芯片的XML配置较长

### 10.2 缓解措施

1. **存档兼容性**：`Scribe_Values.Look` 提供默认值0，旧存档自动使用第一个形态
2. **性能影响**：效果列表通常只有2-3个元素，性能影响可忽略
3. **XML配置复杂度**：提供模板和示例，单形态芯片配置仍然简洁

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-11 | 初始版本，完整架构设计 | Claude Sonnet 4.6 |
