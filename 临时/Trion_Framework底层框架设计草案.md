# Trion Framework 底层框架设计草案

---

## 文档元信息

**摘要**：基于《境界触发者》Trion系统的底层框架设计，采用ECS设计模式，为ProjectWT模组提供可复用的Trion、Trigger、战斗体等核心机制。框架保留原作特色，只抽象必要的底层逻辑。

**版本号**：草案 v0.2
**修改时间**：2026-01-10
**关键词**：Trion Framework、境界触发者、Trigger系统、Combat Body、Bail Out、ECS框架
**标签**：[草稿]

---

## 一、框架定位

### 1.1 设计目标

**Trion Framework** 是为ProjectWT（及未来基于境界触发者题材的模组）提供的**底层框架**，核心目标：

- ✅ 实现Trion能量的四要素管理（Capacity/Reserved/Consumed/Available）
- ✅ 实现Trigger模块化装备系统
- ✅ 实现Combat Body（战斗体）的快照/虚拟伤害机制
- ✅ 实现Bail Out（紧急脱离）系统
- ✅ 为上层应用提供可扩展的组件Worker接口

### 1.2 框架职责边界

**框架提供**（底层机制）：
- Trion能量池的管理和计算
- Trigger装备的槽位、状态机、占用计算
- Combat Body的生成/解除、快照/回滚
- Bail Out的触发和传送机制
- 60 Tick批量消耗计算（性能优化）

**应用层实现**（ProjectWT）：
- 具体的Trigger组件（弧月、炸裂弹、护盾等）
- Trigger的功能实现（Worker类）
- 战斗体的渲染和外观
- UI界面（Gizmo、对话框等）
- AI行为（JobDriver、ThinkNode等）

### 1.3 核心设计原则

| 原则 | 说明 |
|------|------|
| **忠实原作** | 保留Trion、Trigger、Combat Body等原作概念 |
| **ECS架构** | Component存储数据，System处理逻辑 |
| **奥卡姆剃刀** | 只抽象必要的底层机制，不过度设计 |
| **性能优先** | 60 Tick批量计算持续消耗 |
| **扩展友好** | 易于添加新Trigger组件 |

---

## 二、核心概念设计

### 2.1 Trion能量四要素

基于原作设定，Trion能量池包含四要素：

```csharp
public class TrionPool
{
    // === 四要素 ===
    public float Capacity;      // 总容量（个体天赋决定，固定）
    public float Reserved;      // 占用量（Trigger模块锁定，可逆）
    public float Consumed;      // 已消耗量（累计消耗，不可逆）

    // 可用量（派生值）
    public float Available => Capacity - Reserved - Consumed;

    // === 状态 ===
    public bool IsDepleted => Available <= 0;  // 是否耗尽
}
```

**数量关系**：
```
Available = Capacity - Reserved - Consumed
```

**关键规则**：
- **Capacity**：由Trion天赋决定，固定值（当前不可提升）
- **Reserved**：Combat Body生成时锁定，主动解除时返还，被动解除时流失
- **Consumed**：所有消耗累计值，不可逆

### 2.2 Trigger模块系统

基于原作的模块化配置设计：

```csharp
// Trigger组件定义
public class TriggerComponentDef : Def
{
    // === 资源占用 ===
    public float reserveCost;       // 占用值（Combat Body状态下锁定）
    public float activationCost;    // 激活费用（每次激活支付）
    public float sustainCost;       // 持续消耗（每单位时间）
    public float usageCost;         // 使用费用（射击、释放能力等）

    // === 状态机 ===
    public TriggerState currentState;  // Disconnected | Dormant | Active

    // === 装备槽位 ===
    public TriggerSlotDef requiredSlot;  // 左手/右手/特殊

    // === 功能实现 ===
    public Type workerClass;  // 具体功能由Worker实现
}

// Trigger状态
public enum TriggerState
{
    Disconnected,  // 未连接（Combat Body未生成或挂载点被毁）
    Dormant,       // 休眠（Combat Body已生成，组件未激活）
    Active         // 激活（Combat Body已生成，组件已激活）
}
```

**槽位配置**（基于原作的8槽位系统）：

| 槽位类型 | 装备数量 | 同时激活数量 | 绑定部位 |
|---------|---------|-------------|---------|
| 主Trigger（左手） | 多个 | 1 | 左手 |
| 主Trigger（右手） | 多个 | 1 | 右手 |
| 副Trigger | 多个 | 不限 | 无 |

### 2.3 Combat Body（战斗体）系统

基于原作的战斗体机制：

