# 底层框架设计草案 - 基于Trion系统抽象

---

## 文档元信息

**摘要**：基于Trion系统抽象出的通用底层框架设计方案，采用ECS设计模式，遵循奥卡姆剃刀原则，为ProjectWT等未来模组提供可复用的架构基础。

**版本号**：草案 v0.1
**修改时间**：2026-01-10
**关键词**：ECS框架、资源池系统、组件系统、虚拟状态、Trion抽象、底层框架
**标签**：[草稿]

---

## 一、框架总览

### 1.1 设计目标

将Trion系统的核心机制抽象为**通用底层框架**，使其能够：
- ✅ 支持多种资源类型（Trion、魔力、耐力、精力等）
- ✅ 支持多种实体类型（人类、机械体、建筑等）
- ✅ 提供可复用的组件和系统
- ✅ 保持简洁性和高性能

### 1.2 核心设计原则

| 原则 | 说明 | 体现 |
|------|------|------|
| **ECS架构** | Entity-Component-System | 清晰的职责分离 |
| **奥卡姆剃刀** | 避免不必要的复杂度 | 只抽象真正通用的部分 |
| **性能优先** | 避免每Tick计算 | 批量计算（60 Tick） |
| **扩展友好** | 易于添加新资源类型 | 配置驱动 + 接口扩展 |

### 1.3 框架命名

**建议框架名**：`ResourceVirtualFramework` (RVF)

**职责边界**：
- ✅ 做什么：资源池管理、虚拟状态、组件装备、消耗计算
- ❌ 不做什么：具体的游戏逻辑（战斗、UI、AI等）

---

## 二、核心抽象概念

### 2.1 从Trion到通用资源池

**Trion系统的四要素**：
```
Capacity（总容量） - 固定
Reserved（占用量） - 可逆
Consumed（已消耗） - 不可逆
Available（可用量） = Capacity - Reserved - Consumed
```

**抽象为通用资源池**：
```csharp
// 框架核心：ResourcePool
public class ResourcePool
{
    // 四要素（通用）
    public float Capacity { get; set; }     // 总容量
    public float Reserved { get; set; }     // 占用量
    public float Consumed { get; set; }     // 已消耗量
    public float Available => Capacity - Reserved - Consumed;  // 可用量（派生）

    // 资源类型标识
    public ResourceTypeDef resourceType;  // 如：Trion、Mana、Stamina

    // 恢复机制（可配置）
    public float recoveryRate;            // 基础恢复速率
    public List<RecoveryModifier> modifiers;  // 恢复修正因素
}
```

**可应用场景**：
- Trion能量系统
- 魔力系统（Mana）
- 耐力系统（Stamina）
- 精神力系统（Mental Energy）
- 任何有"容量-占用-消耗"模型的资源

### 2.2 从组件到通用装备系统

**Trion组件的核心属性**：
```
占用值 - 战斗体状态下锁定容量
激活费用 - 激活时的一次性消耗
持续消耗 - 每单位时间的额外消耗
使用费用 - 使用功能时的消耗
功能 - 组件提供的具体效果
```

**抽象为通用装备组件**：
```csharp
// 框架核心：EquippableComponent
public class EquippableComponentDef : Def
{
    // 资源占用（通用）
    public ResourceCost reserveCost;      // 占用成本（什么资源，多少量）
    public ResourceCost activationCost;   // 激活成本
    public ResourceCost sustainCost;      // 持续成本（每单位时间）
    public ResourceCost usageCost;        // 使用成本

    // 状态机
    public ComponentState currentState;   // Disconnected | Dormant | Active

    // 装备槽位
    public EquipmentSlotDef requiredSlot; // 需要的装备槽

    // 功能实现（抽象）
    public Type workerClass;              // 具体功能由Worker实现
}

// 资源成本（通用）
public class ResourceCost
{
    public ResourceTypeDef resourceType;  // 资源类型
    public float amount;                  // 数量
}
```

**可应用场景**：
- Trion组件系统
- 技能树系统（技能需要精神力占用）
- 装备强化系统（强化占用材料槽位）
- 魔法装备系统（装备占用魔力）

