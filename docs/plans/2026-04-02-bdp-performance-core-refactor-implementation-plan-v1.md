# 新 BDP 主线性能重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不等待 Trion 完整接线的前提下，把新 BDP 当前最贵的表达读取、AttackExecution 解析和 FormalHost 同步热路径改成发布式运行时投影架构，并保证每个阶段结束后都能暂停、进游戏立即实测。

**Architecture:** 以 `CompTriggerBody` + `TriggerRuntimeCoordinator` 为唯一运行时 owner，先把战斗核心读取切到已发布 `TriggerCombatProjectionState`，再把 UI / 表现读取切到已发布 `TriggerPresentationState`，最后统一版本失效和旧会话收口。整个计划坚持 YAGNI：只为当前性能热点引入最小必要对象，不提前实现 Trion gating、统一因子公式或伤口 drain 规则。

**Tech Stack:** C#, RimWorld/Verse mod runtime, PowerShell smoke tests, `dotnet msbuild`

---

## 1. 前置条件裁剪清单

### 1.1 现在就必须做

- 把 `CompTriggerBody` 当前分散的 `RefreshProjectedOutputs()`、读档后补刷新、宿主同步，收口成单一运行时 owner。
- 把 `AttackExecution`、`AttackExecutionTargetingSource`、自动攻击入口，从“按 `ResultId` 回头现算”切到“只读已发布战斗投影”。
- 把正常读路径从“每次读取重新构建快照”改成“平时 O(1) 纯读，dirty 时才做一次 O(n) 重建”。
- 明确统一失效策略：投影版本失效后，结束当前 BDP 攻击会话，交还上层重新选取，不额外补一套自动恢复逻辑。

### 1.2 现在只留接点，不实现业务

- Trion 运行时状态如果以后进入表达 gating，只需要能成为新的 dirty / invalidation 来源；本次不实现该规则。
- 统一因子接口只需要预留“数值输入位”，本次不实现因子公式，也不把它塞进热路径。
- 伤口 drain 如果以后成为持续高频状态源，只需要能挂进 invalidation 来源；本次不为此提前设计额外调度系统。

### 1.3 本计划明确不做

- 不接 Trion 完整业务线。
- 不实现统一因子业务规则。
- 不为了“未来可能会有更多 runtime signal”先做通用事件总线或复杂调度框架。
- 不保留长期兼容层，不设计新旧双轨共存目标态。

---

## 2. 施工原则

- 每个阶段结束后，代码必须能编译、能跑 smoke test、能进游戏实测。
- 每个阶段只改一个主要热区，避免同时重写表达、攻击、UI 三条链。
- 新对象只保留最小集合：`TriggerRuntimeCoordinator`、`TriggerCombatProjectionState`、`TriggerPresentationState`、`AttackExecutionPreparedContext`。
- 不引入额外缓存塔。目标是“一个 owner 发布，所有消费者纯读”，不是“每层自己再补缓存”。
- 不为了架构而架构。已有投影器、已有协议服务、已有 smoke test 脚本都优先复用。
- 复杂度目标要直白：
- 当前常规读取近似是“每读一次就 O(槽位 + 来源 + 结果 + 投影构建)”。
- 目标态常规读取应是 O(1) 或 O(索引查询)。
- 只有 loadout 变化、启停切换、禁用变化、装备变化、读档恢复这些 dirty 事件发生时，才做一次 O(槽位 + 来源 + 结果 + 投影构建) 的重建。

---

## 3. 实测素材与固定命令

### 3.1 建议固定三份实测存档

- 存档 A：单 Pawn，1 把 Trigger，主副侧各 1 到 2 个芯片，专门测装卸、切换、gizmo、tooltip。
- 存档 B：2 到 6 名 Pawn 持续交火，专门测自动攻击、手动点选、换武器、切模式。
- 存档 C：战斗进行中立即存档再读档，专门测 post-load 恢复、宿主壳状态、攻击会话打断。

### 3.2 每阶段固定命令

构建：

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

