# ProjectTrion Framework 代码修改方案

**文档元信息**

- 摘要：包含所有框架代码的具体修改，提供完整的代码片段和修改位置
- 版本号：1.0
- 修改时间：2026-01-16
- 关键词：代码修改、API 实现、框架改进
- 标签：[待审]

---

## 修改概览

| 修改 | 文件 | 操作 | 代码量 |
|------|------|------|--------|
| 1 | ILifecycleStrategy.cs | 添加虚函数 | +10 行 |
| 2 | CompTrion.cs | 添加方法 + 修改方法 + 删除类 | +95 行, -37 行, ~5 行改 |
| 3 | （应用层维护）| 实现新虚函数 + 迁移类 | 应用层任务 |

---

## 修改 1：ILifecycleStrategy.cs - 添加虚函数

### 修改位置

在文件开头找到 `TalentGrade` 枚举定义之后，`ILifecycleStrategy` 接口的第一个方法之前添加。

**具体位置：** 第 31-42 行之间

### 修改内容

**添加新方法：**

```csharp
/// <summary>
/// 创建战斗体快照实例。
/// 框架将使用此方法创建的实例来保存 Pawn 的物理状态。
/// 应用层可以返回自定义的 CombatBodySnapshot 子类，以实现自定义的过滤规则。
///
/// Create a combat body snapshot instance.
/// Framework uses this to save pawn's physical state during combat body generation.
/// Application can return custom CombatBodySnapshot subclass with custom filtering rules.
/// </summary>
/// <param name="comp">CompTrion 实例</param>
/// <returns>CombatBodySnapshot 实例（或其子类）</returns>
CombatBodySnapshot CreateSnapshot(CompTrion comp);
```

### 修改后的代码上下文

```csharp
namespace ProjectTrion.Core
{
    public enum TalentGrade
    {
        S = 6,
        A = 5,
        B = 4,
        C = 3,
        D = 2,
        E = 1,
    }

    public interface ILifecycleStrategy
    {
        /// <summary>
        /// 获取此策略的唯一标识符。
        /// </summary>
        string StrategyId { get; }

        /// <summary>
        /// 创建战斗体快照实例。
        /// ✅ 新添加的虚函数
        /// </summary>
        CombatBodySnapshot CreateSnapshot(CompTrion comp);

        /// <summary>
        /// 获取单位的初始Trion天赋。
        /// </summary>
        TalentGrade? GetInitialTalent(CompTrion comp);

        // ... 其他方法保持不变 ...
    }
}
```

---

## 修改 2：CompTrion.cs - 3 处修改

### 修改 2.1：删除 TrionCombatBodySnapshot 类定义

#### 删除位置

**第 27-63 行整体删除：**

**删除前：**

```csharp
private class TrionCombatBodySnapshot : CombatBodySnapshot
{
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff == null)
            return false;

        // 忽略Trion相关的Hediff，使其不参与快照/恢复
        // Trion腺体（如果存在）应该在战斗体生成时保留，销毁时继续保留
        // 框架层预留了这个接口，这里演示如何使用

        // TODO: 应用层应该根据实际的Hediff定义来判断
        // 例如：if (hediff.def == TrionDefs.HediffTrionGland) return true;

        // 暂时示例：检查Hediff标签或def名称中是否包含"Trion"
        if (hediff.def != null && hediff.def.defName.Contains("Trion"))
        {
            return true;
        }

        return base.ShouldIgnoreHediff(hediff);
    }

    protected override bool ShouldIgnoreHediff(HediffSnapshot snapshot)
    {
        if (snapshot.def == null)
            return false;

        // 忽略Trion相关的快照
        if (snapshot.def.defName.Contains("Trion"))
        {
            return true;
        }

        return base.ShouldIgnoreHediff(snapshot);
    }
}
```

**删除后：** 这 37 行完全删除，以下代码上移

