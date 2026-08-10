# Trion Formal Transaction Surface Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Lock BDP onto the existing Trion formal transaction surface as the only neutral resource transaction boundary, without introducing any new shared facade or any downstream caller integration.

**Architecture:** Reuse `ITrionCommands` and `TrionSurfaceAccess` as the formal transaction boundary. Implementation work is limited to documentation, boundary-guard comments where needed, and smoke tests that prevent future drift toward redundant shared facades or caller-specific coupling.

**Tech Stack:** C#, RimWorld mod formal surfaces, PowerShell smoke tests

---

## Scope Guard

This plan implements only neutral boundary hardening.

It explicitly does **not** include:

- any downstream caller integration
- any caller-specific lifecycle logic
- any caller-specific amount derivation
- any caller-specific key helper
- any new shared transaction facade
- any business behavior changes

### Task 1: Lock the formal transaction surface contract

**Files:**
- Modify: `Source/BDP/Core/Trion/ITrionCommands.cs`
- Modify: `Source/BDP/Core/Trion/TrionSurfaceAccess.cs`
- Test: `Source/BDP.Tests/TrionFormalTransactionSurfaceContractsSmokeTests.ps1`

**Step 1: Write the failing smoke test**

Create `Source/BDP.Tests/TrionFormalTransactionSurfaceContractsSmokeTests.ps1` asserting:

- `ITrionCommands` exists
- `ITrionCommands` declares:
  - `bool TryConsume(float cost);`
  - `void RegisterDrain(string key, float perDay);`
  - `void UnregisterDrain(string key);`
- `TrionSurfaceAccess.ResolveCommands(Pawn pawn)` exists
- `TrionSurfaceAccess.ResolveCommands` returns `comp.Service`
- no new generic transaction facade interface is referenced from this surface test

Example assertion shape:

```powershell
Assert-True (
    $commandsText -match 'bool TryConsume\(float cost\);'
) 'ITrionCommands must expose TryConsume(float cost).'

Assert-True (
    $surfaceText -match 'public static ITrionCommands ResolveCommands\(Pawn pawn\)'
) 'TrionSurfaceAccess must expose ResolveCommands(Pawn pawn).'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\TrionFormalTransactionSurfaceContractsSmokeTests.ps1'
```

Expected: FAIL until the new smoke test file exists and the expected contract assertions are satisfied.

**Step 3: Apply the minimal implementation**

If needed, refine comments in:

- `Source/BDP/Core/Trion/ITrionCommands.cs`
- `Source/BDP/Core/Trion/TrionSurfaceAccess.cs`

to explicitly describe them as the formal neutral Trion transaction boundary.

Do not add methods.
Do not add a new facade.

**Step 4: Run test to verify it passes**

Run:

```powershell
& '.\Source\BDP.Tests\TrionFormalTransactionSurfaceContractsSmokeTests.ps1'
```

Expected: PASS

**Step 5: Commit**

```bash
git add Source/BDP/Core/Trion/ITrionCommands.cs Source/BDP/Core/Trion/TrionSurfaceAccess.cs Source/BDP.Tests/TrionFormalTransactionSurfaceContractsSmokeTests.ps1
git commit -m "test: lock trion formal transaction surface"
```

### Task 2: Lock the non-goal that CombatBodySession stays out of the neutral transaction boundary

**Files:**
- Modify: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodySessionNonFacadeBoundarySmokeTests.ps1`

**Step 1: Write the failing smoke test**

Create `Source/BDP.Tests/CombatBodySessionNonFacadeBoundarySmokeTests.ps1` asserting:

- `CombatBodySessionService` does not declare a new generic Trion transaction facade contract
- `CombatBodySessionService` does not add generic methods such as:
  - `TryConsumeOnce`
  - `RegisterSustain`
  - `UnregisterSustain`
- `CompTrion` remains independent from CombatBodySession internals

Example assertion shape:

```powershell
Assert-True (
    $combatBodySessionText -notmatch 'TryConsumeOnce\s*\('
) 'CombatBodySessionService must not grow a generic one-shot transaction facade.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionNonFacadeBoundarySmokeTests.ps1'
```

Expected: FAIL until the new smoke test file exists and the assertions are in place.

**Step 3: Apply the minimal test updates**

Update existing smoke tests only to lock these neutral boundaries:

- `CombatBodySession` remains a combat-body session coordinator
- `Trion` remains the neutral transaction surface
- `CompTrion` remains independent from caller-specific coordination internals

Do not add any caller-specific behavior assertions.

**Step 4: Run tests to verify they pass**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionNonFacadeBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
```

