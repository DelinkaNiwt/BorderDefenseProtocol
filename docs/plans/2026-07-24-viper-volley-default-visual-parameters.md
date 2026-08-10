# 毒蛇齐射默认视觉参数实施计划

> 已由 `2026-07-24-viper-volley-inherited-visual-defaults.md` 取代；不再要求毒蛇齐射显式写出默认绘制与姿态字段。

> **For Claude（供 Claude 执行）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:test-driven-development（使用测试驱动开发技能）逐项实施本计划。

**Goal（目标）:** 将毒蛇齐射视觉预设中已显式配置的绘制缩放、南北姿态和东西姿态恢复为 BDP 默认值，其它配置保持不变。

**Architecture（实现方式）:** 不改主模组运行时代码，只修改 DevHarness 的一个视觉预设。使用现有 PowerShell（微软命令行脚本）烟雾测试锁定完整默认值和不变的贴图、枪口配置。

**Tech Stack（技术栈）:** RimWorld 1.6（边缘世界 1.6）、XML（可扩展标记语言）、PowerShell（微软命令行脚本）。

---

### Task 1：增加默认参数契约

**Files（文件）:**

- Modify（修改）: `Source/BDP.Tests/DevHarnessTrackingRangedModuleSmokeTests.ps1:254`
- Test（测试）: `Source/BDP.Tests/DevHarnessTrackingRangedModuleSmokeTests.ps1`

**Step 1：写入失败断言**

在 `BDP_TestVisual_PathLatchVolley` 现有贴图和枪口断言中增加：

```powershell
($pathLatchVolleyVisualBlock -match '<DrawScale>1</DrawScale>') -and
($pathLatchVolleyVisualBlock -match '<SouthNorthPose>\s*<DefaultOffset>\(0, 0, 0\)</DefaultOffset>\s*<DefaultAngle>0</DefaultAngle>\s*<DefaultAltitudeOffset>0\.1</DefaultAltitudeOffset>\s*<SouthZAdjust>0</SouthZAdjust>\s*<NorthZAdjust>0</NorthZAdjust>\s*<SubHandAngleOffset>30</SubHandAngleOffset>\s*<AimMirror>true</AimMirror>\s*<HandMirror>true</HandMirror>\s*<MirrorOnNorth>false</MirrorOnNorth>\s*</SouthNorthPose>') -and
($pathLatchVolleyVisualBlock -match '<EastWestPose>\s*<SideBaseX>0</SideBaseX>\s*<SideDeltaX>0</SideDeltaX>\s*<SideDeltaZ>0</SideDeltaZ>\s*<FrontAltitudeOffset>0\.1</FrontAltitudeOffset>\s*<BackAltitudeOffset>-0\.1</BackAltitudeOffset>\s*<DefaultAngle>0</DefaultAngle>\s*<SubHandAngleOffset>30</SubHandAngleOffset>\s*<AimMirror>true</AimMirror>\s*<HandMirror>false</HandMirror>\s*</EastWestPose>')
```

**Step 2：验证测试按预期失败**

Run（运行）:

```powershell
& .\Source\BDP.Tests\DevHarnessTrackingRangedModuleSmokeTests.ps1
```

Expected（预期）: FAIL（失败），错误指向毒蛇齐射视觉尚未使用默认绘制与姿态参数。

### Task 2：恢复显式默认值

**Files（文件）:**

- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml:198`

**Step 1：最小修改目标数值**

仅修改 `BDP_TestVisual_PathLatchVolley` 内原本已经显式存在的 `DrawScale`、`SouthNorthPose`、`EastWestPose` 字段，使其等于设计文档列出的类默认值。

**Step 2：同步修正失实注释**

删除“比标准 1.0 略大”“对应 DrawScale 1.2”等失实表述，说明绘制和姿态使用显式默认值；枪口数值保持不变。

**Step 3：验证聚焦测试转绿**

Run（运行）:

```powershell
& .\Source\BDP.Tests\DevHarnessTrackingRangedModuleSmokeTests.ps1
```

Expected（预期）: `DevHarnessTrackingRangedModuleSmokeTests PASS`（测试通过）。

### Task 3：回归验证与提交

**Files（文件）:**

- Verify（验证）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`
- Verify（验证）: `Source/BDP.Tests/DevHarnessDualWeaponVisualConfigSmokeTests.ps1`

**Step 1：解析 XML**

Run（运行）:

```powershell
[xml](Get-Content -Raw -Encoding utf8 '..\BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml') | Out-Null
```

Expected（预期）: 命令退出码为 0。

**Step 2：运行相关回归测试**

Run（运行）:

```powershell
& .\Source\BDP.Tests\DevHarnessDualWeaponVisualConfigSmokeTests.ps1
& .\Source\BDP.Tests\DevHarnessTrackingRangedModuleSmokeTests.ps1
```

Expected（预期）: 两项测试均输出 `PASS`（通过）。

**Step 3：检查差异**

Run（运行）:

```powershell
git diff --check
git diff -- Source/BDP.Tests/DevHarnessTrackingRangedModuleSmokeTests.ps1 ../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml
```

Expected（预期）: 无空白错误；差异只包含目标断言、目标参数和对应注释。

**Step 4：提交**

只暂存本计划涉及的测试、XML、设计、实施计划和工作日志，避开工作区内其它已有改动。

## 实施偏差记录

- 原计划准备在 `DevHarnessTrackingRangedModuleSmokeTests.ps1` 中追加断言；红灯阶段发现该测试会先因仍匹配旧 `ParentName="ResourceBase"` 而失败，无法证明本次默认参数需求。
- 为保持测试聚焦，最终新增 `DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`，直接解析目标视觉预设并逐字段核对默认值，同时锁定贴图与枪口不变。
- `DevHarnessDualWeaponVisualConfigSmokeTests.ps1` 也会先因现有旧 `Presentation`（表现入口）断言失败。两项旧测试漂移均不属于本次参数调整，未扩大范围修复。
