# Melee Multi-Tool Step Selection Design

**Date:** 2026-04-03

## Goal

Restore the intended vanilla-style melee multi-tool behavior for BDP melee entries:

- one melee expression entry may declare multiple `tools`
- each combo step may use a different selected tool
- tool selection must read vanilla tool semantics such as `power`, `cooldownTime`, `armorPenetration`, and `chanceFactor`
- BDP must keep ownership of formal host identity, attack-session validity, semantic context, step scheduling, and save/load recovery

## Chosen Approach

Keep BDP in control of melee execution sessions, but add a vanilla-compatible tool-selection layer inside the BDP melee step pipeline.

Do not hand full melee control back to `Pawn_MeleeVerbs`.
Do not expand one melee expression entry into multiple formal-host results.

Instead:

- preserve the declared melee tool list on the expression result
- build a per-tool melee surface set from vanilla tool fields
- precompute a tool sequence for the current combo round
- bind the selected tool surface step-by-step as the melee job advances

## Why This Approach

The current BDP architecture already owns the parts that matter for session integrity:

- automatic melee entry is intercepted by BDP
- formal host identity and runtime tick ownership live in BDP
- melee multi-hit is already step-driven through runtime steps
- semantic source propagation and post-load session validation are already BDP responsibilities

The missing part is not combo scheduling anymore.
The missing part is that the melee pipeline collapses `tools` to `tools[0]` too early, so only one tool ever reaches runtime.

This design restores tool choice without undoing the architecture work already done for:

- multi-hit pacing
- round cooldown ownership
- formal host continuity
- semantic labeling

## Intended Player-Facing Behavior

- A melee chip may declare multiple tools under one expression entry.
- One combo round may visibly produce mixed tool labels across hits, for example `弧月刃1 -> 弧月刃2 -> 弧月刃1`.
- Each hit uses the selected tool's own vanilla semantics for damage, AP, cooldown-derived verb surface, extra damage, and wound label.
- The combo still remains one BDP-managed melee session, not several independent attack sessions.

## Architecture Impact

### Keep

- `AttackExecutionService.Stages` remains the single source of truth for melee runtime-step expansion.
- `JobDriver_BdpMeleeAttackExecution` remains the owner of step pacing, chase continuation, and combo-round progression.
- `BdpVerb_FormalHostMelee` remains the stable formal host shell used by the vanilla combat chain.
- `BdpVerb_MeleeAttackDamage` remains the actual damage landing boundary and semantic runtime carrier.

### Change

- The expression/result/binding pipeline must preserve declared melee tool collections instead of collapsing to a single tool.
- BDP melee runtime must distinguish:
  - declared tool candidates
  - the selected tool surface for the current hit
- A new selector service must produce a deterministic per-round step tool sequence from vanilla-compatible inputs.
- The melee verb shell must support rebinding tool-derived surface fields before each step attack attempt.

### Explicit Non-Goals

- no full handoff of melee verb selection back to `Pawn_MeleeVerbs`
- no creation of one formal-host result per tool
- no random re-roll at damage-application time
- no new author-only custom weight field in the first implementation

## Data Model Changes

### Expression / Contract / Result Layer

Current behavior:

- entry declares `Tool` or `tools`
- interpreter resolves only one final tool

Target behavior:

- preserve the declared melee tool list on the result
- continue exposing one active formal melee result per expression entry

Recommended additions:

- `ChipExpressionEntryContract.DeclaredTools`
- `FormalExpressionResult.DeclaredTools`
- optional `FormalExpressionResult.DeclaredMeleeToolSurfaces`

### Runtime Surface Layer

Introduce a small internal model such as `MeleeToolSurface`:

- `Tool Tool`
- `VerbProperties VerbProps`
- `ManeuverDef Maneuver`
- `DamageDef DamageDef`

This surface is step-local and answers:

- what damage profile this hit uses
- what wound tool label should be carried
- what cooldown semantics the final hit of the round should leave behind

## Selection Semantics

### Selection Timing

Selection happens once per combo round, before the first hit of that round starts.

The selector outputs a complete step sequence for that round, for example:

- step 0 -> tool surface A
- step 1 -> tool surface B
- step 2 -> tool surface A

This avoids mid-round nondeterminism while still allowing per-hit variation.

### Why Not Roll Per Hit At Damage Time

Rolling at damage time would make the session unstable:

- save/load recovery would not know which hit was supposed to use which tool
- logs and diagnostics would become non-repeatable
- final cooldown ownership would become ambiguous
- wound labels could drift between retries or resume paths

### Vanilla Compatibility Goal

The selector should read vanilla tool semantics, not invent a new balancing language.

Priority fields:

- `power`
- `cooldownTime`
- `armorPenetration`
- `chanceFactor`
- `capacities`
- `extraMeleeDamages`
- `linkedBodyPartsGroup`
- `hediff`
- `surpriseAttack`

Use vanilla public helpers wherever practical, especially for:

- adjusted melee damage
- adjusted cooldown
- tool-derived damage base

If a small part of vanilla weighting is not reusable through public APIs, copy only the minimum weighting glue rather than re-implementing the whole vanilla melee system.

## Execution Flow

1. Author declares one melee expression entry with multiple `tools`.
2. Contract interpretation preserves all declared tools and builds tool surfaces.
3. `AttackExecutionService.Stages` still expands the melee result into step runtime casts as it does today.
4. Before a combo round starts, BDP computes the step tool sequence for that round.
5. `JobDriver_BdpMeleeAttackExecution` advances steps in order:
   - select the prepared surface for the current step
   - bind `Tool`, `VerbProps`, and `Maneuver` to the formal melee host
   - launch the normal melee attack attempt
6. `BdpVerb_MeleeAttackDamage` lands damage using the currently bound step tool surface.
7. After the last hit, the remaining cooldown behavior naturally reflects the last bound step surface.

## Cooldown Rule

The combo-round trailing cooldown should come from the last hit's actual selected tool surface.

This is the cleanest rule because:

- it matches the fact that the formal host is bound to one concrete melee surface at cast time
- it avoids averaging or inventing synthetic cooldown semantics
- it keeps the runtime state honest and observable

## Save/Load Considerations

Persist only stable, reconstructable round state:

- current step index
- prepared step tool indices or ids for the current round
- current round identity if needed for invalidation

Do not deep-serialize copied tool surfaces if the same surfaces can be rebuilt from the currently bound formal expression result.

## Testing Strategy

Add smoke coverage in this order:

1. declared melee `tools` are no longer collapsed to the first entry
2. formal result and binding state preserve tool collections
3. step selection sequence is prepared per combo round
4. melee job rebinds per-step tool surface before each attack
5. wound source labels can show different tool labels across hits in one round
6. round-end cooldown reflects the final step tool
7. save/load recovery keeps the prepared step sequence stable

## Recommendation

Implement per-hit tool selection now, not per-round single-tool selection.

The current BDP melee pipeline is already step-driven, so per-hit selection fits the architecture naturally.
The main cost is removing the single-tool assumption cleanly across the expression-to-runtime pipeline.
