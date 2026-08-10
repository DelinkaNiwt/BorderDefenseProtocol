# 全部芯片武器视觉统一回退默认值 Implementation Plan（实施计划）

> **For Claude（供执行代理使用）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:test-driven-development（使用测试驱动开发） to implement this plan task-by-task（逐项执行本计划）。

**Goal（目标）:** 将 DevHarness（伴生测试模组）全部现有武器视觉预设统一为缺省缩放、缺省姿态和无横向枪口偏移。

**Architecture（实现方式）:** 不修改主模组解析逻辑，也不为无视觉配置的芯片新增节点。只修改现有 5 个视觉预设中的 4 个未收口预设，使它们与已完成的毒蛇齐射预设使用同一回退规则，并同步维护现有配置烟雾测试。

**Tech Stack（技术栈）:** RimWorld Def XML（边缘世界定义配置）、PowerShell（命令行脚本）烟雾测试。

---

### Task 1：用聚焦测试锁定全部预设的目标状态

**Files（文件）:**
- Modify（修改）: `Source/BDP.Tests/DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`

**Step 1：扩展预期表**

为 5 个现有武器视觉预设声明预期枪口向量：

```powershell
$expectedMuzzleOffsets = [ordered]@{
    BDP_TestVisual_RangedSequential = '(0, 0, 0.68)'
    BDP_TestVisual_RangedSequential_Composite = '(0, 0, 0.72)'
    BDP_TestVisual_RangedVolley = '(0, 0, 0.58)'
    BDP_TestVisual_RangedVolley_Composite = '(0, 0, 0.61)'
    BDP_TestVisual_PathLatchVolley = '(0, 0, 0.68)'
}
```

逐项断言：

```powershell
($null -eq $preset.DrawScale)
($null -eq $preset.SouthNorthPose)
($null -eq $preset.EastWestPose)
($preset.Muzzle.MuzzleOffset -eq $expectedMuzzleOffset)
($null -eq $preset.Muzzle.HasSubHandMuzzleOffsetOverride)
($null -eq $preset.Muzzle.SubHandMuzzleOffsetOverride)
```

同时断言 XML（可扩展标记语言）中不再存在显式 `SubHandAngleOffset`（副侧角度偏移）。

**Step 2：运行测试确认失败**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
```

Expected（预期）: FAIL（失败），原因是其余 4 个预设仍显式配置缩放、姿态或横向枪口偏移。

### Task 2：最小修改 4 个视觉预设

**Files（文件）:**
- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`

**Step 1：删除绘制和姿态覆盖**

从以下 4 个预设删除 `DrawScale`、`SouthNorthPose`、`EastWestPose`，每个预设只保留一条中文注释说明回退默认值：

```text
BDP_TestVisual_RangedSequential
BDP_TestVisual_RangedSequential_Composite
BDP_TestVisual_RangedVolley
BDP_TestVisual_RangedVolley_Composite
```

**Step 2：收口枪口配置**

分别写入：

```xml
<MuzzleOffset>(0, 0, 0.68)</MuzzleOffset>
<MuzzleOffset>(0, 0, 0.72)</MuzzleOffset>
<MuzzleOffset>(0, 0, 0.58)</MuzzleOffset>
<MuzzleOffset>(0, 0, 0.61)</MuzzleOffset>
```

删除所有 `HasSubHandMuzzleOffsetOverride` 和 `SubHandMuzzleOffsetOverride`，保留 `IsRangedWeapon` 与 `ExtraWorldOffset`。

**Step 3：运行聚焦测试确认通过**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
```

Expected（预期）: PASS（通过）。

### Task 3：同步现有双武器视觉配置断言

**Files（文件）:**
- Modify（修改）: `Source/BDP.Tests/DevHarnessDualWeaponVisualConfigSmokeTests.ps1`

**Step 1：删除旧作者姿态和值断言**

让 `Assert-VisualPreset`（断言视觉预设）改为检查：

- 原贴图和 `drawSize`（绘制尺寸）保持不变。
- 不存在 `DrawScale`、`SouthNorthPose`、`EastWestPose`。
- `MuzzleOffset` 等于各预设新的零横向向量。
- 不存在副侧枪口覆盖开关和覆盖向量。

**Step 2：运行测试并区分既有阻断**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessDualWeaponVisualConfigSmokeTests.ps1'
```

Expected（预期）: 如果仍在既有 `ResourceBase`（原版资源基类）芯片块断言处提前失败，记录为本次范围外阻断；不得为了让测试全绿而修改芯片业务定义。

### Task 4：全量配置验证与工作日志

**Files（文件）:**
- Modify（修改）: `../../日志/Agent工作日志/Agent日志09.md`

**Step 1：验证芯片引用范围**

解析所有芯片 XML，确认：

- 芯片总数仍为 17。
- 仍只有原来的 4 枚芯片引用武器视觉预设。
- 视觉预设总数仍为 5。

**Step 2：验证全部 XML**

Run（运行）:

```powershell
$xmlFiles = @(Get-ChildItem -LiteralPath '..\BorderDefenseProtocol.DevHarness\1.6\Defs' -Recurse -File -Filter '*.xml')
foreach ($xmlFile in $xmlFiles) {
    [xml](Get-Content -Raw -Encoding utf8 -LiteralPath $xmlFile.FullName) | Out-Null
}
```

Expected（预期）: 全部文件解析成功。

**Step 3：检查差异并记录**

确认只改 4 个预设、相关断言、计划和工作日志。目标 XML 在任务开始前已有其它未提交改动，因此不得把整份文件连同无关差异一起提交。