### 2.3 从战斗体到虚拟状态系统

**Trion战斗体的核心机制**：
```
生成时：快照肉身状态 → 生成虚拟身体 → 锁定占用量
虚拟伤害：不扣真实血量
解除时：恢复快照状态 OR 流失占用量（被动）
```

**抽象为虚拟状态系统**：
```csharp
// 框架核心：VirtualState
public class VirtualState
{
    // 快照数据
    public StateSnapshot snapshot;        // 原始状态快照

    // 虚拟数据
    public VirtualHealth virtualHealth;   // 虚拟血量/状态
    public List<VirtualEffect> effects;   // 虚拟效果

    // 解除规则
    public ReleaseRule releaseRule;       // 主动 | 被动（资源耗尽）
    public float releasePenalty;          // 被动解除惩罚
}

// 快照（通用）
public class StateSnapshot
{
    public List<Hediff> hediffs;          // 健康数据
    public List<Apparel> apparels;        // 服装
    public ThingOwner inventory;          // 物品
    // ... 可扩展
}
```

**可应用场景**：
- Trion战斗体系统
- 模拟战斗系统（训练不受伤）
- 虚拟现实系统（VR体验）
- 幻象/分身系统

### 2.4 从Trion消耗到通用消耗管理

**Trion消耗的分类**：
```
一次性消耗：激活、射击、受伤、释放能力
持续性消耗：战斗体维持、组件维持、伤口泄漏（60 Tick批量计算）
```

**抽象为消耗管理系统**：
```csharp
// 框架核心：ConsumptionManager
public class ConsumptionManager
{
    // 一次性消耗记录
    public void RegisterOneTimeConsumption(ResourcePool pool, ResourceCost cost);

    // 持续性消耗记录
    public void RegisterContinuousConsumption(ResourcePool pool, ResourceCost costPerInterval);

    // 批量计算（性能优化）
    public void CalculateAllConsumptions(int intervalTicks = 60);
}
```

**性能约束**：
- ❌ 不可每Tick计算持续消耗
- ✅ 必须批量计算（默认60 Tick）
- ✅ 所有持续消耗项累加求和

### 2.5 从输出功率到能力门槛系统

**输出功率的作用**：
```
限制能力释放：不满足最低功率则禁用
影响组件表现：功率越高，伤害/射程/持续时间等越好
```

**抽象为能力门槛系统**：
```csharp
// 框架核心：CapabilityThreshold
public class CapabilityThreshold
{
    // 能力值
    public float capabilityValue;         // 当前能力值（如输出功率）

    // 门槛规则
    public float minimumThreshold;        // 最低门槛
    public ThresholdEffect effect;        // 影响效果
}

// 影响效果（可配置）
public class ThresholdEffect
{
    public StatDef affectedStat;          // 影响的属性
    public SimpleCurve scalingCurve;      // 缩放曲线
}
```

**可应用场景**：
- Trion输出功率系统
- 技能等级系统
- 属性门槛系统（力量影响武器伤害）

---

## 三、ECS架构设计

### 3.1 整体架构图

```
┌─────────────────────────────────────────────────┐
│  应用层（ProjectWT、其他模组）                   │
│  使用框架提供的Comp和Def，实现具体游戏逻辑        │
└─────────────────────────────────────────────────┘
                     ↓ 依赖
┌─────────────────────────────────────────────────┐
│  框架API层（RVF Public API）                     │
│  - ResourcePoolUtility                          │
│  - EquipmentSystemUtility                       │
│  - VirtualStateUtility                          │
└─────────────────────────────────────────────────┘
                     ↓ 调用
┌─────────────────────────────────────────────────┐
│  System层（逻辑处理）                            │
│  - ResourcePoolSystem                           │
│  - EquipmentSystem                              │
│  - VirtualStateSystem                           │
│  - ConsumptionSystem (60 Tick批量计算)          │
└─────────────────────────────────────────────────┘
                     ↓ 操作
┌─────────────────────────────────────────────────┐
│  Component层（数据容器）                         │
│  - CompResourcePool                             │
│  - CompEquippable                               │
│  - CompVirtualState                             │
│  - CompConsumption                              │
└─────────────────────────────────────────────────┘
                     ↓ 附加到
┌─────────────────────────────────────────────────┐
│  Entity层（RimWorld原生）                        │
│  - Pawn                                         │
│  - Building                                     │
│  - Thing（可扩展）                               │
└─────────────────────────────────────────────────┘
```

