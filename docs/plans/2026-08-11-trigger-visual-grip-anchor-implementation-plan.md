# Trigger Visual Grip Anchor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a formal visual grip anchor and show per-side grip markers through the existing diagnostics overlay.

**Architecture:** Core owns grip configuration, final pose-space resolution, and diagnostics snapshot data. The existing Development marker drawer only consumes those resolved fields; no second diagnostics facility is introduced.

**Tech Stack:** C#（C#语言）, RimWorld/Unity, XML（可扩展标记语言）, PowerShell（命令行脚本）smoke tests.

---

### Task 1: Add failing grip-anchor contract tests

**Files:**
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/TriggerVisualMarkerOverlaySmokeTests.ps1`

**Steps:**
1. Require `<Grip><GripOffset>(0, 0, -0.1953125)</GripOffset></Grip>` in the reference preset.
2. Require the grip config, resolved anchor, pose property, resolver method, diagnostics fields, and existing drawer usage.
3. Require separate main/right and sub/left grip materials and center-to-grip links in the existing drawer.
4. Run the three tests and confirm they fail because grip support is absent.

### Task 2: Add Core grip configuration and resolution

**Files:**
- Create: `Source/BDP/Core/Expressions/Config/ExpressionVisualGripConfig.cs`
- Create: `Source/BDP/Core/Trigger/Visual/ResolvedGripAnchor.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/ResolvedVisualPose.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`

**Steps:**
1. Add the optional `Grip` block and `ResolveGrip()` accessor.
2. Add `ResolvedGripAnchor` with validity, source result, world position, and local offset.
3. Resolve the local grip point from final draw angle and mesh flip without changing `DrawPosition`.
4. Populate an invalid grip anchor when the preset has no grip configuration.

### Task 3: Extend the existing diagnostics chain

**Files:**
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsSnapshot.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs`
- Modify: `Source/BDP.Development/Trigger/Diagnostics/TriggerVisualMarkerOverlayDrawer.cs`

**Steps:**
1. Add `HasGripAnchor`, `GripWorldPosition`, and `GripLocalOffset` to the existing resident snapshot.
2. Copy the resolved grip values in the existing capture path.
3. Add warm-orange main/right and bright-cyan sub/left materials inside the existing drawer.
4. Draw each grip point and a same-side-color link from `ResolvedDrawPosition` inside the existing resident loop.
5. Do not add a new provider, toggle, map component, or draw entry point.

### Task 4: Configure, verify, log, and commit

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/<current-log>.md`

**Steps:**
1. Configure the reference grip offset `(0, 0, -0.1953125)`.
2. Run all three targeted smoke tests and confirm they pass.
3. Build `Source/BDP/BDP.csproj` and `Source/BDP.Development/BDP.Development.csproj` to isolated output directories.
4. Add one reverse-chronological work-log entry.
5. Stage and commit only grip-anchor files and the selected log.
