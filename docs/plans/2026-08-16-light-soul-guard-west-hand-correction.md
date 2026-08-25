# Light Soul Guard West Hand Correction Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task.

**Goal:** Correct the Light Soul guard west-facing main/sub-hand mirror, depth layer, and near/far results to the approved truth table.

**Architecture:** Add one neutral, opt-in east/west pose policy that makes the final displayed mirror depend only on hand side while preserving RimWorld's existing aim mesh behavior. Configure both Light Soul guard presets to use that policy and the existing facing-dependent front/back selection.

**Tech Stack:** RimWorld 1.6 C#, Unity mesh mirroring, XML Defs, PowerShell smoke tests.

---

### Task 1: Lock the corrected four-case contract

**Files:**
- Modify: `Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`

**Step 1:** Require a default-off `FinalMirrorByHandOnly` field and resolver logic that includes west facing when computing the additional hand mirror.

**Step 2:** Replace the old east/west truth table with: east main original/front/near, east sub mirrored/back/far, west main original/back/far, west sub mirrored/front/near. Require both Light Soul guard presets to enable the mirror policy and disable `MainHandAlwaysFront`.

**Step 3:** Run both tests with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ...`. Expected: FAIL because the field and corrected configuration do not yet exist.

### Task 2: Implement the minimal neutral policy and Content configuration

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualEastWestPoseConfig.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1:** Add and document `public bool FinalMirrorByHandOnly = false;`.

**Step 2:** When enabled, compute the extra hand mirror from `isSubHand XOR facingWest`; otherwise preserve the existing `isSubHand` behavior. Keep the feature default off.

**Step 3:** On both Light Soul guard presets set `FinalMirrorByHandOnly` to `true` and `MainHandAlwaysFront` to `false`.

**Step 4:** Re-run the two focused tests. Expected: PASS.

### Task 3: Verify and record

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md`

**Step 1:** Run related visual and Light Soul regression tests.

**Step 2:** Build Core and Content in Release（发布）configuration with zero errors.

**Step 3:** Inspect the exact diff, add the newest-first work-log entry, and commit only task files.