### 3.2 核心组件设计

#### CompResourcePool（资源池组件）

```csharp
public class CompResourcePool : ThingComp
{
    // 数据（只存储数据，不包含逻辑）
    public ResourcePool pool;

    // 配置
    public CompProperties_ResourcePool Props => (CompProperties_ResourcePool)props;

    // 初始化
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            pool = new ResourcePool();
            pool.resourceType = Props.resourceTypeDef;
            pool.Capacity = CalculateInitialCapacity();  // 委托给System
        }
    }

    // 序列化
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref pool, "resourcePool");
    }
}

// 配置Def
public class CompProperties_ResourcePool : CompProperties
{
    public ResourceTypeDef resourceTypeDef;       // 资源类型
    public CapacitySourceDef capacitySource;      // 容量来源（天赋/固定值/配置）
    public RecoveryRuleDef recoveryRule;          // 恢复规则

    public CompProperties_ResourcePool()
    {
        compClass = typeof(CompResourcePool);
    }
}
```

#### CompEquippable（装备组件）

```csharp
public class CompEquippable : ThingComp
{
    // 数据
    public List<EquippedComponent> equippedComponents;
    public Dictionary<EquipmentSlotDef, EquippedComponent> activeComponents;

    // 配置
    public CompProperties_Equippable Props => (CompProperties_Equippable)props;

    // 装备操作（委托给System）
    public bool TryEquip(EquippableComponentDef componentDef, EquipmentSlotDef slot)
    {
        return EquipmentSystem.TryEquip(this, componentDef, slot);
    }

    public bool TryActivate(EquippableComponentDef componentDef)
    {
        return EquipmentSystem.TryActivate(this, componentDef);
    }

    public void Deactivate(EquippableComponentDef componentDef)
    {
        EquipmentSystem.Deactivate(this, componentDef);
    }
}

// 装备槽位配置
public class EquipmentSlotDef : Def
{
    public int maxEquipped = 999;                 // 最多装备数量
    public int maxActive = 1;                     // 最多激活数量
    public BodyPartDef boundBodyPart;             // 绑定部位（可选）
}
```

#### CompVirtualState（虚拟状态组件）

```csharp
public class CompVirtualState : ThingComp
{
    // 数据
    public VirtualState virtualState;
    public bool isVirtualActive = false;

    // 生成虚拟状态
    public bool TryEnterVirtual()
    {
        return VirtualStateSystem.TryEnterVirtual(this);
    }

    // 解除虚拟状态
    public void ExitVirtual(bool forced = false)
    {
        VirtualStateSystem.ExitVirtual(this, forced);
    }
}
```

#### CompConsumption（消耗管理组件）

```csharp
public class CompConsumption : ThingComp
{
    // 数据
    public List<ContinuousConsumption> continuousConsumptions;
    private int tickCounter = 0;

    // 定时计算（60 Tick）
    public override void CompTick()
    {
        base.CompTick();
        tickCounter++;
        if (tickCounter >= 60)
        {
            ConsumptionSystem.CalculateConsumptions(this);
            tickCounter = 0;
        }
    }
}
```

### 3.3 核心系统设计

#### ResourcePoolSystem（资源池管理系统）

