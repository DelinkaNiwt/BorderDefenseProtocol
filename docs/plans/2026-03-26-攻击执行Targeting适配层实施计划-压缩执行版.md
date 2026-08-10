# 攻击执行 Targeting 适配层实施计划（压缩执行版） Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
>
> **执行前提示（必须先读，禁止跳过）：**
>
> 1. 时刻保持模组全局视野。先看 `Trigger`、`Expressions`、`AttackExecution`、原版 `Targeter`、原版 `Verb` 的边界，再决定局部改法。
> 2. 时刻保持架构优先。先判断“正式执行边界是否仍在 BDP”，再判断现象是否会自然收口。
> 3. 禁止结果导向。不能为了图标恢复、持续攻击恢复，就把正式真值偷偷交回裸 `Verb`。
> 4. 必须遵守：`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\docs\00-项目宪章\中文文件与终端规则.md`
> 5. 所有新增成员、修改成员都必须逐成员注释。
> 6. 不做单元测试，也不为了这轮任务补单元测试。
> 7. 这是单人单机模组，禁止脱离模组体量做过重抽象。
> 8. 计划外工作必须先更新计划，再实施。
> 9. 每完成一个任务块，都要补记：`docs/99-会话交接/2026-03-26-工作推进-卷05.md`

**Goal:** 在不退回裸 `Verb` 正式执行边界的前提下，让手动攻击正式兼容原版 `Targeter` / `OnGUI(...)` / `OrderForceTarget(...)` 语义，并让图标反馈与持续攻击回到同一条 BDP 正式执行链。

**Architecture:** 本轮只补两层正式缺口。第一层是 `formal result -> ITargetingSource` 的通用适配层；第二层是 `AttackExecutionRequest` 的独立派单意图。原版负责目标选择交互体验，BDP 负责正式攻击请求与执行分发，二者通过 `AttackExecutionTargetingSource` 连接，不互相越权。

**Tech Stack:** C# 7.3, .NET Framework 4.8, RimWorld `ITargetingSource` / `Targeter` / `Command` / `Verb`, XML Def, `dotnet msbuild`

---

## 完成标准

- 手动入口改走 `BeginTargeting(ITargetingSource)`。
- 新增通用 `AttackExecutionTargetingSource`。
- `AttackExecutionRequest` 正式区分：
  - `ImmediateCast`
  - `ForceTargetOrder`
- `OrderForceTarget(...)` 回到 `AttackExecution`，不直接打一发，也不把正式真值交回裸 `Verb`。
- 合法目标图标反馈走原版语义，非法目标保留禁止语义。
- 主模组单独编译通过。

---

### Task 1: 补齐派单语义

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackDispatchIntent.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`

**Step 1: 新增派单意图枚举**

新增并逐成员注释：

- `ImmediateCast`
- `ForceTargetOrder`

**Step 2: 扩展请求模型**

给 `AttackExecutionRequest` 和已解析请求补 `DispatchIntent`，明确：

- `Reason` = 从哪来
- `DispatchIntent` = 怎么进执行系统

**Step 3: 归一化默认语义**

在 `DefaultAttackExecutionEntry` 中明确：

- 旧路径未声明时，仍按 `ImmediateCast`
- 手动入口后续可以显式传 `ForceTargetOrder`

**Step 4: 编译**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

**Step 5: 日志**

卷05补记“派单语义已拆开”。

---

### Task 2: 建通用 Targeting 适配层

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionVerbBridge.cs`

**Step 1: 新增 `AttackExecutionTargetingSource`**

实现 `ITargetingSource`，稳定输入只保留：

- `Pawn`
- `ResultId`
- `IAttackExecutionEntry`
- 运行时 verb 解析入口
- `DispatchIntent`

**Step 2: 加会话型轻量缓存**

只做最小缓存：

- `cachedContext`
- `cacheStateKey`
- `cacheTick`

目标是减少同一瞄准会话里的重复解析，不做永久真值缓存。

**Step 3: 加统一解析入口**

