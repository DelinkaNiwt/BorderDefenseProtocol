# 攻击执行 Targeting 适配层实施计划（第一版） Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
>
> **执行前提示（必须先读，禁止跳过）：**
>
> 1. 时刻保持模组全局视野。任何局部改动都必须先评估它对 `Trigger`、`Expressions`、`AttackExecution`、原版 `Targeter`、原版 `Verb`、近战链、远程链的边界影响。
> 2. 时刻保持架构优先的评估思维。先判断“边界是否正确、职责是否单一、真值是否唯一、运行时入口是否统一”，再判断代码该怎么写。
> 3. 禁止结果导向。不能为了让图标看起来对、让它多打几下，就跳过正式边界直接补旁路；不能把现象修复误当架构完成。
> 4. 必须遵守中文文件与终端规则：`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\docs\00-项目宪章\中文文件与终端规则.md`
> 5. 任何涉及中文文件的读取、检查、回读，必须先切 UTF-8 输入输出，并显式带 `-Encoding utf8`。
> 6. 手工修改文件必须优先使用 `apply_patch`，并遵守“先读、后改、再回读”的顺序。
> 7. 所有新增成员、修改成员都必须逐成员注释；不能只给类写总注释后放任成员裸奔。
> 8. 如果要做计划外工作，必须先更新计划，再实施。
> 9. 每次完成一个任务块，都必须把进展补记到 `docs/99-会话交接/2026-03-26-工作推进-卷05.md`；当前卷未满 10 条前，不得新开卷。

**Goal:** 在不退回裸 `Verb` 正式执行边界的前提下，新增一层通用 `AttackExecutionTargetingSource`（攻击执行目标选择适配源），把 BDP formal result（正式表达结果）正式接入原版 `ITargetingSource`（目标选择源接口）/ `Targeter`（目标选择器）体系，并同时给 `AttackExecution` 补上 `ForceTargetOrder`（正式强制攻击下单）语义，让手动攻击图标反馈与正式持续攻击命令自然回到同一条正式架构链上。

**Architecture:** 本次实施不把问题拆成“图标补丁”和“多打一发补丁”，而是补两个正式缺层：第一层是 `formal result -> ITargetingSource` 的通用适配层，第二层是 `AttackExecutionRequest` 中独立的派单意图维度。UI/校验行为允许借用当前 resolved `Verb`（当前已解析原版攻击动作对象），但 `OrderForceTarget(...)`（强制下达目标命令）必须回到 `AttackExecution`（攻击执行边界）正式接单；手动入口只是第一批调用方，不得把适配层做成手动专属特判件。

**Tech Stack:** C# 7.3, .NET Framework 4.8, RimWorld `ITargetingSource` / `Targeter` / `Command` / `Verb` / `Job` / `JobDriver`, Harmony patch 环境, XML Def, `dotnet msbuild`

---

## 总执行原则

- 通用适配层只做“formal result 到 targetingSource 的翻译”，不新增真值 owner。
- 不把 UI 层重新变成正式执行边界。
- 不让 `Verb.TryStartCastOn(...)` 继续充当手动强制攻击命令。
- 手动入口是第一批接线方，不是适配层的拥有者。
- `AttackExecutionRequest` 中“来源原因”和“派单意图”必须拆开，不得继续混用。
- `DispatchIntent`（派单意图）优先于 `DriveMode`（推进方式）决定执行分流。
- 所有会影响中文文件的步骤，都必须严格遵守 UTF-8 读取/回读规则。
- 每完成一个任务，必须补日志；日志累计 10 条才允许开新卷。
- 实施过程中如发现现有设计不足，先回写设计或计划，再改代码。

## 完成标准

本计划完成时，应同时满足：

