# Light Soul Guard Directional Pose Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Use separate front and side shield resources and produce the approved four-facing, two-hand Light Soul guard pose for both single-weapon and dual-weapon presentation.

**Architecture:** Reuse RimWorld `Graphic_Multi` for directional materials, while the existing BDP equipment-pose resolver continues to own weapon angle and mesh mirroring. Add only neutral Core support for resolved directional materials, functional east/west sub-hand mirroring, and an opt-in main-hand-front pose policy; author the Light Soul behavior in Content XML.

**Tech Stack:** RimWorld 1.6 C#, Unity materials and meshes, Harmony draw patch, XML Defs, PowerShell smoke tests.

---

### Task 1: Lock the neutral directional-material and east/west hand rules

**Files:**
- Create: `Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualEastWestPoseConfig.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/ResolvedVisualPose.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/ResolvedVisualOverlayPose.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`

**Step 1: Write the failing test**

Create a PowerShell boundary test that requires:

```powershell
Assert-True ($eastWestConfigText -match 'public bool MainHandAlwaysFront = false;') `
    '东西姿态必须提供默认关闭的主手固定前景能力。'
Assert-True ($resolverText -match 'pose\.MainHandAlwaysFront\s*\?\s*!isSubHand') `
    '主手固定前景必须只在作者显式开启时接管前后景裁定。'
Assert-True ($resolverText -match 'bool handMirror = pose\.HandMirror && isSubHand;') `
    '东西姿态手侧镜像必须只给副手额外翻转一次。'
Assert-True ($resolverText -match 'graphic\.MatAt\(facing, sourceThing\)') `
    '完整姿态必须按人物朝向解析 Graphic_Multi 材质。'
Assert-True ($drawPatchText -match 'pose\.DrawMaterial') `
    '正式绘制必须使用姿态解析出的最终方向材质。'
```

**Step 2: Run the test to verify it fails**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1
```

Expected: FAIL because `MainHandAlwaysFront` and `DrawMaterial` do not exist and east/west hand mirroring is not applied.

**Step 3: Implement the minimal neutral capability**

In `ExpressionVisualEastWestPoseConfig`, add and comment:

```csharp
public bool MainHandAlwaysFront = false;
```

In `VisualPoseResolver.ResolveEastWestOffset`, preserve the current rule by default and opt into the approved policy only when configured:

```csharp
bool isFront = pose.MainHandAlwaysFront
    ? !isSubHand
    : sample.Facing == Rot4.East ? !isSubHand : isSubHand;
bool handMirror = pose.HandMirror && isSubHand;
```

Return `HandMirror = handMirror`, `HandMirrorAllowed = pose.HandMirror`, and `ForceHandMirror = pose.HandMirror` so the sub-hand flip is also valid for fixed east/west aim angles.

Add `Material DrawMaterial` to resolved main and overlay poses. Resolve it once with a small shared member:

```csharp
private static Material ResolveDrawMaterial(Graphic graphic, Rot4 facing, Thing sourceThing)
{
    return graphic != null ? graphic.MatAt(facing, sourceThing) : null;
}
```

Populate the material for main and overlay poses. Make `DrawGraphicPose` accept the resolved material and stop reading `graphic.MatSingle`.

