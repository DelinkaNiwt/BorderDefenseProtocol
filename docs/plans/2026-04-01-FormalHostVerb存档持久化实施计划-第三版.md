# FormalHostVerb Persistence v3 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让 BDP formal host 攻击会话在不污染原版攻击入口的前提下，像原版武器 verb 一样跨存档连续。

**Architecture:** 继续使用 v3 设计确定的分层：表达层给结果，fixed formal host 壳作为唯一跨档宿主，执行层按需重建派生 plan。实现上只扩写现有 `TriggerBodyVerbHostManager`、`CompTriggerBody.Lifecycle`、`BdpVerb_Shoot` 和 formal host verb，不新增新的“会话快照体系”或“持久化中间层”。

**Tech Stack:** RimWorld 1.6 C# / Verse Scribe 存档系统 / Harmony / PowerShell smoke tests / `dotnet msbuild`

---

## Plan Rules

- 只让 formal host 壳进入存档树，不让具体芯片攻击 verb 回流为正式宿主。
- 只持久化最小会话真值：壳对象、`HostResultId`、burst cursor。
- 不持久化整包表达快照、整包 emission plan、整套执行服务对象。
- recovery 只做 safety net，不再承担“正常恢复”主逻辑。
- 每完成一个任务就跑对应最小验证，避免一次改太大。

## Task 1: Rewrite the smoke-test target to v3

**Files:**
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Modify: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Reference: `docs/04-架构评估/2026-04-01/2026-04-01-FormalHostVerb存档持久化设计分析-第三版.md`

**Step 1: Write the failing test expectations**

在 `PostLoadAttackSessionRecoverySmokeTests.ps1` 里把旧的“save-time projection / load-time cleanup”预期改成 v3 预期：

- 不再要求 `AttackExecutionSaveProjection.cs`
- 不再要求 `Pawn.ExposeData` 在 `Saving` 阶段插手
- 改为要求 formal host 壳 deep-save
- 改为要求 recovery 只处理失效旧会话

同时在 `FormalHostVerbSmokeTests.ps1` 增加 v3 架构断言：

- manager 暴露 verb shell 序列化入口
- lifecycle 接入 shell 存读档
- formal host verb 存在首次 post-load 重绑保态逻辑
- `BdpVerb_Shoot` 暴露 `ExposeData()` 并持久化 `HostResultId` / cursor

示例断言片段：

```powershell
Assert-True (
    $hostManagerText -match 'ExposeVerbShells'
) 'TriggerBodyVerbHostManager must expose formal host shells to the save tree.'

Assert-True (
    $shootVerbText -match 'public override void ExposeData\(\)'
) 'BdpVerb_Shoot must persist host-result identity and burst cursor.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- 至少一条断言失败
- 失败点应明确指向“尚未实现 shell deep-save / ExposeData / 保态重绑”

**Step 3: Write minimal implementation placeholder updates**

如果测试脚本中需要先删除旧断言噪音，再加入 v3 新断言，则只做测试侧最小调整，不提前改生产代码。

**Step 4: Run test to verify it now fails for the right reason**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- 失败原因聚焦到 v3 目标缺口
- 不再被旧路线断言干扰

**Step 5: Commit**

```powershell
git add Source/BDP.Tests/FormalHostVerbSmokeTests.ps1 Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
git commit -m "test: retarget formal host persistence smokes to v3"
```

### Task 2: Deep-save formal host shells without adding new architecture

**Files:**
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Reference: `Source/BDP/Core/VerbHosting/BdpFormalVerbBinding.cs`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**Step 1: Write the failing test**

在 `FormalHostVerbSmokeTests.ps1` 中加入最小断言，要求：

- `TriggerBodyVerbHostManager` 存在 `ExposeVerbShells()` 或等价明确入口
- 使用 `Scribe_Collections.Look(..., LookMode.Deep)` 保存远程壳和近战壳
- `CompTriggerBody.Lifecycle.PostExposeData()` 在 `LoadingVars` / `PostLoadInit` 路径上接入 manager 的存读档入口

示例断言：

```powershell
Assert-True (
    $hostManagerText -match 'Scribe_Collections\.Look'
) 'TriggerBodyVerbHostManager must deep-save formal host shell collections.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- FAIL，提示 manager/lifecycle 尚未暴露 shell deep-save 链

**Step 3: Write minimal implementation**

在 `TriggerBodyVerbHostManager` 中新增最小持久化能力：

- 私有的远程壳列表缓存
- 私有的近战壳列表缓存
- `ExposeVerbShells()`：按 `CompTriggerBody.FormalHostSlots` 固定顺序 deep-save
- `RestoreShellsPostLoad()`：按固定顺序把已恢复壳重新挂回 `bindings`

