# Melee Multi-Hit Step-Driven Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make BDP melee multi-hit execute as real step-driven combos with per-step interruption and `HitIntervalTicks` timing.

**Architecture:** Reuse the existing melee runtime-step expansion and keep formal-host session validity unchanged. Change only the melee execution driver so it consumes planned steps in order instead of behaving like an unbounded continuous melee loop.

**Tech Stack:** C#, RimWorld Verse job/verb system, PowerShell smoke tests, .NET Framework build via `dotnet msbuild`

---

### Task 1: Add the failing regression smoke test

**Files:**
- Create: `Source/BDP.Tests/MeleeMultiHitStepSchedulingSmokeTests.ps1`
- Test: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`
- Test: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`

**Step 1: Write the failing test**

Assert that:

- melee context no longer maps persistent attack orders to `int.MaxValue`
- melee job exposes explicit per-step scheduling state
- melee job consumes per-step interval timing instead of blindly chaining attacks

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeMultiHitStepSchedulingSmokeTests.ps1'
```

Expected: FAIL because the current melee context still returns `int.MaxValue` and the job has no explicit interval/step scheduling state.

### Task 2: Make melee required-step count finite

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`

**Step 1: Change required-count logic**

Remove the special-case that turns `ForceTargetOrder` and `AutoAttackOrder` into `int.MaxValue` for melee.

**Step 2: Keep driver selection logic intact**

Manual and auto melee should still route through the continuous job when needed for chase or multi-step progression, but the planned combo length must remain finite.

**Step 3: Run the new smoke test**

Expected: still FAIL, but now only on missing per-step interval scheduling in the melee job.

### Task 3: Implement step-driven melee scheduling

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`

**Step 1: Add minimal job state**

Track:

- current step index or completed cast count
- remaining ticks before the next step may fire

Persist both through `ExposeData()`.

**Step 2: Use the planned melee runtime steps**

Each successful melee attack should advance exactly one planned step.

**Step 3: Honor interval timing**

After a successful step, arm the next delay from the consumed step's `IntervalTicksAfter`.

**Step 4: Preserve interruption semantics**

Keep the existing checks for:

- invalid target
- downed target
- projection/session invalidation
- pathing / chase
- full-body busy stance

**Step 5: Run the new smoke test**

Expected: PASS

### Task 4: Verify no obvious regression in nearby contracts

**Files:**
- Test: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Test: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`

**Step 1: Run targeted smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeMultiHitStepSchedulingSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected: PASS

### Task 5: Build verification

**Files:**
- Test: `Source/BDP/BDP.csproj`

**Step 1: Run build**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; $env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected: Build succeeds with exit code 0.
