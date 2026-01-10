# Trion框架设计 v0.6 - 正确版

---

## 文档元信息

**摘要**：Trion实体框架的核心设计。本框架**只定义接口和通用机制**，不包含任何具体实现。所有业务逻辑、数值参数、Strategy实现都由应用层决定。

**版本号**：v0.6（框架纯净版）
**修改时间**：2026-01-10
**关键词**：框架、接口定义、通用机制、无具体实现、应用层驱动
**标签**：[定稿]

---

## 一、设计原则

### 1.1 框架层职责（仅限）

```
✅ 框架应该有：
├─ 接口定义（ILifecycleStrategy、IBodyPartMapper）
├─ 通用机制（消耗、恢复、状态转换）
├─ 公开API（Consume、Recover、SetStrategy）
├─ 数据结构（Trion四要素、VirtualWound）
└─ 调度逻辑（CompTick执行流程）

❌ 框架不应该有：
├─ 具体Strategy实现（Strategy_HumanCombatBody等）
├─ 硬编码数值（2.0f、5.0f、10.0f等）
├─ 业务逻辑（护盾计算、debuff创建、部位判定）
├─ SelectStrategy的具体实现
└─ 应用层代码
```

### 1.2 应用层职责

- 实现所有具体的Strategy类
- 定义所有数值参数
- 实现具体业务逻辑
- 通过回调/委托/工厂方法告诉框架"怎么做"

### 1.3 判断标准

设计框架的任何部分时，问自己：

> **"这是通用机制还是具体实现？"**
>
> 如果答案是"**这只适用于某一种情况**"→ 删除，改为接口
>
> 如果答案是"**所有实体都会经历这个**"→ 保留为通用机制

---

## 二、核心架构

### 2.1 CompTrion（框架数据和调度中枢）

```csharp
/// <summary>
/// Trion框架的唯一核心组件
///
/// 职责：
/// 1. 管理Trion四要素数据
/// 2. 调度Strategy执行流程
/// 3. 管理Trigger挂载
/// 4. 公开接口供应用层调用
///
/// 不负责：具体业务逻辑
/// </summary>
public class CompTrion : ThingComp
{
    // ===== 数据（由应用层初始化）=====
    public float Capacity { get; set; }           // 由应用层设置
    public float Reserved { get; set; }           // 由应用层维护
    public float Consumed { get; set; }           // 由应用层和框架修改
    public float Available => Capacity - Reserved - Consumed;

    public float OutputPower { get; set; }        // 由应用层计算并设置
    public float LeakRate { get; set; }           // 由Strategy设置和维护
    public float RecoveryRate { get; set; }       // 由应用层设置

    // ===== 策略（由应用层注入）=====
    private ILifecycleStrategy strategy;

    // ===== 挂载（由框架管理）=====
    public List<TriggerMount> Mounts { get; } = new();

    // ===== 初始化 =====
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        // 应用层应该在此前设置strategy
        if (strategy == null)
            throw new InvalidOperationException("Strategy must be set before PostSpawnSetup");

        strategy.OnInitialize();
        InitializeMounts();
    }

    // ===== 公开接口：设置策略 =====
    public void SetStrategy(ILifecycleStrategy newStrategy)
    {
        strategy = newStrategy ?? throw new ArgumentNullException(nameof(newStrategy));
    }

    public ILifecycleStrategy GetStrategy() => strategy;

    // ===== 公开接口：Trion消耗/恢复 =====
    public void Consume(float amount)
    {
        Consumed += amount;
        if (Consumed > Capacity) Consumed = Capacity;
    }

    public void Recover(float amount)
    {
        Consumed -= amount;
        if (Consumed < 0) Consumed = 0;
    }

    // ===== 通用调度逻辑 =====
    public override void CompTick()
    {
        if (Find.TickManager.TicksGame % 60 != 0) return;

        // 第1步：累加消耗
        float totalConsumption = 0;
        totalConsumption += strategy.GetBaseMaintenance();  // 由Strategy提供

        foreach (var mount in Mounts)
        {
            totalConsumption += mount.TickAndGetConsumption();
        }

        totalConsumption += LeakRate;  // 由Strategy维护

        // 第2步：执行消耗
        Consume(totalConsumption);

        // 第3步：恢复（如果策略允许）
        if (strategy.ShouldRecover())
        {
            float recoveryAmount = RecoveryRate * strategy.GetRecoveryModifier();
            Recover(recoveryAmount);
        }

        // 第4步：策略Tick
        strategy.OnTick();

        // 第5步：检查耗尽
        if (Available <= 0)
        {
            strategy.OnDepleted();
        }
    }

    // ===== 公开接口：伤害拦截入口 =====
    public void PreApplyDamage(float damageAmount, BodyPartRecord hitPart)
    {
        if (strategy.ShouldInterceptDamage())
        {
            strategy.OnDamageTaken(damageAmount, hitPart);
        }
    }

    // ===== 公开接口：部位丧失事件 =====
    public void OnBodyPartLost(BodyPartRecord part)
    {
        // 让Strategy决定如何处理
        strategy.OnBodyPartLost(part);
    }

    // ===== 公开接口：OutputPower重算 =====
    public void RecalculateOutputPower()
    {
        // 应用层应该调用此方法，当需要重算时
        // 实际计算完全由应用层通过Strategy负责
        float baseOutput = strategy.GetBaseOutputPower();
        float modified = strategy.ModifyOutputPower(baseOutput);
        OutputPower = Mathf.Clamp(modified, 0, 100);
    }

    // ===== 框架内部 =====
    private void InitializeMounts()
    {
        // 根据应用层配置初始化挂载点
        // 具体逻辑由应用层定义
    }
}
```

