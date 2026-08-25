# Light Soul Dual Guard Inset Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task.

**Goal:** Make the Light Soul shield use the same body-adjacent horizontal bases in dual-weapon drawing as in single-weapon drawing, without changing any other visual behavior.

**Architecture:** Keep the existing four-facing resolver and hand rules unchanged. Express the business adjustment entirely in the dual Light Soul visual preset, with a focused XML contract test protecting the approved values.

**Tech Stack:** RimWorld 1.6 XML Defs, PowerShell smoke tests, .NET builds.

---

### Task 1: Lock the approved dual-weapon position contract

**Files:**
- Modify: `Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulRealWeaponBoundarySmokeTests.ps1`

**Step 1: Change the focused assertions before production configuration**

Require the dual south/north distance to equal the single distance `0.12`, require both east/west bases to equal `0.28`, and keep the existing `SideDeltaX = 0.04` hand separation.

Also replace the older real-weapon boundary assertion that requires the guard dual preset to be farther out than the single preset. Keep the corresponding flexible-shield assertion unchanged because this request does not modify that visual.

**Step 2: Run the test and observe the intended failure**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1
```

Expected: FAIL because the current dual values are still `0.30` and `0.44`.

### Task 2: Apply the minimal Content configuration change

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Update only the dual preset**

Change dual `SouthNorthPose/DefaultOffset` from `(0.30, 0, 0.18)` to `(0.12, 0, 0.18)` and dual `EastWestPose/SideBaseX` from `0.44` to `0.28`. Update the adjacent Chinese comments so they describe shared body-adjacent bases instead of extra dual-weapon expansion.

**Step 2: Re-run the focused test**

Run the same PowerShell test. Expected: PASS.

### Task 3: Verify, document, and commit

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md`

**Step 1: Run related regression tests and production builds**

Run the focused Light Soul guard tests and build `BDP.csproj` plus `BDP.Content.csproj` in `Release`（发布）配置. Expected: all selected tests pass and both builds have zero errors.

**Step 2: Inspect scope**

Run `git diff --check` and inspect status/diff so no unrelated user changes enter the task.

**Step 3: Add a newest-first work-log entry and commit**

Record the two value changes, unchanged visual rules, test/build evidence, and commit only the files owned by this task.
