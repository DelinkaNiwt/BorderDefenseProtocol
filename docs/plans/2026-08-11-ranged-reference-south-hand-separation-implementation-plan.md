# Ranged Reference South-Hand Separation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give the ranged reference a symmetric 0.24-cell separation only during dual-weapon drawing while preserving vanilla single-weapon pose.

**Architecture:** Keep the base reference preset pose-free for the existing single-weapon texture-only path. Add a separate composite preset with the dual-only offset, and select it through the gun shell's existing `compositeVisualPresetDefName` override.

**Tech Stack:** RimWorld Def XML（定义配置）, PowerShell（命令行脚本）smoke test.

---

### Task 1: Lock the single/dual preset contract

**Files:**
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

1. Preserve the assertion that the base preset has no `SouthNorthPose` or `EastWestPose`.
2. Add a failing assertion requiring `BDP_Visual_RangedWeaponReference_Dual` with the same graphic, grip, and muzzle data as the base preset.
3. Require the dual preset's `SouthNorthPose.DefaultOffset` to equal `(0.12, 0, 0)` and be its only explicit pose child.
4. Require the assault-rifle shell to map normal visual to the base preset and composite visual to the dual preset.
5. Run the smoke test and confirm it fails because the dual preset and composite mapping are absent.

### Task 2: Add the dual-only composite preset

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `1.6/Content/Defs/ChipGunShellDef/Presets.xml`

1. Leave `BDP_Visual_RangedWeaponReference` pose-free.
2. Add `BDP_Visual_RangedWeaponReference_Dual` with the same graphic, grip, and muzzle plus a `SouthNorthPose` containing only `<DefaultOffset>(0.12, 0, 0)</DefaultOffset>`.
3. Add `<compositeVisualPresetDefName>BDP_Visual_RangedWeaponReference_Dual</compositeVisualPresetDefName>` to the assault-rifle shell.
4. Run `RangedWeaponReferenceVisualSmokeTests.ps1` and `SingleWeaponExplicitPoseVisualSmokeTests.ps1` and confirm both pass.
5. Parse both XML files and run `git diff --check`.

### Task 3: Record and commit

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`

1. Add one reverse-chronological work-log entry.
2. Stage only the three implementation files, the corrected design/plan documents, and the selected log.
3. Commit the scoped change.