在 `CompTriggerBody.Lifecycle` 中做最小接线：

- `LoadingVars` 前也确保 `EnsureInternalState()`
- `PostExposeData()` 主区段接入 `verbHostManager.ExposeVerbShells()`
- `PostLoadInit` 中在 `RefreshProjectedOutputs()` 前调用 `verbHostManager.RestoreShellsPostLoad()`

禁止新增：

- 新的序列化 DTO
- 新的宿主仓储类
- 新的恢复编排服务

**Step 4: Run test to verify it passes**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

**Step 5: Commit**

```powershell
git add Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs Source/BDP.Tests/FormalHostVerbSmokeTests.ps1
git commit -m "feat: deep-save formal host shells"
```

### Task 3: Persist minimal BDP session truth in `BdpVerb_Shoot`

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Test: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`

**Step 1: Write the failing test**

在 `FormalHostVerbSmokeTests.ps1` 增加断言，要求：

- `BdpVerb_Shoot` 覆盖 `ExposeData()`
- `HostResultId` 通过 backing field 持久化
- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

示例断言：

```powershell
Assert-True (
    $shootVerbText -match 'Scribe_Values\.Look\(ref hostResultId'
) 'BdpVerb_Shoot must persist HostResultId.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- FAIL，指出 `BdpVerb_Shoot` 尚未持久化最小会话真值

**Step 3: Write minimal implementation**

在 `BdpVerb_Shoot` 中：

- 把 `HostResultId` 从自动属性改为 backing field
- 新增 `public override void ExposeData()`
- `base.ExposeData()` 后追加最小 `Scribe_Values.Look(...)`
- 只保存：
  - `hostResultId`
  - `pendingWindowIndex`
  - `pendingWindowProjectilePlanIndex`
  - 可选 `pendingEmissionConsumedCount`

不要保存：

- `pendingVerbEmissionPlan`
- `pendingEmissionWindows`
- `SemanticContext`

**Step 4: Run tests to verify they pass**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

Expected:

- `FormalHostVerbSmokeTests PASS`
- `DefaultBurstParitySmokeTests PASS`

**Step 5: Commit**

```powershell
git add Source/BDP/Core/Verbs/BdpVerb_Shoot.cs Source/BDP.Tests/FormalHostVerbSmokeTests.ps1
git commit -m "feat: persist formal host session identity"
```

### Task 4: Add first post-load rebind preserve-state logic

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**Step 1: Write the failing test**

在 `FormalHostVerbSmokeTests.ps1` 中增加断言，要求：

- formal host 壳存在一次性保态标记
- `RestoreShellsPostLoad()` 会为已加载壳打标记
- `SyncFormalBinding()` 存在“同槽位、同结果、同会话时不 Reset”分支

示例断言：

```powershell
Assert-True (
    $formalHostShootText -match 'preserveLoadedStateOnce'
) 'Formal host ranged shell must preserve loaded state across the first post-load rebind.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- FAIL，指出 formal host 壳仍会在首次刷新时误 `Reset()`

**Step 3: Write minimal implementation**

在 `BdpVerb_FormalHostShoot` / `BdpVerb_FormalHostMelee` 中：

- 新增一次性标记，例如 `preserveLoadedStateOnce`
- 提供最小内部方法，例如 `MarkPreserveLoadedStateOnce()`
- 调整 `SyncFormalBinding()` / `ShouldResetForBindingChange()`：
  - 同槽位、同 `HostResultId`、同会话的首次重绑只重注入派生表面
  - 不调用 `Reset()`

在 manager 的 `RestoreShellsPostLoad()` 中：

- 只给“从存档恢复回来的壳”打保态标记

不要新增新的“会话比较服务”或“重绑策略对象”。  
比较逻辑直接留在 formal host verb 内部即可。

**Step 4: Run test to verify it passes**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

**Step 5: Commit**

```powershell
git add Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs Source/BDP.Tests/FormalHostVerbSmokeTests.ps1
git commit -m "feat: preserve loaded formal host state on first rebind"
```

### Task 5: Resume warmup and burst by lazy plan rebuild

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Reference: `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolService.cs`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Test: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`

**Step 1: Write the failing test**

在 `PostLoadAttackSessionRecoverySmokeTests.ps1` 中增加 v3 续接断言：

- `WarmupComplete()` 允许在无 pending plan 但有合法 `HostResultId` 时惰性补 plan
- `TryCastShot()` 允许在 burst 继续时惰性补 plan
- 补 plan 后会把消费位置推进到 cursor
- 不要求 save-time projection

示例断言：

