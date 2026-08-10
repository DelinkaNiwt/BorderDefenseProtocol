# Dual Weapon Live Muzzle Origin Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use `systematic-debugging` evidence first. Use `test-driven-development` before code edits. Execute this plan task-by-task. Do not read git history. Do not commit.

**Goal:** 让双武器投射物真实发射根点在开火当刻读取当前主/副侧视觉枪口，而不是在 warmup 前把视觉枪口冻结成旧的绝对世界坐标。

**Architecture:** `ProjectileInitPlan.AbsoluteOriginWorld` 保留为“显式模块覆盖”专用通道。视觉枪口路径只保留 `plan.ResultId`/`emit.SourceResultId` 身份，发射阶段通过 `TriggerVisualLaunchOriginResolver.ResolveLaunchRoot` 实时解析当前枪口。诊断日志必须区分“计划阶段视觉探测”与“显式绝对原点覆盖”。

**Tech Stack:** C# RimWorld Mod、Harmony、PowerShell smoke tests、`dotnet msbuild`。

---

### Task 1: 更新边界测试契约

**Files:**
- Modify: `Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`

**Step 1: 修改视觉边界测试**

在 `VisualPoseResolverBoundarySmokeTests.ps1` 中替换旧断言：

```powershell
Assert-True (
    ($projectileInitStageText -match 'HasAbsoluteOriginWorld') -and
    ($projectileInitStageText -match 'AbsoluteOriginWorld') -and
    ($projectileInitStageText -match 'SourceResultId')
) 'ProjectileInitStageService must prepare to freeze visual-driven absolute projectile origins by emit source result.'
```

改为断言：

```powershell
Assert-True (
    ($projectileInitStageText -match 'TryProbeVisualMuzzleOrigin') -and
    ($projectileInitStageText -match 'SourceResultId') -and
    ($projectileInitStageText -notmatch 'plan\.HasAbsoluteOriginWorld\s*=\s*true;\s*[\r\n\s]*plan\.AbsoluteOriginWorld\s*=\s*resolution\.RootOriginWorld')
) 'ProjectileInitStageService must probe visual muzzle by emit source result but must not freeze visual-driven origins into AbsoluteOriginWorld.'
```

**Step 2: 增加远程协议边界断言**

在 `RangedProtocolBoundarySmokeTests.ps1` 的 `ProjectileInit baseline must not pre-freeze...` 附近增加：

```powershell
Assert-True (
    $allStageServiceText -notmatch 'plan\.HasAbsoluteOriginWorld\s*=\s*true;\s*[\r\n\s]*plan\.AbsoluteOriginWorld\s*=\s*resolution\.RootOriginWorld'
) 'ProjectileInit visual muzzle path must not write visual-driven roots into AbsoluteOriginWorld.'
```

**Step 3: 先运行测试，确认当前代码失败**

Run:

```powershell
& '.\Source\BDP.Tests\VisualPoseResolverBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:
- `VisualPoseResolverBoundarySmokeTests.ps1` 失败，提示视觉枪口不能冻结成 `AbsoluteOriginWorld`。
- `RangedProtocolBoundarySmokeTests.ps1` 失败，提示视觉枪口路径仍写入 `AbsoluteOriginWorld`。

---

### Task 2: 停止视觉枪口冻结绝对坐标

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/ResolvedMuzzleAnchor.cs`

**Step 1: 重命名计划阶段视觉方法**

把 `TryApplyVisualAbsoluteOrigin(...)` 改名为 `TryProbeVisualMuzzleOrigin(...)`。

**Step 2: 改写方法注释**

使用中文注释明确：

```csharp
/// <summary>
/// 尝试在 ProjectileInit 阶段探测当前 emit 对应的视觉枪口。
/// 这里只做诊断和主副侧身份校验，不把视觉枪口写成绝对世界坐标。
/// 真正发射时必须按 plan.ResultId 在当前视觉姿态上实时解析枪口，避免 warmup 期间姿态变化导致旧坐标漂移。
/// </summary>
```

**Step 3: 删除视觉路径写入**

在该方法内删除：

```csharp
plan.HasAbsoluteOriginWorld = true;
plan.AbsoluteOriginWorld = resolution.RootOriginWorld;
```

方法成功解析视觉枪口时仍返回 `true`，用于日志记录。

**Step 4: 保留显式覆盖写入**

保留贡献模块路径：

```csharp
if (planContribution.HasOverrideOriginWorld)
{
    contributionOverrideApplied = true;
    contributionOverrideOriginWorld = planContribution.OverrideOriginWorld;
    plan.HasAbsoluteOriginWorld = true;
    plan.AbsoluteOriginWorld = planContribution.OverrideOriginWorld;
}
```