基础 smoke tests：

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
```

原则：

- 每阶段至少跑和该阶段直接相关的脚本。
- 阶段 2 以后，每次都要进游戏做一次“切换中断当前动作”验证。
- 阶段 4 以后，每次都要做一次“战斗中读档”验证。

---

## 4. 阶段总览

| 阶段 | 目标 | 主要收益 | 你能立刻回游戏测什么 |
| --- | --- | --- | --- |
| 阶段 1 | 收口运行时 owner 与战斗投影发布骨架 | dirty / 发布 / formal host 同步不再散落在 `CompTriggerBody` 各处 | 装卸芯片、启停切换、装备卸下、读档后武器是否还正常 |
| 阶段 2 | 切断战斗热路径的读时现算 | 攻击执行、自动攻击、targeting 不再按 `ResultId` 回头现算 | 手动攻击、自动攻击、换武器/换策略是否打断并重新选取 |
| 阶段 3 | 切断 UI / 表现热路径的读时现算 | gizmo、说明、视觉投影改读已发布 presentation state | gizmo 更新、tooltip、显示顺序、视觉表现是否同步 |
| 阶段 4 | 收口版本失效与 post-load 会话恢复 | 版本过期后的行为统一，不再多处各自猜测恢复 | 战斗中切换、禁用、读档，动作是否正确中断且不留脏状态 |
| 阶段 5 | 删除旧语义并做最终性能验收 | 读时重建入口、旧 resolver、无效缓存彻底清干净 | 多 Pawn 连续战斗时卡顿是否明显下降、是否有旧逻辑回流 |

---

### Task 1: 运行时 Owner 收口与战斗投影发布骨架

**Files:**
- Create: `Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs`
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerCombatProjectionState.cs`
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**目标**

- 先把“谁负责发布当前正式战斗结果”定死。
- 本阶段不追求一次把所有消费者切完，只先把 owner、dirty、发布入口收口到位。

**Step 1: 新建最小运行时对象**

- `TriggerCombatProjectionState` 只保留：
- `ProjectionVersion`
- `ExpressionSnapshot Snapshot`
- `IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex`
- `IReadOnlyDictionary<string, CompositeExpressionReference> CompositeReferenceIndex`
- `IReadOnlyDictionary<string, BdpFormalVerbHostSlot> ResultIdToFormalSlot`
- `bool IsEmpty`
- `TriggerRuntimeCoordinator` 只先保留：
- `owner`
- `currentProjectionVersion`
- `currentCombatProjection`
- `projectionDirty`
- `dirtyReason`

**Step 2: 在 `CompTriggerBody` 持有 `TriggerRuntimeCoordinator`**

- 初始化放在构造函数。
- `CompTriggerBody` 不再直接承担投影发布流程本体，只负责：
- 真值写入
- 触发 dirty
- 对外暴露已发布状态的纯读口

**Step 3: 把发布链从 `RefreshProjectedOutputs()` 挪进 coordinator**

- `RefreshProjectedOutputs()` 的主体逻辑搬进 `TriggerRuntimeCoordinator.RebuildAndPublish(...)`。
- `TryFinalizePostLoadProjectionRefresh()` 改成委托 coordinator 完成首次发布。
- `Notify_Unequipped()` 改成让 coordinator 清空发布状态并清理 formal host。

**Step 4: 把 mutation 点统一改成“标 dirty + 发布”**

- `TryLoadChip()`
- `TryUnloadChip()`
- `NotifySlotActivationCommitted()`
- `NotifySlotDeactivated()`
- post-load finalize
- 这些点都不再直接各自拼发布逻辑，只能走 coordinator。

**Step 5: 让 `TriggerBodyVerbHostManager` 改为消费已发布战斗投影**

- 暂时允许内部还是从 `projection.Snapshot` 取结果，不允许再自己定义“当前该绑定什么”。
- `Refresh(...)` 的输入改成 `TriggerCombatProjectionState`，不是裸 `ExpressionSnapshot`。

**Step 6: 补 smoke test 合同**

- `TriggerSingleTruthSmokeTests.ps1` 增加断言：
- `CompTriggerBody` 不再直接持有长期发布流程。
- `TriggerRuntimeCoordinator` 存在且是唯一发布 owner。
- `FormalHostVerbSmokeTests.ps1` 增加断言：
- `TriggerBodyVerbHostManager` 改读战斗投影而不是裸快照入口。

**Step 7: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- 编译通过。
- single truth / formal host 相关 smoke test 通过。

**阶段停点：你可以立刻回游戏测**

- 装上武器后 gizmo 还能正常出现。
- 装卸芯片、启停切换后，当前可用攻击结果没有明显错乱。
- 卸下武器后 formal host 不残留。
- 读档后 weapon / host / gizmo 没有第一时间坏掉。

