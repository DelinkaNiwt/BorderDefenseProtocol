# BDP Unnecessary Cost Elimination Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Eliminate the current non-essential structural costs in BDP in one clean refactor pass so that runtime singleton service-locator drift, weak cross-owner coupling, mirrored attack-session state, oversized orchestration centers, and reflection-based verb mutation are no longer valid review findings after implementation completes.

**Architecture:** Keep BDP aligned with RimWorld mod scale and BDP’s actual needs: preserve the three truth owners (`Trion`, `CombatBody`, `Trigger`), keep immutable Def-time caches static, and move only runtime behavior to explicit owner-held services. Replace string and version-fragment protocols with explicit value objects, split cross-system orchestration by transaction theme instead of piling more logic into owner comps, and remove reflection-driven runtime mutation by introducing BDP-owned resolved verb specs that the formal-host and execution layers consume directly.

**Tech Stack:** C#, RimWorld Verse/Harmony runtime, owner-hosted ThingComp services, PowerShell smoke tests in `Source/BDP.Tests`, .NET Framework build via `dotnet msbuild`

---

## Hard Constraints

- No compatibility shim layer, no temporary bridge types, no duplicate old/new protocols kept alive in parallel.
- No dependency-injection container, plugin bus, or enterprise-style frameworkization.
- Static singletons remain allowed only for immutable Def-time caches and pure stateless constants; runtime execution services must become owner-held or explicitly composed.
- `Trion` must not reference `Trigger` types or namespaces after the refactor.
- `CombatBodySessionService` must remain a thin orchestration facade; resource binding, activation exit settlement, and subscription glue must move to focused collaborators.
- `AttackExecution` and formal-host continuation must use one explicit attack-session identity object instead of scattered mirrored fields.
- `Expression` and `Verb` runtime behavior must no longer depend on reflection to mutate private Verse internals.

## Final Review Standard

When this plan is fully implemented, a fresh code review should conclude all of the following:

1. No runtime global singleton/service-locator coupling remains in `AttackExecution`, `Expressions`, or ranged protocol assembly.
2. `Trion` is fully decoupled from `Trigger` semantics except through neutral value objects owned by `Trion`.
3. Attack-session identity is single-source and no longer mirrored as ad-hoc `ProjectionVersion` fragments across multiple runtime objects.
4. `CombatBodySessionService` is an orchestration façade, not a hidden fourth owner.
5. `CompTriggerBody`, `BdpVerb_Shoot`, and `ExpressionSnapshotBuilder` are no longer carrying unrelated cross-boundary responsibilities.
6. Expression-to-verb runtime mutation is explicit and typed; reflection-based `VerbProperties` patching is gone.
7. Existing gameplay semantics and existing smoke-test guarantees still hold.

## Scope Map

### P0 Blockers

- Lock the architecture boundaries in smoke tests first.
- Define the exact no-regression standards before touching runtime code.

### P1 Boundary Cleanup

- Remove runtime singleton service locators.
- Replace `Trion` drain string protocol with typed keys.
- Remove `Trion -> Trigger` weak reverse dependency.

### P2 Runtime Session Cleanup

- Introduce a single `AttackSessionToken`.
- Reduce publish/binding/verb/job mirrored state.
- Thin `CombatBodySessionService` into façade + transactions.

### P3 Targeted Core Thinning

- Replace reflection-based resolved verb mutation.
- Split `ExpressionSnapshotBuilder` by stage.
- Extract focused helpers out of `CompTriggerBody` and `BdpVerb_Shoot`.

### P4 Final Audit

- Build, run all related smoke tests, and perform a final architecture boundary sweep.

---

### Task 1: Lock the architecture debt as failing smoke tests

**Priority:** P0