```powershell
Assert-True (
    $shootVerbText -match 'TryPreparePendingEmission'
) 'BdpVerb_Shoot must lazily rebuild emission plans after load.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- FAIL，指出当前仍缺少 lazy rebuild / cursor resume

**Step 3: Write minimal implementation**

在 `BdpVerb_Shoot` 中：

- 保留现有 `WarmupComplete()` 语义，不改成 `base.WarmupComplete()`
- 在 `WarmupComplete()` 入口前补“无 plan 时惰性准备”
- 在 `TryCastShot()` 入口前补“burst 继续时惰性准备”
- plan 重建后按 cursor 快进
- 如果当前表达结果与 cursor 不兼容，直接终止旧会话，不强续

在 `JobDriver_BdpRangedAttackExecution` 中：

- 只做最小兼容检查，确保读档续接场景仍走正式 execution job
- 不新增新的驱动层状态机

**Step 4: Run tests to verify they pass**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

Expected:

- `PostLoadAttackSessionRecoverySmokeTests PASS`
- `DefaultBurstParitySmokeTests PASS`

**Step 5: Commit**

```powershell
git add Source/BDP/Core/Verbs/BdpVerb_Shoot.cs Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
git commit -m "feat: resume formal host warmup and burst after load"
```

### Task 6: Shrink post-load recovery into a safety net

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Modify: `Source/BDP/Patches/Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`

**Step 1: Write the failing test**

在 `PostLoadAttackSessionRecoverySmokeTests.ps1` 中加入断言：

- 不再要求 `Saving` 分支 save projection
- recovery 仍存在
- recovery 只在 `PostLoadInit` 调用
- recovery 只终止无效旧会话，不一刀切取消所有 formal host 忙姿态

示例断言：

```powershell
Assert-True (
    $pawnPatchText -notmatch 'LoadSaveMode\.Saving'
) 'Pawn.ExposeData post-load recovery patch must stop using save-time projection.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- FAIL，指出 patch/recovery 仍含旧路线逻辑

**Step 3: Write minimal implementation**

在 `AttackExecutionPostLoadRecovery` 中：

- 保留 helper 作为兜底层
- 改为只终止：
  - verb 丢失
  - `HostResultId` 失效
  - 当前 binding 已切换
  - cursor 非法

在 `Patch_Pawn_ExposeData_PostLoadAttackRecovery` 中：

- 只保留 `PostLoadInit` 路径
- 去掉 save-time projection 相关逻辑

如果 `AttackExecutionSaveProjection.cs` 仍残留且完全不再使用，则在此任务中删除。

**Step 4: Run test to verify it passes**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- PASS

**Step 5: Commit**

```powershell
git add Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs Source/BDP/Patches/Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
git commit -m "refactor: narrow post-load attack recovery to safety net"
```

### Task 7: Full build and smoke verification

**Files:**
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Test: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`
- Test: `Source/BDP.Tests/AutoAttackSeparationSmokeTests.ps1`

**Step 1: Run build**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- Build succeeded

**Step 2: Run smoke tests**

Run:

```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
```

Expected:

- 全部输出 `PASS`

**Step 3: Manual verification checklist**

在游戏内最少验证 4 个场景：

1. 触发体本体攻击按钮仍存在。
2. 自动攻击继续走 BDP 默认主攻击，而不是原版本体攻击。
3. 暖机中存档，读档后继续暖机并开火。
4. burst 中段存档，读档后从剩余部分继续，不从头重复。

**Step 4: Commit**

```powershell
git add Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs Source/BDP/Core/Verbs/BdpVerb_Shoot.cs Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs Source/BDP/Patches/Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs Source/BDP.Tests/FormalHostVerbSmokeTests.ps1 Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
git commit -m "feat: persist and resume formal host attack sessions"
```

## Out of Scope

- 不新增新的“BDP 攻击会话快照”总对象。
- 不把具体芯片攻击 verb 池重新做成长期持久化主链。
- 不改动原版触发体本体攻击的保留逻辑。
- 不为这次需求重构表达系统或远程协议整体结构。
- 不追求保存所有中间运行时细节，只追求最小真值下的正确续接。

## Done Criteria

- formal host 壳进入存档树，存档时不再出现 `not deep-saved`
- 读档后 `Job.verbToUse` / `Stance_Warmup.verb` 能重新解析到 formal host 壳
- 第一次 post-load rebind 不误清 warmup / burst 状态
- `HostResultId` + burst cursor 足以恢复正确剩余攻击
- recovery 只做 safety net
- 自动攻击与原版攻击入口继续彻底分家

Plan complete and saved to `docs/plans/2026-04-01-FormalHostVerb存档持久化实施计划-第三版.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

Which approach?
