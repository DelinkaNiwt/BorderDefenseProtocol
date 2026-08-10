# 弧月手持双层视觉第一步 Implementation Plan（实施计划）

> **For Claude（供执行代理使用）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:executing-plans（使用实施计划执行技能） and superpowers:test-driven-development（测试驱动开发技能） to implement this plan task-by-task（逐项执行本计划）。

**Goal（目标）:** 让单枚弧月武器芯片在继续沿用原版持握姿态的前提下，同时绘制手柄主层和独立发光刀刃层。

**Architecture（实现方式）:** 保留 `ReplaceTextureOnly`（只替换贴图）单武器快捷路径，只让它在计算一次原版位置、角度、镜像和后坐力后继续绘制视觉预设的 `OverlayLayers`（附加绘制层）。主模组只增加中性多层支持；弧月贴图、材质和引用只配置在 DevHarness（伴生测试模组），不进入 `VisualPoseResolver`（视觉姿态解析器）。

**Tech Stack（技术栈）:** RimWorld 1.6（边缘世界 1.6）、C#（C#语言）、XML（可扩展标记语言）、Harmony（和谐补丁库）、PowerShell（微软命令行脚本）。

**Workspace rule（工作区规则）:** 按项目约束直接在当前工程和当前分支执行，不创建 worktree（工作树）或新分支。目标文件已有其它未提交改动时，先记录基线，只暂存本计划新增的独立差异；不能安全拆分时宁可保留未暂存并记录原因，也不得吸收既有改动。

---

### Task 1：用失败测试锁定单武器附加层契约

**Files（文件）:**

- Create（新建）: `Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1`
- Inspect（检查）: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`
- Inspect（检查）: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`

**Step 1：记录目标文件基线**

运行：

```powershell
git status --short -- `
  'Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs' `
  'Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1'
```

保存补丁文件当前哈希与差异摘要，后续不得把本计划开始前的改动误当作本次内容提交。

**Step 2：新增聚焦失败测试**

测试读取绘制补丁源码并断言：

```powershell
$overlayDrawMatch = [regex]::Match(
    $drawPatchText,
    '(?s)private static void DrawTextureOnlyOverlayLayers\(.*?\n        \}')

Assert-True ($overlayDrawMatch.Success) `
    '单武器贴图替换路径必须提供独立的附加层绘制帮助方法。'

$overlayDrawBody = $overlayDrawMatch.Value
Assert-True (
    ($overlayDrawBody -match 'preset\.OverlayLayers') -and
    ($overlayDrawBody -match 'layer\.ResolveGraphic\(false,\s*sourceThing\)') -and
    ($overlayDrawBody -match 'layer\.OnlyWhenActive') -and
    ($overlayDrawBody -match 'layer\.LocalOffset') -and
    ($overlayDrawBody -match 'layer\.AltitudeOffset') -and
    ($overlayDrawBody -match 'layer\.AngleOffset') -and
    ($overlayDrawBody -match 'layer\.DrawScale')
) '单武器附加层必须沿用未激活态语义和已有附加层变换字段。'

Assert-True (
    ($overlayDrawBody -notmatch 'VisualPoseResolver') -and
    ($overlayDrawBody -notmatch 'VisualPoseRequest')
) '第一步不得进入完整视觉姿态解析器。'
```

同时断言 `TryDrawSingleWeaponTextureReplacement` 仍通过 `ReplaceTextureOnly` 路径进入，并继续保留原版 `EquipmentUtility.Recoil`（装备后坐力）计算。

**Step 3：运行测试确认红灯**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\SingleWeaponOverlayVisualSmokeTests.ps1'
```

Expected（预期）: FAIL（失败），原因是当前不存在 `DrawTextureOnlyOverlayLayers` 方法。

---

### Task 2：最小扩展单武器快捷绘制

**Files（文件）:**

- Modify（修改）: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`
- Test（测试）: `Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1`
- Test（测试）: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`

**Step 1：把当前预设交给原版姿态绘制方法**

将调用改为：

```csharp
DrawTextureOnlyReplacement(equipment, sourceThing, preset, graphic, sample);
```

为 `DrawTextureOnlyReplacement` 增加 `ExpressionVisualPresetDef preset` 参数，但保持现有位置、角度、镜像和后坐力计算不变。

**Step 2：复用一次计算结果绘制主层与附加层**

基础姿态计算完成后调用：

```csharp
DrawTextureOnlyGraphic(
    sourceThing,
    graphic,
    mesh,
    drawPosition,
    drawAngle,
    1f);
DrawTextureOnlyOverlayLayers(
    preset,
    sourceThing,
    mesh,
    drawPosition,
    drawAngle);
```

主层缩放继续固定为 `1f`，保持现有单武器“只替换材质和贴图尺寸”的契约。

**Step 3：增加通用单武器附加层方法**

按下列逻辑实现：