这条路径是当前唯一允许写 `AbsoluteOriginWorld` 的业务入口。

**Step 5: 修正枪口锚点注释**

把 `ResolvedMuzzleAnchor.cs` 中“可直接冻结为绝对发射原点”的说明改成“可在发射边界实时解析为发射根点”，避免后续维护者继续把视觉枪口当作计划期绝对坐标。

---

### Task 3: 调整 ProjectileInit 诊断语义

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionDiagnostics.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`

**Step 1: 改方法参数名**

把 `LogProjectileInitOriginSnapshot(...)` 的参数：

```csharp
bool visualFreezeApplied,
TriggerVisualLaunchOriginResolution visualFreezeResolution,
```

改为：

```csharp
bool visualMuzzleProbeResolved,
TriggerVisualLaunchOriginResolution visualMuzzleProbeResolution,
```

同步更新调用点。

**Step 2: 改日志字段名**

把日志字段：

```text
visualFreezeApplied
visualFreezeResultId
visualFreezeRootOriginWorld
visualFreezeSourceKind
visualFreezeFailureKind
visualFreezeProjectionVersion
visualFreezePoseSampleTick
overrideAfterVisualFreeze
```

改为：

```text
visualMuzzleProbeResolved
visualMuzzleProbeResultId
visualMuzzleProbeRootOriginWorld
visualMuzzleProbeSourceKind
visualMuzzleProbeFailureKind
visualMuzzleProbeProjectionVersion
visualMuzzleProbePoseSampleTick
overrideAfterVisualProbe
```

**Step 3: 保留最终绝对坐标字段**

继续输出：

```text
finalHasAbsoluteOriginWorld
finalAbsoluteOriginWorld
```

验收条件：当前双武器视觉枪口路径中 `finalHasAbsoluteOriginWorld=False`；只有显式 `OverrideOriginWorld` 模块路径才为 `True`。

---

### Task 4: 验证发射阶段实时枪口路径

**Files:**
- Read: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Read: `Source/BDP/Core/Trigger/Visual/TriggerVisualLaunchOriginResolver.cs`

**Step 1: 确认无需改发射主链**

确认 `BdpVerb_Shoot.TryEmitPlan` 仍调用：

```csharp
TriggerVisualLaunchOriginResolver.ResolveLaunchRoot(
    CasterPawn,
    plan.ResultId,
    plan.HasAbsoluteOriginWorld,
    plan.AbsoluteOriginWorld,
    drawPos);
```

**Step 2: 确认实时路径会生效**

当 Task 2 后视觉路径不再写 `HasAbsoluteOriginWorld`，`ResolveLaunchRoot` 会跳过 `FrozenPlanAbsolute`，进入：

```csharp
TryResolveVisualMuzzleRoot(pawn, sourceResultId, out TriggerVisualLaunchOriginResolution liveResolution)
```

验收条件：发射日志 `actualRootSourceKind=LiveVisualMuzzle`，且 `actualRootOriginWorld` 与 `liveVisualRootOriginWorld` 一致。

---

### Task 5: 运行验证

**Files:**
- Test: `Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Build: `Source/BDP/BDP.csproj`

**Step 1: 跑边界烟测**

Run:

```powershell
& '.\Source\BDP.Tests\VisualPoseResolverBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:
- 两个脚本输出 PASS。

**Step 2: 编译主模组**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj'
```

Expected:
- Build succeeded。

**Step 3: 游戏内验收日志**

重新进入同一类 dual 测试，重点看 `event=projectile_init_origin_snapshot` 和 `event=emit_origin_evidence`：

```text
projectile_init_origin_snapshot:
  visualMuzzleProbeResolved=True
  finalHasAbsoluteOriginWorld=False

emit_origin_evidence:
  actualRootSourceKind=LiveVisualMuzzle
  deltaActualRootVsLiveVisual=(0,0,0)
  deltaTheoreticalVsLiveVisual=(0,0,0)  // 当 OriginOffsetWorld=(0,0,0) 时
```

---

### Task 6: 不做事项

- 不删除 `ProjectileInitPlan.HasAbsoluteOriginWorld`。
- 不删除 `ProjectileInitPlan.AbsoluteOriginWorld`。
- 不删除 `ProjectileInitContribution.HasOverrideOriginWorld`。
- 不改散布算法。
- 不改贴图参数。
- 不读 git 历史。
- 不提交 commit。