**Files:**
- Create: `Source/BDP.Tests/RuntimeServiceLocatorBoundarySmokeTests.ps1`
- Create: `Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1`
- Create: `Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1`
- Create: `Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1`
- Create: `Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1`
- Test: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Test: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionSurfaceAccess.cs`
- Test: `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolSurfaceAccess.cs`
- Test: `Source/BDP/Core/Trion/TrionDrainKeys.cs`
- Test: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Test: `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- Test: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`

**Step 1: Write the service-locator boundary smoke test**

Assert that the following runtime fields must not exist:

- `private static readonly AttackExecutionService ExecutionEntry`
- `private static readonly ExpressionRuntimeRepository runtimeRepository`
- `private static readonly ExpressionService service`
- `private static readonly RangedAttackProtocolService Service`
- `private static readonly RangedAttackTrionGate TrionGate`

Allow static Def-time caches only in definition-facing modules such as `ChipSurfaceAccess` and `ComboSurfaceAccess`.

**Step 2: Write the `Trion` boundary smoke test**

Assert that:

- `Source/BDP/Core/Trion/` contains no `using BDP.Core.Trigger`
- no `TriggerSide` symbol appears under `Source/BDP/Core/Trion`
- drain registration no longer uses plain string factory helpers

**Step 3: Write the attack-session token smoke test**

Assert that:

- a single `AttackSessionToken` type exists
- `BdpVerb_Shoot` and `BdpVerb_MeleeAttackDamage` no longer store standalone `HostProjectionVersion`
- post-load recovery and execution entry validate against token state rather than detached version fragments

**Step 4: Write the resolved-verb-spec smoke test**

Assert that:

- `ExpressionSnapshotBuilder` does not use `MemberwiseClone`
- `ExpressionSnapshotBuilder` does not write `forcedMissRadiusField`
- a typed resolved-verb-spec model exists and is consumed downstream

**Step 5: Write the thin-facade battle-session smoke test**

Assert that `CombatBodySessionService` no longer directly contains:

- drain registration logic
- drain subscription lifecycle logic
- trigger slot mass-deactivation logic
- all activation and exit settlement details inline

**Step 6: Run the new smoke tests and verify they fail**

Run:

```powershell
& '.\Source\BDP.Tests\RuntimeServiceLocatorBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\TrionDrainKeyBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackSessionTokenBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionResolvedVerbSpecBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'
```

Expected: FAIL on the current banned patterns.

**Step 7: Commit**

```powershell
git add Source/BDP.Tests/RuntimeServiceLocatorBoundarySmokeTests.ps1 `
        Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1 `
        Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1 `
        Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1 `
        Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1
git commit -m "test: lock architectural debt boundaries"
```

### Task 2: Replace string drain keys with a `Trion`-owned typed protocol

**Priority:** P1

**Files:**
- Create: `Source/BDP/Core/Trion/TrionDrainKey.cs`
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerDrainKeyFactory.cs`
- Modify: `Source/BDP/Core/Trion/ITrionCommands.cs`
- Modify: `Source/BDP/Core/Trion/ITrionReader.cs`
- Modify: `Source/BDP/Core/Trion/TrionService.cs`
- Modify: `Source/BDP/Core/Trion/CompTrion.cs`
- Delete: `Source/BDP/Core/Trion/TrionDrainKeys.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.DetachTeardown.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Test: `Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**Step 1: Add `TrionDrainKey` as a value object**

Implement a `readonly struct` or sealed immutable type with explicit fields:

- `Domain`
- `Channel`
- `Index`
- `Tag`

Add value equality and a stable `ToString()` only for diagnostics, not as the storage identity.

**Step 2: Move all drain registration APIs to the typed key**

Change:

- `RegisterDrain(string key, float perDay)`
- `UnregisterDrain(string key)`
- `GetDrainSnapshot()`

to use `TrionDrainKey`.

**Step 3: Move `Trigger`-specific key translation out of `Trion`**

Create `TriggerDrainKeyFactory` under `Trigger`, mapping:

- `TriggerSide.Main`
- `TriggerSide.Sub`
- `TriggerSide.Special`

into neutral `TrionDrainKey` instances.

**Step 4: Remove `TrionDrainKeys.cs` completely**

Do not leave a forwarding wrapper, obsolete alias, or string fallback path.

**Step 5: Update all current call sites**

Refactor:

- chip drain registration
- chip drain teardown
- combat-body maintenance drain registration
- drain snapshot reads

to use `TrionDrainKey`.

**Step 6: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\TrionDrainKeyBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

Expected: PASS

**Step 7: Commit**

