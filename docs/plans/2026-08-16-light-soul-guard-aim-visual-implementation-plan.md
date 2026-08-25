# Light Soul Guard Aim Visual Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make every held weapon visual continuously rotate and move toward the active Light Soul guard-watch target without entering an attack stance.

**Architecture:** BDP.Content exposes one authoritative current-watch-target query on `Verb_LightSoulGuardWatch`. A high-priority BDP.Content prefix adjusts `PawnRenderUtility.DrawEquipmentAiming`'s `drawLoc` and `aimAngle` by reversing the vanilla carried-weapon offset and applying the vanilla aiming offset; the existing BDP.Core visual prefix then naturally samples the adjusted pose for single- and dual-weapon rendering.

**Tech Stack:** C# 7.3, RimWorld 1.6, Harmony, PowerShell source smoke tests, MSBuild Release build.

---

### Task 1: Lock the behavior with a failing smoke test

**Files:**
- Create: `Source/BDP.Tests/LightSoulGuardAimVisualSmokeTests.ps1`

**Step 1: Write the failing test**

Assert that the guard Verb provides a unified manual/automatic target query and that a high-priority `DrawEquipmentAiming` prefix accepts `ref Vector3 drawLoc` and `ref float aimAngle`, reverses the four vanilla carried offsets, calculates `AngleFlat`, and applies the vanilla `0.4f + equippedDistanceOffset` aiming displacement.

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardAimVisualSmokeTests.ps1`

Expected: FAIL because the visual prefix does not yet exist.

### Task 2: Implement authoritative target resolution

**Files:**
- Modify: `Source/BDP.Content/LightSoul/Verb_LightSoulGuardWatch.cs`

**Step 1: Add minimal implementation**

Add `TryGetCurrentWatchTarget(out LocalTargetInfo target)`. Prefer the current manual guard job when its `verbToUse` is this Verb and `CanHitTarget` is true; otherwise delegate to the existing automatic target query. Keep target validity and line-of-sight/range authority in the Verb.

**Step 2: Run the focused test**

Run the Task 1 command. Expected: still FAIL only on the missing render prefix.

### Task 3: Convert carried pose to vanilla aiming pose

**Files:**
- Create: `Source/BDP.Content/LightSoul/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_LightSoulGuardWatch.cs`

**Step 1: Add minimal implementation**

Add a `[HarmonyPriority(Priority.First)]` prefix. Resolve the owning Pawn, require an active guard-watch target, subtract RimWorld's facing-specific carried offset multiplied by `equipmentDrawDistanceFactor`, calculate the target's continuous `AngleFlat`, add the vanilla aiming displacement multiplied by the same factor, and update both arguments by reference.

**Step 2: Run focused tests**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardAimVisualSmokeTests.ps1
powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardWatchSmokeTests.ps1
powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1
```

Expected: all PASS.

### Task 4: Verify integration and commit

**Files:**
- Test: `Source/BDP.Tests/*.ps1`
- Build: `Source/BDP.sln`

**Step 1: Run related visual smoke tests**

Run all LightSoul guard, texture-only, dual-weapon pose, and visual resolver smoke tests. Expected: PASS.

**Step 2: Build Release**

Run: `dotnet build Source/BDP.sln -c Release`

Expected: build succeeds with zero errors.

**Step 3: Commit**

Commit only the new test, guard Verb change, render prefix, and this plan document with message `feat: 让注视警戒持械视觉跟随目标`.