- 主模组新增通用 `AttackExecutionTargetingSource`（攻击执行目标选择适配源）。
- 主模组新增 `Command_BdpManualEntryTarget`（BDP 手动入口目标选择命令）。
- `AttackExecutionRequest` 新增 `AttackDispatchIntent`（攻击派单意图）。
- 手动入口正式走 `BeginTargeting(ITargetingSource)` 路径，而不是普通 callback 路径。
- `Targeter` 在手动入口下能自然使用 `OnGUI(...)` / `ValidateTarget(...)` / `DrawHighlight(...)` / `OrderForceTarget(...)`。
- 手动合法目标时不再只显示通用红十字，非法目标时保留禁止反馈。
- 手动攻击不再自然退化成“只打一发就停”的 immediate cast。
- `AttackExecution` 能明确区分：
  - `ImmediateCast`（立即施放）
  - `ForceTargetOrder`（正式强制攻击下单）
- 代码成员注释完整，文档与卷05日志同步回写。
- 主模组编译通过；如条件允许，DevHarness 编译也通过。

## 当前已确认设计事实

- 当前问题不是两个独立 bug，而是“targeting 接口缺层 + 派单语义缺层”的同根表现。
- 当前 `Command_Action + BeginTargeting(TargetingParameters, callback)` 路径天然不会触发原版 `ITargetingSource` 语义。
- 原版 `Targeter` 的图标反馈、合法性校验和正式下单都依赖 `ITargetingSource`。
- 原版 `Verb.OnGUI(...)` 能自然给出“合法目标攻击图标 / 非法目标禁止图标”反馈。
- 当前 `AttackExecution` 只区分来源原因，不区分派单意图，所以会把手动强制攻击误翻译成单次施放。
- 通用适配层必须采用“稳定身份 + 会话型轻量缓存”，而不是“永久缓存 formal 真值”。

---

### Task 1: 定稿实施计划与开工提示

**Files:**
- Create: `docs/plans/2026-03-26-攻击执行Targeting适配层实施计划-第一版.md`
- Modify: `docs/99-会话交接/2026-03-26-工作推进-卷05.md`

**Step 1: 写正式实施计划**

把本计划落到 `docs/plans/`，开头必须明确写死：

- 全局视野
- 架构优先
- 禁止结果导向
- 必须遵守中文文件与终端规则
- 逐成员注释
- 计划外工作先改计划

**Step 2: 回写卷05日志**

记录：

- 设计已转入正式实施计划
- 实施入口是“targeting 适配层 + 派单语义补强”
- 当前仍未进入代码改造

**Step 3: Commit**

```bash
git add docs/plans/2026-03-26-攻击执行Targeting适配层实施计划-第一版.md docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "docs: add targeting adapter implementation plan"
```

---

### Task 2: 补齐 AttackExecution 请求语义

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackDispatchIntent.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 新增 `AttackDispatchIntent`**

正式新增派单意图枚举，并逐成员注释。

第一版至少包含：

- `ImmediateCast`（立即施放）
- `ForceTargetOrder`（正式强制攻击下单）

**Step 2: 扩展 `AttackExecutionRequest`**

新增：

- `DispatchIntent`

并逐成员注释解释：

- `Reason` 只回答“从哪来”
- `DispatchIntent` 只回答“怎么进执行系统”

**Step 3: 让 `DefaultAttackExecutionEntry` 正式接受新语义**

当前先不改复杂运行时，只先确保：

- 缺少 `DispatchIntent` 时行为明确
- 手动入口未来能传 `ForceTargetOrder`
- 旧 immediate 行为仍可保留给非手动路径

**Step 4: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 5: 回写卷05日志**

记录：

- `AttackExecutionRequest` 已正式拥有派单意图维度
- 手动强制攻击和立即施放在模型上已经拆开

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "feat: add attack dispatch intent to execution request"
```

---

### Task 3: 新增通用 `AttackExecutionTargetingSource` 骨架

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionVerbBridge.cs`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 创建通用适配层对象**

新增 `AttackExecutionTargetingSource`，实现：

- `ITargetingSource`

第一版先写清稳定输入成员并逐成员注释：

- `Pawn`
- `ResultId`
- `IAttackExecutionEntry`
- `IExpressionVerbHostResolver` 或同等解析口
- `AttackDispatchIntent`

**Step 2: 写内部轻量上下文**

在同文件内新增或内嵌轻量上下文对象，至少回答：

