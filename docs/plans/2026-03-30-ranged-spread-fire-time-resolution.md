# Ranged Spread Fire-Time Resolution Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Keep ranged origin spread gated by explicit `SpreadRadius` declaration while resolving the actual spread offset at fire time so movement or aim timing cannot freeze an outdated world offset.

**Architecture:** Split spread into two layers. Planning/runtime assembly carries only declaration data such as spread radius and sequence index/count. `BdpVerb_Shoot` resolves the actual world-space offset at emit time from the current source/target geometry, using a zero-mean spread function so a volley's center does not drift.

**Tech Stack:** C#, RimWorld verb/projectile runtime, PowerShell smoke tests.

---

### Task 1: Add regression coverage for fire-time spread resolution

**Files:**
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`

**Step 1: Write the failing test**

Add assertions that:
- `AttackExecutionService.Stages` no longer calls `ResolveEmitOriginOffset(...)` when building ranged emits.
- `AttackExecutionEmit`/`FireEmitRecord`/`ProjectileInitPlan` carry spread declaration fields instead of only frozen world offsets.
- `BdpVerb_Shoot` resolves spread at fire time from the current launch base.

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1`
Expected: FAIL on old frozen-spread behavior assertions.

### Task 2: Move spread declaration through the ranged pipeline

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionEmit.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/FireEmitRecord.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/ProjectileInitPlan.cs`

**Step 1: Write minimal implementation**

Carry:
- `OriginSpreadRadius`
- `OriginSpreadSequenceIndex`
- `OriginSpreadSequenceCount`

through the pipeline, and stop freezing spread into `OriginOffsetWorld` during planning.

**Step 2: Run focused smoke test**

Run: `powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1`
Expected: PASS

### Task 3: Resolve zero-mean spread at emit time

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: Write minimal implementation**

Add a helper that resolves spread from the current fire-time source/target geometry only when `OriginSpreadRadius > 0`, and ensure the spread pattern is zero-mean.

**Step 2: Re-run focused verification**

Run: `powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1`
Expected: PASS

### Task 4: Build verification

**Files:**
- Modify: none

**Step 1: Run build**

Run: `dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: Build succeeds.