```csharp
public static class ResourcePoolSystem
{
    // 计算初始容量
    public static float CalculateInitialCapacity(Thing thing, CapacitySourceDef source)
    {
        switch (source.sourceType)
        {
            case CapacitySourceType.FixedValue:
                return source.fixedValue;

            case CapacitySourceType.AttributeBased:
                // 如：基于Trion天赋
                var attribute = GetAttributeValue(thing, source.attributeDef);
                return source.capacityCurve.Evaluate(attribute);

            case CapacitySourceType.SizeBased:
                // 如：基于体积
                return thing.def.size.x * thing.def.size.z * source.sizeMultiplier;

            default:
                return 100f;  // 默认值
        }
    }

    // 恢复计算
    public static void ApplyRecovery(CompResourcePool comp, int ticks)
    {
        var pool = comp.pool;
        var rule = comp.Props.recoveryRule;

        // 基础恢复速率
        float recovery = rule.baseRecoveryRate * (ticks / 60f);

        // 应用修正因素
        foreach (var modifier in rule.modifiers)
        {
            if (modifier.CheckCondition(comp.parent))
            {
                recovery *= modifier.multiplier;
            }
        }

        // 恢复已消耗量
        pool.Consumed = Mathf.Max(0, pool.Consumed - recovery);
    }
}
```

#### EquipmentSystem（装备管理系统）

```csharp
public static class EquipmentSystem
{
    // 装备组件
    public static bool TryEquip(CompEquippable comp, EquippableComponentDef componentDef, EquipmentSlotDef slot)
    {
        // 检查槽位容量
        var slotComponents = comp.equippedComponents.Where(c => c.slot == slot).ToList();
        if (slotComponents.Count >= slot.maxEquipped)
        {
            return false;
        }

        // 创建装备实例
        var equipped = new EquippedComponent
        {
            def = componentDef,
            slot = slot,
            state = ComponentState.Disconnected
        };

        comp.equippedComponents.Add(equipped);
        return true;
    }

    // 激活组件
    public static bool TryActivate(CompEquippable comp, EquippableComponentDef componentDef)
    {
        var equipped = comp.equippedComponents.Find(c => c.def == componentDef);
        if (equipped == null || equipped.state == ComponentState.Disconnected)
        {
            return false;
        }

        // 检查资源
        var resourcePool = comp.parent.GetComp<CompResourcePool>();
        if (!CanAfford(resourcePool, componentDef.activationCost))
        {
            return false;
        }

        // 扣除激活费用
        ConsumeResource(resourcePool, componentDef.activationCost);

        // 切换状态
        equipped.state = ComponentState.Active;

        // 注册持续消耗
        if (componentDef.sustainCost != null)
        {
            var consumption = comp.parent.GetComp<CompConsumption>();
            consumption.continuousConsumptions.Add(new ContinuousConsumption
            {
                source = componentDef,
                cost = componentDef.sustainCost
            });
        }

        return true;
    }
}
```

#### VirtualStateSystem（虚拟状态管理系统）

```csharp
public static class VirtualStateSystem
{
    // 进入虚拟状态
    public static bool TryEnterVirtual(CompVirtualState comp)
    {
        var pawn = comp.parent as Pawn;
        if (pawn == null) return false;

        // 创建快照
        var snapshot = CreateSnapshot(pawn);

        // 创建虚拟状态
        comp.virtualState = new VirtualState
        {
            snapshot = snapshot,
            virtualHealth = new VirtualHealth(pawn)
        };

        // 锁定资源占用（如果有）
        var resourcePool = pawn.GetComp<CompResourcePool>();
        var equippable = pawn.GetComp<CompEquippable>();
        if (resourcePool != null && equippable != null)
        {
            float totalReserve = CalculateTotalReserve(equippable);
            resourcePool.pool.Reserved += totalReserve;
        }

        // 标记虚拟状态激活
        comp.isVirtualActive = true;

        return true;
    }

    // 退出虚拟状态
    public static void ExitVirtual(CompVirtualState comp, bool forced)
    {
        var pawn = comp.parent as Pawn;
        if (pawn == null || !comp.isVirtualActive) return;

        // 恢复快照
        RestoreSnapshot(pawn, comp.virtualState.snapshot);

        // 释放资源占用
        var resourcePool = pawn.GetComp<CompResourcePool>();
        if (resourcePool != null)
        {
            if (forced)
            {
                // 被动解除：占用量流失
                resourcePool.pool.Consumed += resourcePool.pool.Reserved;
                resourcePool.pool.Reserved = 0;

                // 施加惩罚
                ApplyReleasePenalty(pawn);
            }
            else
            {
                // 主动解除：占用量返还
                resourcePool.pool.Reserved = 0;
            }
        }

        // 清除虚拟状态
        comp.virtualState = null;
        comp.isVirtualActive = false;
    }
}
```