**这阶段如果出问题，优先怀疑**

- `CompTriggerBody.Lifecycle.cs`
- `CompTriggerBody.Integrity.cs`
- `TriggerRuntimeCoordinator`
- `TriggerBodyVerbHostManager`

---

### Task 2: AttackExecution 与 Targeting 全量切到已发布战斗投影

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackExecutionPreparedContext.cs`
- Create: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultMeleeAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Delete: `Source/BDP/Core/AttackExecution/AttackExecutionPlanRuntimeStore.cs`
- Modify or Delete: `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/AutoAttackSeparationSmokeTests.ps1`

**目标**

- 把攻击执行热路径彻底从 `ExpressionService.BuildSelectedSnapshot()` 上切下来。
- 这一阶段做完，战斗相关的读路径就该基本从“每次读时现算”改成“只读已发布投影”。

**Step 1: 给 `AttackExecutionRequest` 增加投影版本**

- 新增 `ProjectionVersion` 字段。
- 所有正式下单入口在创建请求时，都必须带上当时命中的 `ProjectionVersion`。

**Step 2: 用 `AttackExecutionPreparedContext` 取代旧 resolved request 语义**

- 新对象只承载：
- 原始请求
- 已验证版本
- 命中的已发布结果
- 已发布快照引用
- plan / runtime steps
- 不再保留“我可以回头重新解析 result”的语义。

**Step 3: 重写 `DefaultAttackExecutionEntry` 的入口解析**

- 删除 `SnapshotAttackExecutionResolver` 这套“按 `ResultId` 回头找当前快照”的默认路线。
- 新路线直接从 `CompTriggerBody` / `TriggerRuntimeCoordinator` 读取当前 `TriggerCombatProjectionState`。
- 如果 `ResultId` 不在发布投影里，直接拒绝，不再临时重算。

**Step 4: 切 `AttackExecutionTargetingSource`**

- `ResolveCurrentContext()` 改成读已发布战斗投影 + formal host binding。
- 删掉现在这套“按 tick + stateKey 缓存当前上下文”的补丁式缓存。
- 如果仍保留缓存，只允许按 `ProjectionVersion` 做 UI 级浅缓存，不允许重新解析表达。

**Step 5: 切自动攻击入口**

- `AttackExecutionSurfaceAccess.TryGetAutoRangedVerb()`
- `AttackExecutionSurfaceAccess.TryExecuteAutoMelee()`
- 这两条路径统一改成读取已发布 `PrimaryRanged` / `PrimaryMelee`。
- 不再调用 `BuildSelectedSnapshot()`。

**Step 6: 切执行上下文与 job 驱动**

- `RangedAttackExecutionContext`
- `MeleeAttackExecutionContext`
- `JobDriver_BdpRangedAttackExecution`
- `JobDriver_BdpMeleeAttackExecution`
- `BdpVerb_Shoot`
- 这些运行时入口都改读 prepared context 和 projection version。

**Step 7: 删除不需要的旧对象**

- 删除 `AttackExecutionPlanRuntimeStore.cs`。
- `AttackExecutionResolvedRequest` 如果仍存在，只允许作为短期施工文件，阶段完成前必须删掉或改名成新的 prepared context。

**Step 8: 补 smoke test 合同**

- 新增 `AttackExecutionProjectionVersionSmokeTests.ps1`：
- 断言 `AttackExecutionRequest` 带 `ProjectionVersion`
- 断言 `DefaultAttackExecutionEntry` 不再通过 `TryGetSelectedResult()` 解析
- 断言 `AttackExecutionTargetingSource` 不再走读时表达重算
- 更新 `RangedProtocolBoundarySmokeTests.ps1`
- 更新 `AutoAttackSeparationSmokeTests.ps1`

**Step 9: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected:

- 编译通过。
- ranged protocol / auto attack / projection version 相关脚本通过。

**阶段停点：你可以立刻回游戏测**

- 手动点选远程攻击。
- 手动点选近战攻击。
- 原版自动攻击接上 BDP 结果是否正常。
- 攻击准备中换武器、切换策略、停用 Trigger 后，当前动作是否打断，然后交还上层该干嘛干嘛。

**这阶段如果出问题，优先怀疑**

- `DefaultAttackExecutionEntry.cs`
- `AttackExecutionTargetingSource.cs`
- `AttackExecutionSurfaceAccess.cs`
- `RangedAttackExecutionContext.cs`
- `MeleeAttackExecutionContext.cs`

---

### Task 3: UI / 表现读取切到已发布 Presentation State

**Files:**
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerPresentationState.cs`
- Create: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Contracts/IExpressionReader.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionManualGizmoBridge.cs`
- Modify: `Source/BDP/Core/Trigger/External/TriggerEquippedGizmoService.cs`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`