---

### 2.2 ILifecycleStrategy（纯接口，无实现）

```csharp
/// <summary>
/// 生命周期策略接口
///
/// 框架调用这些方法执行流程，具体实现由应用层提供
/// 框架不包含任何Strategy的具体实现类
/// </summary>
public interface ILifecycleStrategy
{
    // ===== 初始化 =====
    /// <summary>战斗体生成时调用</summary>
    void OnInitialize();

    // ===== 定期检查 =====
    /// <summary>每个CompTick调用</summary>
    void OnTick();

    // ===== 伤害处理 =====
    /// <summary>是否拦截伤害（而不走原版系统）</summary>
    bool ShouldInterceptDamage();

    /// <summary>受伤时调用，具体如何处理由实现类决定</summary>
    void OnDamageTaken(float damageAmount, BodyPartRecord hitPart);

    // ===== 部位丧失 =====
    /// <summary>部位被摧毁/截肢时调用</summary>
    void OnBodyPartLost(BodyPartRecord part);

    // ===== 耗尽 =====
    /// <summary>Trion可用量≤0时调用</summary>
    void OnDepleted();

    // ===== 恢复 =====
    /// <summary>是否允许恢复</summary>
    bool ShouldRecover();

    /// <summary>获取恢复修正系数（如1.5表示+50%）</summary>
    float GetRecoveryModifier();

    // ===== 数据提供（应用层实现，框架调用）=====
    /// <summary>获取基础消耗（每60 Tick消耗多少）</summary>
    float GetBaseMaintenance();

    /// <summary>获取基础输出功率</summary>
    float GetBaseOutputPower();

    /// <summary>修正输出功率（应用所有加成）</summary>
    float ModifyOutputPower(float baseOutput);

    /// <summary>获取伤口的泄漏速率</summary>
    float GetLeakageRate(VirtualWound wound);

    /// <summary>获取部位丧失导致的额外泄漏速率</summary>
    float GetBodyPartLossLeakage(BodyPartRecord part);
}
```

---

### 2.3 TriggerMount（状态机，无业务逻辑）