```powershell
git add Source/BDP/Core/Trion/TrionDrainKey.cs `
        Source/BDP/Core/Trigger/Runtime/TriggerDrainKeyFactory.cs `
        Source/BDP/Core/Trion/ITrionCommands.cs `
        Source/BDP/Core/Trion/ITrionReader.cs `
        Source/BDP/Core/Trion/TrionService.cs `
        Source/BDP/Core/Trion/CompTrion.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.DetachTeardown.cs `
        Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs `
        Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1
git rm Source/BDP/Core/Trion/TrionDrainKeys.cs
git commit -m "refactor: replace string drain protocol with typed trion keys"
```

### Task 3: Introduce owner-held trigger runtime composition and delete runtime singleton service ownership

**Priority:** P1

**Files:**
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeServices.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolSurfaceAccess.cs`
- Modify: `Source/BDP/Core/VerbHosting/VerbHostSurfaceAccess.cs`
- Test: `Source/BDP.Tests/RuntimeServiceLocatorBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/PrimaryTriggerRuntimeOwnershipSmokeTests.ps1`
- Test: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`

**Step 1: Create `TriggerRuntimeServices`**

This owner-held runtime root must hold only runtime execution services required by one `CompTriggerBody`, including:

- trigger-local `ExpressionService`
- trigger-local `AttackExecutionService`
- trigger-local `RangedAttackProtocolService`
- trigger-local `RangedAttackTrionGate`
- any small runtime collaborators that belong to the current published projection lifecycle

Do not move immutable Def caches here.

**Step 2: Keep immutable caches global, move runtime behavior local**

Retain static immutable caches only in definition-facing modules:

- `ChipDefinitionCache`
- `ComboRuntimeIndex`
- expression contract cache if it remains immutable and definition-facing

Remove runtime service ownership from the static access classes.

**Step 3: Convert static access classes into pure owner resolvers**

Refactor:

- `AttackExecutionSurfaceAccess`
- `ExpressionSurfaceAccess`
- `RangedAttackProtocolSurfaceAccess`

so they no longer own runtime service instances. They may remain as thin static resolvers if and only if they resolve the current owner-held service from `Pawn` or `CompTriggerBody`.

**Step 4: Ensure `CompTriggerBody` owns the runtime composition root**

Create and store `TriggerRuntimeServices` inside `CompTriggerBody` construction/ensure logic, and ensure the runtime root follows Trigger owner lifecycle.

**Step 5: Update all callers**

Refactor:

- patches
- manual entry bridge
- verb-host lookups
- trigger runtime publish path
- auto-attack entry points

to use the owner-held runtime services.

**Step 6: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\RuntimeServiceLocatorBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
```

Expected: PASS

**Step 7: Commit**

```powershell
git add Source/BDP/Core/Trigger/Runtime/TriggerRuntimeServices.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs `
        Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs `
        Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs `
        Source/BDP/Core/Expressions/Access/Surfaces/ExpressionSurfaceAccess.cs `
        Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolSurfaceAccess.cs `
        Source/BDP/Core/VerbHosting/VerbHostSurfaceAccess.cs
git commit -m "refactor: move runtime services under trigger ownership"
```

### Task 4: Split `CombatBodySessionService` into façade plus activation/exit/binding collaborators

**Priority:** P2

**Files:**
- Create: `Source/BDP/Core/CombatBodySession/CombatBodyActivationTransaction.cs`
- Create: `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`
- Create: `Source/BDP/Core/CombatBodySession/CombatBodySessionTrionBinding.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Test: `Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`

**Step 1: Extract activation transaction**

Move the full activation transaction sequence out of `CombatBodySessionService`:

- resolve primary trigger
- validate `Trion`
- calculate allocation
- perform allocation
- enter active phase
- trigger post-activation side effects

**Step 2: Extract exit transaction**

Move the full exit/cleanup sequence out of `CombatBodySessionService`:

- deactivate trigger slots
- release or deplete `Trion`
- unregister maintenance drain
- clear subscriptions
- enter cooldown / exit reason

**Step 3: Extract `Trion` event binding lifecycle**

Move:

- `AvailableDepleted` subscription
- unsubscription
- maintenance drain binding

into `CombatBodySessionTrionBinding`.

**Step 4: Reduce `CombatBodySessionService` to façade responsibilities**

After extraction, `CombatBodySessionService` should do only:

- expose the `CombatBody` public reader/command/event contract
- choose which collaborator to call
- preserve public orchestration order

It must not carry the low-level transaction logic inline anymore.

**Step 5: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
```

Expected: PASS

**Step 6: Commit**

```powershell
git add Source/BDP/Core/CombatBodySession/CombatBodyActivationTransaction.cs `
        Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs `
        Source/BDP/Core/CombatBodySession/CombatBodySessionTrionBinding.cs `
        Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs `
        Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs `
        Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs `
        Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1
git commit -m "refactor: thin battle session into façade and transactions"
```

### Task 5: Introduce a single `AttackSessionToken` and remove mirrored version fragments

**Priority:** P2

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackSessionToken.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPreparedContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`
- Modify: `Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Test: `Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**Step 1: Add `AttackSessionToken`**

Define a single runtime identity object that holds:

- `AttackInstanceId`
- `ResultId`
- `ProjectionVersion`
- `OwnerPawnThingId`

Add value equality and simple validity checks.

**Step 2: Move request/prepared-context identity onto the token**

Refactor execution request and prepared context so they carry token state as one field, not detached scalar fragments.

**Step 3: Remove standalone `HostProjectionVersion` fields from verbs**

Replace per-verb mirrored projection version state with the token.

**Step 4: Update post-load recovery and continuation**

Refactor recovery to validate and optionally rebind one token object, not separate `ResultId` plus `ProjectionVersion` fragments.

**Step 5: Update verb-host binding state**

Ensure binding state, formal hosts, and job drivers all exchange the same token object rather than reconstructing session identity ad hoc.

**Step 6: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\AttackSessionTokenBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected: PASS

**Step 7: Commit**

```powershell
git add Source/BDP/Core/AttackExecution/AttackSessionToken.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionPreparedContext.cs `
        Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs `
        Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs `
        Source/BDP/Core/AttackExecution/JobDriver_BdpMeleeAttackExecution.cs `
        Source/BDP/Core/Verbs/BdpVerb_Shoot.cs `
        Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs `
        Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs `
        Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs `
        Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs `
        Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs `
        Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1
git commit -m "refactor: unify attack session identity with token"
```

### Task 6: Replace reflection-based verb mutation with a typed resolved-verb-spec pipeline

**Priority:** P2

**Files:**
- Create: `Source/BDP/Core/Expressions/Model/ResolvedVerbSpec.cs`
- Create: `Source/BDP/Core/Expressions/Pipeline/ResolvedVerbSpecFactory.cs`
- Modify: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceDeclaration.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionSourceMaterial.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP/Core/Combos/Contract/ComboResolvedVerbProps.cs`
- Modify: `Source/BDP/Core/Combos/Contract/ComboDefinitionContractResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs`
- Test: `Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/MeleeNormalRhythmContractSmokeTests.ps1`

**Step 1: Introduce `ResolvedVerbSpec`**

Create a BDP-owned explicit model for runtime verb behavior, including:

- range
- burst count
- burst interval
- warmup
- forced miss radius
- projectile def
- tool / maneuver payload where needed

This model becomes the canonical runtime contract.

**Step 2: Make expression and combo resolution produce `ResolvedVerbSpec`**

Refactor expression and combo interpreters so they no longer rely on mutating `VerbProperties` as the canonical runtime object.

**Step 3: Remove reflection from expression pipeline**

Delete all reflection-based `VerbProperties` mutation in `ExpressionSnapshotBuilder`, including:

- `MemberwiseClone`
- `forcedMissRadiusField`
- any private field probing

**Step 4: Update downstream consumers**

Make:

- formal-host verbs
- attack execution planner
- ranged protocol

consume `ResolvedVerbSpec` directly.

**Step 5: Keep Verse-facing projection only at the edge**

If the Verse layer still needs a `VerbProperties` instance, create it in one explicit edge-only helper from `ResolvedVerbSpec` using supported fields only. The edge helper must not use reflection.

**Step 6: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionResolvedVerbSpecBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\MeleeNormalRhythmContractSmokeTests.ps1'
```

Expected: PASS

**Step 7: Commit**