```csharp
public class CombatBody
{
    // === 快照数据 ===
    public BodySnapshot snapshot;  // 肉身状态快照

    // === 虚拟伤害 ===
    public List<VirtualWound> wounds;  // 虚拟伤口列表
    public float leakageRate;          // 当前泄漏速率

    // === 状态 ===
    public bool isActive;              // 是否激活
    public int ticksSinceActivation;   // 激活后经过的Tick数
}

// 快照内容（基于原作设定）
public class BodySnapshot
{
    public List<Hediff> hediffs;       // 健康数据
    public List<Apparel> apparels;     // 服装
    public ThingOwner inventory;       // 物品
    public Thing equipment;            // 武器
}
```

**生成流程**（基于原作）：
1. 快照肉身状态
2. 冻结生理活动
3. 禁用肉身装备
4. 生成战斗体形态
5. Trigger组件注册（未连接 → 休眠）
6. 计算Reserved占用量
7. 锁定配置

**解除流程**：

| 解除方式 | 触发条件 | 流程 | 代价 |
|---------|---------|------|------|
| **主动解除** | 玩家操作 | 战斗体消失 → 组件注销 → Reserved返还 → 快照恢复 | 无 |
| **被动解除** | Trion≤0 或 核心被毁 | 战斗体崩溃 → 组件注销 → Reserved流失 → 快照恢复 → debuff | Reserved流失 + debuff |

### 2.4 Bail Out（紧急脱离）系统

基于原作的紧急脱离机制：

```csharp
public class BailOutSystem
{
    // === 触发方式 ===
    public enum BailOutTrigger
    {
        TrionDepleted,      // Trion耗尽（自动）
        CoreDestroyed,      // 核心被毁（自动）
        ManualActivation    // 手动触发
    }

    // === 效果 ===
    // 1. 瞬间传送到传送锚位置
    // 2. Combat Body立即解除（被动解除）
    // 3. 肉身恢复到快照状态
    // 4. 施加debuff："Trion枯竭"

    // === 代价 ===
    // - Bail Out组件占用量（400）永久流失
    // - 其他所有组件占用量永久流失
    // - 通常导致Trion完全归零
}
```

**前提条件**：必须装备"Bail Out组件"（占用值：400）

**优先级**：
- 核心被摧毁 = 最高优先级（立即触发）
- 手动触发 = 最高优先级
- Trion耗尽 = 普通优先级

### 2.5 Trion消耗管理

基于原作的消耗分类：

```csharp
// 消耗类型
public enum ConsumptionType
{
    OneTime,      // 一次性消耗
    Continuous    // 持续性消耗
}

// 一次性消耗场景
public class OneTimeConsumption
{
    public enum Scenario
    {
        Activation,      // 激活组件
        Shooting,        // 射击
        ShieldBlock,     // 护盾抵挡
        AbilityUse,      // 释放能力
        DamageTaken,     // 受到伤害
        ReserveRelease   // 占用流失（被动解除时）
    }
}

// 持续性消耗场景（60 Tick批量计算）
public class ContinuousConsumption
{
    public enum Scenario
    {
        CombatBodyMaintain,   // 战斗体维持（固定值）
        ComponentSustain,     // 组件维持（如变色龙）
        WoundLeakage          // 伤口泄漏（伤势严重度相关）
    }
}
```

**性能约束**：
- ❌ 不可每Tick计算持续消耗
- ✅ 必须每60 Tick批量计算
- ✅ 所有持续消耗项累加求和

---

## 三、ECS架构设计

### 3.1 整体架构图

```
┌─────────────────────────────────────────────────┐
│  应用层（ProjectWT）                             │
│  - 具体Trigger组件（弧月、炸裂弹、护盾等）        │
│  - Worker实现（攻击、防御、能力等）               │
│  - UI、AI、渲染                                  │
└─────────────────────────────────────────────────┘
                     ↓ 使用
┌─────────────────────────────────────────────────┐
│  框架API层（Trion Framework Public API）         │
│  - TrionUtility                                 │
│  - TriggerUtility                               │
│  - CombatBodyUtility                            │
│  - BailOutUtility                               │
└─────────────────────────────────────────────────┘
                     ↓ 调用
┌─────────────────────────────────────────────────┐
│  System层（逻辑处理）                            │
│  - TrionSystem（能量计算）                       │
│  - TriggerSystem（装备管理）                     │
│  - CombatBodySystem（战斗体管理）                │
│  - BailOutSystem（紧急脱离）                     │
│  - ConsumptionSystem（60 Tick批量计算）          │
└─────────────────────────────────────────────────┘
                     ↓ 操作
┌─────────────────────────────────────────────────┐
│  Component层（数据容器）                         │
│  - CompTrionPool（Trion能量池）                  │
│  - CompTrigger（Trigger装备）                    │
│  - CompCombatBody（战斗体状态）                  │
│  - CompBailOut（紧急脱离）                       │
│  - CompConsumption（消耗管理）                   │
└─────────────────────────────────────────────────┘
                     ↓ 附加到
┌─────────────────────────────────────────────────┐
│  Entity层（RimWorld原生）                        │
│  - Pawn（殖民者）                                │
│  - TrionSoldier（Trion兵，类机械体）             │
│  - Building_TrionEquipment（Trion建筑）         │
└─────────────────────────────────────────────────┘
```