- 当前已解析 `FormalExpressionResult`
- 当前已解析 `Verb`
- 当前是否可用

并逐成员注释。

**Step 3: 补会话型缓存骨架**

第一版先搭出以下最小结构：

- `cachedContext`
- `cacheStateKey`
- `cacheTick`

不先追求最优性能，只先保证：

- 同一 targeting 会话内可复用
- 状态变化时能失效重建

**Step 4: 写统一解析入口**

实现内部统一入口，例如：

- `GetOrRefreshResolvedContext()`

要求：

- 所有 UI / 校验成员都只能走这一个入口
- 禁止每个 `ITargetingSource` 成员各自单独解析

**Step 5: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 6: 回写卷05日志**

记录：

- 通用适配层骨架已成立
- 当前只是适配层，不是新真值 owner
- 当前缓存策略是“会话型轻量缓存”

**Step 7: Commit**

```bash
git add Source/BDP/Core/AttackExecution Source/BDP/Core/Expressions/Projection/ExpressionVerbBridge.cs docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "feat: add attack execution targeting source skeleton"
```

---

### Task 4: 实现 `ITargetingSource` 的 UI 与校验接口

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 实现基础接口成员**

逐成员实现并逐成员注释：

- `CasterIsPawn`
- `IsMeleeAttack`
- `Targetable`
- `MultiSelect`
- `HidePawnTooltips`
- `Caster`
- `CasterPawn`
- `GetVerb`
- `UIIcon`
- `targetParams`
- `DestinationSelector`

要求：

- 优先基于当前 resolved `Verb`
- 无法解析时给出保守兜底，不制造假语义

**Step 2: 实现 UI / 校验行为**

逐成员实现并逐成员注释：

- `CanHitTarget(...)`
- `ValidateTarget(...)`
- `DrawHighlight(...)`
- `OnGUI(...)`

要求：

- 统一走 `GetOrRefreshResolvedContext()`
- 正常情况下转发给当前 resolved `Verb`
- `OnGUI(...)` 以恢复原版攻击图标 / 禁止图标语义为目标

**Step 3: 审核 `BdpVerb_*` 宿主是否满足 UI 借用条件**

检查：

- `BdpVerb_Shoot`
- `BdpVerb_MeleeAttackDamage`

确认其 UI 相关行为仍与原版 `Verb` 语义兼容。
如必须补最小注释或最小辅助成员，只补最小必要量。

**Step 4: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 5: 回写卷05日志**

记录：

- 通用适配层已具备原版 targeting UI / 校验能力
- 图标反馈恢复路径已从 callback 切到 `ITargetingSource`

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs Source/BDP/Core/Verbs/BdpVerb_Shoot.cs Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "feat: implement targeting UI bridge for attack execution"
```

---

### Task 5: 实现 `OrderForceTarget(...)` 正式下单路径

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultMeleeAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanner.cs`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 实现 `OrderForceTarget(...)`**

严格按正式边界实现：

```text
OrderForceTarget(target)
    -> AttackExecution.TryExecute(request)
```

请求必须携带：

- `Pawn`
- `ResultId`
- `Target`
- `Reason = Manual`
- `DispatchIntent = ForceTargetOrder`

禁止在这里直接：

- `verb.TryStartCastOn(...)`

**Step 2: 调整 `DefaultAttackExecutionEntry` 分流**

先按 `DispatchIntent` 分流，再谈 `DriveMode`：

- `ForceTargetOrder` 先走正式命令链
- `ImmediateCast` 才走现有单次/多次施放链

**Step 3: 调整近战/远程执行器**

确认：

- 手动入口下来的 `ForceTargetOrder` 不会再自然掉回 immediate cast
- 近战和远程都能进入正式 job / driver 语义

必要时补最小 planner 消费逻辑，但禁止顺手重写整套攻击编排。

**Step 4: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 5: 回写卷05日志**

记录：

