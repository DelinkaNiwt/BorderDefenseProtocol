# Ranged Trion Consumption Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Wire Trion consumption into BDP ranged attack execution so that ranged actions check affordability before warmup, re-check and atomically consume before the first emission of the round, and charge exactly once per ranged round rather than once per projectile.

**Architecture:** Reuse `FormalExpressionResult.Trion` as the source-level declaration, map `UseCost` and `MinimumRequired` into ranged protocol prepare output, and keep Trion bookkeeping on the existing `ITrionCommands` surface. Add only a thin ranged Trion gate around the existing `BdpVerb_Shoot` lifecycle so protocol derives cost, the gate evaluates/commits it, and the verb session enforces the two-stage timing.

**Tech Stack:** C#, RimWorld verb lifecycle, existing BDP ranged protocol pipeline, PowerShell smoke tests

---

## Scope Guard

This plan implements only ranged attack Trion consumption.

It explicitly does **not** include:

- melee Trion-per-use integration
- ability or hediff resource integration
- projectile-level per-shot charging
- CombatBodySession-based ranged charge coordination
- any new generic resource transaction facade

### Task 1: Lock the desired boundary and timing with smoke tests

**Files:**
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Create: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- Reference: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/PrepareRecord.cs`
- Reference: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: Write the failing smoke test**

Create `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1` asserting at least:

- ranged prepare output carries formal Trion round-cost data
- `BdpVerb_Shoot` performs a pre-warmup affordability check
- `BdpVerb_Shoot` performs a pre-emission final affordability/consume step
- `BdpVerb_Shoot` tracks that the current round has already paid
- ranged code does **not** consume Trion per projectile launch

Example assertion shape:

```powershell
Assert (
    $shootVerbText -match 'TryEnsureRoundTrionAdmission'
) 'BdpVerb_Shoot must gate ranged warmup on Trion admission.'

Assert (
    $shootVerbText -match 'TryCommitRoundTrionBeforeFirstEmission'
) 'BdpVerb_Shoot must atomically re-check and consume Trion before first emission.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: FAIL until the new smoke test exists and the required boundaries are implemented.

**Step 3: Extend the existing boundary test**

Update `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1` to lock:

- prepare-stage round resource fields remain protocol-owned
- ranged Trion integration stays out of projectile and flight protocol layers
- ranged Trion integration stays off `CombatBodySession`

**Step 4: Run the smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: FAIL first, then PASS after implementation.

**Step 5: Commit**

```bash
git add Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1 Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1
git commit -m "test: lock ranged trion consumption boundaries"
```

### Task 2: Extend ranged protocol prepare output with round-cost semantics

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/PrepareRecord.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/PrepareContribution.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/PrepareStageService.cs`
- Create: `Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/RangedTrionPrepareModule.cs`
- Reference: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Reference: `Source/BDP/Core/Expressions/Config/ExpressionSourceTrionConfig.cs`

**Step 1: Write the failing smoke assertions**

Add assertions requiring:

- `PrepareRecord` exposes round-cost and minimum-required fields explicitly
- `RangedTrionPrepareModule` exists
- the module reads from `entry.SourceResult.Trion`
- the module maps `UseCost` to round cost
- the module maps `MinimumRequired` to the gate threshold

Example target shape:

```csharp
public float ResourceCost { get; set; }
public float MinimumRequired { get; set; }
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: FAIL because the explicit fields and module do not yet exist.

**Step 3: Implement the minimal protocol changes**

- extend `PrepareRecord` with an explicit minimum-required field for round admission
- extend `PrepareContribution` with matching contribution fields if needed
- create `RangedTrionPrepareModule`
- in that module:
  - read `entry.SourceResult.Trion`
  - add `UseCost` into `AddedResourceCost`
  - carry `MinimumRequired`
  - do not perform any Trion I/O
  - do not display messages

Keep protocol responsibilities limited to deriving round semantics.

**Step 4: Register the module at the protocol assembly boundary**

Modify the ranged protocol assembly access so `RangedTrionPrepareModule` is included in the prepare module list without changing unrelated stage responsibilities.

**Step 5: Run smoke tests to verify they pass**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected: PASS for protocol-boundary assertions.

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution/RangedProtocol/Model/PrepareRecord.cs Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/PrepareContribution.cs Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/PrepareStageService.cs Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/RangedTrionPrepareModule.cs Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1 Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1
git commit -m "feat: derive ranged trion round cost in prepare stage"
```

### Task 3: Add a thin ranged Trion gate for admission and first-emission commit

**Files:**
- Create: `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackTrionGate.cs`
- Create: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/RangedAttackTrionGateResult.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolSurfaceAccess.cs`
- Reference: `Source/BDP/Core/Trion/ITrionCommands.cs`
- Reference: `Source/BDP/Core/Trion/TrionSurfaceAccess.cs`