**目标**

- 把 gizmo / info / visual 的读取也切到发布式。
- 这一阶段做完后，正常 UI 打开、鼠标悬停、gizmo 刷新都不该再触发表达全量重建。

**Step 1: 新建 `TriggerPresentationState`**

- 只保留：
- `ProjectionVersion`
- `ExpressionInfoProjection`
- `ManualEntryProjection`
- `VisualExpressionProjection`

**Step 2: coordinator 同时发布战斗投影和表现投影**

- 构建顺序固定：
- 先生成一份 `ExpressionSnapshot`
- 再从同一份 snapshot 派生 combat state
- 再从同一份 snapshot 派生 presentation state
- 两者必须共享同一个 `ProjectionVersion`

**Step 3: 改 `ExpressionService` 的默认读取语义**

- `GetSnapshot()`、`GetInfoProjection()`、`GetManualProjection()`、`GetVisualProjection()` 改为默认读取已发布状态。
- 真正的 snapshot 构建入口只给 coordinator 使用，不再给普通读取口直接开放。

**Step 4: 切 gizmo / UI 入口**

- `ExpressionManualGizmoBridge`
- `TriggerEquippedGizmoService`
- 这些 UI 入口只能读取已发布 `ManualEntryProjection`，不允许再次触发 snapshot rebuild。

**Step 5: 补 smoke test 合同**

- 新增 `ExpressionPublishedProjectionSmokeTests.ps1`
- 断言普通 UI 读取口不再直接调用 `BuildSelectedSnapshot(pawn)`
- 断言 presentation state 与 combat state 共用 `ProjectionVersion`

**Step 6: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 编译通过。
- UI / published projection 相关脚本通过。

**阶段停点：你可以立刻回游戏测**

- 装卸芯片后 gizmo 是否立刻正确更新。
- 切主副侧、禁用状态变化后，说明和手动入口是否同步变化。
- 鼠标反复悬停、频繁打开 gizmo 菜单时，卡顿是否明显下降。

**这阶段如果出问题，优先怀疑**

- `TriggerRuntimeCoordinator.cs`
- `ExpressionFormalSurfaces.cs`
- `ExpressionManualGizmoBridge.cs`
- `TriggerEquippedGizmoService.cs`

---

### Task 4: 统一版本失效策略与 Post-Load 攻击会话收口

**Files:**
- Modify: `Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Patches/Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Modify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`

**目标**

- 任何导致投影版本变化的事件，都必须走同一套收口语义。
- 不允许某些入口 silently 继续攻击，某些入口又自己中断。

**Step 1: 定死 dirty / invalidation 来源**

- loadout 变化
- 启停正式提交
- 禁用状态变化
- 装备卸下
- post-load finalize
- 这些来源全部落到 `ProjectionDirtyReason`

**Step 2: 统一“版本失效怎么处理”**

- 规则固定为：
- 当前 BDP 攻击会话结束
- 不做 BDP 内部二次自动补派
- 交还上层重新选取
- 这必须同时作用于：
- targeting 下单后版本变化
- job 推进中版本变化
- post-load 恢复时宿主结果已失效

**Step 3: 改 post-load recovery**

- `AttackExecutionPostLoadRecovery` 改成先校验当前 `ProjectionVersion` 与 binding 身份。
- 不再自己补做 snapshot re-resolve。
- 读档后如果当前攻击上下文已过期，直接收口结束，不做“猜一把现在应该打什么”。

**Step 4: 把切换中断语义和你的需求对齐**

- 策略变化、换武器、结果失效后，只做“打断原动作，然后该干嘛干嘛，不干涉”。
- 不在 BDP 内部偷偷补一套“替你续上自动攻击”的特殊逻辑。

**Step 5: 补 smoke test 合同**

- `PostLoadAttackSessionRecoverySmokeTests.ps1` 断言 recovery 逻辑依赖发布版本，不回头现算。
- `AttackExecutionProjectionVersionSmokeTests.ps1` 断言版本过期时走统一结束策略。