- `OrderForceTarget(...)` 已正式回到 `AttackExecution`
- 手动强制攻击已不再等于“立刻打一发”

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "feat: route force target orders through attack execution"
```

---

### Task 6: 新增手动入口命令对象并接线到通用适配层

**Files:**
- Create: `Source/BDP/Core/Expressions/Projection/Command_BdpManualEntryTarget.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionManualGizmoBridge.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultExpressionVerbHostResolver.cs`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 新增命令对象**

新增 `Command_BdpManualEntryTarget`，职责只限于：

- 显示按钮
- 在 `ProcessInput(...)` 时启动 `Find.Targeter.BeginTargeting(source, ...)`

并逐成员注释。

**Step 2: 改 `DefaultManualEntryGizmoResolver`**

把当前：

- `Command_Action + BeginTargeting(TargetingParameters, callback)`

改为：

- 构建 `AttackExecutionTargetingSource`
- 构建 `Command_BdpManualEntryTarget`

要求：

- Resolver 仍只负责“把投影翻译成按钮”
- 不直接承担执行

**Step 3: 保持桥接层边界**

`ExpressionManualGizmoBridge` 继续只负责：

- 从正式投影取数
- 把 gizmo 返回给外部

不能在这里新增执行语义。

**Step 4: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 5: 回写卷05日志**

记录：

- 手动入口已正式切到 targetingSource 路径
- 按钮层与执行边界已重新分离

**Step 6: Commit**

```bash
git add Source/BDP/Core/Expressions/Projection docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "refactor: route manual gizmos through targeting source command"
```

---

### Task 7: 复核缓存、职责和旁路残留

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionVerbBridge.cs`
- Modify: `docs/01-决策记录/2026-03-26-攻击执行边界与运行时驱动裁定-第一版.md`
- Test: `Source/BDP/BDP.csproj`

**Step 1: 复核缓存边界**

确认当前缓存没有滑向：

- 永久缓存 formal 真值
- UI 每次都全量重解析
- 靠旁路字段绕过统一解析入口

**Step 2: 复核职责边界**

确认：

- `AttackExecutionTargetingSource` 只是适配层
- `DefaultManualEntryGizmoResolver` 只是按钮解析器
- `ExpressionVerbBridge` 只是辅助桥
- `AttackExecution` 仍是正式执行边界

**Step 3: 补决策记录**

把本轮新增的正式裁定补进决策记录：

- 为什么适配层做成通用层
- 为什么采用会话型轻量缓存
- 为什么 `OrderForceTarget(...)` 必须回到 `AttackExecution`

**Step 4: 编译验证**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 5: 回写卷05日志**

记录：

- 缓存策略和职责边界已复核
- 没有把适配层误做成新的真值 owner

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs Source/BDP/Core/Expressions/Projection/ExpressionVerbBridge.cs docs/01-决策记录/2026-03-26-攻击执行边界与运行时驱动裁定-第一版.md docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "docs: freeze targeting adapter boundary decisions"
```

---

### Task 8: 最终验证、日志与收口

**Files:**
- Modify: `docs/99-会话交接/2026-03-26-工作推进-卷05.md`
- Test: `Source/BDP/BDP.csproj`
- Test: `Source/BDP.DevHarness/BDP.DevHarness.csproj`

**Step 1: 编译主模组**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 2: 编译 DevHarness**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild BDP.DevHarness.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 3: 回写卷05日志**

必须明确记录：

- 通用适配层是否已成立
- 手动入口是否已正式改走 `ITargetingSource`
- `DispatchIntent` 是否已拆开
- 图标反馈与持续攻击是否已回到同一正式链路
- 当前仍未完成但不阻塞本轮的事项有哪些

**Step 4: Commit**

```bash
git add docs/99-会话交接/2026-03-26-工作推进-卷05.md
git commit -m "docs: record targeting adapter rollout status"
```

Plan complete and saved to `docs/plans/2026-03-26-攻击执行Targeting适配层实施计划-第一版.md`. Two execution options:

1. Subagent-Driven (this session) - I dispatch fresh subagent per task, review between tasks, fast iteration

2. Parallel Session (separate) - Open new session with executing-plans, batch execution with checkpoints

Which approach?
