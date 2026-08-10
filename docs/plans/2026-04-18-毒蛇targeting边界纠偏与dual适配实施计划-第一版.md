# 毒蛇 Targeting 边界纠偏与 Dual 适配 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 以最小而干净的架构纠偏方式，同时修正“非法第一个锚点导致视觉/会话异常消失”和“毒蛇错误退回为射手到最终目标直射语义”两类问题，并保证 dual 在必要 LOS / 非必要 LOS 三种组合下都按既定需求工作。

**Architecture:** 不整版回退，保留已经修对的“非法候选点不应让当前目标失效”方向；对最近修错的 targeting 宿主边界做选择性回退与重写。宿主层只负责原版 `Targeter` 适配与交互流转，不再替毒蛇做业务裁定；dual 层只裁定“是否必须做射手到真实目标的必要 LOS”；毒蛇模块继续独占自己的分段相邻点合法性判断。

**Tech Stack:** C# / RimWorld 1.6 / BDP AttackExecution + TargetingProtocol / PowerShell smoke tests / `dotnet msbuild`

---

### Task 1: 锁定两类 BUG 与 dual 三种组合的红测

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchSegmentLosBoundarySmokeTests.ps1`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchFirstAnchorContinuitySmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchTargetingContinuitySmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DualRangedNecessaryLosSemanticsSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DualRangedManualTargetingLegalitySmokeTests.ps1`

**Step 1: Write the failing tests**

补 5 组结构性断言：

```powershell
# 非法第一个锚点只拒绝输入，不应清空当前目标
$targetingSourceText -notmatch 'public bool CanHitTarget\(LocalTargetInfo target\)[\s\S]*TryEvaluateCurrentTargetLegality'
$targetingSourceText -notmatch 'public bool CanHitTarget\(LocalTargetInfo target\)[\s\S]*context\.Verb\.CanHitTarget\(target\)'

# 毒蛇最终目标确认不能回落为“射手->最终目标直射”
$pathLatchText -match 'TryValidateSegmentCandidate\(record,\s*state,\s*selectedTarget,\s*out rejectReason\)'
$pathLatchText -notmatch 'TryValidateFinalTarget\([\s\S]*Verb\.CanHitTarget'

# dual 三种组合按必要 LOS 语义裁定，而不是按模块内部 LOS 裁定
$dualText -match 'RequiresDirectTargetLineOfSight'
$dualText -notmatch 'non_necessary_side[\s\S]*sourceVerb\.CanHitTarget'
```

**Step 2: Run tests to verify they fail**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchTargetingContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchSegmentLosBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchFirstAnchorContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedNecessaryLosSemanticsSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedManualTargetingLegalitySmokeTests.ps1'
```

Expected: FAIL，明确卡在当前 `CanHitTarget(...)` / `ValidateTarget(...)` / dual side legality 回落点。

**Step 3: Keep the tests narrow**

- 测试只锁边界，不锁具体实现写法。
- 不把 `PathLatchModule` 业务词硬塞进 dual / targeting 公共层测试名义里。

**Step 4: Re-run after each task**

- 后续每完成一组改动，都先回跑对应 smoke tests，再继续下一组。

### Task 2: 收回宿主层越权，修正 `CanHitTarget` / `ValidateTarget` 分工

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchTargetingContinuitySmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchFirstAnchorContinuitySmokeTests.ps1`

**Step 1: Write the failing test**

锁定以下分工：

```powershell
# CanHitTarget 只服务“不要让 Targeter 清掉当前目标”
$targetingSourceText -notmatch 'public bool CanHitTarget\(LocalTargetInfo target\)[\s\S]*context\.Verb\.CanHitTarget\(target\)'

# ValidateTarget 不再直接承载毒蛇业务合法性
$targetingSourceText -notmatch 'public bool ValidateTarget\(LocalTargetInfo target, bool showMessages = true\)[\s\S]*context\.Verb\.ValidateTarget\(target,\s*showMessages\)'

# OnGUI 继续只做视觉反馈
$targetingSourceText -match 'TryEvaluateCurrentTargetLegality\(context,\s*target,\s*false,\s*out bool currentTargetIsLegal\)'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchTargetingContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchFirstAnchorContinuitySmokeTests.ps1'
```

**Step 3: Write minimal implementation**

在 `AttackExecutionTargetingSource.cs` 中：

1. `CanHitTarget(...)`
   - 不再消费模块业务合法性去清目标
   - 不再直接回落到 `context.Verb.CanHitTarget(target)`
   - 改为仅返回“当前目标是否仍允许进入交互表面”的中性真值
2. `ValidateTarget(...)`
   - 不再直接回落到 `context.Verb.ValidateTarget(target, showMessages)`
   - 仅保证点击能进入 `OrderForceTarget -> interactionDriver -> BuildConfirmRecord`
3. `OnGUI(...)`
   - 继续用 `TryEvaluateCurrentTargetLegality(...)` 做禁止图标
   - 不再让“当前候选非法”通过原版 `Targeter` 语义清空目标

