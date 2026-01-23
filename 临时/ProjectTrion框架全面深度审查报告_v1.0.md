# ProjectTrion Framework 全面深度审查报告

**文档元信息**

- 摘要：基于"框架层 = 定义规则、提供接口、约束验证、扩展点"的标准，对ProjectTrion框架进行系统化全面审查，找出所有缺失的扩展点和设计缺陷
- 版本号：1.0
- 修改时间：2026-01-16
- 关键词：框架扩展性、虚函数缺口、引导机制、状态转换、组件管理
- 标签：[待审]
- 审查方法：逐文件、逐方法、逐场景分析

---

## 第一部分：框架层的四项职责对标

### 框架标准
```
框架层 = 定义规则 + 提供接口 + 约束验证 + 扩展点
```

---

## 第二部分：逐模块审查结果

### 审查对象1：CompTrion.cs - CompTick() 和消耗计算

**位置**：L282-327

**现状代码**：
```csharp
public override void CompTick()
{
    base.CompTick();
    if (!_isInCombat || _strategy == null)
        return;

    if (this.parent.IsHashIntervalTick(60))
    {
        TickConsumption();
    }
}

private void TickConsumption()
{
    float baseMaintenance = _strategy.GetBaseMaintenance();

    float mountConsumption = 0f;
    foreach (var mount in _mounts.Where(m => m.IsActive))
    {
        mountConsumption += mount.GetConsumptionRate();
    }

    float leak = GetLeakRate();
    float totalConsumption = baseMaintenance + mountConsumption + leak;
    Consume(totalConsumption);

    if (Available <= 0)
    {
        TriggerBailOut();
    }

    _strategy.OnTick(this);
}
```

**设定需求对标**（Trion系统设定文档 L269-299）：
- 持续消耗计算必须每60Tick执行一次 ✅ 已实现
- 持续消耗包括：战斗体维持 + 组件功效维持 + 伤口泄漏 ✅ 已实现
- **关键：引导期间的组件也应该产生消耗**（如变色龙激活引导期间仍消耗）❌ **未实现**

**设定中的具体场景**（Trion战斗系统流程 L21-22）：
```
激活变色龙：-5/一次性，+1/每单位消耗
耗时1单位：-1
备注：激活完成前，变色龙未解除，依旧计算消耗。
```

**问题分析**：
1. TickConsumption() 中的 `mount.IsActive` 过滤掉了所有处于引导态的组件
2. TriggerMount.cs L114-120 有 Tick() 方法处理导引倒数，但 CompTrion.CompTick() **没有调用它**
3. 无法区分"纯激活态"和"引导态"，导致引导期间无消耗

**缺失的扩展点**：

❌ **缺陷1**：无虚函数让应用层自定义"哪些组件参与消耗计算"

应该添加：
```csharp
protected virtual bool ShouldIncludeInConsumption(TriggerMount mount)
{
    return mount.IsActive;  // 框架默认：只计入激活状态
    // 应用层可重写为：return mount.IsActive || IsGuiding(mount);
}

// TickConsumption 中改为：
foreach (var mount in _mounts)
{
    if (ShouldIncludeInConsumption(mount))
    {
        mountConsumption += mount.GetConsumptionRate();
    }
}
```

❌ **缺陷2**：CompTick() 没有调用 TriggerMount.Tick()

应该添加：
```csharp
public override void CompTick()
{
    base.CompTick();
    if (!_isInCombat || _strategy == null)
        return;

    // ← 新增：更新每个组件的状态（引导倒计时）
    foreach (var mount in _mounts)
    {
        mount.Tick();
    }

    if (this.parent.IsHashIntervalTick(60))
    {
        TickConsumption();
    }
}
```

❌ **缺陷3**：无虚函数钩子让应用层在引导完成时处理（如同槽位组件切换）

应该添加：
```csharp
protected virtual void OnMountGuidanceCompleted(TriggerMount mount)
{
    // 框架默认：什么都不做
    // 应用层可重写：处理同槽位切换、触发效果等
}

// 在 CompTick 中，更新后检查引导完成：
foreach (var mount in _mounts)
{
    mount.Tick();

    // 检查引导是否刚完成
    if (mount.IsActive && mount.activationTicks == 0 && mount.wasGuiding)
    {
        mount.wasGuiding = false;
        OnMountGuidanceCompleted(mount);  // ← 虚函数钩子
    }
}
```

