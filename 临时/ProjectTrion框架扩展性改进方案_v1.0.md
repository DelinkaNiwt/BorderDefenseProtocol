# ProjectTrion 框架扩展性改进方案

**摘要**：本文档提供 ProjectTrion 框架的完整扩展性改进方案，包括 8 个虚函数接口、CompTick() 关键缺陷修复、引导机制完整实现。所有改进均遵循"框架=定义规则+提供接口+扩展点"的设计原则。

**版本号**：v1.0
**修改时间**：2026-01-16
**关键词**：扩展点设计、虚函数钩子、引导机制、状态管理、消耗计算、应用层回调
**标签**：[待审]

---

## 目录
1. [核心问题分析](#核心问题分析)
2. [改进方案总体设计](#改进方案总体设计)
3. [8个虚函数完整代码](#8个虚函数完整代码)
4. [CompTick() 关键缺陷修复](#comptick-关键缺陷修复)
5. [引导机制完整实现](#引导机制完整实现)
6. [状态管理改进](#状态管理改进)
7. [修改清单](#修改清单)
8. [测试检查表](#测试检查表)

---

## 核心问题分析

### 问题1：缺少虚函数扩展点

**现状**：框架的 `CompTick()` 和 `ActivateComponent()` 等关键方法中缺少虚函数钩子，导致应用层无法定制行为。

**影响范围**：
- 应用层无法自定义"哪些组件参与消耗计算"
- 无法在组件激活前进行验证（如输出功率检查）
- 无法在导引完成时处理特殊逻辑
- 无法处理组件失效或失去连接的场景

**解决方案**：添加 8 个虚函数接口到 `CompTrion` 类，同时保持框架的验证责任。

### 问题2：CompTick() 没有调用 mount.Tick()

**现状**（L282-293）：
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
    // ❌ 缺失：没有调用 mount.Tick() 更新导引倒计时
}
```

**后果**：
- `activationTicks` 倒计时永远不会递减
- 组件状态无法从"导引中"转移到"激活"
- 导引机制完全无法工作

**影响设定示例**：
```
激活变色龙：-5/一次性，+1/每单位消耗，耗时1单位：-1

预期行为：
- 按下激活键时：IsActive=true, activationTicks=1, 消耗 -5
- 第1个CompTick周期（60个普通Tick）：activationTicks 递减 60 次变成 0, 消耗 +1
- 导引完成后组件进入完全激活状态

实际行为：
- activationTicks 永不递减，组件永远卡在"引导中"状态
- 导引永不完成
```

### 问题3：Guiding 组件无法参与消耗计算

**现状**（L306-310）：
```csharp
float mountConsumption = 0f;
foreach (var mount in _mounts.Where(m => m.IsActive))  // ❌ 仅包含 IsActive==true
{
    mountConsumption += mount.GetConsumptionRate();
}
```

**问题链条**：
1. `IsActive` 是一个布尔值，无法区分"导引中"vs"完全激活"
2. 在导引期间，`activationTicks > 0` 但 `IsActive == true`（两者都是true/false的组合）
3. 设定要求导引期间也要消耗 Trion，但没有办法表达这个状态
4. `GetConsumptionRate()` 返回 `def.consumptionRate` 当 `IsActive == true`

**解决方案**：
- 添加 `IsGuiding` 属性检测 `activationTicks > 0`
- 添加虚函数 `ShouldIncludeInConsumption()` 让应用层定制逻辑
- 让 `GetConsumptionRate()` 在导引期间也能返回消耗值

---

## 改进方案总体设计

### 改进目标
1. ✅ 使框架成为真正的"规则定义器+接口提供者"
2. ✅ 让应用层能通过虚函数回调自定义所有关键行为
3. ✅ 修复 CompTick() 使导引机制正常运作
4. ✅ 提供清晰的状态管理能力
5. ✅ 保持向后兼容性

### 改进点清单

| 优先级 | 改进项 | 文件 | 行号范围 | 改进类型 |
|------|------|------|--------|--------|
| 🔴 高 | CompTick() 调用 mount.Tick() | CompTrion.cs | 282-293 | 关键缺陷修复 |
| 🔴 高 | 添加 ShouldIncludeInConsumption() | CompTrion.cs | TickConsumption | 虚函数 |
| 🔴 高 | 修复消耗计算逻辑 | CompTrion.cs | 306-310 | 逻辑修复 |
| 🟡 中 | 添加 CanActivateComponent() | CompTrion.cs | ActivateComponent | 虚函数 |
| 🟡 中 | 添加 OnMountActivationStarted() | CompTrion.cs | ActivateComponent | 虚函数 |
| 🟡 中 | 添加 OnMountGuidanceCompleted() | CompTrion.cs | CompTick | 虚函数 |
| 🟡 中 | 添加 OnMountDeactivated() | CompTrion.cs | DeactivateComponent | 虚函数 |
| 🟢 低 | 添加 IsGuiding 属性 | TriggerMount.cs | - | 状态管理 |
| 🟢 低 | 添加 OnVitalPartDisconnected() | CompTrion.cs | NotifyVitalPartDestroyed | 虚函数 |
| 🟢 低 | 添加 OnCombatBodyBroken() | CompTrion.cs | DestroyCombatBody | 虚函数 |

---

## 8个虚函数完整代码

### VF-1: ShouldIncludeInConsumption()

**位置**：CompTrion.cs，在 `TickConsumption()` 前添加

**目的**：让应用层决定某个组件是否参与消耗计算。默认包括所有激活和导引中的组件。

```csharp
/// <summary>
/// 检查某个组件是否应该参与消耗计算。
/// 框架默认：激活中或导引中的组件才参与。
/// 应用层可重写以实现：
/// - 条件激活（如需要足够的输出功率才能参与）
/// - 特殊模式（如省电模式下只有部分组件参与）
/// - 冷却机制（如组件刚停用后有延迟才能再次参与）
///
/// Check if a component should participate in consumption calculation.
/// Framework default: only active or guiding components participate.
/// Application can override to implement: conditional activation, special modes, cooldown, etc.
/// </summary>
/// <param name="mount">要检查的组件</param>
/// <returns>true=参与消耗计算, false=跳过此组件</returns>
protected virtual bool ShouldIncludeInConsumption(TriggerMount mount)
{
    if (mount == null || mount.def == null)
        return false;

    // 框架规则：仅激活或导引中的组件参与
    return mount.IsActive && (mount.activationTicks >= 0);
}
```

### VF-2: OnMountGuidanceCompleted()

**位置**：CompTrion.cs，CompTick() 中调用

**目的**：当组件导引完成（activationTicks 从 1 变成 0）时的回调。应用层可在此处理导引完成后的特殊效果。

```csharp
/// <summary>
/// 当组件导引完成时的回调。
/// 在 activationTicks 递减到 0 后立即调用。
/// 应用层可在此处理：
/// - 播放导引完成特效
/// - 应用临时增益
/// - 记录日志
/// - 触发关联事件
///
/// Called when component guidance completes (activationTicks decrements to 0).
/// Application can handle post-guidance effects, visual effects, bonuses, etc.
/// </summary>
/// <param name="mount">完成导引的组件</param>
protected virtual void OnMountGuidanceCompleted(TriggerMount mount)
{
    // 框架默认：无特殊处理
    // 应用层可重写此方法
}
```

### VF-3: CanActivateComponent()

**位置**：CompTrion.cs，`ActivateComponent()` 前添加

**目的**：在激活前检查是否满足条件。框架验证 Trion 足够，应用层可加入业务逻辑验证。

```csharp
/// <summary>
/// 检查是否可以激活某个组件。
/// 在 ActivateComponent() 的费用检查后调用。
/// 框架已验证：Trion足够、组件未激活。
/// 应用层可再验证：
/// - 输出功率是否足够
/// - 组件是否损坏或冷却中
/// - 冲突检查（如两个组件不能同时激活）
/// - 权限检查（如宿主是否有权限使用此组件）
///
/// Check if component activation is allowed.
/// Framework has already verified: sufficient Trion, component not active.
/// Application can verify: sufficient power, damage/cooldown status, conflicts, permissions, etc.
/// </summary>
/// <param name="mount">要激活的组件</param>
/// <returns>true=允许激活, false=拒绝激活</returns>
protected virtual bool CanActivateComponent(TriggerMount mount)
{
    // 框架默认：无额外限制
    // 应用层可重写此方法添加业务逻辑
    return true;
}
```

### VF-4: OnBeforeMountActivation()

**位置**：CompTrion.cs，`ActivateComponent()` 中调用，在费用消耗之前

**目的**：激活前的介入点。可用于准备工作或最后的验证。

```csharp
/// <summary>
/// 在组件激活费用消耗前的介入点。
/// 适合：
/// - 最后的准备工作
/// - 性能检查（确保不会因激活而超载）
/// - 触发预激活事件
/// - 应用临时效果
///
/// Hook before component activation cost is consumed.
/// Good for: final preparations, performance checks, pre-activation events.
/// </summary>
/// <param name="mount">将要激活的组件</param>
protected virtual void OnBeforeMountActivation(TriggerMount mount)
{
    // 框架默认：无特殊处理
}
```

### VF-5: OnMountActivationStarted()

**位置**：CompTrion.cs，`ActivateComponent()` 中，费用消耗后

**目的**：激活成功后的回调。应用层可处理激活后的效果。

```csharp
/// <summary>
/// 组件激活成功后的回调。
/// 费用已消耗，mount.IsActive 已设为 true，导引倒计时已初始化。
/// 应用层可处理：
/// - 播放激活特效/声音
/// - 记录激活时间点
/// - 应用激活增益
/// - 触发事件通知其他系统
///
/// Called after component activation succeeds.
/// Cost already consumed, mount.IsActive=true, guidance ticks initialized.
/// </summary>
/// <param name="mount">刚激活的组件</param>
protected virtual void OnMountActivationStarted(TriggerMount mount)
{
    // 框架默认：无特殊处理
}
```

### VF-6: OnMountDeactivated()

**位置**：CompTrion.cs，`DeactivateComponent()` 中

**目的**：停用成功后的回调。应用层可处理停用后的清理工作。

```csharp
/// <summary>
/// 组件停用后的回调。
/// mount.IsActive 已设为 false，activationTicks 已清零。
/// 应用层可处理：
/// - 移除激活增益
/// - 播放停用特效
/// - 记录停用时间
/// - 启动冷却计时器
///
/// Called after component deactivation.
/// mount.IsActive=false, activationTicks=0.
/// </summary>
/// <param name="mount">刚停用的组件</param>
protected virtual void OnMountDeactivated(TriggerMount mount)
{
    // 框架默认：无特殊处理
}
```

### VF-7: OnVitalPartDisconnected()

**位置**：CompTrion.cs，`NotifyVitalPartDestroyed()` 中

**目的**：供给器官被摧毁时的回调。应用层可处理关联组件的失效。

```csharp
/// <summary>
/// 当供给器官被摧毁时的回调。
/// 框架将自动停用依赖此器官的所有组件。
/// 应用层可在此处理：
/// - 增加泄漏速率
/// - 应用损坏debuff
/// - 记录受损部位
/// - 触发应急程序
///
/// Called when a vital part is destroyed.
/// Framework will auto-deactivate dependent components.
/// Application can handle: leak increase, damage debuff, logging, emergency procedures.
/// </summary>
/// <param name="mount">依赖于被摧毁器官的组件</param>
/// <param name="destroyedPart">被摧毁的器官</param>
protected virtual void OnVitalPartDisconnected(TriggerMount mount, BodyPartRecord destroyedPart)
{
    // 框架默认：无特殊处理
    // 应用层可重写以实现组件特定的失效逻辑
}
```

### VF-8: OnCombatBodyBroken()

**位置**：CompTrion.cs，`DestroyCombatBody()` 中，快照恢复后

**目的**：战斗体被摧毁（破裂）时的回调。应用层可应用后续效果（如 debuff）。

```csharp
/// <summary>
/// 战斗体被摧毁（破裂）时的回调。
/// 在快照恢复完成后调用，此时肉身已恢复到原始状态。
/// 注意：BailOut成功时不调用此方法（直接传送，无破裂）。
///
/// 应用层可在此处理：
/// - 应用"Trion耗尽"debuff
/// - 触发爆炸效果
/// - 记录战斗数据
/// - 处理战斗后遗症
///
/// Called when combat body is destroyed/broken.
/// Called after snapshot restore, so pawn has returned to original state.
/// Note: NOT called if BailOut succeeds (direct teleport, no breaking).
/// Application can apply Trion Depletion debuff, explosion effects, logging, sequelae.
/// </summary>
/// <param name="pawn">战斗体被摧毁的 Pawn</param>
/// <param name="reason">摧毁原因</param>
protected virtual void OnCombatBodyBroken(Pawn pawn, DestroyReason reason)
{
    // 框架默认：无特殊处理
    // 应用层可重写以应用 debuff、特效等
}
```

---

## CompTick() 关键缺陷修复

### 修复前（L282-327）

```csharp
public override void CompTick()
{
    base.CompTick();

    if (!_isInCombat || _strategy == null)
        return;

    // 每60Tick执行一次消耗计算
    if (this.parent.IsHashIntervalTick(60))
    {
        TickConsumption();
    }
}

private void TickConsumption()
{
    // 步骤1：基础维持消耗
    float baseMaintenance = _strategy.GetBaseMaintenance();

    // 步骤2：组件激活消耗
    float mountConsumption = 0f;
    foreach (var mount in _mounts.Where(m => m.IsActive))  // ❌ 问题1：只包含IsActive
    {
        mountConsumption += mount.GetConsumptionRate();
    }

    // 步骤3：泄漏消耗（缓存以提高性能）
    float leak = GetLeakRate();

    // 步骤4：累加并消耗
    float totalConsumption = baseMaintenance + mountConsumption + leak;
    Consume(totalConsumption);

    // 步骤5：检查耗尽
    if (Available <= 0)
    {
        TriggerBailOut();
    }

    // 步骤6：委托给Strategy处理复杂逻辑
    _strategy.OnTick(this);
}
```

**问题**：
1. ❌ `CompTick()` 没有调用 `mount.Tick()` 更新导引倒计时
2. ❌ `TickConsumption()` 只检查 `IsActive`，无法区分导引状态
3. ❌ 无虚函数扩展点

### 修复后（完整版本）

```csharp
public override void CompTick()
{
    base.CompTick();

    if (!_isInCombat || _strategy == null)
        return;

    // 🟢 修复1：更新每个组件的状态（导引倒计时递减）
    UpdateAllMountStates();

    // 每60Tick执行一次消耗计算
    if (this.parent.IsHashIntervalTick(60))
    {
        TickConsumption();
    }
}

/// <summary>
/// 更新所有组件的状态。
/// 特别是递减导引倒计时，检测导引完成。
/// </summary>
private void UpdateAllMountStates()
{
    foreach (var mount in _mounts)
    {
        if (mount == null || mount.def == null)
            continue;

        // 记录导引完成前的状态
        int prevActivationTicks = mount.activationTicks;

        // 调用 TriggerMount.Tick() 更新状态（递减导引倒计时）
        mount.Tick();

        // 🟢 修复2：检测导引完成（从 activationTicks > 0 变成 == 0）
        if (prevActivationTicks > 0 && mount.activationTicks == 0)
        {
            OnMountGuidanceCompleted(mount);
        }
    }
}

private void TickConsumption()
{
    // 步骤1：基础维持消耗
    float baseMaintenance = _strategy.GetBaseMaintenance();

    // 步骤2：组件激活消耗
    // 🟢 修复3：使用虚函数扩展点，允许应用层定制消耗参与逻辑
    float mountConsumption = 0f;
    foreach (var mount in _mounts)
    {
        if (mount != null && ShouldIncludeInConsumption(mount))
        {
            mountConsumption += mount.GetConsumptionRate();
        }
    }

    // 步骤3：泄漏消耗（缓存以提高性能）
    float leak = GetLeakRate();

    // 步骤4：累加并消耗
    float totalConsumption = baseMaintenance + mountConsumption + leak;
    Consume(totalConsumption);

    // 步骤5：检查耗尽
    if (Available <= 0)
    {
        TriggerBailOut();
    }

    // 步骤6：委托给Strategy处理复杂逻辑
    _strategy.OnTick(this);
}
```

### 修改要点

| 改进 | 代码位置 | 作用 |
|------|--------|------|
| **修复1** | `UpdateAllMountStates()` | 每帧调用所有 `mount.Tick()` |
| **修复2** | `导引倒计时检测` | 检测导引完成，触发回调 |
| **修复3** | `ShouldIncludeInConsumption()` | 虚函数扩展点 |

---

## 引导机制完整实现

### 完整工作流程

```
激活按钮 → ActivateComponent()
    ├─ CanActivateComponent() [VF-3] 验证
    ├─ OnBeforeMountActivation() [VF-4] 介入点
    ├─ Consume(activationCost) 消耗一次性费用
    ├─ mount.IsActive = true
    ├─ mount.activationTicks = guidanceTicks (1-5)
    ├─ OnMountActivationStarted() [VF-5] 回调
    └─ 进入导引状态 ✓

导引期间 (1-5 个 CompTick 周期)
    ├─ mount.Tick() 递减 activationTicks
    ├─ ShouldIncludeInConsumption() 检查消耗参与
    ├─ 持续消耗 mount.GetConsumptionRate() 的费用
    └─ 等待 activationTicks 变成 0

导引完成 → activationTicks == 0
    ├─ OnMountGuidanceCompleted() [VF-2] 回调
    └─ 进入完全激活状态 ✓

停用按钮 → DeactivateComponent()
    ├─ mount.IsActive = false
    ├─ mount.activationTicks = 0
    ├─ OnMountDeactivated() [VF-6] 回调
    └─ 退出激活状态 ✓
```

### TriggerMount.Tick() 的作用

```csharp
public void Tick()
{
    if (IsActive && activationTicks > 0)
    {
        activationTicks--;  // 🔴 关键：递减导引倒计时
    }
}
```

这个方法被 `UpdateAllMountStates()` 每帧调用，确保导引倒计时正常进行。

### 导引消耗的实现

在 `ShouldIncludeInConsumption()` 中：

```csharp
protected virtual bool ShouldIncludeInConsumption(TriggerMount mount)
{
    if (mount == null || mount.def == null)
        return false;

    // ✓ 激活中的组件参与消耗
    // ✓ 导引中的组件也参与消耗（activationTicks >= 0）
    return mount.IsActive && (mount.activationTicks >= 0);
}
```

这样设置确保：
- 导引期间（activationTicks = 1-5）：`mount.IsActive = true` 且 `activationTicks > 0`，返回 true
- 导引完成（activationTicks = 0）：`mount.IsActive = true` 且 `activationTicks == 0`，仍然返回 true
- 停用后（activationTicks = 0，IsActive = false）：返回 false

---

## 状态管理改进

### 问题：缺少清晰的状态区分

**现在**：TriggerMount 只有一个 `bool IsActive` 无法区分状态：

```
IsActive | activationTicks | 实际状态
---------|-----------------|----------
false    | 0               | ❓ 已停用 / 未激活
true     | 0               | ✓ 完全激活
true     | 1-5             | ✓ 导引中
```

### 解决方案1：添加 IsGuiding 属性（推荐）

**位置**：TriggerMount.cs（无需修改 ExposeData）

```csharp
/// <summary>
/// 组件是否处于导引状态（正在激活中）。
/// 导引状态表示组件已激活但还未完全就位，需要 1-5 tick 的时间。
/// </summary>
public bool IsGuiding
{
    get { return IsActive && activationTicks > 0; }
}

/// <summary>
/// 组件是否处于完全激活状态（导引已完成）。
/// </summary>
public bool IsFullyActivated
{
    get { return IsActive && activationTicks == 0; }
}

/// <summary>
/// 组件是否处于非活动状态。
/// </summary>
public bool IsDormant
{
    get { return !IsActive; }
}
```

### 改进后的状态图

```
+-----+       激活      +--------+      导引完成      +----------+
| 休眠 | ─────────────→ | 导引中  | ─────────────→ | 完全激活 |
+-----+       (IsActive | (IsGuiding= +----------+
              =true,   | true)       ↓
         activationTicks| activationTicks==0
             =1-5)      |
                         |
                         |________停用________↓
                         ← 返回 休眠 ←
```

### TriggerMount 的状态查询方法

```csharp
/// <summary>
/// 获取组件的当前状态字符串（用于调试和日志）。
/// Get current state as string for debugging.
/// </summary>
public string GetStateString()
{
    if (IsDormant) return "Dormant";
    if (IsGuiding) return $"Guiding({activationTicks}ticks)";
    if (IsFullyActivated) return "FullyActivated";
    return "Unknown";
}
```

### 在日志中使用

```csharp
// ActivateComponent()
Log.Message($"CompTrion: {parent?.Label} 组件 {mount.def.label} 已激活");
Log.Message($"  当前状态: {mount.GetStateString()}");
Log.Message($"  导引需时: {mount.activationTicks} ticks");
```

---

## 修改清单

### CompTrion.cs 修改项

| 序号 | 修改类型 | 位置 | 具体内容 |
|-----|--------|------|--------|
| 1 | 添加虚函数 | L300 前 | `ShouldIncludeInConsumption()` |
| 2 | 添加虚函数 | L300 前 | `OnMountGuidanceCompleted()` |
| 3 | 添加方法 | CompTick() 后 | `UpdateAllMountStates()` |
| 4 | 修改 CompTick() | L282-293 | 添加 `UpdateAllMountStates()` 调用 |
| 5 | 修改 TickConsumption() | L306-310 | 改用 `ShouldIncludeInConsumption()` 替换 Where 过滤 |
| 6 | 修改 ActivateComponent() | L471-505 | 添加 VF-3, VF-4, VF-5 调用 |
| 7 | 修改 DeactivateComponent() | L511-531 | 添加 VF-6 调用 |
| 8 | 修改 NotifyVitalPartDestroyed() | L718-738 | 添加 VF-7 调用 |
| 9 | 修改 DestroyCombatBody() | L574-615 | 添加 VF-8 调用 |
| 10 | 添加虚函数 | CompTick() 附近 | `CanActivateComponent()` |
| 11 | 添加虚函数 | CompTick() 附近 | `OnBeforeMountActivation()` |
| 12 | 添加虚函数 | CompTick() 附近 | `OnMountActivationStarted()` |
| 13 | 添加虚函数 | CompTick() 附近 | `OnMountDeactivated()` |
| 14 | 添加虚函数 | CompTick() 附近 | `OnVitalPartDisconnected()` |
| 15 | 添加虚函数 | CompTick() 附近 | `OnCombatBodyBroken()` |

### TriggerMount.cs 修改项

| 序号 | 修改类型 | 位置 | 具体内容 |
|-----|--------|------|--------|
| 1 | 添加属性 | L31-37 后 | `IsGuiding { get; }` |
| 2 | 添加属性 | - | `IsFullyActivated { get; }` |
| 3 | 添加属性 | - | `IsDormant { get; }` |
| 4 | 添加方法 | L108 后 | `GetStateString()` |

---

## 测试检查表

### Unit Test 检查表

- [ ] 测试 `ShouldIncludeInConsumption()` 在导引中返回 true
- [ ] 测试 `ShouldIncludeInConsumption()` 在停用后返回 false
- [ ] 测试 `OnMountGuidanceCompleted()` 在 activationTicks 变成 0 时被调用
- [ ] 测试 `CanActivateComponent()` 返回 false 时激活失败
- [ ] 测试 `OnBeforeMountActivation()` 在费用消耗前被调用
- [ ] 测试 `OnMountActivationStarted()` 在 IsActive 设为 true 后被调用
- [ ] 测试 `OnMountDeactivated()` 停用时被调用
- [ ] 测试 IsGuiding 属性在导引期间为 true
- [ ] 测试 IsGuiding 属性在导引完成后为 false

### 集成测试检查表

- [ ] 设定案例：激活变色龙（-5/一次性，+1/每单位消耗，耗时1单位：-1）
  - [ ] 按激活键：IsActive 变成 true，activationTicks = 1，Available 减 5
  - [ ] 等待 60ticks：activationTicks 递减 60 次变成 0，Available 减 1
  - [ ] 验证导引完成回调被触发
  - [ ] 继续激活：Available 继续减 1/60tick

- [ ] 部位摧毁测试
  - [ ] 供给器官被摧毁时：OnVitalPartDisconnected() 被调用
  - [ ] 相关组件被自动停用
  - [ ] 泄漏速率增加

- [ ] 战斗体破裂测试
  - [ ] Trion 耗尽时：OnCombatBodyBroken() 被调用
  - [ ] 快照成功恢复
  - [ ] Bail Out 成功时：OnCombatBodyBroken() 不被调用

### 性能检查表

- [ ] UpdateAllMountStates() 遍历所有 mount 的性能开销（应该 O(n)，n = mount 数量）
- [ ] ShouldIncludeInConsumption() 调用次数（每 60ticks 调用一次）
- [ ] 虚函数调用开销（应该可忽略）

---

## 实施步骤

### 第一阶段：添加虚函数接口

1. 在 CompTrion.cs 中添加 8 个虚函数（复制上面的代码片段）
2. 确保所有虚函数都有 `protected virtual` 修饰符
3. 编译验证：0 errors

### 第二阶段：修复 CompTick() 缺陷

1. 添加 `UpdateAllMountStates()` 方法
2. 在 `CompTick()` 中调用此方法
3. 修改 `TickConsumption()` 使用 `ShouldIncludeInConsumption()`
4. 编译验证：0 errors

### 第三阶段：改进状态管理

1. 在 TriggerMount.cs 中添加 `IsGuiding` 等属性
2. 添加 `GetStateString()` 方法
3. 更新所有日志消息使用新属性
4. 编译验证：0 errors

### 第四阶段：测试验证

1. 运行 Unit Test
2. 在 RimWorld 中测试实际设定
3. 监控日志输出
4. 验证没有新的编译警告

---

## 相关文件参考

- **审查报告**：`ProjectTrion框架全面深度审查报告_v1.0.md`
- **改进需求**：同级目录
- **源代码**：`../模组工程/ProjectTrion_Framework/Source/ProjectTrion/`

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|--------|--------|------|
| v1.0 | 初始版本，包含 8 个虚函数、CompTick 缺陷修复、引导机制实现、状态管理改进 | 2026-01-16 | Claude |

**END OF DOCUMENT**