### 3.2 核心组件设计

#### CompTrionPool（Trion能量池组件）

```csharp
public class CompTrionPool : ThingComp
{
    // === 数据 ===
    public TrionPool pool;

    // === 配置 ===
    public CompProperties_TrionPool Props => (CompProperties_TrionPool)props;

    // === 初始化 ===
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            pool = new TrionPool();
            pool.Capacity = TrionSystem.CalculateInitialCapacity(parent, Props);
        }
    }

    // === 序列化 ===
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref pool, "trionPool");
    }
}

// 配置Def
public class CompProperties_TrionPool : CompProperties
{
    public TrionCapacitySource capacitySource;  // Talent | Fixed | Custom
    public float fixedCapacity = 1000f;         // 固定容量（如果使用Fixed）
    public TrionTalentDef talentDef;            // 天赋Def（如果使用Talent）

    public CompProperties_TrionPool()
    {
        compClass = typeof(CompTrionPool);
    }
}

// 容量来源
public enum TrionCapacitySource
{
    Talent,   // 基于Trion天赋
    Fixed,    // 固定值（Trion兵、建筑）
    Custom    // 自定义计算（接口扩展）
}
```

#### CompTrigger（Trigger装备组件）

```csharp
public class CompTrigger : ThingComp
{
    // === 数据 ===
    public List<EquippedTrigger> equippedTriggers;  // 已装备的Trigger
    public Dictionary<TriggerSlotDef, EquippedTrigger> activeBySlot;  // 各槽位激活的Trigger

    // === 配置 ===
    public CompProperties_Trigger Props => (CompProperties_Trigger)props;

    // === 装备操作 ===
    public bool TryEquip(TriggerComponentDef triggerDef, TriggerSlotDef slot)
    {
        return TriggerSystem.TryEquip(this, triggerDef, slot);
    }

    public bool TryActivate(TriggerComponentDef triggerDef)
    {
        return TriggerSystem.TryActivate(this, triggerDef);
    }

    public void Deactivate(TriggerComponentDef triggerDef)
    {
        TriggerSystem.Deactivate(this, triggerDef);
    }
}

// 已装备的Trigger
public class EquippedTrigger
{
    public TriggerComponentDef def;
    public TriggerSlotDef slot;
    public TriggerState state;
    public int activationDelay;  // 激活引导时间（1-5 Tick）
}
```

#### CompCombatBody（战斗体组件）

```csharp
public class CompCombatBody : ThingComp
{
    // === 数据 ===
    public CombatBody combatBody;
    public bool isActive = false;

    // === Combat Body操作 ===
    public bool TryEnterCombatBody()
    {
        return CombatBodySystem.TryEnter(this);
    }

    public void ExitCombatBody(bool forced = false)
    {
        CombatBodySystem.Exit(this, forced);
    }

    // === 虚拟伤害 ===
    public void ApplyVirtualDamage(DamageInfo dinfo)
    {
        CombatBodySystem.ApplyVirtualDamage(this, dinfo);
    }
}
```

#### CompBailOut（紧急脱离组件）

```csharp
public class CompBailOut : ThingComp
{
    // === 配置 ===
    public CompProperties_BailOut Props => (CompProperties_BailOut)props;

    // === Bail Out操作 ===
    public void TriggerBailOut(BailOutSystem.BailOutTrigger trigger)
    {
        BailOutSystem.Execute(parent, trigger, Props.anchorDef);
    }
}

// 配置Def
public class CompProperties_BailOut : CompProperties
{
    public float reserveCost = 400f;  // 占用值（原作参考）
    public ThingDef anchorDef;        // 传送锚ThingDef

    public CompProperties_BailOut()
    {
        compClass = typeof(CompBailOut);
    }
}
```

#### CompConsumption（消耗管理组件）

