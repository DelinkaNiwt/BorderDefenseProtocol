# Ranged Weapon Reference Visual Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a default-pose ranged-weapon reference visual and assign it to the assault-rifle gun shell.

**Architecture:** Keep the change entirely in Content data: copy the approved texture, add one `ExpressionVisualPresetDef`, and redirect one gun-shell reference. Core rendering remains unchanged.

**Tech Stack:** RimWorld XML（可扩展标记语言）Defs, PNG（便携式网络图形）texture, PowerShell（命令行脚本）smoke test.

---

### Task 1: Add the failing reference-visual smoke test

**Files:**
- Create: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**Step 1: Write the failing test**

The test must assert that the copied texture exists and is 512 × 512, that `BDP_Visual_RangedWeaponReference` uses it with no explicit pose or scale, that its muzzle offset is `(0, 0, 0.48828125)`, and that the assault-rifle shell references it.

**Step 2: Run test to verify it fails**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

Expected: FAIL because the reference texture and preset do not exist yet.

### Task 2: Add the minimal Content configuration

**Files:**
- Create: `1.6/Textures/Things/Trigger/Visual/RangedWeaponReference.png`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `1.6/Content/Defs/ChipGunShellDef/Presets.xml`

**Step 1: Copy the approved image unchanged**

Copy `参考资源/通用资源/占位贴图/远程武器测试图.png` to the target texture path.

**Step 2: Add the minimal preset**

```xml
<!-- 远程武器基准参考视觉：保持默认绘制值，只声明贴图与右边缘中心枪口。 -->
<BDP.Core.Expressions.ExpressionVisualPresetDef>
  <defName>BDP_Visual_RangedWeaponReference</defName>
  <GraphicData>
    <texPath>Things/Trigger/Visual/RangedWeaponReference</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </GraphicData>
  <Muzzle>
    <IsRangedWeapon>true</IsRangedWeapon>
    <MuzzleOffset>(0, 0, 0.48828125)</MuzzleOffset>
  </Muzzle>
</BDP.Core.Expressions.ExpressionVisualPresetDef>
```

**Step 3: Redirect the assault-rifle gun shell**

Change only its `visualPresetDefName` to `BDP_Visual_RangedWeaponReference` and update the adjacent XML comment.

**Step 4: Run test to verify it passes**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

Expected: PASS.

### Task 3: Verify, log, and commit

**Files:**
- Update: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/<current-log>.md`

**Step 1: Build**

Run: `dotnet build Source/BDP/BDP.csproj --no-restore`

Expected: build succeeds with zero errors.

**Step 2: Review scoped changes**

Check the exact diff and confirm no existing unrelated changes are staged.

**Step 3: Write the work log**

Add one concise reverse-chronological entry without exceeding 20 entries in a log file.

**Step 4: Commit**

Stage and commit only the reference visual, smoke test, configuration changes, and the selected work-log file.