```powershell
git add Source/BDP/Core/Expressions/Model/ResolvedVerbSpec.cs `
        Source/BDP/Core/Expressions/Pipeline/ResolvedVerbSpecFactory.cs `
        Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs `
        Source/BDP/Core/Expressions/Model/ExpressionSourceDeclaration.cs `
        Source/BDP/Core/Expressions/Model/ExpressionSourceMaterial.cs `
        Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs `
        Source/BDP/Core/Combos/Contract/ComboResolvedVerbProps.cs `
        Source/BDP/Core/Combos/Contract/ComboDefinitionContractResolver.cs `
        Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs `
        Source/BDP/Core/Verbs/BdpVerb_Shoot.cs `
        Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs `
        Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs `
        Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1
git commit -m "refactor: replace reflective verb mutation with resolved verb specs"
```

### Task 7: Split `ExpressionSnapshotBuilder` by stage without over-fragmenting the pipeline

**Priority:** P3

**Files:**
- Create: `Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- Create: `Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs`
- Create: `Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- Modify: `Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs`
- Test: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- Test: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`

**Step 1: Extract source collection**

Move Trigger-source enumeration and material collection out of `ExpressionSnapshotBuilder` into `ExpressionSourceCollector`.

**Step 2: Extract single-side result construction**

Move single-side result assembly into `SingleSideExpressionBuilder`.

**Step 3: Extract composite resolution**

Move dual/combo composite result resolution into `CompositeExpressionResolver`.

**Step 4: Leave `ExpressionSnapshotBuilder` as a small pipeline coordinator**

After extraction, `ExpressionSnapshotBuilder` should only:

- call collector
- call filter
- call single-side builders
- call composite resolver
- assemble the final snapshot

Do not replace this with many interfaces; keep the collaborators concrete and local to the pipeline folder.

**Step 5: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected: PASS

**Step 6: Commit**

```powershell
git add Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs `
        Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs `
        Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs `
        Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs `
        Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs
git commit -m "refactor: split expression snapshot pipeline by stage"
```

### Task 8: Thin `CompTriggerBody` by extracting cross-owner resource and teardown transactions

**Priority:** P3

**Files:**
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerTrionBindingService.cs`
- Create: `Source/BDP/Core/Trigger/Runtime/TriggerDetachTeardownTransaction.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.DetachTeardown.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Test: `Source/BDP.Tests/TriggerDetachTeardownSmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**Step 1: Extract Trigger-to-Trion binding logic**

Move out of `CompTriggerBody`:

- reserved sync recalculation and submission
- chip drain registration
- chip drain unregistration

into `TriggerTrionBindingService`.

**Step 2: Extract detach teardown transaction**

Move unequip/detach cleanup into `TriggerDetachTeardownTransaction`, including:

- chip drain cleanup
- combat-body teardown coordination entry
- projection/host cleanup sequencing

**Step 3: Keep `CompTriggerBody` as the state owner**

After extraction, `CompTriggerBody` remains the only Trigger truth owner and surface host, but should delegate cross-owner binding/cleanup work instead of implementing all of it inline.

**Step 4: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

Expected: PASS

**Step 5: Commit**

```powershell
git add Source/BDP/Core/Trigger/Runtime/TriggerTrionBindingService.cs `
        Source/BDP/Core/Trigger/Runtime/TriggerDetachTeardownTransaction.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.DetachTeardown.cs `
        Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs
git commit -m "refactor: extract trigger resource binding and teardown transactions"
```

### Task 9: Thin `BdpVerb_Shoot` into focused emission/round-state collaborators

**Priority:** P3

**Files:**
- Create: `Source/BDP/Core/Verbs/RangedVerbRoundState.cs`
- Create: `Source/BDP/Core/Verbs/RangedVerbEmissionCursor.cs`
- Create: `Source/BDP/Core/Verbs/RangedVerbContinuationPlanner.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Test: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- Test: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`

**Step 1: Extract round-state bookkeeping**

Move round-local cost and emission progress bookkeeping out of `BdpVerb_Shoot` into `RangedVerbRoundState`.

**Step 2: Extract emission-window cursor logic**

Move emission-window and projectile-plan consumption state into `RangedVerbEmissionCursor`.

**Step 3: Extract continuation planning**

Move follow-up prepare/continuation request creation into `RangedVerbContinuationPlanner`.

