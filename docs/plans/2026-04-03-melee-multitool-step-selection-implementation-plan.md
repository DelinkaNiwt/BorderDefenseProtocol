# Melee Multi-Tool Step Selection Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restore vanilla-style multi-tool melee behavior in BDP so one melee combo round can use different declared tools on different hits while preserving BDP formal-host session ownership.

**Architecture:** Keep BDP in charge of melee formal-host lifecycle, runtime steps, semantics, and save/load recovery. Replace the current single-tool collapse with a declared-tool pipeline plus a deterministic per-round step tool sequence that is rebound onto the formal melee host before each hit.

**Tech Stack:** C#, RimWorld Verse verb/job system, Harmony-adjacent BDP runtime pipeline, PowerShell smoke tests, .NET Framework build via `dotnet msbuild`

---

### Task 1: Lock the current bug in failing smoke tests

**Files:**
- Create: `Source/BDP.Tests/MeleeMultiToolDeclarationSmokeTests.ps1`
- Create: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`
- Test: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Test: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Test: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`

**Step 1: Write the declaration smoke test**

Assert that the melee expression interpreter no longer resolves `config.tools[0]` as the only runtime tool source.

**Step 2: Write the step-selection smoke test**

Assert that the melee pipeline contains:

- a declared-tool collection on the runtime result/binding path
- a step-level selected tool or prepared step-tool sequence concept
- a per-step formal-host rebinding point before attack launch

**Step 3: Run the new tests to verify they fail**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeMultiToolDeclarationSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeStepToolSelectionSmokeTests.ps1'
```

Expected: FAIL because the current code still collapses `tools` to the first entry and exposes only one bound tool.

### Task 2: Preserve declared tool collections through the expression pipeline

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ChipExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/ChipExpressionEntryContract.cs`
- Modify: `Source/BDP/Core/Combos/Config/ComboExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceDeclaration.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceMaterial.cs`
- Modify: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Test: `Source/BDP.Tests/MeleeMultiToolDeclarationSmokeTests.ps1`

**Step 1: Add declared-tool collection fields**

Add explicit collection properties that preserve the full melee tool list alongside the existing single-tool compatibility field where still needed during migration.

**Step 2: Stop collapsing `tools` to `tools[0]` as the only truth**

Keep compatibility for legacy single-tool reads, but make the declared collection the canonical source for melee entries.

**Step 3: Run the declaration smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeMultiToolDeclarationSmokeTests.ps1'
```

Expected: still FAIL, but now only on missing runtime selection/binding behavior.

### Task 3: Introduce step-local melee tool surfaces

**Files:**
- Create: `Source/BDP/Core/AttackExecution/MeleeToolSurface.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Test: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`

**Step 1: Add the `MeleeToolSurface` model**

Represent:

- the original `Tool`
- step-local `VerbProps`
- step-local `Maneuver`
- derived `DamageDef`

**Step 2: Build one surface per declared tool**

Reuse the current tool-to-verb derivation rules, but generate surfaces for every declared melee tool instead of only the first one.

**Step 3: Propagate surfaces into formal results and binding state**

Formal-host binding must be able to access all declared candidate surfaces for the active melee result.

**Step 4: Run the step-selection smoke test**

Expected: still FAIL, but now only on missing selector and per-step rebinding.

### Task 4: Add a vanilla-compatible melee tool selector

**Files:**
- Create: `Source/BDP/Core/AttackExecution/VanillaCompatibleMeleeToolSelector.cs`
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`
- Test: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`

**Step 1: Add selector service**

The selector input should include:

- pawn
- target
- active formal result
- candidate tool surfaces
- planned melee step count

**Step 2: Make the selector output a full round sequence**

Return a deterministic ordered list of step tool indices or surfaces for the current combo round.

**Step 3: Keep weighting vanilla-compatible**

Use vanilla semantics from public helpers wherever possible for:

- adjusted damage
- adjusted cooldown
- chance factor

Only copy the minimum weighting glue that cannot be reused directly.

**Step 4: Attach the prepared sequence to melee execution context**

The context must carry the prepared step tool sequence for the current round.

**Step 5: Run the step-selection smoke test**

Expected: still FAIL, but now only on missing per-step host rebinding and persistence.

### Task 5: Rebind the formal melee host per step

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`
- Test: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`

**Step 1: Add a step-surface apply method on the melee formal host**

Support rebinding:

- `tool`
- tool-derived `verbProps`
- `maneuver`

without invalidating the formal-host identity itself.

**Step 2: Apply the selected step surface before each melee attack attempt**

`JobDriver_BdpMeleeAttackExecution` should select the prepared surface for `currentStepIndex` and bind it before calling `TryMeleeAttack`.

**Step 3: Keep combo pacing logic intact**

Do not disturb:

- step interval timing
- round-end cooldown ownership
- chase continuation
- busy stance ownership rules

**Step 4: Run the step-selection smoke test**

Expected: PASS

### Task 6: Persist prepared step-tool state across save/load

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`

**Step 1: Persist reconstructable sequence state**

Store step tool indices or stable tool ids for the current combo round.

**Step 2: Rebuild transient surfaces after load**

On recovery, map the stored indices back onto the currently bound declared surface list.

**Step 3: Verify loaded sessions do not reshuffle tools mid-round**

Run:

```powershell
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeStepToolSelectionSmokeTests.ps1'
```

Expected: PASS

### Task 7: Add coverage for damage source labels and cooldown ownership

**Files:**
- Create: `Source/BDP.Tests/MeleeStepToolSourceLabelSmokeTests.ps1`
- Create: `Source/BDP.Tests/MeleeFinalStepCooldownSmokeTests.ps1`
- Test: `Source/BDP/Core/Semantics/BdpDamageSemanticBridge.cs`
- Test: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`
- Test: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`

**Step 1: Write source-label smoke coverage**

Assert that the wound source path still preserves the selected step tool label for the current hit.

**Step 2: Write final-step cooldown smoke coverage**

Assert that combo trailing cooldown remains governed by the final selected step surface, not by the first tool or a synthetic average.

**Step 3: Run the new tests to verify behavior**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeStepToolSourceLabelSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeFinalStepCooldownSmokeTests.ps1'
```

Expected: PASS

### Task 8: Run focused regression verification

**Files:**
- Test: `Source/BDP.Tests/MeleeMultiHitStepSchedulingSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeComboPacingOwnershipSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeComboRoundCooldownSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeInjurySourceToolLabelSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeMultiToolDeclarationSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeStepToolSelectionSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeStepToolSourceLabelSmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeFinalStepCooldownSmokeTests.ps1`

**Step 1: Run all targeted melee smokes**

Run:

```powershell
& '.\Source\BDP.Tests\MeleeMultiHitStepSchedulingSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeComboPacingOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeComboRoundCooldownSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeInjurySourceToolLabelSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeMultiToolDeclarationSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeStepToolSelectionSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeStepToolSourceLabelSmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeFinalStepCooldownSmokeTests.ps1'
```

Expected: PASS

### Task 9: Build verification

**Files:**
- Test: `Source/BDP/BDP.csproj`

**Step 1: Run the build**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected: Build succeeds with exit code 0.