```csharp
public class CompConsumption : ThingComp
{
    // === 数据 ===
    public List<ContinuousConsumptionEntry> continuousConsumptions;
    private int tickCounter = 0;

    // === 定时计算（60 Tick） ===
    public override void CompTick()
    {
        base.CompTick();
        tickCounter++;
        if (tickCounter >= 60)
        {
            ConsumptionSystem.Calculate(this);
            tickCounter = 0;
        }
    }
}

// 持续消耗条目
public class ContinuousConsumptionEntry
{
    public object source;  // 消耗来源（Trigger组件、伤口等）
    public float costPerInterval;  // 每单位时间消耗
}
```

### 3.3 核心系统设计

#### TrionSystem（Trion管理系统）

```csharp
public static class TrionSystem
{
    // === 计算初始容量 ===
    public static float CalculateInitialCapacity(Thing thing, CompProperties_TrionPool props)
    {
        switch (props.capacitySource)
        {
            case TrionCapacitySource.Fixed:
                return props.fixedCapacity;

            case TrionCapacitySource.Talent:
                // 基于Trion天赋计算
                var pawn = thing as Pawn;
                if (pawn != null)
                {
                    var talent = GetTrionTalent(pawn);
                    return CalculateCapacityFromTalent(talent);
                }
                return props.fixedCapacity;

            case TrionCapacitySource.Custom:
                // 调用自定义接口
                return CustomCapacityCalculator.Calculate(thing);

            default:
                return 1000f;
        }
    }

    // === Trion恢复 ===
    public static void ApplyRecovery(CompTrionPool comp, int ticks)
    {
        var pool = comp.pool;

        // 基础恢复速率
        float recovery = GetBaseRecoveryRate(comp.parent) * (ticks / 60f);

        // 应用修正因素
        recovery *= GetRecoveryMultiplier(comp.parent);

        // 恢复已消耗量
        pool.Consumed = Mathf.Max(0, pool.Consumed - recovery);
    }

    // === 获取恢复速率修正 ===
    private static float GetRecoveryMultiplier(Thing thing)
    {
        float multiplier = 1f;

        var pawn = thing as Pawn;
        if (pawn != null)
        {
            // 饱食度检查（原作设定：饥饿时不恢复）
            var foodNeed = pawn.needs?.food;
            if (foodNeed != null && foodNeed.CurLevelPercentage < 0.3f)
            {
                return 0f;  // 饥饿时不恢复
            }

            // 其他修正因素（特性、建筑等）
            // ...
        }

        return multiplier;
    }
}
```

#### TriggerSystem（Trigger管理系统）

```csharp
public static class TriggerSystem
{
    // === 装备Trigger ===
    public static bool TryEquip(CompTrigger comp, TriggerComponentDef triggerDef, TriggerSlotDef slot)
    {
        // 检查槽位容量
        var slotTriggers = comp.equippedTriggers.Where(t => t.slot == slot).ToList();
        if (slotTriggers.Count >= slot.maxEquipped)
        {
            Messages.Message("槽位已满", MessageTypeDefOf.RejectInput);
            return false;
        }

        // 创建装备实例
        var equipped = new EquippedTrigger
        {
            def = triggerDef,
            slot = slot,
            state = TriggerState.Disconnected
        };

        comp.equippedTriggers.Add(equipped);
        return true;
    }

    // === 激活Trigger ===
    public static bool TryActivate(CompTrigger comp, TriggerComponentDef triggerDef)
    {
        var equipped = comp.equippedTriggers.Find(t => t.def == triggerDef);
        if (equipped == null || equipped.state == TriggerState.Disconnected)
        {
            return false;
        }

        // 检查Combat Body状态
        var combatBody = comp.parent.GetComp<CompCombatBody>();
        if (combatBody == null || !combatBody.isActive)
        {
            Messages.Message("需要先进入战斗体状态", MessageTypeDefOf.RejectInput);
            return false;
        }

        // 检查Trion
        var trionPool = comp.parent.GetComp<CompTrionPool>();
        if (trionPool == null || trionPool.pool.Available < triggerDef.activationCost)
        {
            Messages.Message("Trion不足", MessageTypeDefOf.RejectInput);
            return false;
        }

        // 检查同槽位激活数量
        var slotActive = comp.activeBySlot.ContainsKey(equipped.slot)
            ? comp.activeBySlot[equipped.slot]
            : null;
        if (slotActive != null && equipped.slot.maxActive <= 1)
        {
            // 同槽位切换：旧组件关闭 → 新组件引导 → 新组件激活
            Deactivate(comp, slotActive.def);
        }

        // 扣除激活费用
        trionPool.pool.Consumed += triggerDef.activationCost;

        // 切换状态（考虑引导时间）
        if (triggerDef.activationDelay > 0)
        {
            equipped.activationDelay = triggerDef.activationDelay;
            // 下一个Tick检查引导完成
        }
        else
        {
            equipped.state = TriggerState.Active;
            comp.activeBySlot[equipped.slot] = equipped;
        }

        // 注册持续消耗
        if (triggerDef.sustainCost > 0)
        {
            var consumption = comp.parent.GetComp<CompConsumption>();
            consumption.continuousConsumptions.Add(new ContinuousConsumptionEntry
            {
                source = triggerDef,
                costPerInterval = triggerDef.sustainCost
            });
        }

        return true;
    }

    // === 关闭Trigger ===
    public static void Deactivate(CompTrigger comp, TriggerComponentDef triggerDef)
    {
        var equipped = comp.equippedTriggers.Find(t => t.def == triggerDef);
        if (equipped == null || equipped.state != TriggerState.Active)
        {
            return;
        }

        // 瞬间关闭
        equipped.state = TriggerState.Dormant;
        comp.activeBySlot.Remove(equipped.slot);

        // 移除持续消耗
        var consumption = comp.parent.GetComp<CompConsumption>();
        consumption.continuousConsumptions.RemoveAll(c => c.source == triggerDef);
    }
}
```

