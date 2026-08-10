# 毒蛇齐射继承视觉默认值实施计划

> **For Claude（供 Claude 执行）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:test-driven-development（使用测试驱动开发技能）逐项实施本计划。

**Goal（目标）:** 将 BDP 南北/东西姿态的副侧角度默认值改为 0，并让毒蛇齐射预设删除显式绘制与姿态块后自动回退框架默认值。

**Architecture（实现方式）:** 主模组只修改两个中性配置类的字段默认值；DevHarness 只删除毒蛇视觉预设中的三个重复默认配置块。保留运行时解析器、其它预设、贴图和枪口不变。

**Tech Stack（技术栈）:** RimWorld 1.6（边缘世界 1.6）、C#（C#语言）、XML（可扩展标记语言）、PowerShell（微软命令行脚本）。

---

### Task 1：更新聚焦契约并验证红灯

**Files（文件）:**

- Modify（修改）: `Source/BDP.Tests/DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`
- Test（测试）: `Source/BDP.Tests/DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`

**Step 1：修改断言**

- 读取两个姿态配置类源码，要求 `SubHandAngleOffset = 0f`。
- 要求毒蛇视觉预设的 `DrawScale`、`SouthNorthPose`、`EastWestPose` 节点均为空。
- 继续断言 `GraphicData` 与 `Muzzle` 原值。
- 统计其它预设显式 `8` 和 `10` 的数量保持各 4 处。

**Step 2：运行测试验证失败**

Run（运行）:

```powershell
& .\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1
```

Expected（预期）: FAIL（失败），原因是类默认值仍为 30，且毒蛇仍存在三个显式配置块。

### Task 2：实施最小默认值修改

**Files（文件）:**

- Modify（修改）: `Source/BDP/Core/Expressions/Config/ExpressionVisualSouthNorthPoseConfig.cs:41`
- Modify（修改）: `Source/BDP/Core/Expressions/Config/ExpressionVisualEastWestPoseConfig.cs:43`

**Step 1：修改南北默认值**

将 `SubHandAngleOffset = 30f` 改为 `0f`，注释改为默认不附加角度、作者可按需显式配置。

**Step 2：修改东西默认值**

做同样的默认值与注释修改。

### Task 3：删除毒蛇重复默认配置

**Files（文件）:**

- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml:198`

**Step 1：删除三个配置块**

从 `BDP_TestVisual_PathLatchVolley` 删除：

```xml
<DrawScale>...</DrawScale>
<SouthNorthPose>...</SouthNorthPose>
<EastWestPose>...</EastWestPose>
```

不保留空节点，不修改 `GraphicData` 与 `Muzzle`。

**Step 2：验证聚焦测试转绿**

Run（运行）:

```powershell
& .\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1
```

Expected（预期）: `DevHarnessViperVolleyVisualDefaultsSmokeTests PASS`（测试通过）。

### Task 4：回归验证与版本控制

**Step 1：验证 XML**

解析 DevHarness 全部 XML 文件，预期无错误。

**Step 2：运行相关测试**

运行聚焦测试、单武器贴图替换测试和可到达本次视觉链的现有测试；对已知旧断言漂移单独记录，不扩大范围。

**Step 3：检查范围**

确认只有两个默认字段、对应注释、毒蛇三个配置块和聚焦测试发生本次修改；其它显式 `8`、`10` 保持原值。

**Step 4：记录日志并安全提交**

只提交能够与工作区既有改动安全分离的文件；不得吸收目标文件中本次开始前已有的未提交内容。

## 验证记录

- 聚焦继承默认值测试、视觉姿态诊断测试、视觉运行时边界测试、单武器贴图替换测试通过。
- DevHarness 23 个 XML（可扩展标记语言）文件全部解析通过；主模组与 DevHarness 的 Release（发布）配置编译通过。
- `VisualPoseResolverBoundarySmokeTests.ps1` 在现有“投射物初始化阶段不得冻结视觉发射原点”断言处失败，未进入本次副侧默认角度或毒蛇预设检查；该问题不在本次范围内。