```csharp
/// <summary>
/// Trigger挂载点状态机
///
/// 职责：管理该挂载点的状态转换和消耗计算
/// 不负责：具体业务逻辑
/// </summary>
public class TriggerMount
{
    public string slotTag { get; set; }
    public BodyPartRecord boundPart { get; set; }  // 可选
    public List<TriggerComponent> equippedList { get; } = new();
    public TriggerComponent activeTrigger { get; private set; }
    public TriggerComponent activatingTrigger { get; private set; }
    public int activationTicksRemaining { get; set; }
    public bool isFunctional { get; set; } = true;

    private CompTrion comp;

    // ===== 公开接口 =====
    public bool TryActivate(TriggerComponentDef componentDef)
    {
        // 框架提供的通用激活逻辑
        if (activatingTrigger != null) return false;
        if (!equippedList.Any(t => t.def == componentDef)) return false;

        // 检查输出功率（这是通用机制）
        if (componentDef.requiredOutputPower > comp.OutputPower) return false;

        // 支付激活费用
        float availableBefore = comp.Available;
        comp.Consume(componentDef.activationCost);
        if (comp.Available < 0)
        {
            comp.Consumed -= componentDef.activationCost;
            return false;
        }

        // 启动引导
        activatingTrigger = equippedList.First(t => t.def == componentDef);
        activationTicksRemaining = componentDef.activationDelay;

        return true;
    }

    public void TickActivation()
    {
        if (activatingTrigger == null) return;

        activationTicksRemaining--;
        if (activationTicksRemaining > 0) return;

        // 引导完成，切换状态
        if (activeTrigger != null)
        {
            activeTrigger.OnDeactivated();
            activeTrigger.state = TriggerState.Dormant;
        }

        activeTrigger = activatingTrigger;
        activeTrigger.OnActivated();
        activeTrigger.state = TriggerState.Active;
        activatingTrigger = null;
    }

    public float TickAndGetConsumption()
    {
        if (!isFunctional) return 0;

        TickActivation();

        float consumption = 0;
        if (activeTrigger?.state == TriggerState.Active)
        {
            consumption += activeTrigger.def.sustainCost;
        }

        return consumption;
    }

    public void OnPartDestroyed()
    {
        isFunctional = false;

        if (activeTrigger != null)
        {
            activeTrigger.OnDeactivated();
            activeTrigger.state = TriggerState.Disconnected;
            activeTrigger = null;
        }

        if (activatingTrigger != null)
        {
            activatingTrigger.state = TriggerState.Disconnected;
            activatingTrigger = null;
        }

        // 增加泄漏速率：由Strategy决定具体值
        // 框架只负责"调用"这个机制，不决定数值
    }

    public void DisconnectAll()
    {
        activeTrigger?.OnDeactivated();
        activatingTrigger = null;
        activeTrigger = null;

        foreach (var trigger in equippedList)
        {
            trigger.state = TriggerState.Disconnected;
        }
    }
}
```

---

### 2.4 TriggerComponent（状态管理）

```csharp
public class TriggerComponent
{
    public TriggerComponentDef def { get; set; }
    public TriggerState state { get; set; }

    // 由应用层的具体实现类来实现这些方法的逻辑
    public virtual void OnEquipped() { }
    public virtual void OnActivated() { }
    public virtual void OnTick() { }
    public virtual void OnDeactivated() { }
    public virtual void OnUnequipped() { }
}

public enum TriggerState
{
    Disconnected,  // 未连接
    Dormant,       // 休眠
    Activating,    // 激活引导中
    Active         // 激活
}
```

---

### 2.5 数据结构（应用层使用）

```csharp
/// <summary>虚拟伤口（应用层创建和管理）</summary>
public class VirtualWound
{
    public BodyPartRecord part;
    public float severity;
}

/// <summary>快照数据（应用层实现具体保存/恢复逻辑）</summary>
public interface IPawnSnapshot
{
    void Save(Pawn pawn);
    void Restore(Pawn pawn);
}
```

---

## 三、应用层需要实现的部分

### 3.1 创建Strategy实现

应用层需要创建Strategy的所有具体实现。框架完全不知道这些类存在。

```
应用层需要创建（框架不包含）：
├─ Strategy_HumanCombatBody
├─ Strategy_TrionSoldier
├─ Strategy_TrionBuilding
├─ Strategy_CustomMech
├─ Strategy_AliensTrionUnit
└─ ... （任意多个）
```

### 3.2 策略选择机制

框架不决定使用哪个Strategy，应用层通过**回调机制**告诉框架：