#### CombatBodySystem（战斗体管理系统）

```csharp
public static class CombatBodySystem
{
    // === 进入战斗体 ===
    public static bool TryEnter(CompCombatBody comp)
    {
        var pawn = comp.parent as Pawn;
        if (pawn == null || comp.isActive)
        {
            return false;
        }

        // 1. 创建快照
        var snapshot = CreateSnapshot(pawn);

        // 2. 创建战斗体
        comp.combatBody = new CombatBody
        {
            snapshot = snapshot,
            wounds = new List<VirtualWound>(),
            isActive = true
        };

        // 3. 锁定Trigger占用量
        var trigger = pawn.GetComp<CompTrigger>();
        var trionPool = pawn.GetComp<CompTrionPool>();
        if (trigger != null && trionPool != null)
        {
            float totalReserve = CalculateTotalReserve(trigger);
            trionPool.pool.Reserved += totalReserve;
        }

        // 4. 切换Trigger状态：Disconnected → Dormant
        if (trigger != null)
        {
            foreach (var equipped in trigger.equippedTriggers)
            {
                if (equipped.state == TriggerState.Disconnected)
                {
                    equipped.state = TriggerState.Dormant;
                }
            }
        }

        // 5. 冻结生理活动
        FreezeBiologicalNeeds(pawn);

        comp.isActive = true;
        return true;
    }

    // === 退出战斗体 ===
    public static void Exit(CompCombatBody comp, bool forced)
    {
        var pawn = comp.parent as Pawn;
        if (pawn == null || !comp.isActive)
        {
            return;
        }

        // 1. 恢复快照
        RestoreSnapshot(pawn, comp.combatBody.snapshot);

        // 2. 释放占用量
        var trionPool = pawn.GetComp<CompTrionPool>();
        if (trionPool != null)
        {
            if (forced)
            {
                // 被动解除：占用量流失
                trionPool.pool.Consumed += trionPool.pool.Reserved;
                trionPool.pool.Reserved = 0;

                // 施加debuff
                ApplyDepletionDebuff(pawn);
            }
            else
            {
                // 主动解除：占用量返还
                trionPool.pool.Reserved = 0;
            }
        }

        // 3. 切换Trigger状态：Dormant/Active → Disconnected
        var trigger = pawn.GetComp<CompTrigger>();
        if (trigger != null)
        {
            foreach (var equipped in trigger.equippedTriggers)
            {
                equipped.state = TriggerState.Disconnected;
            }
            trigger.activeBySlot.Clear();
        }

        // 4. 恢复生理活动
        ResumeBiologicalNeeds(pawn);

        comp.combatBody = null;
        comp.isActive = false;
    }

    // === 应用虚拟伤害 ===
    public static void ApplyVirtualDamage(CompCombatBody comp, DamageInfo dinfo)
    {
        if (!comp.isActive)
        {
            return;
        }

        var trionPool = comp.parent.GetComp<CompTrionPool>();
        if (trionPool == null)
        {
            return;
        }

        // 伤害转化为Trion消耗（1:1）
        float damageAmount = dinfo.Amount;
        trionPool.pool.Consumed += damageAmount;

        // 注册伤口（增加泄漏速率）
        var wound = new VirtualWound
        {
            bodyPart = dinfo.HitPart,
            severity = damageAmount,
            leakageRate = CalculateLeakageRate(damageAmount)
        };
        comp.combatBody.wounds.Add(wound);

        // 更新总泄漏速率
        comp.combatBody.leakageRate = comp.combatBody.wounds.Sum(w => w.leakageRate);

        // 注册持续消耗
        var consumption = comp.parent.GetComp<CompConsumption>();
        consumption.continuousConsumptions.Add(new ContinuousConsumptionEntry
        {
            source = wound,
            costPerInterval = wound.leakageRate
        });

        // 检查核心部位
        if (IsCorePart(dinfo.HitPart) && damageAmount >= GetCoreThreshold())
        {
            // 核心被毁 → 触发Bail Out
            var bailOut = comp.parent.GetComp<CompBailOut>();
            if (bailOut != null)
            {
                bailOut.TriggerBailOut(BailOutSystem.BailOutTrigger.CoreDestroyed);
            }
            else
            {
                // 无Bail Out → 原地解除
                Exit(comp, forced: true);
            }
        }
    }
}
```

