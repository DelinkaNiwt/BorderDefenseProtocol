# Kogetsu Held Pose Step 2 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让显式配置姿态的单武器进入现有完整姿态管线，并给弧月补回旧版主副侧四朝向参数。

**Architecture:** 主模组只增加一条中性选择规则：单枚激活武器的最终视觉预设声明南北或东西姿态时，宿主装备绘制模式由 `ReplaceTextureOnly`（只替换贴图）升级为 `Replace`（完整替换）；否则保持原版姿态快捷路径。DevHarness（伴生测试模组）只给弧月写入旧版参数，绘制继续复用现有 `VisualPoseResolver`（视觉姿态解析器），不复制算法。

**Tech Stack:** C#、RimWorld 1.6 / Verse Def 数据库、XML（可扩展标记语言）、PowerShell 静态冒烟测试、.NET `dotnet build`

---

## 执行约束

- 按项目 `AGENTS.md` 要求直接使用当前工作区，不创建 worktree（工作树）、分支或子代理。
- 当前仓库有大量用户已有改动；每次只暂存本计划明确列出的文件，提交前必须检查 `git diff --cached --name-only`。
- `ExpressionVisualPresetDefs_Test.xml`、`DefaultVisualProjectionBuilder.cs` 和第一步测试已有未提交依赖。若本次增量不能从当前索引安全分离，只提交设计、计划与工作日志，不把他人的整文件改动混入提交。
- 不改旋空弧月招式、攻击语义、近战时序、贴图资源或其它武器预设。

### Task 1: 用失败测试锁定单武器显式姿态切换规则

**Files:**

- Create: `Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1`
- Inspect: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs`
- Inspect: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs`

**Step 1: Write the failing test**

创建 PowerShell 静态冒烟测试，读取上述两个 C# 文件并断言：

```powershell
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'
$presetPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionVisualPresetDef.cs'
$builderPath = Join-Path $bdpSourceRoot 'Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$presetText = Get-Content -Raw -Encoding utf8 -LiteralPath $presetPath
$builderText = Get-Content -Raw -Encoding utf8 -LiteralPath $builderPath

Assert-True (
    $presetText -match 'bool\s+HasExplicitPose\s*=>\s*SouthNorthPose\s*!=\s*null\s*\|\|\s*EastWestPose\s*!=\s*null'
) '视觉预设必须公开只读的显式姿态语义。'

Assert-True (
    ($builderText -match 'using\s+Verse;') -and
    ($builderText -match 'VisualExpressionRelationKind\s+relationKind\s*=\s*ResolveRelationKind') -and
    ($builderText -match 'ResolveHostEquipmentRenderMode\(\s*residentEntries,\s*activeWeaponChipInstanceCount,\s*relationKind\s*\)')
) '视觉投影必须先确定关系，再用最终预设决定单武器绘制模式。'

Assert-True (
    ($builderText -match 'ResolveVisualPresetDefName') -and
    ($builderText -match 'relationKind\s*!=\s*VisualExpressionRelationKind\.SingleSide') -and
    ($builderText -match 'CompositeVisualPresetDefName') -and
    ($builderText -match 'DefDatabase<ExpressionVisualPresetDef>\.GetNamed') -and
    ($builderText -match '\.HasExplicitPose')
) '单武器姿态判断必须按普通/复合关系选出最终 Def，并读取其显式姿态。'

$singleWeaponBranch = [regex]::Match(
    $builderText,
    '(?s)if\s*\(\s*activeWeaponChipInstanceCount\s*==\s*1\s*\)\s*\{.*?\}'
).Value
Assert-True (
    ($singleWeaponBranch -match 'HostEquipmentRenderMode\.Replace') -and
    ($singleWeaponBranch -match 'HostEquipmentRenderMode\.ReplaceTextureOnly')
) '单武器必须只在最终预设显式声明姿态时升级为完整替换。'

Assert-True (
    ($builderText -match 'ResolveExecutionFocusPolicy') -and
    ($builderText -match 'ResolveMuzzleFollowPolicy') -and
    ($builderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualExecutionFocusPolicy\.None') -and
    ($builderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualMuzzleFollowPolicy\.None')
) '单武器完整姿态不得顺带开启执行焦点或枪口跟随。'

Write-Output 'SingleWeaponExplicitPoseVisualSmokeTests PASS'
```