**Step 4: Run tests to verify they pass**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchTargetingContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchFirstAnchorContinuitySmokeTests.ps1'
```

Expected: PASS

### Task 3: 保留毒蛇模块的分段相邻点语义，不再允许宿主回灌直射语义

**Files:**
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/RangedModules/Samples/PathLatchModule.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchSegmentLosBoundarySmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ViperPathLatchBoundarySmokeTests.ps1`

**Step 1: Write the failing test**

锁定：

```powershell
$pathLatchText -match 'ResolveCurrentSegmentOriginCell'
$pathLatchText -match 'TargetingSegmentLegalityRequest\.FromRecord\(record,\s*originCell,\s*target'
$pathLatchText -notmatch 'TryValidateFinalTarget\([\s\S]*context\.Verb'
$pathLatchText -notmatch 'TryResolvePreviewTargetRejectReason\([\s\S]*caster\.Position[\s\S]*record\.Target'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchSegmentLosBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchBoundarySmokeTests.ps1'
```

**Step 3: Write minimal implementation**

- 保持 `PathLatchModule` 的正式确认逻辑仍然是：
  - `TryValidateSegmentCandidate(...)`
  - `TryValidateFinalTarget(...)`
- 若宿主层剥离后出现依赖缺口，只补中性 bridging，不往毒蛇模块里重新塞原版 `Verb` 直射裁定
- 预览阶段继续只按：
  - 非法锚点格
  - 当前相邻段是否合法
  来画红线

**Step 4: Run tests to verify they pass**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchSegmentLosBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchLegacyVisualBaselineSmokeTests.ps1'
```

Expected: PASS

### Task 4: 重新收口 dual 的必要 LOS 裁定，只让 dual 管自己该管的事

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/AttackExecution/GroupedAttackExecutionTargetingSource.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DualRangedNecessaryLosSemanticsSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DualRangedManualTargetingLegalitySmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DualRangedLosPruningSmokeTests.ps1`

**Step 1: Write the failing test**

锁定 3 种 dual 相关组合：

```powershell
# 必要 + 必要
$dualText -match 'requires_direct_target_los_both'

# 必要 + 非必要
$dualText -match 'necessary_side_pruned_non_necessary_side_survives'

# 非必要 + 非必要
$dualText -match 'dual_layer_does_not_block_non_necessary_sides'
```

并补结构断言：

```powershell
$targetingSourceText -notmatch 'if \(resolvedSpec == null \|\| !resolvedSpec\.RequiresDirectTargetLineOfSight\)[\s\S]*sourceVerb\.CanHitTarget'
$groupedText -notmatch 'OrderForceTarget\(LocalTargetInfo target\)[\s\S]*source\.ValidateTarget\(target,\s*false\)'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedNecessaryLosSemanticsSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedManualTargetingLegalitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedLosPruningSmokeTests.ps1'
```

**Step 3: Write minimal implementation**

1. `TryEvaluateDualWeaponTargetLegality(...)`
   - 只裁定 per-side 的“必要直射”
2. `EvaluateDualWeaponSideTargetLegality(...)`
   - 必要直射侧：按自己的 formal host 判定
   - 非必要直射侧：不在 dual 层调用 `sourceVerb.CanHitTarget/ValidateTarget`
   - 直接放行给各自模块/正式确认链
3. `GroupedAttackExecutionTargetingSource.OrderForceTarget(...)`
   - 不再用错误的 `source.ValidateTarget(...)` 预筛后才下单
   - 改成按新的中性边界与正式确认结果决定是否派发

**Step 4: Run tests to verify they pass**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedNecessaryLosSemanticsSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedManualTargetingLegalitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedLosPruningSmokeTests.ps1'
```

Expected: PASS

### Task 5: 做最小回归验证，确认“保留已修对部分 + 修正修错部分”

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/docs/01-决策记录/2026-04-18-毒蛇targeting边界纠偏记录-第一版.md`

**Step 1: Run targeted tests**

Run:

```powershell
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchTargetingContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchFirstAnchorContinuitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchSegmentLosBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchBoundarySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ViperPathLatchLegacyVisualBaselineSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedNecessaryLosSemanticsSmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedManualTargetingLegalitySmokeTests.ps1'
& '.\模组工程\BorderDefenseProtocol\Source\BDP.Tests\DualRangedLosPruningSmokeTests.ps1'
dotnet msbuild '.\模组工程\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\BDP.DevHarness.csproj'
```

**Step 2: Write concise record**

记录以下 4 点：

- 哪些修复被保留（非法锚点不再让目标失效）
- 哪些改动被局部回退（宿主层错误回落到 `Verb.CanHitTarget/ValidateTarget`）
- 为什么这次属于“架构纠正”而不是“毒蛇补丁”
- dual 三种必要 LOS 组合最终各由哪一层负责

**Step 3: Stop if any regression appears**

- 若单毒蛇 + Pawn 目标仍有未解释表象，不继续猜测
- 仅补最小诊断日志到：
  - `AttackExecutionTargetingSource.CanHitTarget(...)`
  - `AttackExecutionTargetingSource.ValidateTarget(...)`
  - `AttackExecutionTargetingSource.OrderForceTarget(...)`
  - `BuildConfirmRecord(...)`
  - `PathLatchModule.IConfirmStageModule.Contribute(...)`