**Step 1: Write the failing smoke assertions**

Add assertions requiring:

- a thin gate service exists for ranged Trion timing
- the gate resolves `ITrionCommands` through `TrionSurfaceAccess`
- the gate exposes separate admission and commit entry points
- the gate returns a structured result instead of leaking magic strings everywhere

Example target shape:

```csharp
internal bool TryAdmitWarmup(...)
internal bool TryCommitBeforeFirstEmission(...)
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: FAIL until the gate objects exist.

**Step 3: Implement the minimal gate**

Create `RangedAttackTrionGate.cs` that:

- resolves `ITrionCommands` via `TrionSurfaceAccess.ResolveCommands(pawn)`
- checks `MinimumRequired` and `ResourceCost` for warmup admission
- re-checks before first emission
- calls `TryConsume(ResourceCost)` only at final commit time
- returns a structured result for caller-side prompt handling

Do not:

- move Trion truth into the gate
- move protocol derivation into the gate
- introduce any CombatBodySession dependency

**Step 4: Keep gate wiring local to the ranged protocol surface**

Expose the gate via the ranged protocol surface assembly boundary so callers do not construct it ad hoc.

**Step 5: Run smoke tests to verify they pass**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: PASS for gate-presence and boundary assertions.

**Step 6: Commit**

```bash
git add Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackTrionGate.cs Source/BDP/Core/AttackExecution/RangedProtocol/Model/RangedAttackTrionGateResult.cs Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolSurfaceAccess.cs Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1
git commit -m "feat: add ranged trion admission gate"
```

### Task 4: Integrate the gate into `BdpVerb_Shoot` without per-projectile charging

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Reference: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`

**Step 1: Write the failing smoke assertions**

Add assertions requiring:

- `TryStartCastOn` calls the admission gate before entering warmup
- `TryCastShot` calls the final commit path before first emission
- `BdpVerb_Shoot` stores a per-round “already charged” flag
- the flag resets when the current emission plan is cleared or the round ends
- projectile launch helpers do not call `TryConsume`

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: FAIL until `BdpVerb_Shoot` is wired correctly.

**Step 3: Implement minimal verb-session integration**

In `BdpVerb_Shoot`:

- add per-round state such as:
  - required round cost
  - minimum required threshold
  - current round charged flag
- when binding/applying the current protocol result, cache the round Trion semantics
- before warmup begins, call the admission gate
- before the first emission of the round, call the commit gate
- after successful commit, mark the round as charged
- when the current round fully ends or the pending plan is cleared, reset the charged flag

**Step 4: Handle failure visibility**

When the gate reports insufficient Trion:

- reject entering the action or terminate the current round
- clear/stop the current pending emission plan when appropriate
- show a player-facing insufficient Trion message

Keep the message translation local to caller/UI-facing code, not inside `ITrionCommands`.

**Step 5: Keep projectile and flight layers untouched**

Do not modify:

- `Source/BDP/Core/Projectiles/BdpBullet.cs`
- `Source/BDP/Core/Projectiles/BdpExplosiveProjectile.cs`
- ranged flight protocol files

Charging remains round-based at verb-session level.

**Step 6: Run targeted smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected: PASS

**Step 7: Commit**

```bash
git add Source/BDP/Core/Verbs/BdpVerb_Shoot.cs Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1 Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1
git commit -m "feat: charge trion once per ranged round"
```

### Task 5: Verify end-to-end boundaries stay intact

**Files:**
- Verify: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- Verify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Verify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Verify: `Source/BDP.Tests/AttackExecutionLoggingSmokeTests.ps1`
- Verify: `Source/BDP/BDP.csproj`

**Step 1: Run the direct new smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
```

Expected: PASS

**Step 2: Run adjacent ranged boundary tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionLoggingSmokeTests.ps1'
```

Expected: PASS

**Step 3: Build the project**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; $env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected: BUILD SUCCESSFUL / zero compile errors

**Step 4: Commit**

```bash
git add Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1 Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1 Source/BDP/Core/AttackExecution/RangedProtocol/Prepare/RangedTrionPrepareModule.cs Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackTrionGate.cs Source/BDP/Core/AttackExecution/RangedProtocol/Model/RangedAttackTrionGateResult.cs Source/BDP/Core/Verbs/BdpVerb_Shoot.cs
git commit -m "feat: integrate ranged trion round consumption"
```