#### ConsumptionSystem（消耗计算系统）

```csharp
public static class ConsumptionSystem
{
    // 批量计算消耗（60 Tick）
    public static void CalculateConsumptions(CompConsumption comp)
    {
        var resourcePool = comp.parent.GetComp<CompResourcePool>();
        if (resourcePool == null) return;

        // 累加所有持续消耗
        float totalConsumption = 0f;

        foreach (var continuous in comp.continuousConsumptions)
        {
            totalConsumption += continuous.cost.amount;
        }

        // 应用消耗
        resourcePool.pool.Consumed += totalConsumption;

        // 检查资源耗尽
        if (resourcePool.pool.Available <= 0)
        {
            OnResourceDepleted(comp.parent);
        }
    }

    // 资源耗尽处理
    private static void OnResourceDepleted(Thing thing)
    {
        // 触发虚拟状态被动解除
        var virtualState = thing.GetComp<CompVirtualState>();
        if (virtualState != null && virtualState.isVirtualActive)
        {
            VirtualStateSystem.ExitVirtual(virtualState, forced: true);
        }
    }
}
```

---

## 四、配置系统设计

### 4.1 核心Def定义

#### ResourceTypeDef（资源类型定义）

```xml
<!-- Example: Trion资源类型 -->
<ResourceTypeDef>
    <defName>Trion</defName>
    <label>Trion</label>
    <description>特殊能量资源</description>

    <!-- 显示配置 -->
    <uiIcon>UI/Resources/Trion</uiIcon>
    <uiColor>(0.2, 0.6, 1.0)</uiColor>

    <!-- 恢复规则 -->
    <recoveryRule>
        <baseRecoveryRate>1.0</baseRecoveryRate>
        <modifiers>
            <li Class="RecoveryModifier_NeedBased">
                <needDef>Food</needDef>
                <threshold>0.3</threshold>
                <belowThresholdMultiplier>0</belowThresholdMultiplier>
            </li>
        </modifiers>
    </recoveryRule>
</ResourceTypeDef>
```

#### EquippableComponentDef（装备组件定义）

```xml
<!-- Example: Trion组件 - 炸裂弹 -->
<EquippableComponentDef>
    <defName>Component_BurstBullet</defName>
    <label>炸裂弹</label>

    <!-- 资源成本 -->
    <reserveCost>
        <resourceType>Trion</resourceType>
        <amount>10</amount>
    </reserveCost>

    <activationCost>
        <resourceType>Trion</resourceType>
        <amount>5</amount>
    </activationCost>

    <usageCost>
        <resourceType>Trion</resourceType>
        <amount>1</amount>
    </usageCost>

    <!-- 槽位需求 -->
    <requiredSlot>Slot_RightHand</requiredSlot>

    <!-- 功能实现 -->
    <workerClass>ProjectWT.ComponentWorker_RangedWeapon</workerClass>
</EquippableComponentDef>
```

#### EquipmentSlotDef（装备槽位定义）

```xml
<!-- Example: 左手槽位 -->
<EquipmentSlotDef>
    <defName>Slot_LeftHand</defName>
    <label>左手</label>

    <maxEquipped>999</maxEquipped>
    <maxActive>1</maxActive>

    <boundBodyPart>LeftHand</boundBodyPart>
</EquipmentSlotDef>
```

### 4.2 实体配置示例

#### 人类殖民者配置