**Step 4: Keep `BdpVerb_Shoot` focused**

After extraction, `BdpVerb_Shoot` should mainly:

- bridge into Verse firing behavior
- apply the already-prepared round state
- forward continuation decisions to the planner

It must no longer own every piece of round bookkeeping inline.

**Step 5: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected: PASS

**Step 6: Commit**

```powershell
git add Source/BDP/Core/Verbs/RangedVerbRoundState.cs `
        Source/BDP/Core/Verbs/RangedVerbEmissionCursor.cs `
        Source/BDP/Core/Verbs/RangedVerbContinuationPlanner.cs `
        Source/BDP/Core/Verbs/BdpVerb_Shoot.cs `
        Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs
git commit -m "refactor: split ranged formal host round state and continuation"
```

### Task 10: Align formal-host refresh and publish invalidation on the single runtime flow

**Priority:** P3

**Files:**
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Test: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Test: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`

**Step 1: Make publish invalidation flow explicit**

Ensure the publish path is the only place that:

- bumps projection version
- updates published result index
- refreshes formal-host bindings
- triggers stale-session invalidation

**Step 2: Remove any remaining parallel refresh path**

Delete any secondary or ad-hoc refresh path that rebuilds host state outside the publish boundary.

**Step 3: Keep formal-host identity stable across publish boundaries**

Formal-host refresh should remap against the published token/result identity, not rebuild hidden parallel state.

**Step 4: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected: PASS

**Step 5: Commit**

```powershell
git add Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs `
        Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs `
        Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs `
        Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs
git commit -m "refactor: unify publish invalidation and formal host refresh"
```

### Task 11: Run full architecture and gameplay regression gates

**Priority:** P4

**Files:**
- Test: `Source/BDP.Tests/RuntimeServiceLocatorBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`
- Test: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Test: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- Test: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Test: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Test: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Test: `Source/BDP.Tests/PrimaryTriggerRuntimeOwnershipSmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerDetachTeardownSmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerSwitchTimingSmokeTests.ps1`
- Modify: `Source/BDP/BDP.csproj`

**Step 1: Run the architecture boundary suite**

Run:

```powershell
& '.\Source\BDP.Tests\RuntimeServiceLocatorBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\TrionDrainKeyBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackSessionTokenBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionResolvedVerbSpecBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'
```

Expected: PASS

**Step 2: Run the Trigger / CombatBody / Trion integration suite**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSwitchTimingSmokeTests.ps1'
```

Expected: PASS

**Step 3: Run the expression / publish / formal-host suite**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
```

Expected: PASS

**Step 4: Run the ranged execution suite**

Run:

```powershell
& '.\Source\BDP.Tests\RangedAttackTrionConsumptionSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

Expected: PASS

**Step 5: Build the mod**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected: BUILD SUCCEEDED

**Step 6: Perform the final source audit**

Verify by source search:

- no `using BDP.Core.Trigger` under `Source/BDP/Core/Trion`
- no runtime singleton service fields in the banned surface-access classes
- no `HostProjectionVersion` field remains in `BdpVerb_Shoot` or `BdpVerb_MeleeAttackDamage`
- no `MemberwiseClone` or `forcedMissRadiusField` remains in `ExpressionSnapshotBuilder`

**Step 7: Commit**

```powershell
git add Source/BDP/Core Source/BDP.Tests Source/BDP/BDP.csproj
git commit -m "refactor: eliminate non-essential architectural costs across bdp runtime"
```

## Execution Notes

- Execute tasks strictly in order.
- Do not mix multiple tasks into one unreviewed batch.
- If a task exposes an unplanned behavior break, fix it inside the same task before moving on; do not create a compatibility shim or postpone cleanup.
- Do not preserve any old API shape “just in case.” Once the new boundary is in, remove the old path immediately.
- Keep the implementation consistent with RimWorld mod scale: explicit owner-held services, concrete helper classes, PowerShell smoke tests, no heavy abstraction framework.

## Completion Standard

This plan is complete only when:

- all new boundary smoke tests pass
- all affected legacy smoke tests pass
- the mod builds
- the forbidden patterns listed in Task 1 no longer exist in source
- no reviewer can still reasonably describe the current state using the same problems this plan targets