**Step 6: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected:

- 编译通过。
- post-load / projection version 相关脚本通过。

**阶段停点：你可以立刻回游戏测**

- 战斗准备中切换 Trigger 或换武器，原动作是否立刻打断。
- 战斗进行中存档再读档，是否不会留下幽灵宿主、卡死 job、错误续射。
- 禁用状态变化后，角色是否按上层逻辑重新选取行为。

**这阶段如果出问题，优先怀疑**

- `AttackExecutionPostLoadRecovery.cs`
- `JobDriver_BdpRangedAttackExecution.cs`
- `JobDriver_BdpMeleeAttackExecution.cs`
- `TriggerRuntimeCoordinator.cs`

---

### Task 5: 删除旧语义并完成最终性能验收

**Files:**
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Delete: `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- Delete: 任何仅服务旧“读时现算”语义的辅助代码
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`

**目标**

- 把施工过程里残留的旧读口、旧 resolver、旧缓存彻底删干净。
- 最终交付的不是“新旧两套都能跑”，而是只有目标态。

**Step 1: 删除旧读时现算入口**

- `TryGetSelectedResult(...)`
- 面向普通消费者的 `BuildSelectedSnapshot(pawn)`
- 任何给 AttackExecution / targeting / UI 用的“顺手再算一次”入口

**Step 2: 删除旧 resolved request 语义**

- 如果阶段 2 还留下兼容壳，此阶段全部删除。
- 最终只保留 `AttackExecutionPreparedContext`。

**Step 3: 删除无意义缓存**

- 删除 `AttackExecutionTargetingSource` 中只为补救旧读时现算而存在的缓存逻辑。
- 如果还保留某些缓存，必须证明它只是在已发布 projection 上做只读浅缓存。

**Step 4: 压缩 smoke tests 到最终合同**

- `TriggerSingleTruthSmokeTests.ps1`
- `FormalHostVerbSmokeTests.ps1`
- `RangedProtocolBoundarySmokeTests.ps1`
- `PostLoadAttackSessionRecoverySmokeTests.ps1`
- `AutoAttackSeparationSmokeTests.ps1`
- `AttackExecutionProjectionVersionSmokeTests.ps1`
- `ExpressionPublishedProjectionSmokeTests.ps1`
- 这些脚本共同覆盖最终目标态，不再保留旧语义断言。

**Step 5: 做最终构建与全量脚本验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 全量编译通过。
- 全量 smoke test 通过。

**Step 6: 做最终游戏实测**

- 单 Pawn 高频开关 gizmo / tooltip。
- 多 Pawn 连续交火。
- 战斗中切换、卸下、读档。
- 观察是否还有：
- 明显 UI 卡顿
- 切换后旧动作残留
- 读档后幽灵宿主
- 自动攻击选错宿主

**阶段停点：你可以立刻回游戏测**

- 这就是最终交付停点。
- 如果这里稳定，后面就可以再单独立项接 Trion invalidation hook、统一因子数值口，不需要重开这次性能重构。

**这阶段如果出问题，优先怀疑**

- 是否还有旧读时现算入口没删净。
- 是否有消费者绕过 coordinator 自己去拿表达。
- 是否有 post-load / auto-attack 还偷偷保留旧路径。

---

## 5. 实施顺序建议

推荐严格按阶段 1 -> 2 -> 3 -> 4 -> 5 执行，不建议并行切多个热区。

理由：

- 阶段 1 先把 owner 定死，不然后面每切一个消费者都要重新判断“到底谁是真值”。
- 阶段 2 是收益最大的性能刀，优先级最高。
- 阶段 3 再处理 UI，可以避免战斗链和 UI 链一起改导致定位困难。
- 阶段 4 是行为正确性收口，必须建立在前面两个读路径已经切完之上。
- 阶段 5 才删旧入口，能降低施工期定位成本，但不会把兼容层带到最终交付。

---

## 6. 本计划的暂停汇报模板

每完成一个阶段，汇报只需要固定回答四件事：

1. 本阶段切掉了哪些旧热路径。
2. 本阶段改动会直接影响哪些游戏内行为。
3. 你现在应该回游戏重点测哪 3 到 5 项。
4. 如果出现异常，最可能是哪个模块出了问题。

这样你可以快速回游戏实测，不需要重新读一遍代码。