```xml
<!-- ThingDef_AlienRace配置（使用HAR框架） -->
<ThingDef_AlienRace ParentName="BasePawn">
    <defName>Human_WithResourcePool</defName>

    <!-- 添加框架组件 -->
    <comps>
        <!-- 资源池组件 -->
        <li Class="RVF.CompProperties_ResourcePool">
            <resourceTypeDef>Trion</resourceTypeDef>
            <capacitySource>
                <sourceType>AttributeBased</sourceType>
                <attributeDef>TrionTalent</attributeDef>
                <capacityCurve>
                    <points>
                        <li>(0, 1000)</li>   <!-- E级 -->
                        <li>(1, 2000)</li>   <!-- D级 -->
                        <li>(2, 4000)</li>   <!-- C级 -->
                        <li>(3, 7000)</li>   <!-- B级 -->
                        <li>(4, 12000)</li>  <!-- A级 -->
                        <li>(5, 20000)</li>  <!-- S级 -->
                    </points>
                </capacityCurve>
            </capacitySource>
            <recoveryRule>TrionRecovery</recoveryRule>
        </li>

        <!-- 装备组件 -->
        <li Class="RVF.CompProperties_Equippable">
            <slots>
                <li>Slot_LeftHand</li>
                <li>Slot_RightHand</li>
                <li>Slot_Special</li>
            </slots>
        </li>

        <!-- 虚拟状态组件 -->
        <li Class="RVF.CompProperties_VirtualState">
            <snapshotIncludes>
                <li>Hediffs</li>
                <li>Apparels</li>
                <li>Equipment</li>
                <li>Inventory</li>
            </snapshotIncludes>
            <releasePenalty>Hediff_ResourceDepleted</releasePenalty>
        </li>

        <!-- 消耗管理组件 -->
        <li Class="RVF.CompProperties_Consumption">
            <calculationInterval>60</calculationInterval>
        </li>
    </comps>
</ThingDef_AlienRace>
```

---

## 五、与Trion系统的对应关系

### 5.1 概念映射表

| Trion系统概念 | 框架抽象概念 | 说明 |
|--------------|-------------|------|
| Trion能量 | ResourcePool（资源类型：Trion） | 特化为Trion类型的资源池 |
| 战斗体 | VirtualState | 虚拟状态的一种应用 |
| 组件 | EquippableComponent | 通用装备组件 |
| 触发器 | Entity + CompEquippable | 可装备组件的实体 |
| 输出功率 | CapabilityValue | 能力门槛值 |
| Trion天赋 | AttributeDef | 影响容量的属性 |
| 占用量 | ResourcePool.Reserved | 资源池的占用字段 |
| 已消耗量 | ResourcePool.Consumed | 资源池的已消耗字段 |
| Bail Out | VirtualState强制解除 + 传送 | 虚拟状态解除机制 + 应用层逻辑 |

### 5.2 Trion系统实现方式

使用框架实现Trion系统只需：

1. **定义Trion资源类型**（XML配置）
2. **定义Trion组件**（XML配置）
3. **实现组件Worker**（少量C#代码）
4. **配置殖民者Def**（XML配置）

**工作量对比**：
- 不使用框架：需要从零实现所有系统（资源池、组件、状态等）
- 使用框架：80%配置XML，20%实现具体Worker

---

## 六、扩展性与未来规划

### 6.1 支持的扩展场景

框架设计支持以下扩展：

1. **新资源类型**：添加ResourceTypeDef即可
   - 例：魔力（Mana）、耐力（Stamina）、精神力（MentalEnergy）

2. **新组件类型**：添加EquippableComponentDef + Worker
   - 例：魔法技能、特殊能力、装备强化

3. **新实体类型**：添加Comp到任何Thing
   - 例：机械体、建筑、动物

4. **新虚拟状态**：配置不同的快照规则
   - 例：模拟战斗、训练系统

5. **新消耗规则**：实现IConsumptionRule接口
   - 例：环境消耗、社交消耗

### 6.2 未实现的Trion特性（需应用层实现）

框架**不包含**以下Trion特有逻辑（留给应用层ProjectWT实现）：

- ❌ Bail Out传送机制（应用层逻辑）
- ❌ 护盾概率判定（应用层Worker）
- ❌ 战斗体外观渲染（应用层Graphic）
- ❌ 组件UI界面（应用层Gizmo）
- ❌ Trion兵AI（应用层JobDriver）

---

## 七、性能考量

### 7.1 性能约束