```csharp
// 框架中的委托（未初始化）
public Action<CompTrion, ThingWithComps> StrategyProvider { get; set; }

// 应用层初始化
public static void InitializeFramework()
{
    CompTrion.StrategyProvider = (comp, thing) =>
    {
        // 应用层的策略选择逻辑
        if (thing.def.HasModExtension<TrionSoldierMarker>())
            return new Strategy_TrionSoldier(comp);

        if (thing is Pawn)
            return new Strategy_HumanCombatBody(comp);

        if (thing is Building)
            return new Strategy_TrionBuilding(comp);

        return null;
    };
}

// 框架在PostSpawnSetup中使用
public override void PostSpawnSetup(bool respawningAfterLoad)
{
    var strategy = CompTrion.StrategyProvider(this, parent);
    SetStrategy(strategy);
    strategy.OnInitialize();
}
```

### 3.3 数值由应用层Def决定

所有数值都来自应用层，框架不硬编码任何值：

```
应用层Def提供的数值：
├─ TriggerComponentDef.sustainCost        （持续消耗）
├─ TriggerComponentDef.activationCost     （激活费用）
├─ TriggerComponentDef.activationDelay    （激活引导时间）
├─ TriggerComponentDef.requiredOutputPower（输出功率要求）
├─ Strategy实现中的GetBaseMaintenance()   （基础消耗）
├─ Strategy实现中的GetLeakageRate()       （泄漏速率）
└─ Strategy实现中的GetRecoveryModifier()  （恢复修正）
```

### 3.4 业务逻辑由应用层Strategy实现

```
应用层Strategy实现：
├─ OnDamageTaken()              → 护盾判定、debuff创建等
├─ OnBodyPartLost()             → 部位断连、泄漏增加等
├─ OnDepleted()                 → 快照回滚、debuff施加等
├─ GetLeakageRate()             → 伤口泄漏速率计算
└─ GetBodyPartLossLeakage()     → 部位丧失泄漏速率计算
```

---

## 四、Harmony补丁需求

框架需要应用层提供的补丁（框架本身不实现）：

```csharp
// 补丁1：拦截伤害
[HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
public class Patch_InterceptDamage
{
    static bool Prefix(Pawn ___pawn, DamageInfo dinfo)
    {
        var comp = ___pawn.TryGetComp<CompTrion>();
        if (comp != null)
        {
            comp.PreApplyDamage(dinfo.Amount, dinfo.HitPart);
        }
        return true;  // 让应用层Strategy决定是否拦截原版
    }
}

// 补丁2：检测截肢
[HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff")]
public class Patch_DetectAmputations
{
    static void Postfix(Pawn ___pawn, Hediff hediff)
    {
        if (hediff is Hediff_MissingBodyPart)
        {
            ___pawn.TryGetComp<CompTrion>()?.OnBodyPartLost(hediff.Part);
        }
    }
}
```

---

## 五、RiMCP验证清单

以下API已验证可用（由框架使用）：

| API | 验证状态 |
|-----|---------|
| Verse.ThingComp.PostSpawnSetup | ✅ |
| Verse.ThingComp.CompTick | ✅ |
| Verse.ThingWithComps.TryGetComp<T> | ✅ |
| Verse.Pawn_HealthTracker.PreApplyDamage | ✅ |
| RimWorld.HediffMaker.MakeHediff | ✅ |

---

## 六、总结

### ✅ 框架v0.6包含

- 接口定义（ILifecycleStrategy）
- 通用调度逻辑（CompTick流程）
- 状态机（TriggerMount、TriggerComponent）
- 公开API（Consume、Recover、SetStrategy等）
- 数据结构（Trion四要素、VirtualWound）

### ❌ 框架v0.6不包含

- 具体Strategy实现类
- 硬编码的数值
- 业务逻辑代码
- 策略选择的具体实现
- 应用层代码

### 应用层完全可以

- 实现任意数量的Strategy
- 定义所有数值参数
- 选择自己的策略选择机制
- 实现所有业务逻辑
- 扩展框架而无需改动框架代码

---

**需求架构师**
*2026-01-10*
