# Light Soul Dual Guard Depth and Position Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Bring the dual-weapon Light Soul guard shield closer in side views, raise it slightly in all four facings, and place it beyond the paired weapon in south/north draw order.

**Architecture:** Keep the existing neutral pose resolver unchanged. Adjust only the dual guard Content XML values and lock the independent screen-position and draw-layer contracts in the existing PowerShell smoke test.

**Tech Stack:** RimWorld 1.6 XML Defs, PowerShell smoke tests, .NET Release builds.

---

### Task 1: Lock the approved values

**Files:**
- Modify: `Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`

**Step 1: Write the failing assertions**

Require the dual preset to use south/north offset `(0.12, 0, 0.23)`, north correction `0.46`, south/north draw layer magnitude `0.12`, east/west base X `0.12`, side base Z `0.23`, and unchanged side delta X `0.04`. Also require the single preset to retain its current values.

**Step 2: Verify the red test**

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`.

Expected: FAIL because the dual preset still contains the old position and layer values.

### Task 2: Apply the minimal Content change

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Change only the dual guard preset**

Write the approved values and update adjacent Chinese comments to distinguish screen position from draw-layer order.

**Step 2: Verify the green test**

Run the same focused test. Expected: PASS.

### Task 3: Regress, log, and commit

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md`

**Step 1: Run related visual and Light Soul tests**

Expected: all selected scripts print PASS.

**Step 2: Build formal assemblies**

Build `Source/BDP/BDP.csproj` and `Source/BDP.Content/BDP.Content.csproj` with `-c Release`. Expected: zero errors and zero warnings.

**Step 3: Check scope and commit**

Run `git diff --check`, add only task-owned files, add one newest-first work-log entry, and commit without including existing unrelated workspace changes.