```csharp
using System;
using System.Collections.Generic;
// ... using 语句 ...

namespace ProjectTrion.Components
{
    public class CompTrion : ThingComp
    {
        // ❌ TrionCombatBodySnapshot 类已删除

        public static Func<TalentGrade, float> TalentCapacityProvider;

        // ... 后续代码 ...
    }
}
```

---

### 修改 2.2：添加 SetReserved() 方法

#### 添加位置

在 `PostSpawnSetup()` 方法之前添加。

**具体位置：** 第 188 行之前（原来的 PostSpawnSetup 开始处）

#### 添加代码

```csharp
/// <summary>
/// 设置Trion占用值（由组件锁定）。
/// 框架会进行数据一致性检查。
/// 通常在 Strategy.OnCombatBodyGenerated() 中由应用层调用。
///
/// Set Trion reserved capacity (locked by components).
/// Framework validates data consistency.
/// Usually called by application layer in Strategy.OnCombatBodyGenerated().
/// </summary>
/// <param name="amount">要设置的占用值</param>
public void SetReserved(float amount)
{
    _reserved = Mathf.Clamp(amount, 0, _capacity);
    ValidateDataConsistency();

    // 额外检查：Reserved + Consumed 不能超过 Capacity
    if (_reserved + _consumed > _capacity)
    {
        Log.Warning(
            $"CompTrion: {parent?.Label} SetReserved({amount}) 导致 " +
            $"Reserved({_reserved}) + Consumed({_consumed}) > Capacity({_capacity})。" +
            $"这将导致 Available 计算不正确。应用层应该检查这个错误。"
        );
    }
}

/// <summary>
/// 激活一个组件。
/// 检查 Available 是否足够激活费用，然后扣除费用并标记为激活。
/// 返回激活是否成功。
///
/// Activate a component.
/// Checks if Available is sufficient for activation cost, deducts cost, and marks as active.
/// Returns whether activation succeeded.
/// </summary>
/// <param name="mount">要激活的组件</param>
/// <returns>激活是否成功</returns>
public bool ActivateComponent(TriggerMount mount)
{
    if (mount == null)
    {
        Log.Error($"CompTrion: {parent?.Label} 尝试激活空组件");
        return false;
    }

    if (mount.IsActive)
    {
        Log.Warning($"CompTrion: {parent?.Label} 组件{mount.def.label}已处于激活状态");
        return false;
    }

    // 检查费用
    float activationCost = mount.GetActivationCost();
    if (Available < activationCost)
    {
        Log.Warning(
            $"CompTrion: {parent?.Label} 无法激活{mount.def.label}，" +
            $"需要 {activationCost} Trion，仅有 {Available} Trion 可用"
        );
        return false;
    }

    // 扣除费用
    Consume(activationCost);

    // 标记为激活
    mount.IsActive = true;

    // 启动导引计时器（如果有）
    mount.activationTicks = mount.def?.activationGuidanceTicks ?? 0;

    Log.Message(
        $"CompTrion: {parent?.Label} 组件{mount.def.label}已激活，" +
        $"消耗 {activationCost} Trion，剩余 {Available} Trion"
    );

    return true;
}

/// <summary>
/// 停用一个组件。
/// 立即停用，无延迟，无费用。
///
/// Deactivate a component.
/// Immediate deactivation, no delay, no cost.
/// </summary>
/// <param name="mount">要停用的组件</param>
/// <returns>停用是否成功</returns>
public bool DeactivateComponent(TriggerMount mount)
{
    if (mount == null)
    {
        Log.Error($"CompTrion: {parent?.Label} 尝试停用空组件");
        return false;
    }

    if (!mount.IsActive)
    {
        Log.Warning($"CompTrion: {parent?.Label} 组件{mount.def.label}未处于激活状态");
        return false;
    }

    // 立即停用
    mount.IsActive = false;
    mount.activationTicks = 0;

    Log.Message($"CompTrion: {parent?.Label} 组件{mount.def.label}已停用");

    return true;
}

public override void PostExposeData()
{
    // ... 现有代码保持不变 ...
}

public override void PostSpawnSetup(bool respawningAfterLoad)
{
    // ... 现有代码保持不变 ...
}
```