**Step 2: Run test to verify it fails**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1
```

Expected: FAIL（失败），首先报告缺少 `HasExplicitPose`。

### Task 2: 实现中性的显式姿态选择

**Files:**

- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs:23-34`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs:1-27,172-199`
- Test: `Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1`
- Test: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`
- Test: `Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1`

**Step 1: Add the preset semantic**

在 `ExpressionVisualPresetDef` 的姿态字段后增加带成员注释的只读属性：

```csharp
/// <summary>
/// 当前预设是否由作者显式声明自定义手持姿态。
/// </summary>
public bool HasExplicitPose => SouthNorthPose != null || EastWestPose != null;
```

**Step 2: Resolve the relation once**

在 `Build` 中复用一次关系解析结果：

```csharp
List<VisualResidentEntry> residentEntries = CollectResidentEntries(snapshot);
int activeWeaponChipInstanceCount = CountActiveWeaponChipInstances(snapshot);
VisualExpressionRelationKind relationKind = ResolveRelationKind(snapshot, residentEntries);
return new VisualExpressionProjection
{
    RelationKind = relationKind,
    ResidentEntries = residentEntries,
    ActiveWeaponChipInstanceCount = activeWeaponChipInstanceCount,
    HostEquipmentRenderMode = ResolveHostEquipmentRenderMode(
        residentEntries,
        activeWeaponChipInstanceCount,
        relationKind),
    ExecutionFocusPolicy = ResolveExecutionFocusPolicy(residentEntries, activeWeaponChipInstanceCount),
    MuzzleFollowPolicy = ResolveMuzzleFollowPolicy(residentEntries, activeWeaponChipInstanceCount)
};
```

文件头加入：

```csharp
using Verse;
```

**Step 3: Upgrade only presets with explicit pose**

把宿主装备绘制模式方法改为接收关系，并增加两个小型只读辅助方法：

```csharp
private static HostEquipmentRenderMode ResolveHostEquipmentRenderMode(
    List<VisualResidentEntry> residentEntries,
    int activeWeaponChipInstanceCount,
    VisualExpressionRelationKind relationKind)
{
    if (residentEntries == null || residentEntries.Count == 0)
    {
        return HostEquipmentRenderMode.Keep;
    }

    if (activeWeaponChipInstanceCount == 1)
    {
        return HasExplicitPose(residentEntries, relationKind)
            ? HostEquipmentRenderMode.Replace
            : HostEquipmentRenderMode.ReplaceTextureOnly;
    }

    for (int i = 0; i < residentEntries.Count; i++)
    {
        if (residentEntries[i] != null && residentEntries[i].ForceSuppressHostEquipment)
        {
            return HostEquipmentRenderMode.Suppress;
        }
    }

    return HostEquipmentRenderMode.Replace;
}

/// <summary>
/// 判断当前单武器最终使用的任一视觉预设是否显式声明手持姿态。
/// </summary>
private static bool HasExplicitPose(
    List<VisualResidentEntry> residentEntries,
    VisualExpressionRelationKind relationKind)
{
    for (int i = 0; i < residentEntries.Count; i++)
    {
        string presetDefName = ResolveVisualPresetDefName(residentEntries[i], relationKind);
        if (string.IsNullOrWhiteSpace(presetDefName))
        {
            continue;
        }

        ExpressionVisualPresetDef preset =
            DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
        if (preset != null && preset.HasExplicitPose)
        {
            return true;
        }
    }

    return false;
}

/// <summary>
/// 按当前视觉关系解析条目最终使用的视觉预设名称。
/// </summary>
private static string ResolveVisualPresetDefName(
    VisualResidentEntry entry,
    VisualExpressionRelationKind relationKind)
{
    if (entry == null)
    {
        return null;
    }

    if (relationKind != VisualExpressionRelationKind.SingleSide
        && !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName))
    {
        return entry.CompositeVisualPresetDefName;
    }

    return entry.VisualPresetDefName;
}
```