| 约束项 | 规则 | 原因 |
|--------|------|------|
| 持续消耗计算 | 必须60 Tick批量计算 | 避免每Tick性能开销 |
| 资源池查询 | 缓存Available值 | 减少重复计算 |
| 组件状态切换 | 最小化状态机复杂度 | 提高响应速度 |
| 快照创建 | 只保存必要数据 | 减少内存占用 |

### 7.2 性能优化策略

```csharp
// 优化1：缓存可用量计算
public class ResourcePool
{
    private float _cachedAvailable;
    private bool _isDirty = true;

    public float Available
    {
        get
        {
            if (_isDirty)
            {
                _cachedAvailable = Capacity - Reserved - Consumed;
                _isDirty = false;
            }
            return _cachedAvailable;
        }
    }

    public void SetDirty() => _isDirty = true;
}

// 优化2：批量计算
public static class ConsumptionSystem
{
    private static List<CompConsumption> allConsumptions = new List<CompConsumption>();

    public static void RegisterConsumption(CompConsumption comp)
    {
        allConsumptions.Add(comp);
    }

    public static void CalculateAllConsumptions(int currentTick)
    {
        if (currentTick % 60 != 0) return;

        foreach (var comp in allConsumptions)
        {
            CalculateConsumptions(comp);
        }
    }
}
```

---

## 八、问题与待决策

### 8.1 设计问题

1. **资源类型是否支持多资源池**？
   - 当前设计：每个实体一种资源
   - 扩展方案：CompResourcePool支持多个ResourcePool
   - 建议：先支持单一资源，需要时再扩展

2. **组件激活是否需要引导时间**？
   - Trion系统：有1-5 Tick引导时间
   - 框架设计：暂未包含
   - 建议：添加`activationDelay`参数（默认0）

3. **虚拟状态是否支持嵌套**？
   - 当前设计：不支持嵌套虚拟状态
   - 使用场景：暂无
   - 建议：暂不支持，保持简单

4. **资源恢复是否支持负恢复**？
   - 当前设计：只支持正恢复
   - 使用场景：环境持续消耗
   - 建议：扩展为支持负恢复（通过modifier实现）

### 8.2 技术选型

1. **是否使用Harmony补丁**？
   - 当前设计：尽量避免Harmony
   - 必要场景：虚拟伤害拦截
   - 建议：仅在必要时使用，优先用Comp和Def

2. **是否依赖HAR框架**？
   - 当前设计：不强制依赖
   - 兼容方案：提供HAR集成模块（可选）
   - 建议：框架独立，提供HAR适配器

3. **是否使用DefModExtension**？
   - 当前设计：使用DefModExtension扩展原版Def
   - 优势：兼容性好，不侵入原版
   - 建议：优先使用

---

## 九、下一步工作

### 9.1 设计阶段

- [ ] 用户审阅设计方案
- [ ] 确认抽象范围是否合理
- [ ] 确认ECS架构是否清晰
- [ ] 确认命名是否准确
- [ ] 决策待定问题

### 9.2 交付阶段（用户审阅通过后）

生成6份设计文档：
1. 框架核心概念设计
2. 功能详细设计说明书
3. 数据结构设计规范
4. 系统交互流程设计
5. 配置参数定义
6. 技术方案选择与考量

附加：RiMCP验证清单

---

## 十、总结

### 10.1 框架核心价值

1. **可复用性**：不仅服务Trion系统，可用于任何资源管理系统
2. **简洁性**：遵循奥卡姆剃刀，只抽象真正通用的部分
3. **扩展性**：配置驱动，易于添加新资源类型和组件
4. **性能**：批量计算，避免每Tick开销

### 10.2 与Trion系统的关系

- **框架提供**：资源池、组件、虚拟状态、消耗管理的通用机制
- **ProjectWT提供**：Trion特有的配置、Worker实现、UI、AI逻辑

### 10.3 建议的框架名称

**ResourceVirtualFramework (RVF)**
- Resource：资源池管理
- Virtual：虚拟状态系统
- Framework：底层框架

或

**AdaptiveComponentFramework (ACF)**
- Adaptive：适应多种资源和实体
- Component：组件驱动
- Framework：底层框架

---

**需求架构师**
*2026-01-10*