---

### 修改 2.3：修改 GenerateCombatBody() 方法

#### 修改位置

**大约第 489-513 行**

#### 修改前

```csharp
public void GenerateCombatBody()
{
    if (_isInCombat)
    {
        Log.Warning($"CompTrion: {parent?.Label}已在战斗体状态，不能重复生成");
        return;
    }

    var pawn = this.parent as Pawn;
    if (pawn == null)
        return;

    // 保存快照（使用应用层子类TrionCombatBodySnapshot，实现ShouldIgnoreHediff）
    // 应用层可以通过子类定义哪些Hediff应该被保护
    _snapshot = new TrionCombatBodySnapshot();  // ❌ 硬编码
    _snapshot.CaptureFromPawn(pawn);

    // 标记为战斗体激活
    _isInCombat = true;

    // 回调Strategy
    _strategy?.OnCombatBodyGenerated(this);

    Log.Message($"CompTrion: {pawn.Name}的战斗体已生成，可用Trion: {Available}");
}
```

#### 修改后

```csharp
public void GenerateCombatBody()
{
    if (_isInCombat)
    {
        Log.Warning($"CompTrion: {parent?.Label}已在战斗体状态，不能重复生成");
        return;
    }

    var pawn = this.parent as Pawn;
    if (pawn == null)
        return;

    // ✅ 让 Strategy 决定使用哪个 Snapshot 类
    // 框架默认使用 CombatBodySnapshot，应用层可以通过 Strategy 返回子类
    if (_strategy != null)
    {
        _snapshot = _strategy.CreateSnapshot(this);
    }
    else
    {
        Log.Error($"CompTrion: {parent?.Label} 的 Strategy 为 null，使用框架默认 Snapshot");
        _snapshot = new CombatBodySnapshot();
    }

    _snapshot.CaptureFromPawn(pawn);

    // 标记为战斗体激活
    _isInCombat = true;

    // 回调Strategy
    _strategy?.OnCombatBodyGenerated(this);

    Log.Message($"CompTrion: {pawn.Name}的战斗体已生成，可用Trion: {Available}");
}
```

#### 修改说明

- **原因**：框架不应该硬编码应用层的类
- **方式**：通过 Strategy 虚函数让应用层决定
- **兼容性**：如果 Strategy 为 null，使用框架默认

---

## 应用层需要做的修改

### 应用层 Strategy 实现中的修改

#### 1. 实现 CreateSnapshot() 虚函数

```csharp
// 应用层的 Strategy 实现（例如：Strategy_HumanCombatBody）

public override CombatBodySnapshot CreateSnapshot(CompTrion comp)
{
    // 应用层返回自己定义的 Snapshot 子类
    return new TrionCombatBodySnapshot();
}
```

#### 2. 定义应用层的 TrionCombatBodySnapshot 类

**这个类应该定义在应用层，不在框架中。**

```csharp
// 应用层新增文件或在 Strategy 实现文件中定义

/// <summary>
/// 应用层定义的 Snapshot 类。
/// 实现了应用特定的 Hediff 过滤规则。
/// </summary>
private class TrionCombatBodySnapshot : CombatBodySnapshot
{
    protected override bool ShouldIgnoreHediff(Hediff hediff)
    {
        if (hediff == null)
            return false;

        // 应用层规则：忽略 Trion 相关的 Hediff
        // 这个规则可以根据具体的应用需求修改
        if (hediff.def != null && hediff.def.defName.Contains("Trion"))
        {
            return true;
        }

        return base.ShouldIgnoreHediff(hediff);
    }

    protected override bool ShouldIgnoreHediff(HediffSnapshot snapshot)
    {
        if (snapshot.def == null)
            return false;

        // 同样的规则应用于快照
        if (snapshot.def.defName.Contains("Trion"))
        {
            return true;
        }

        return base.ShouldIgnoreHediff(snapshot);
    }
}
```