```csharp
private static void DrawTextureOnlyOverlayLayers(
    ExpressionVisualPresetDef preset,
    Thing sourceThing,
    Mesh mesh,
    Vector3 drawPosition,
    float drawAngle)
{
    if (preset?.OverlayLayers == null)
    {
        return;
    }

    for (int i = 0; i < preset.OverlayLayers.Count; i++)
    {
        ExpressionVisualOverlayLayerConfig layer = preset.OverlayLayers[i];
        if (layer == null || layer.OnlyWhenActive)
        {
            continue;
        }

        Graphic overlayGraphic = layer.ResolveGraphic(false, sourceThing);
        if (overlayGraphic == null)
        {
            continue;
        }

        Vector3 overlayPosition = drawPosition + layer.LocalOffset;
        overlayPosition.y += layer.AltitudeOffset;
        float overlayScale = layer.DrawScale > 0f
            ? layer.DrawScale
            : preset.ResolveDrawScale();
        DrawTextureOnlyGraphic(
            sourceThing,
            overlayGraphic,
            mesh,
            overlayPosition,
            drawAngle + layer.AngleOffset,
            overlayScale);
    }
}
```

把现有材质解析和 `Graphics.DrawMesh`（图形网格绘制）收进一个小型 `DrawTextureOnlyGraphic` 方法。该方法只接收已经解析好的网格、位置、角度和缩放，不重新计算姿态或后坐力。