Expected: PASS

**Step 5: Commit**

```bash
git add Source/BDP.Tests/CombatBodySessionNonFacadeBoundarySmokeTests.ps1 Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1 Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1
git commit -m "test: keep battle-session out of neutral trion boundary"
```

### Task 3: Lock sustain registration semantics at the Trion owner level

**Files:**
- Modify: `Source/BDP/Core/Trion/CompTrion.cs`
- Test: `Source/BDP.Tests/TrionDrainRegistrationSemanticsSmokeTests.ps1`

**Step 1: Write the failing smoke test**

Create `Source/BDP.Tests/TrionDrainRegistrationSemanticsSmokeTests.ps1` asserting:

- `CompTrion.RegisterDrain(string key, float perDay)` exists
- repeated registration for the same key overwrites the current value
- `CompTrion.UnregisterDrain(string key)` exists
- unregistering an unknown key is safe
- the owner does not infer caller-specific semantics from the key

Example assertion shape:

```powershell
Assert-True (
    $compTrionText -match 'drainRegistry\[key\] = perDay;'
) 'CompTrion.RegisterDrain must overwrite by key.'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\TrionDrainRegistrationSemanticsSmokeTests.ps1'
```

Expected: FAIL until the new smoke test file exists and the expected assertions are checked.

**Step 3: Apply the minimal implementation**

If necessary, refine comments in `CompTrion.cs` to clarify:

- drain registration is key-based
- overwrite-by-key is intentional
- unregister-missing is safe

Do not add caller-specific logic.

**Step 4: Run test to verify it passes**

Run:

```powershell
& '.\Source\BDP.Tests\TrionDrainRegistrationSemanticsSmokeTests.ps1'
```

Expected: PASS

**Step 5: Commit**

```bash
git add Source/BDP/Core/Trion/CompTrion.cs Source/BDP.Tests/TrionDrainRegistrationSemanticsSmokeTests.ps1
git commit -m "test: lock trion drain registration semantics"
```

### Task 4: Sync documentation to the neutral boundary and remove facade drift

**Files:**
- Modify: `docs/plans/2026-04-05-trion-formal-transaction-surface-design.md`
- Modify: `docs/plans/2026-04-05-trion-formal-transaction-surface-implementation-plan.md`

**Step 1: Re-read the design guardrails**

Confirm the docs consistently state:

- the neutral transaction boundary is `ITrionCommands`
- the formal access path is `TrionSurfaceAccess.ResolveCommands`
- `CombatBodySession` is not expanded into a generic resource transaction surface

**Step 2: Run all new targeted smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\TrionFormalTransactionSurfaceContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionNonFacadeBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\TrionDrainRegistrationSemanticsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
```

Expected: PASS

**Step 3: Run build verification**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected: BUILD SUCCEEDED

**Step 4: Update docs only if names or file paths drifted**

Keep the docs synchronized with actual code identifiers and test file paths.

**Step 5: Commit**

```bash
git add docs/plans/2026-04-05-trion-formal-transaction-surface-design.md docs/plans/2026-04-05-trion-formal-transaction-surface-implementation-plan.md Source/BDP/Core/Trion/ITrionCommands.cs Source/BDP/Core/Trion/TrionSurfaceAccess.cs Source/BDP/Core/Trion/CompTrion.cs Source/BDP.Tests/TrionFormalTransactionSurfaceContractsSmokeTests.ps1 Source/BDP.Tests/CombatBodySessionNonFacadeBoundarySmokeTests.ps1 Source/BDP.Tests/TrionDrainRegistrationSemanticsSmokeTests.ps1 Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1 Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1
git commit -m "docs: align on trion formal transaction surface"
```