所有 UI / 校验成员统一走一个入口，例如：

- `GetOrRefreshResolvedContext()`

**Step 4: 编译**

使用同一条 `dotnet msbuild BDP.csproj ...` 命令。

**Step 5: 日志**

卷05补记“适配层骨架已成立，不是新真值 owner”。

---

### Task 3: 接回原版 Targeter UI 语义

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Check: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Check: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`

**Step 1: 实现基础 targeting 成员**

逐成员注释并实现：

- `Caster`
- `CasterPawn`
- `GetVerb`
- `UIIcon`
- `targetParams`

**Step 2: 实现 UI / 校验成员**

逐成员注释并实现：

- `CanHitTarget(...)`
- `ValidateTarget(...)`
- `DrawHighlight(...)`
- `OnGUI(...)`

原则：

- 优先借当前 resolved `Verb`
- 无法解析时只给保守兜底
- 不伪造“其实能打”的假语义

**Step 3: 复核 BDP Verb**

只确认是否满足 UI 借用条件，不做计划外扩张。

**Step 4: 编译**

使用同一条 `dotnet msbuild BDP.csproj ...` 命令。

**Step 5: 日志**

卷05补记“图标反馈正式切到 targetingSource 语义”。

---

### Task 4: 让 `OrderForceTarget(...)` 回到 BDP 正式执行链

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultMeleeAttackExecutor.cs`

**Step 1: 实现正式下单**

把：

```text
source.OrderForceTarget(target)
```

落成：

```text
AttackExecution.TryExecute(
  Pawn + ResultId + Target + Manual + ForceTargetOrder
)
```

**Step 2: 调整执行分流**

先按 `DispatchIntent` 分：

- `ForceTargetOrder` -> 正式命令链
- `ImmediateCast` -> 旧即时施放链

**Step 3: 近战 / 远程分别复核**

目标只确认两件事：

- 不再掉回“只打一发”
- 不重复计伤

如发现需要计划外扩展，先停，先改计划。

**Step 4: 编译**

使用同一条 `dotnet msbuild BDP.csproj ...` 命令。

**Step 5: 日志**

卷05补记“`OrderForceTarget(...)` 已回到 `AttackExecution`”。

---

### Task 5: 切手动入口到通用适配层

**Files:**
- Create: `Source/BDP/Core/Expressions/Projection/Command_BdpManualEntryTarget.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionManualGizmoBridge.cs`

**Step 1: 新增手动命令对象**

职责只保留：

- 显示按钮
- `ProcessInput(...)` 中调用 `Find.Targeter.BeginTargeting(source)`

**Step 2: 改 `DefaultManualEntryGizmoResolver`**

把旧的：

- `Command_Action + callback targeting`

改为新的：

- `Command_BdpManualEntryTarget + AttackExecutionTargetingSource`

**Step 3: 复核桥接边界**

确认 `ExpressionManualGizmoBridge` 仍只负责投影翻译，不承担执行。

**Step 4: 编译**

使用同一条 `dotnet msbuild BDP.csproj ...` 命令。

**Step 5: 日志**

卷05补记“手动入口已正式切到 targetingSource 路径”。

---

### Task 6: 收口与最终验证

**Files:**
- Modify: `docs/01-决策记录/2026-03-26-攻击执行边界与运行时驱动裁定-第一版.md`
- Modify: `docs/99-会话交接/2026-03-26-工作推进-卷05.md`

**Step 1: 补决策记录**

只补 3 条：

- 适配层为什么是通用层
- 为什么采用会话型轻量缓存
- 为什么 `OrderForceTarget(...)` 必须先回 `AttackExecution`

**Step 2: 最终编译**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

**Step 3: 最终日志**

卷05必须明确写清：

- 通用适配层是否成立
- 手动入口是否正式切线
- 图标与持续攻击是否已回到同一条正式链路
- 当前仍未做但不阻塞的事项

Plan complete and saved to `docs/plans/2026-03-26-攻击执行Targeting适配层实施计划-压缩执行版.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

Which approach?