**Step 4：运行聚焦测试确认绿灯**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\SingleWeaponOverlayVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\SingleWeaponTextureOnlyVisualSmokeTests.ps1'
```

Expected（预期）: 两项均输出 `PASS`（通过）。

**Step 5：检查提交可分离性**

若补丁文件中的本次方法依赖同文件尚未提交的 `ReplaceTextureOnly` 前置实现，不能在 Git（版本控制工具）索引中独立表示，则暂不提交该文件；记录依赖关系，禁止连同前置工作一起提交。若能安全拆分，只提交本任务测试与新增代码。

---

### Task 3：用失败测试锁定弧月两层配置

**Files（文件）:**

- Create（新建）: `Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1`
- Inspect（检查）: `../BorderDefenseProtocol.DevHarness/1.6/Textures/Things/Trigger/Chip/kogetsu_handle.png`
- Inspect（检查）: `../BorderDefenseProtocol.DevHarness/1.6/Textures/Things/Trigger/Chip/kogetsu_blade.png`

**Step 1：新增弧月配置测试**

测试解析视觉预设与芯片定义，要求：

```powershell
$kogetsuPreset = @($visualDefs.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_TestVisual_Kogetsu' }
$overlayLayers = @($kogetsuPreset.OverlayLayers.li)

Assert-True (
    ($kogetsuPreset.GraphicData.texPath -eq 'Things/Trigger/Chip/kogetsu_handle') -and
    ($kogetsuPreset.GraphicData.graphicClass -eq 'Graphic_Single') -and
    ($kogetsuPreset.GraphicData.shaderType -eq 'Cutout') -and
    ($kogetsuPreset.GraphicData.drawSize -eq '(1.2, 1.2)')
) '弧月主层必须使用旧版手柄贴图、裁切材质和尺寸。'

Assert-True (
    ($overlayLayers.Count -eq 1) -and
    ($overlayLayers[0].LayerId -eq 'kogetsu_blade') -and
    ($overlayLayers[0].GraphicData.texPath -eq 'Things/Trigger/Chip/kogetsu_blade') -and
    ($overlayLayers[0].GraphicData.graphicClass -eq 'Graphic_Single') -and
    ($overlayLayers[0].GraphicData.shaderType -eq 'MoteGlow') -and
    ($overlayLayers[0].GraphicData.color -eq '(1.0, 1.0, 0.95)') -and
    ($overlayLayers[0].GraphicData.drawSize -eq '(1.2, 1.2)')
) '弧月附加层必须使用旧版刀刃贴图、发光材质、颜色和尺寸。'
```

继续断言：

- `BDP_TestVisual_Kogetsu` 不含 `DrawScale`、`SouthNorthPose`、`EastWestPose`。
- `test_kogetsu_primary` 的 `Presentation.VisualPresetDefName` 等于 `BDP_TestVisual_Kogetsu`。
- 两个 PNG（便携式网络图形）文件均存在。

**Step 2：运行测试确认红灯**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessKogetsuHeldVisualSmokeTests.ps1'
```

Expected（预期）: FAIL（失败），原因是弧月视觉预设和表达条目引用尚未配置。

---

### Task 4：接入弧月业务视觉配置

**Files（文件）:**

- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`
- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_SenkuKogetsu.xml`
- Modify（修改）: `Source/BDP.Tests/DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`
- Test（测试）: `Source/BDP.Tests/DevHarnessKogetsuHeldVisualSmokeTests.ps1`

**Step 1：新增弧月视觉预设**

在现有视觉预设文件中新增：

```xml
<!-- 弧月单武器双层视觉：第一步只复现手柄与发光刀刃，不配置持握姿态。 -->
<BDP.Core.Expressions.ExpressionVisualPresetDef>
  <defName>BDP_TestVisual_Kogetsu</defName>
  <label>测试弧月手持视觉</label>
  <GraphicData>
    <texPath>Things/Trigger/Chip/kogetsu_handle</texPath>
    <graphicClass>Graphic_Single</graphicClass>
    <shaderType>Cutout</shaderType>
    <drawSize>(1.2, 1.2)</drawSize>
  </GraphicData>
  <OverlayLayers>
    <li>
      <LayerId>kogetsu_blade</LayerId>
      <GraphicData>
        <texPath>Things/Trigger/Chip/kogetsu_blade</texPath>
        <graphicClass>Graphic_Single</graphicClass>
        <shaderType>MoteGlow</shaderType>
        <color>(1.0, 1.0, 0.95)</color>
        <drawSize>(1.2, 1.2)</drawSize>
      </GraphicData>
    </li>
  </OverlayLayers>
</BDP.Core.Expressions.ExpressionVisualPresetDef>
```

不得添加 `DrawScale`、`SouthNorthPose` 或 `EastWestPose`。

**Step 2：让弧月表达条目引用预设**

在 `test_kogetsu_primary` 中加入：

```xml
<!-- 手持视觉只引用双层贴图预设；持握姿态留到第二步。 -->
<Presentation>
  <VisualPresetDefName>BDP_TestVisual_Kogetsu</VisualPresetDefName>
</Presentation>
```

不增加 `CompositeVisualPresetDefName`、`ForceSuppressHostEquipment` 或姿态字段。

**Step 3：同步既有视觉预设数量断言**

把现有“共 5 个视觉预设”断言改为“5 个远程预设加 1 个弧月预设”，但现有远程枪口循环仍只检查原来的 5 个名称：

```powershell
Assert-True (
    $weaponVisualPresets.Count -eq ($expectedMuzzleOffsets.Count + 1)
) 'DevHarness 应保留 5 个远程武器视觉预设，并新增 1 个弧月双层视觉预设。'
```

**Step 4：运行配置测试确认绿灯**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessKogetsuHeldVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessDualWeaponVisualConfigSmokeTests.ps1'
```

Expected（预期）: 弧月与毒蛇测试通过；双武器视觉测试若命中已有范围外断言失败，只记录现象，不修改无关业务定义。

**Step 5：提交可安全分离的配置差异**

只暂存本任务新增的弧月预设、`Presentation` 引用、数量断言和新测试。目标 XML 已有其它改动时使用逐块暂存；不得整文件吸收既有差异。

---

### Task 5：回归验证、记录与停在游戏确认点

**Files（文件）:**

- Modify（修改）: `../../日志/Agent工作日志/Agent日志*.md`
- Verify（验证）: `Source/BDP/BDP.csproj`
- Verify（验证）: `../BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj`

**Step 1：运行聚焦回归**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\SingleWeaponOverlayVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\SingleWeaponTextureOnlyVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessKogetsuHeldVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessDualWeaponVisualConfigSmokeTests.ps1'
```

Expected（预期）: 新增聚焦测试与现有单武器测试全部通过；任何既有测试阻断必须给出准确断言位置。

**Step 2：解析全部 DevHarness XML**

Run（运行）:

```powershell
$xmlFiles = @(Get-ChildItem -LiteralPath '..\BorderDefenseProtocol.DevHarness\1.6\Defs' -Recurse -File -Filter '*.xml')
foreach ($xmlFile in $xmlFiles) {
    [xml](Get-Content -Raw -Encoding utf8 -LiteralPath $xmlFile.FullName) | Out-Null
}
Write-Output "Parsed XML files: $($xmlFiles.Count)"
```

Expected（预期）: 全部 XML 文件解析成功。

**Step 3：编译主模组与 DevHarness**

Run（运行）:

```powershell
dotnet build '.\Source\BDP\BDP.csproj' -c Release
dotnet build '..\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\BDP.DevHarness.csproj' -c Release
```

Expected（预期）: 两个项目均 `Build succeeded`（编译成功）。

**Step 4：检查范围与 UTF-8 编码**

运行 `git diff --check`，确认本次只增加：

- 单武器附加层通用绘制。
- 两个聚焦测试。
- 弧月视觉预设及表达条目引用。
- 必要的既有数量断言调整。
- 设计、实施计划与工作日志。

不修改持握角度、位置偏移、南北/东西姿态或左右手规则。

**Step 5：写工作日志**

在 `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志` 中选择最新且不足 20 条的日志文件，按时间倒序写入本次变更、验证结果、提交号和“等待游戏内确认第一步”的状态；满 20 条则新建下一编号文件。

**Step 6：安全提交**

只提交能与工作区既有改动安全分离的本次文件或补丁块。提交后再次检查 `git status`，确认没有把用户既有改动带入提交。

**Step 7：停止并交付游戏内检查项**

向用户报告：

1. 手柄与发光刀刃两层已接通。
2. 自动测试、XML 和编译结果。
3. 需要进游戏确认人物手中两层是否重合、刀刃是否正常发光。

在用户确认前不得开始第二步角度、偏移和朝向调整。
