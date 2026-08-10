# Melee Multi-Hit Step-Driven Design

**Date:** 2026-04-03

## Goal

Make BDP melee multi-hit behave as true step-driven combo execution:

- one attack request may expand into multiple melee steps
- each step is a real melee attack attempt
- each step may be interrupted by target loss, projection invalidation, or stance interruption
- step-to-step timing must honor `HitIntervalTicks`

## Chosen Approach

Use the existing `AttackExecution` runtime-step model as the single source of truth for melee combo scheduling.

Do not add a new "committed combo session" abstraction.
Do not guarantee that the whole combo finishes once the first hit starts.

## Why This Approach

This matches the current BDP architecture:

- melee execution is already expanded into per-cast runtime steps
- formal host validity is already guarded by projection version/session checks
- active tick advancement already exists through `JobDriver_BdpMeleeAttackExecution`

The missing part is not planning. The missing part is execution scheduling.

## Intended Player-Facing Behavior

- A multi-hit melee chip should look like rapid repeated melee starts, not one swing with stacked hidden damage.
- If the target dies, moves out of range, becomes invalid, or the session is invalidated, the remaining hits stop naturally.
- `HitIntervalTicks` defines the wait after each completed melee step before the next step can start.

This is closer to "rapid three-hit combo" than to "single animation with three damage pops".

## Architecture Impact

### Keep

- `AttackExecutionService.Stages` continues to expand melee into one runtime step per cast.
- `MeleeAttackExecutionContext` continues to compute planned step count from runtime steps.
- `BdpVerb_MeleeAttackDamage` continues to carry execution/session identity only.

### Change

- `JobDriver_BdpMeleeAttackExecution` must advance through the prepared melee step sequence instead of acting like an unbounded vanilla melee retry loop.
- The job must track the current step index and a tick countdown until the next step becomes eligible.
- The job must stop after the planned melee step count for the current request, even for manual and auto attack orders.

### Explicit Non-Goals

- no forced combo lock
- no target snap or teleport compensation
- no guaranteed full combo completion after first hit
- no new combo-specific persistence contract beyond current attack-session validity rules

## Data Flow

1. Expression result carries melee execution style (`HitCount`, `HitIntervalTicks`).
2. `AttackExecutionService.Stages` expands it into multiple melee casts and runtime steps.
3. `MeleeAttackExecutionContext` computes required step count from those runtime steps.
4. `JobDriver_BdpMeleeAttackExecution` consumes steps in order:
   - wait until current delay expires
   - verify target/session still valid
   - attempt one melee attack
   - advance to next step and arm the next delay
5. Job ends when all steps are consumed or when normal interruption rules invalidate continuation.

## Failure and Interruption Rules

- invalid target: stop
- target downed/dead/despawned: stop
- projection/session invalid: interrupt
- out of range: chase until reachable, then continue current step
- pawn in full-body busy state: wait, do not advance step
- failed `TryMeleeAttack` on a valid reachable target: treat as failure and end job, preserving current safety semantics

## Persistence

Minimal extra state is acceptable in the melee job:

- current completed/next step index
- current remaining delay before next step

No new long-lived combo contract should be added to formal host or projection state.

## Testing Strategy

Add a smoke test that proves:

- melee runtime planning still expands into multiple steps
- melee job no longer converts `ForceTargetOrder` or `AutoAttackOrder` into effectively infinite attack count
- melee job tracks step progression and interval scheduling explicitly

Then run the targeted smoke test plus a build.