#### BailOutSystem（紧急脱离系统）

```csharp
public static class BailOutSystem
{
    // === 执行Bail Out ===
    public static void Execute(Thing thing, BailOutTrigger trigger, ThingDef anchorDef)
    {
        var pawn = thing as Pawn;
        if (pawn == null)
        {
            return;
        }

        // 1. 找到传送锚
        var anchor = FindNearestAnchor(pawn, anchorDef);
        if (anchor == null)
        {
            Log.Error($"Bail Out失败：找不到传送锚");
            return;
        }

        // 2. 传送
        pawn.Position = anchor.Position;
        pawn.Map = anchor.Map;

        // 3. 强制解除Combat Body
        var combatBody = pawn.GetComp<CompCombatBody>();
        if (combatBody != null && combatBody.isActive)
        {
            CombatBodySystem.Exit(combatBody, forced: true);
        }

        // 4. 效果提示
        EffecterDefOf.Skip_ExitNoDelay.Spawn(pawn.Position, pawn.Map);
        Messages.Message($"{pawn.LabelShort} 已紧急脱离", MessageTypeDefOf.NeutralEvent);
    }
}
```

#### ConsumptionSystem（消耗计算系统）

```csharp
public static class ConsumptionSystem
{
    // === 批量计算消耗（60 Tick） ===
    public static void Calculate(CompConsumption comp)
    {
        var trionPool = comp.parent.GetComp<CompTrionPool>();
        if (trionPool == null)
        {
            return;
        }

        // 累加所有持续消耗
        float totalConsumption = 0f;

        foreach (var entry in comp.continuousConsumptions)
        {
            totalConsumption += entry.costPerInterval;
        }

        // 应用消耗
        trionPool.pool.Consumed += totalConsumption;

        // 检查Trion耗尽
        if (trionPool.pool.Available <= 0)
        {
            OnTrionDepleted(comp.parent);
        }
    }

    // === Trion耗尽处理 ===
    private static void OnTrionDepleted(Thing thing)
    {
        // 触发Bail Out
        var bailOut = thing.GetComp<CompBailOut>();
        if (bailOut != null)
        {
            bailOut.TriggerBailOut(BailOutSystem.BailOutTrigger.TrionDepleted);
        }
        else
        {
            // 无Bail Out → 强制解除Combat Body
            var combatBody = thing.GetComp<CompCombatBody>();
            if (combatBody != null && combatBody.isActive)
            {
                CombatBodySystem.Exit(combatBody, forced: true);
            }
        }
    }
}
```

---

## 四、配置系统设计

### 4.1 核心Def定义

#### TriggerComponentDef（Trigger组件定义）

```xml
<!-- Example: 炸裂弹 Trigger -->
<TriggerComponentDef>
    <defName>Trigger_BurstBullet</defName>
    <label>炸裂弹</label>
    <description>远程武器，发射炸裂弹药。</description>

    <!-- 资源成本 -->
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>1</usageCost>

    <!-- 激活引导时间（Tick） -->
    <activationDelay>3</activationDelay>

    <!-- 槽位需求 -->
    <requiredSlot>TriggerSlot_RightHand</requiredSlot>

    <!-- 功能实现 -->
    <workerClass>ProjectWT.TriggerWorker_RangedWeapon</workerClass>
</TriggerComponentDef>
```

#### TriggerSlotDef（Trigger槽位定义）

```xml
<!-- Example: 左手主Trigger槽 -->
<TriggerSlotDef>
    <defName>TriggerSlot_LeftHand</defName>
    <label>主Trigger（左手）</label>

    <maxEquipped>4</maxEquipped>
    <maxActive>1</maxActive>

    <boundBodyPart>LeftHand</boundBodyPart>
</TriggerSlotDef>

<!-- Example: 副Trigger槽 -->
<TriggerSlotDef>
    <defName>TriggerSlot_Sub</defName>
    <label>副Trigger</label>

    <maxEquipped>4</maxEquipped>
    <maxActive>999</maxActive>  <!-- 不限制 -->
</TriggerSlotDef>
```