#### 3. 在 OnCombatBodyGenerated() 中使用 SetReserved()

```csharp
// 应用层 Strategy 的 OnCombatBodyGenerated 实现

public override void OnCombatBodyGenerated(CompTrion comp)
{
    // 1. 计算所有组件的占用值
    float totalReserved = 0f;
    foreach (var mount in comp.Mounts)
    {
        totalReserved += mount.GetReservedCost();
    }

    // 2. 调用框架 API 设置占用值 ✅
    comp.SetReserved(totalReserved);

    // 3. 可选：在这里自动激活配置为默认激活的组件
    foreach (var mount in comp.Mounts)
    {
        if (mount.def.activateByDefault)
        {
            // ✅ 使用新的 API
            comp.ActivateComponent(mount);
        }
    }

    Log.Message(
        $"战斗体生成完成 - {comp.parent.Label}：" +
        $"占用={totalReserved}, 可用={comp.Available}"
    );
}
```

#### 4. 在组件激活逻辑中使用 ActivateComponent()

```csharp
// 应用层的组件激活逻辑（可以在 Strategy.OnTick() 中或其他地方）

public override void OnTick(CompTrion comp)
{
    // 示例：如果玩家想激活某个组件
    // if (玩家输入激活)
    // {
    //     bool success = comp.ActivateComponent(targetMount);
    //     if (!success)
    //     {
    //         // Trion 不足，框架已打印日志
    //     }
    // }
}
```

---

## 验证清单

### 修改完成后的检查

- [ ] **CompTrion.cs**
  - [ ] 第 27-63 行已删除（TrionCombatBodySnapshot 类）
  - [ ] SetReserved() 方法已添加（约 15 行）
  - [ ] ActivateComponent() 方法已添加（约 35 行）
  - [ ] DeactivateComponent() 方法已添加（约 20 行）
  - [ ] GenerateCombatBody() 方法已修改（调用 Strategy.CreateSnapshot()）

- [ ] **ILifecycleStrategy.cs**
  - [ ] CreateSnapshot() 虚函数已添加（约 10 行）

- [ ] **编译**
  - [ ] ProjectTrion.Framework 编译无错误
  - [ ] ProjectTrion.Framework 编译无警告（除非应用层未实现新虚函数）

- [ ] **应用层**
  - [ ] Strategy 实现了 CreateSnapshot() 虚函数
  - [ ] TrionCombatBodySnapshot 类已迁移到应用层
  - [ ] OnCombatBodyGenerated() 中调用了 SetReserved()
  - [ ] 组件激活逻辑使用了 ActivateComponent()

### 功能测试

- [ ] 战斗体生成时，占用值被正确计算和设置
- [ ] Reserved + Consumed <= Capacity 的验证有效
- [ ] ActivateComponent() 检查 Available 并正确扣费
- [ ] DeactivateComponent() 正确停用组件
- [ ] ShouldIgnoreHediff 规则正确应用
- [ ] 快照和恢复流程正常工作

---

## 代码修改汇总

### CompTrion.cs 的净改动

```
删除行数：37 行（TrionCombatBodySnapshot 类）
添加行数：95 行（SetReserved, ActivateComponent, DeactivateComponent）
修改行数：5 行（GenerateCombatBody 方法）

净增长：95 - 37 = 58 行
```

### ILifecycleStrategy.cs 的净改动

```
添加行数：10 行（CreateSnapshot 虚函数）

净增长：10 行
```

### 总体改动

```
框架代码净增长：~70 行
框架代码变更：2 个文件
应用层需要调整：Strategy 实现 + 类迁移
```

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|----------|--------|
| 1.0 | 初版，包含所有修改的代码片段和具体位置 | 2026-01-16 | knowledge-refiner |

---

**📍 Knowledge-Refiner**
*2026-01-16*