---

### 审查对象2：CompTrion.cs - ActivateComponent()

**位置**：L471-505

**现状代码**：
```csharp
public bool ActivateComponent(TriggerMount mount)
{
    if (mount == null || mount.IsActive)
        return false;

    float activationCost = mount.GetActivationCost();
    if (Available < activationCost)
        return false;

    Consume(activationCost);
    mount.IsActive = true;
    mount.activationTicks = mount.def?.activationGuidanceTicks ?? 0;

    Log.Message(...);
    return true;
}
```

**问题分析**：

❌ **缺陷4**：无虚函数让应用层在激活前介入（检查槽位冲突、验证条件等）

**设定需求**（Trion系统设定文档 L104-105）：
```
同槽位内切换激活：新组件进入引导 → 引导完成后 → 旧组件瞬间关闭 → 新组件激活
```

框架无法自动实现这个流程（因为不知道槽位概念），应该提供虚函数让应用层处理：

```csharp
protected virtual bool CanActivateComponent(TriggerMount mount)
{
    // 框架默认：总是可以激活（只要Available足够）
    return true;
    // 应用层可重写：检查输出功率、槽位冲突等
}

protected virtual void OnBeforeMountActivation(TriggerMount mount)
{
    // 框架默认：什么都不做
    // 应用层可重写：关闭同槽位的其他组件
}

public bool ActivateComponent(TriggerMount mount)
{
    // ... 现有验证 ...

    if (!CanActivateComponent(mount))  // ← 新增虚函数检查
        return false;

    OnBeforeMountActivation(mount);  // ← 新增钩子

    Consume(activationCost);
    mount.IsActive = true;
    mount.activationTicks = mount.def?.activationGuidanceTicks ?? 0;

    return true;
}
```

❌ **缺陷5**：无虚函数让应用层在激活完成时处理回调

应该添加：
```csharp
protected virtual void OnMountActivationStarted(TriggerMount mount)
{
    // 框架默认：什么都不做
    // 应用层可重写：播放激活效果、修改UI等
}

public bool ActivateComponent(TriggerMount mount)
{
    // ... 所有验证通过 ...

    Consume(activationCost);
    mount.IsActive = true;
    mount.activationTicks = mount.def?.activationGuidanceTicks ?? 0;

    OnMountActivationStarted(mount);  // ← 新增回调

    return true;
}
```

---

### 审查对象3：CompTrion.cs - DeactivateComponent()

**位置**：L511-531

**现状代码**：
```csharp
public bool DeactivateComponent(TriggerMount mount)
{
    if (mount == null || !mount.IsActive)
        return false;

    mount.IsActive = false;
    mount.activationTicks = 0;

    Log.Message($"CompTrion: {parent?.Label} 组件{mount.def.label}已停用");
    return true;
}
```

**问题分析**：

❌ **缺陷6**：无虚函数让应用层在停用后处理（如清除效果、恢复隐身等）

```csharp
protected virtual void OnMountDeactivated(TriggerMount mount)
{
    // 框架默认：什么都不做
    // 应用层可重写：清除组件效果、恢复状态等
}

public bool DeactivateComponent(TriggerMount mount)
{
    if (mount == null || !mount.IsActive)
        return false;

    mount.IsActive = false;
    mount.activationTicks = 0;

    OnMountDeactivated(mount);  // ← 新增回调

    Log.Message(...);
    return true;
}
```

---

### 审查对象4：CompTrion.cs - NotifyVitalPartDestroyed()

**位置**：L718-738

**现状代码**：
```csharp
public void NotifyVitalPartDestroyed(BodyPartRecord part)
{
    if (!_isInCombat || _strategy == null)
        return;

    Log.Message($"CompTrion: {parent?.Label}的关键部位被摧毁：{part.Label}");

    _strategy.OnVitalPartDestroyed(this, part);

    TriggerBailOut();
}
```

**问题分析**：

**设定需求**（Trion系统设定文档 L164-170）：
```
| 部位类型 | 后果                                       |
| 手臂     | 该手挂载点所有组件断连，增加泄漏速率       |
| 单腿     | 移速大幅下降，增加泄漏速率                 |
| 双腿     | 失去移动能力，增加泄漏速率                 |
```