#### TrionTalentDef（Trion天赋定义）

```xml
<TrionTalentDef>
    <defName>TrionTalent</defName>
    <label>Trion天赋</label>

    <!-- 天赋等级对应容量 -->
    <capacityByLevel>
        <E>1000</E>
        <D>2000</D>
        <C>4000</C>
        <B>7000</B>
        <A>12000</A>
        <S>20000</S>
    </capacityByLevel>
</TrionTalentDef>
```

### 4.2 实体配置示例

#### 人类殖民者配置

```xml
<!-- ThingDef_AlienRace配置（使用HAR框架） -->
<ThingDef_AlienRace ParentName="BasePawn">
    <defName>Human_TrionUser</defName>

    <!-- 添加Trion Framework组件 -->
    <comps>
        <!-- Trion能量池 -->
        <li Class="TrionFramework.CompProperties_TrionPool">
            <capacitySource>Talent</capacitySource>
            <talentDef>TrionTalent</talentDef>
        </li>

        <!-- Trigger装备 -->
        <li Class="TrionFramework.CompProperties_Trigger">
            <slots>
                <li>TriggerSlot_LeftHand</li>
                <li>TriggerSlot_RightHand</li>
                <li>TriggerSlot_Sub</li>
            </slots>
        </li>

        <!-- 战斗体 -->
        <li Class="TrionFramework.CompProperties_CombatBody">
            <snapshotIncludes>
                <li>Hediffs</li>
                <li>Apparels</li>
                <li>Equipment</li>
                <li>Inventory</li>
            </snapshotIncludes>
        </li>

        <!-- Bail Out -->
        <li Class="TrionFramework.CompProperties_BailOut">
            <reserveCost>400</reserveCost>
            <anchorDef>Building_BailOutAnchor</anchorDef>
        </li>

        <!-- 消耗管理 -->
        <li Class="TrionFramework.CompProperties_Consumption">
            <calculationInterval>60</calculationInterval>
        </li>
    </comps>
</ThingDef_AlienRace>
```

---

## 五、扩展性设计

### 5.1 Worker接口（应用层实现）

框架提供抽象Worker接口，应用层实现具体功能：

```csharp
// Trigger Worker接口
public abstract class TriggerWorker
{
    public TriggerComponentDef def;

    // 激活时调用
    public virtual void OnActivated(Thing user) { }

    // 关闭时调用
    public virtual void OnDeactivated(Thing user) { }

    // 使用功能时调用（射击、释放能力等）
    public abstract void Use(Thing user, LocalTargetInfo target);

    // 每Tick调用（如果需要）
    public virtual void Tick(Thing user) { }
}

// Example: 远程武器Worker
public class TriggerWorker_RangedWeapon : TriggerWorker
{
    public override void Use(Thing user, LocalTargetInfo target)
    {
        // 检查Trion
        var trionPool = user.GetComp<CompTrionPool>();
        if (trionPool.pool.Available < def.usageCost)
        {
            return;
        }

        // 扣除使用费用
        trionPool.pool.Consumed += def.usageCost;

        // 发射子弹
        ShootBullet(user, target);
    }
}
```

### 5.2 支持的扩展场景

框架设计支持以下扩展：

1. **新Trigger组件**：定义TriggerComponentDef + 实现Worker
   - 攻击手Trigger（弧月、蝎子、光岭等）
   - 枪手/射手Trigger（各种弹药类型）
   - 狙击手Trigger（白鹭、闪电等）
   - 辅助Trigger（护盾、变色龙、蚱蜢等）

2. **新实体类型**：添加Comp到Thing
   - Trion兵（类机械体）
   - Trion建筑（炮台、护盾发生器等）

3. **新天赋类型**：扩展TrionTalentDef
   - 副作用（Side Effect）系统

4. **新消耗规则**：实现ConsumptionEntry
   - 环境消耗、特殊状态消耗等

---

## 六、与Trion系统设定文档的对应关系

### 6.1 概念映射表

| 设定文档概念 | 框架实现 | 说明 |
|------------|---------|------|
| Trion能量 | TrionPool | 四要素模型 |
| 战斗体 | CompCombatBody | 快照/虚拟伤害 |
| Trigger组件 | TriggerComponentDef | 模块化配置 |
| 触发器 | CompTrigger | 装备管理 |
| Bail Out | CompBailOut | 紧急脱离 |
| 占用值 | TrionPool.Reserved | 锁定容量 |
| 已消耗量 | TrionPool.Consumed | 累计消耗 |
| Trion天赋 | TrionTalentDef | 容量来源 |

