# Grouped Manual Attack Targeting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让多选相同攻击入口时的 BDP 手动攻击按钮正确聚合，并在一次 targeting 后对可执行成员分别下正式攻击单。

**Architecture:** 保留现有每 Pawn 一份手动入口投影与单体 `AttackExecutionTargetingSource`，只在 gizmo 交互层补一层极薄的组级 targeting 适配。继续复用原版 gizmo 分组显示，不 patch 原版 `Targeter`，不改 `AttackExecutionService` 正式执行边界。

**Tech Stack:** C# 7.3, RimWorld/Verse gizmo + targeting API, PowerShell smoke tests

---

### Task 1: 锁定多选入口聚合契约

**Files:**
- Modify: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**Step 1: Write the failing test**
- 增加断言：手动攻击入口命令必须显式设置分组语义，而不是只依赖 `Label/icon`。
- 增加断言：`Command_BdpManualEntryTarget` 必须使用 `ProcessGroupInput(...)` 作为组点击入口。
- 增加断言：`ProcessInput(...)` 不再直接调用 `BeginTargeting(...)`。

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:
- FAIL
- 失败点集中在手动入口命令还没有组级交互语义。

**Step 3: Write minimal implementation**
- 先只补测试，不动生产代码。

**Step 4: Run test again**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:
- 仍为 FAIL，作为后续实现起点。

### Task 2: 给手动入口命令补齐分组点击行为

**Files:**
- Modify: `Source/BDP/Core/Expressions/Projection/Command_BdpManualEntryTarget.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`

**Step 1: Implement minimal command changes**
- 给 `Command_BdpManualEntryTarget` 增加入口键参数。
- 在构造时设置 `groupKey`，使用入口键的稳定哈希。
- `ProcessInput(...)` 不再启动 targeting。
- 在 `ProcessGroupInput(...)` 中：
  - 从 `group` 里收集同类命令；
  - 组大小为 1 时，启动原单体 source；
  - 组大小大于 1 时，启动组级 source。

**Step 2: Wire resolver**
- 在 `DefaultManualEntryGizmoResolver` 中，把 `ManualEntryProjectionGroup.GroupId` 传给命令构造函数。

**Step 3: Run targeted tests**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:
- PASS on command grouping contract assertions.

### Task 3: 增加组级 targeting 适配器

**Files:**
- Create: `Source/BDP/Core/AttackExecution/GroupedAttackExecutionTargetingSource.cs`
- Test: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`

**Step 1: Write the failing test**
- 增加断言：组级 targeting source 必须实现“任一成员可命中即可确认、逐成员下单”的语义。
- 增加断言：它不直接构建新的 `AttackExecutionRequest` 结构，只复用底层单体 source 的 `OrderForceTarget(...)`。

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:
- FAIL
- 因为组级 targeting source 尚不存在。

**Step 3: Write minimal implementation**
- 新建 `GroupedAttackExecutionTargetingSource : ITargetingSource`。
- 内部仅保存 `IReadOnlyList<AttackExecutionTargetingSource>`。
- 展示属性委托给第一个有效成员。
- `CanHitTarget/ValidateTarget` 按“任一成员可行”处理。
- `OrderForceTarget` 遍历成员，对通过校验的成员逐个调用底层 `OrderForceTarget(target)`。

**Step 4: Run test to verify it passes**

Run:
```powershell
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:
- PASS

### Task 4: 做行为回归验证

**Files:**
- Test: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`

**Step 1: Run focused regression tests**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:
- PASS

**Step 2: Manual in-game verification**
- 选择两个拥有同一攻击入口的 Pawn。
- 确认底部只有一个聚合按钮。
- 在两人都能命中的位置点目标，应两人都攻击。
- 在只有一人可命中的位置点目标，应只有一人攻击。
- 在两人都不可命中的位置点目标，应无法确认。

**Step 3: Stop**
- 不继续扩展为全局组会话系统。
- 不继续抽象额外聚合服务。
- 到行为闭环成立即收口。