同步更新原方法注释：无显式姿态的单武器只替换贴图；显式姿态单武器进入完整替换。

**Step 4: Run focused tests**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1
```

Expected: 三项 PASS（通过）。旧快捷路径仍存在，执行焦点和枪口跟随仍为 `None`（无）。

**Step 5: Check the focused diff**

Run:

```powershell
git diff --check -- Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1
git diff -- Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs
```

Expected: 无空白错误，只出现计划内增量。

### Task 3: 用失败测试锁定弧月旧版姿态参数

**Files:**

- Create: `Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1`
- Modify: `Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1:50-55`
- Inspect: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`

**Step 1: Remove the completed-stage prohibition**

第一步测试继续锁定贴图层和缩放，不再禁止第二步姿态：

```powershell
Assert-True (
    $null -eq $kogetsuPreset.DrawScale
) '弧月继续继承主模组默认绘制缩放。'
```

并把上方注释改为“弧月双层手持视觉的第一步结果必须保持不变”。

**Step 2: Write the failing pose test**

新测试解析 DevHarness XML，找到 `BDP_TestVisual_Kogetsu`，分别断言：

```powershell
$southNorth = $kogetsuPreset.SouthNorthPose
Assert-True (
    ($southNorth.DefaultOffset -eq '(-0.20, 0, 0.1)') -and
    ($southNorth.DefaultAngle -eq '-50') -and
    ($southNorth.DefaultAltitudeOffset -eq '0.05') -and
    ($southNorth.SouthZAdjust -eq '-0.05') -and
    ($southNorth.NorthZAdjust -eq '0.05') -and
    ($southNorth.SubHandAngleOffset -eq '15')
) '弧月南北姿态必须逐项复现旧版。'

$eastWest = $kogetsuPreset.EastWestPose
Assert-True (
    ($eastWest.SideBaseX -eq '0.08') -and
    ($eastWest.SideDeltaX -eq '0.03') -and
    ($eastWest.FrontAltitudeOffset -eq '0.05') -and
    ($eastWest.BackAltitudeOffset -eq '-0.05') -and
    ($eastWest.DefaultAngle -eq '-50') -and
    ($eastWest.SubHandAngleOffset -eq '15')
) '弧月东西姿态必须逐项复现旧版。'

Assert-True (
    ($null -eq $southNorth.AimMirror) -and
    ($null -eq $southNorth.HandMirror) -and
    ($null -eq $southNorth.MirrorOnNorth) -and
    ($null -eq $eastWest.SideDeltaZ) -and
    ($null -eq $eastWest.AimMirror) -and
    ($null -eq $eastWest.HandMirror)
) '与骨架默认值相同的姿态字段必须省略，避免重复配置。'
```

测试还应重复断言手柄与刀刃的 `texPath`、`shaderType` 和 `drawSize`，防止第二步破坏第一步。

**Step 3: Run test to verify it fails**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1
```

Expected: FAIL（失败），因为弧月尚无 `SouthNorthPose`。

### Task 4: 写入弧月八视角姿态配置

**Files:**

- Modify: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml:110-142`
- Test: `Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1`
- Test: `Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1`

**Step 1: Update comments**

把弧月预设块说明改成“第二阶段复现旧版主副侧四朝向手持姿态”，刀刃层说明改成“与手柄共享完整姿态”。

**Step 2: Add the exact legacy pose**

在 `GraphicData` 与 `OverlayLayers` 之间加入：