**Step 4: Run the focused test and build Core**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1
dotnet build Source/BDP/BDP.csproj -c Release
```

Expected: test PASS; build succeeds with zero errors.

**Step 5: Commit**

```powershell
git add Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1 Source/BDP/Core/Expressions/Config/ExpressionVisualEastWestPoseConfig.cs Source/BDP/Core/Trigger/Visual/ResolvedVisualPose.cs Source/BDP/Core/Trigger/Visual/ResolvedVisualOverlayPose.cs Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs
git commit -m "feat: 支持方向材质与东西手位镜像"
```

### Task 2: Lock and author the Light Soul four-facing guard pose

**Files:**
- Create: `Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulRealWeaponBoundarySmokeTests.ps1`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Replace: `1.6/Textures/Effects/Shield/energy_shield_block_curved.png`
- Create: `1.6/Textures/Effects/Shield/energy_shield_block_curved_north.png`
- Create: `1.6/Textures/Effects/Shield/energy_shield_block_curved_east.png`
- Create: `1.6/Textures/Effects/Shield/energy_shield_block_curved_south.png`
- Create: `1.6/Textures/Effects/Shield/energy_shield_block_curved_west.png`
- Source asset: `C:/NiwtDatas/Projects/RimworldModStudio/日志/大盾朝东主手.png`

**Step 1: Write the failing content test**

Require both guard presets to use the same `Graphic_Multi` base and the approved pose flags:

```powershell
Assert-True ($guard.GraphicData.graphicClass -eq 'Graphic_Multi') '举盾必须使用原版多朝向贴图。'
Assert-True ([single]$guard.SouthNorthPose.DefaultAngle -eq -68) '竖向正视图必须得到约 15 度向内斜握。'
Assert-True ([string]$guard.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true') '南北屏幕左侧必须镜像成向内斜握。'
Assert-True ([single]$guard.EastWestPose.DefaultAngle -eq -53) '侧视图必须抵消原版 53 度角并保持竖直。'
Assert-True ([string]$guard.EastWestPose.HandMirror -eq 'true') '副手必须额外水平镜像。'
Assert-True ([string]$guard.EastWestPose.MainHandAlwaysFront -eq 'true') '主手必须固定使用前景姿态。'
```

Also assert the four east/west truth-table results from `facingWest XOR isSubHand`, verify main always selects front and sub always selects back, verify all four 512 by 512 directional assets have alpha, and require dual south/north X distance to be greater than single distance.

**Step 2: Run the content tests to verify they fail**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/LightSoulChipSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/LightSoulRealWeaponBoundarySmokeTests.ps1
```

Expected: the new test fails on missing multi-directional assets and stale angles; the two existing tests fail once stale expectations are updated before Content changes.

**Step 3: Install the approved resources**

Preserve the user's modified front texture bytes as the north and south resources. Copy the provided east-main side texture bytes as both east and west resources; the existing weapon mesh plus sub-hand mirror performs the required runtime horizontal flips. Remove the now-unused unsuffixed single texture only after all four suffixed targets have been verified.

Expected texture base:

```text
Effects/Shield/energy_shield_block_curved
```

Expected files:

```text
energy_shield_block_curved_north.png  = wide front resource
energy_shield_block_curved_south.png  = wide front resource
energy_shield_block_curved_east.png   = narrow side resource
energy_shield_block_curved_west.png   = same narrow source; runtime mesh decides mirroring
```

**Step 4: Author the two guard presets**

For both `BDP_Visual_LightSoulShieldGuard` and `BDP_Visual_LightSoulShieldGuard_Dual`:

```xml
<graphicClass>Graphic_Multi</graphicClass>
```

Use the approved common pose values:

```xml
<SouthNorthPose>
  <DefaultAngle>-68</DefaultAngle>
  <HandMirrorOnlyWhenIdle>true</HandMirrorOnlyWhenIdle>
  <DefaultAltitudeOffset>0.08</DefaultAltitudeOffset>
  <NorthZAdjust>0.36</NorthZAdjust>
</SouthNorthPose>
<EastWestPose>
  <DefaultAngle>-53</DefaultAngle>
  <HandMirror>true</HandMirror>
  <MainHandAlwaysFront>true</MainHandAlwaysFront>
  <FrontAltitudeOffset>0.08</FrontAltitudeOffset>
  <BackAltitudeOffset>-0.05</BackAltitudeOffset>
</EastWestPose>
```

Start single south/north hand distance at `0.12`, dual at `0.30`; keep their current east/west bases `0.28` and `0.44`. Keep `Z = 0.18`, draw size `1.45`, color, shader, and all previously approved height and impulse values.

**Step 5: Update stale guard assertions**

Replace old “37 degrees keeps the horizontal source vertical” checks with final-angle checks for the vertical source:

```powershell
# South main and north sub resolve to +15 degrees after hand mirroring.
# South sub and north main resolve to -15 degrees.
# East and west resolve to 0 degrees before the explicit mesh mirror truth table.
```

Retain the `0.11` north-over-south final height assertion.

**Step 6: Run all focused tests**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/DirectionalVisualMaterialSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/LightSoulChipSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/LightSoulRealWeaponBoundarySmokeTests.ps1
```

Expected: all four tests PASS.

**Step 7: Commit**

```powershell
git add Source/BDP.Tests/LightSoulGuardDirectionalPoseSmokeTests.ps1 Source/BDP.Tests/LightSoulChipSmokeTests.ps1 Source/BDP.Tests/LightSoulRealWeaponBoundarySmokeTests.ps1 1.6/Content/Defs/ExpressionDef/Visual.xml 1.6/Textures/Effects/Shield/energy_shield_block_curved_north.png 1.6/Textures/Effects/Shield/energy_shield_block_curved_east.png 1.6/Textures/Effects/Shield/energy_shield_block_curved_south.png 1.6/Textures/Effects/Shield/energy_shield_block_curved_west.png 1.6/Textures/Effects/Shield/energy_shield_block_curved.png
git commit -m "feat: 实现光魂举盾四向姿态"
```

### Task 3: Regression verification and work log

**Files:**
- Modify or create: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md` or the next available log file according to the 20-entry limit

**Step 1: Run the broader visual regression suite**

Run all `Source/BDP.Tests/*Visual*SmokeTests.ps1` files plus the focused Light Soul tests. Expected: every invoked script prints PASS.

**Step 2: Build both production assemblies**

Run:

```powershell
dotnet build Source/BDP/BDP.csproj -c Release
dotnet build Source/BDP.Content/BDP.Content.csproj -c Release
```

Expected: both builds succeed with zero errors.

**Step 3: Inspect the final diff**

Run `git diff --check`, inspect `git status --short`, and confirm no unrelated user files are staged. Expected: only the implementation, tests, approved assets, and work log are part of this task.

**Step 4: Record the work log**

Add one newest-first entry describing the directional material support, four-way hand truth table, Light Soul XML values, assets, tests, builds, and commits. Do not exceed 20 entries in one log file.

**Step 5: Commit the log**

```powershell
git add C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/<selected-log-file>.md
git commit -m "docs: 记录光魂举盾四向姿态"
```