❌ **缺陷7**：框架没有为应用层提供处理部位毁坏的机会（断连组件、增加泄漏等）

```csharp
protected virtual void OnVitalPartDestroyed_Framework(TriggerMount mount, BodyPartRecord part)
{
    // 框架默认：断连该部位的所有组件
    if (mount.belongsToBodyPart == part)
    {
        DeactivateComponent(mount);
        mount.state = MountState.Disconnected;
    }
}

public void NotifyVitalPartDestroyed(BodyPartRecord part)
{
    if (!_isInCombat || _strategy == null)
        return;

    // 框架处理：断连相关组件
    foreach (var mount in _mounts.ToList())
    {
        OnVitalPartDestroyed_Framework(mount, part);
    }

    // 策略层处理：增加泄漏等业务逻辑
    _strategy.OnVitalPartDestroyed(this, part);

    TriggerBailOut();
}
```

---

### 审查对象5：CompTrion.cs - DestroyCombatBody()

**位置**：L718-738

**现状代码**：
```csharp
public void DestroyCombatBody(DestroyReason reason)
{
    if (!_isInCombat)
        return;

    var pawn = this.parent as Pawn;
    if (pawn == null)
        return;

    if (_snapshot != null && _props.enableSnapshot)
    {
        _snapshot.RestoreToPawn(pawn);
    }

    if (reason == DestroyReason.TrionDepleted || reason == DestroyReason.BailOutSuccess)
    {
        _consumed += _reserved;
        _reserved = 0;
    }
    else
    {
        _reserved = 0;
    }

    _isInCombat = false;
    _leakCacheTickExpire = 0;

    _strategy?.OnCombatBodyDestroyed(this, reason);

    Log.Message(...);
}
```

**问题分析**：

**设定需求**（Trion系统设定文档 L205-207）：
```
流程：
1. 战斗体瞬间崩溃
2. 组件注销
3. 占用量全部流失（永久消耗）
4. 快照数据恢复到肉身
5. 生理活动恢复
6. 施加 debuff："Trion 枯竭"
```

❌ **缺陷8**：框架无法让应用层在被动破裂时处理debuff等结果

现在 OnCombatBodyDestroyed 只能接收 CompTrion 和 reason，无法访问 Pawn 施加 Hediff。

```csharp
protected virtual void OnCombatBodyBroken(Pawn pawn, DestroyReason reason)
{
    // 框架默认：什么都不做
    // 应用层可重写：施加"Trion枯竭"debuff等
}

public void DestroyCombatBody(DestroyReason reason)
{
    // ... 现有恢复逻辑 ...

    var pawn = this.parent as Pawn;

    // ... 现有处理 ...

    _strategy?.OnCombatBodyDestroyed(this, reason);

    // ← 新增：仅在被动破裂时调用
    if ((reason == DestroyReason.TrionDepleted ||
         reason == DestroyReason.BailOutSuccess ||
         reason == DestroyReason.VitalPartDestroyed ||
         reason == DestroyReason.BailOutFailed) && pawn != null)
    {
        OnCombatBodyBroken(pawn, reason);
    }
}
```

---

### 审查对象6：ILifecycleStrategy.cs - 接口完整性

**现状**：12个方法和1个属性

**问题分析**：

❌ **缺陷9**：ILifecycleStrategy 缺少组件相关的回调

设定中提到了很多组件相关的场景（激活、停用、切换、引导等），但 ILifecycleStrategy 没有对应的回调。

应该添加新的虚函数到CompTrion（而不是ILifecycleStrategy），因为这些涉及具体的状态管理逻辑。

---

### 审查对象7：TriggerMount.cs - 状态表示

**现状代码**：
```csharp
public int activationTicks;
private bool _isActive = false;
```

**问题分析**：

❌ **缺陷10**：无法区分"引导中"和"已激活"状态

现在只有一个 `IsActive` boolean，当 `activationTicks > 0` 且 `IsActive = true` 时是引导态，但这个状态的含义不明确。

应该添加：
```csharp
// TriggerMount.cs 中添加

/// <summary>
/// 判断是否处于引导状态。
/// </summary>
public bool IsGuiding => IsActive && activationTicks > 0;

/// <summary>
/// 判断引导是否刚完成。
/// </summary>
public bool GuidanceJustCompleted { get; set; }
```

