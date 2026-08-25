# Light Soul Guard Final Height and Feedback Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task.

**Goal:** Finish the approved Light Soul shield feedback, single-guard side presentation, and north-facing height adjustments using Content configuration only.

**Architecture:** Keep Core and all image resources unchanged. Encode the single-guard side result by making its near/far and depth branches equivalent while retaining RimWorld's native west-facing mesh mirror; author the height and block-feedback values directly in XML.

**Tech Stack:** RimWorld 1.6 XML Defs, PowerShell smoke tests, .NET release builds.

---

### Task 1: Lock the approved configuration contracts

**Files:**
- Modify: `Source/BDP.Tests/LightSoulBlockFeedbackSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`

**Step 1:** Require both shield Hediffs to use `blockVisualImpulseDistance = 0.02` with 8 ticks unchanged.

**Step 2:** Require single guard east/west to resolve to distance `0.08`, foreground depth `+0.08`, east original and west mirrored independent of actual entry hand.

**Step 3:** Require single and dual guard south/east/west BDP screen height `0.23`, north `0.28`, with both `NorthZAdjust = 0.51`.

**Step 4:** Run the focused tests and confirm they fail on the old `0.04`, single-height `0.18`, and north-adjust values.

### Task 2: Apply the minimal XML changes

**Files:**
- Modify: `1.6/Content/Defs/HediffDef/LightSoul.xml`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1:** Change both block impulse distances to `0.02` and update their comments.

**Step 2:** Set the single guard side result to fixed `0.08` distance and foreground depth, disable extra hand mirroring, and set its shared side height to `0.23`.

**Step 3:** Set single south/north base height to `0.23` and both guard north adjustments to `0.51`.

**Step 4:** Re-run focused tests and confirm PASS.

### Task 3: Verify, record, and commit

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md`

**Step 1:** Run related Light Soul and four-facing regression tests.

**Step 2:** Build Core and Content in Release（发布）configuration with zero errors.

**Step 3:** Inspect scope, write the newest-first work log, and commit only task-owned files.
