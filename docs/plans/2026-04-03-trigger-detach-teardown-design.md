# Trigger Detach Teardown Design

**Date:** 2026-04-03

**Problem:** `CompTriggerBody` used to clear published projections on unequip, but could leave slot activation truth alive through chip winddown. Re-equipping the same trigger could then resurrect stale active chips without any valid owner continuity.

**Decision:** Treat "this trigger is no longer `Pawn.equipment.Primary`" as a dedicated owner teardown event, not as a normal trigger-side deactivate request.

**Owner Rules**

- `CompTriggerBody` remains the owner of slot activation truth and switch contexts.
- `CombatBodySessionService.RequestDeactivate()` continues to mean "exit battle mode while the trigger is still equipped".
- Detach teardown means the trigger has lost runtime ownership and must not retain any active slot truth.

**Detach Teardown Requirements**

- Clear all trigger-side switch contexts immediately.
- Clear all slot activation truth immediately.
- Unregister chip drains and combat body maintenance drain against the detached pawn.
- Clear published trigger projections.
- Interrupt stale attack execution sessions.

**Current Wiring**

- `CompTriggerBody.Notify_Unequipped(Pawn pawn)` still requests battle-mode exit first when combat body is active.
- After the equip relationship is removed, it now routes cleanup through `ForceTeardownOnDetach(pawn)`.

**Invariant**

If a trigger is no longer the pawn's current primary weapon, it must not retain:

- active slots
- switch contexts
- published projections
- live attack sessions