### 6.2 实现覆盖度

框架已实现：
- ✅ Trion四要素管理
- ✅ Trigger模块化装备
- ✅ Combat Body生成/解除
- ✅ 虚拟伤害系统
- ✅ Bail Out系统
- ✅ 60 Tick批量消耗计算
- ✅ 快照/回滚机制

留给应用层实现：
- ⏳ 具体Trigger组件（弧月、炸裂弹等）
- ⏳ 输出功率系统
- ⏳ 副作用（Side Effect）系统
- ⏳ 黑触发（Black Trigger）系统
- ⏳ UI界面（Gizmo、对话框等）
- ⏳ 渲染和外观

---

## 七、性能考量

### 7.1 性能约束

| 约束项 | 规则 | 原因 |
|--------|------|------|
| 持续消耗计算 | 必须60 Tick批量计算 | 避免每Tick性能开销 |
| Trion池查询 | 缓存Available值 | 减少重复计算 |
| Trigger状态切换 | 最小化状态机复杂度 | 提高响应速度 |
| 快照创建 | 只保存必要数据 | 减少内存占用 |

### 7.2 性能优化策略

```csharp
// 优化1：缓存可用量计算
public class TrionPool
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
// 已在ConsumptionSystem中实现
```

---

## 八、待决策问题

### 8.1 设计问题

1. **Trigger激活引导时间的处理方式？**
   - 当前设计：添加`activationDelay`参数（1-5 Tick）
   - 实现方式：Tick中检查引导完成
   - 建议：保留此设计

2. **虚拟伤害的精确转化比例？**
   - 当前设计：1:1转化（1点伤害 = 1点Trion消耗）
   - 是否需要调整：待平衡性测试
   - 建议：先1:1，后续通过配置调整

3. **Combat Body是否支持部分禁用某些Hediff？**
   - 当前设计：全部快照/回滚
   - 特殊需求：某些Hediff可能需要在Combat Body中保留
   - 建议：添加过滤规则配置

4. **是否需要输出功率系统？**
   - 原作设定：有输出功率影响组件表现
   - 当前设计：未包含
   - 建议：作为扩展模块，框架提供接口

### 8.2 技术选型

1. **是否使用Harmony补丁拦截伤害？**
   - 当前设计：通过CompCombatBody.ApplyVirtualDamage处理
   - 必要场景：可能需要Harmony拦截原版伤害逻辑
   - 建议：仅在必要时使用，优先用Comp

2. **是否依赖HAR框架？**
   - 当前设计：不强制依赖
   - 兼容方案：提供HAR集成示例
   - 建议：框架独立，提供HAR适配指南

---

## 九、下一步工作

### 9.1 设计阶段

- [ ] 用户审阅设计方案
- [ ] 确认框架职责边界是否清晰
- [ ] 确认ECS架构是否合理
- [ ] 确认是否保留了原作特色
- [ ] 决策待定问题

### 9.2 交付阶段（用户审阅通过后）

生成6份设计文档：
1. Trion Framework核心概念设计
2. 功能详细设计说明书
3. 数据结构设计规范
4. 系统交互流程设计
5. 配置参数定义
6. 技术方案选择与考量

附加：RiMCP验证清单

---

## 十、总结

### 10.1 框架核心价值

1. **忠实原作**：保留Trion、Trigger、Combat Body等核心概念
2. **清晰职责**：框架提供底层机制，应用层实现具体功能
3. **扩展友好**：易于添加新Trigger组件和功能
4. **性能优化**：60 Tick批量计算，避免性能问题

### 10.2 与Trion系统的关系

- **框架提供**：Trion能量管理、Trigger装备、Combat Body、Bail Out的底层机制
- **ProjectWT提供**：具体Trigger组件、Worker实现、UI、AI、渲染

### 10.3 框架命名

**Trion Framework**
- 明确体现境界触发者题材
- 核心概念一目了然
- 为ProjectWT提供专属基础

---

**需求架构师**
*2026-01-10*

---

## 参考资料

- [境界触发者 - 维基百科](https://zh.wikipedia.org/zh-hans/境界觸發者)
- [境界触发者资料站 - 灰机wiki](https://worldtrigger.huijiwiki.com/wiki/首页)
- Trion系统设定文档_v3.0_结构化版.md
- Trion系统分析对话记录合集.md
- Trion系统分析重点总结.md
