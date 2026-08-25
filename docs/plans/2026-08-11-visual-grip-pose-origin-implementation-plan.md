# Visual Grip Pose Origin Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Align dual-weapon graphics from their configured grip anchors while preserving every preset that does not opt in.

**Architecture:** Add one opt-in flag to the existing grip configuration. `VisualPoseResolver` subtracts the final rotated/mirrored grip offset from the pose target before resolving overlays, grip, and muzzle anchors; only the dual reference preset enables it.

**Tech Stack:** C#（C#语言）, RimWorld/Unity, XML（可扩展标记语言）, PowerShell（命令行脚本）smoke tests.

---

### Task 1: Add failing grip-origin contracts

**Files:**
- Modify: `Source/BDP.Tests/VisualGripAnchorSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

1. Require `ExpressionVisualGripConfig.UseAsPoseOrigin` with a false default.
2. Require `VisualPoseResolver.AlignDrawPositionToGrip` and the subtraction of `TransformGraphicLocalOffset(grip.GripOffset, calculation)` from `calculation.DrawPosition`.
3. Require the base reference preset to omit the flag and the dual preset to set it to `true`.
4. Run both tests and confirm they fail because the flag and resolver alignment are absent.

### Task 2: Implement opt-in pose-origin alignment

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualGripConfig.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`

1. Add the commented member `public bool UseAsPoseOrigin = false;`.
2. Add `AlignDrawPositionToGrip(VisualPoseRequest request, PoseCalculation calculation)`.
3. Return without changes when grip configuration is absent or the flag is false.
4. Otherwise subtract the transformed grip offset from `calculation.DrawPosition` before resolving overlays and anchors.
5. Run `VisualGripAnchorSmokeTests.ps1` and confirm only the XML opt-in assertion remains failing.

### Task 3: Enable alignment only for the dual reference preset

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

1. Leave the base reference preset's `Grip` block unchanged.
2. Add `<UseAsPoseOrigin>true</UseAsPoseOrigin>` only to `BDP_Visual_RangedWeaponReference_Dual`.
3. Run the grip-anchor, reference-preset, and single-weapon regression tests.
4. Parse the XML and run `git diff --check`.

### Task 4: Build, deploy, log, and commit

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`
- Build output: `1.6/Assemblies/BDP.Core.dll`
- Build output: `1.6/Assemblies/BDP.Core.pdb`

1. Build Core to an isolated output directory and confirm 0 warnings and 0 errors.
2. Build Core Debug to the normal game-loading directory for immediate in-game observation.
3. Add one reverse-chronological work-log entry.
4. Stage only the two source files, XML, two tests, Core build outputs, design/plan documents, and the selected log.
5. Commit the scoped change.