或者更完整的状态枚举：
```csharp
public enum MountState
{
    Disconnected = 0,  // 未连接（战斗体未生成或部位被毁）
    Dormant = 1,       // 休眠（战斗体激活但组件未激活）
    Guiding = 2,       // 引导中（激活过程中，activationTicks > 0）
    Active = 3,        // 已激活（activationTicks <= 0）
}

// 替换 IsActive 为 State 属性
public MountState state { get; set; } = MountState.Disconnected;
```

---

## 第三部分：缺失的扩展点汇总

### 关键缺失的虚函数

| ID | 虚函数名 | 所在类 | 用途 | 优先级 |
|----|---------|-------|------|--------|
| 1 | ShouldIncludeInConsumption() | CompTrion | 决定哪些组件参与消耗计算 | 🔴 高 |
| 2 | OnMountGuidanceCompleted() | CompTrion | 引导完成时的回调 | 🔴 高 |
| 3 | CanActivateComponent() | CompTrion | 激活前的条件检查 | 🟡 中 |
| 4 | OnBeforeMountActivation() | CompTrion | 激活前的干预点 | 🟡 中 |
| 5 | OnMountActivationStarted() | CompTrion | 激活开始时的回调 | 🟡 中 |
| 6 | OnMountDeactivated() | CompTrion | 停用后的回调 | 🟡 中 |
| 7 | OnVitalPartDestroyed_Framework() | CompTrion | 部位毁坏时的框架处理 | 🟡 中 |
| 8 | OnCombatBodyBroken() | CompTrion | 被动破裂时的回调 | 🟡 中 |

### 关键缺失的机制

| ID | 机制 | 影响 | 优先级 |
|----|------|------|--------|
| 11 | CompTick() 未调用 mount.Tick() | 引导倒计时不工作 | 🔴 高 |
| 12 | TriggerMount 无状态枚举 | 状态转换不清晰 | 🟡 中 |
| 13 | 无法区分"引导中"和"已激活" | 消耗计算错误 | 🔴 高 |

---

## 第四部分：严重程度评级

### 🔴 高优先级（影响核心功能正确性）

1. **CompTick() 未调用 mount.Tick()**
   - 影响：引导倒计时无法工作
   - 后果：激活流程错误，无法自动完成激活
   - 修复难度：低

2. **缺少 ShouldIncludeInConsumption() 虚函数**
   - 影响：引导期间无法参与消耗计算
   - 后果：能量消耗计算错误
   - 修复难度：低

3. **无法区分"引导中"和"已激活"**
   - 影响：状态转换逻辑不清晰
   - 后果：应用层难以正确处理状态
   - 修复难度：中

### 🟡 中优先级（影响扩展性和应用层集成）

4. **缺少激活验证虚函数 (CanActivateComponent)**
   - 影响：无法在激活前做条件检查
   - 后果：应用层无法检查输出功率等条件
   - 修复难度：低

5. **缺少部位毁坏的框架处理**
   - 影响：部位毁坏时无法自动断连组件
   - 后果：需要应用层手动管理
   - 修复难度：中

6. **缺少被动破裂的结果处理回调**
   - 影响：无法在破裂时施加debuff等效果
   - 后果：应用层需要额外机制处理
   - 修复难度：低

---

## 第五部分：框架设计评价

### ✅ 正确的地方

- 能量四要素的数据模型设计正确
- 组件系统的基础架构合理
- 虚拟伤害系统的Harmony补丁恰当
- Bail Out 机制的流程正确
- 快照和恢复机制设计优秀

### ❌ 缺陷地方

- **扩展点不足**：缺少虚函数让应用层介入关键流程
- **状态管理不清晰**：无明确的组件状态枚举
- **缺少生命周期钩子**：引导完成、激活完成、停用完成等没有回调
- **缺少框架层的部位处理**：部位毁坏时应有框架层的标准处理
- **缺少前置条件检查**：激活前无虚函数让应用层做条件验证

### 总体评价

框架的**核心思想正确**（定义规则+提供接口+约束验证），但**扩展性设计不足**。

应该补充**8个虚函数**和**修复1个关键的Tick()调用缺陷**，才能让框架真正支持应用层的各种需求。

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| 1.0 | 初版：全面深度审查，找出10个缺陷和8个缺失虚函数 | 2026-01-16 | knowledge-refiner |

---

**knowledge-refiner**
*2026-01-16*