```xml
    <!-- 南北姿态：逐项映射旧版 defaultOffset、defaultAngle 与前后层级。 -->
    <SouthNorthPose>
      <DefaultOffset>(-0.20, 0, 0.1)</DefaultOffset>
      <DefaultAngle>-50</DefaultAngle>
      <DefaultAltitudeOffset>0.05</DefaultAltitudeOffset>
      <SouthZAdjust>-0.05</SouthZAdjust>
      <NorthZAdjust>0.05</NorthZAdjust>
      <SubHandAngleOffset>15</SubHandAngleOffset>
    </SouthNorthPose>
    <!-- 东西姿态：逐项映射旧版侧身位移、前后遮挡与副侧角度。 -->
    <EastWestPose>
      <SideBaseX>0.08</SideBaseX>
      <SideDeltaX>0.03</SideDeltaX>
      <FrontAltitudeOffset>0.05</FrontAltitudeOffset>
      <BackAltitudeOffset>-0.05</BackAltitudeOffset>
      <DefaultAngle>-50</DefaultAngle>
      <SubHandAngleOffset>15</SubHandAngleOffset>
    </EastWestPose>
```

不写 `SideDeltaZ`、`AimMirror`、`HandMirror`、`MirrorOnNorth` 等与新骨架默认值相同的冗余字段。

**Step 3: Run focused configuration tests**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1
```

Expected: 两项 PASS（通过）。

### Task 5: 验证 XML、编译和回归边界

**Files:**

- Verify: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs`
- Verify: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs`
- Verify: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`

**Step 1: Run all five focused tests**

Run:

```powershell
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1
pwsh -NoProfile -File Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1
```

Expected: 五项 PASS（通过）。

**Step 2: Parse all DevHarness XML**

Run:

```powershell
Get-ChildItem '..\BorderDefenseProtocol.DevHarness\1.6' -Recurse -Filter '*.xml' |
    ForEach-Object { [void]([xml](Get-Content -Raw -Encoding utf8 -LiteralPath $_.FullName)) }
```

Expected: 命令退出码 0，无 XML 解析异常。

**Step 3: Build both projects**

Run:

```powershell
dotnet build Source/BDP/BDP.csproj -c Release
dotnet build ..\BorderDefenseProtocol.DevHarness\Source/BDP.DevHarness/BDP.DevHarness.csproj -c Release
```

Expected: 两个项目均 0 error（错误）；记录 warning（警告）数量，不把已有警告误报为本次问题。

**Step 4: Inspect final scope**

Run:

```powershell
git diff --check -- Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs Source/BDP.Tests/SingleWeaponExplicitPoseVisualSmokeTests.ps1 Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1 Source/BDP.Tests/DevHarnessKogetsuHeldPoseSmokeTests.ps1 ..\BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml
git status --short
```

Expected: 无空白错误；本次文件与工作区原有改动边界清楚。

### Task 6: 记录结果并停在游戏内八视角确认点

**Files:**

- Modify: `../../日志/Agent工作日志/Agent日志10.md`（若已满 20 条则新建下一编号日志）

**Step 1: Read the latest log**

确认日志条数和倒序格式，只加入一条本次结果；不要改写已有记录。

**Step 2: Write the work log**

记录：

- 根因与采用的中性切换规则。
- 主模组与 DevHarness 的实际变更。
- 自动测试、XML 解析和编译结果。
- 本次未做的范围。
- 因工作区已有依赖改动而能否安全提交实现文件。
- 下一步只剩游戏内主/副侧 × 南/北/东/西共 8 种确认。

**Step 3: Commit only safely separable files**

优先单独提交新建测试、实施计划和日志。对已有未提交依赖的 C#/XML 文件，仅当 `git diff --cached` 能明确隔离本次增量时才提交；否则保留工作区并在汇报中说明。

**Step 4: Apply verification-before-completion**

使用 `superpowers:verification-before-completion`（完成前验证）重新读取本轮最后一次测试、XML 与编译输出，确认没有凭旧结果宣称完成。

**Step 5: Handoff**

向用户报告“自动实现已完成，等待游戏内确认”，并给出固定检查顺序：

```text
主侧：南 → 北 → 东 → 西
副侧：南 → 北 → 东 → 西
```

每项只检查位置、角度、镜像、前后遮挡，以及手柄和发光刀刃是否继续重合。
