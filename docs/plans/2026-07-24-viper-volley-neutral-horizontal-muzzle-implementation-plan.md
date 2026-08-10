# 毒蛇齐射枪口横向偏移归零 Implementation Plan（实施计划）

> **For Claude（供执行代理使用）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:test-driven-development（使用测试驱动开发） to implement this plan task-by-task（逐项执行本计划）。

**Goal（目标）:** 取消毒蛇齐射主副侧枪口横向偏移，同时保留前向偏移 `0.68` 和其它视觉配置。

**Architecture（实现方式）:** 不修改主模组解析逻辑，只调整 DevHarness（伴生测试模组）的单个视觉预设。主侧显式保留完整向量 `(0, 0, 0.68)`，副侧删除覆盖字段并使用现有回退逻辑继承主侧。

**Tech Stack（技术栈）:** RimWorld Def XML（边缘世界定义配置）、PowerShell（命令行脚本）烟雾测试。

---

### Task 1：锁定预期配置

**Files（文件）:**
- Modify（修改）: `Source/BDP.Tests/DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1`
- Modify（修改）: `Source/BDP.Tests/DevHarnessTrackingRangedModuleSmokeTests.ps1`

**Step 1：先写失败断言**

- 断言 `MuzzleOffset` 等于 `(0, 0, 0.68)`。
- 断言不存在 `HasSubHandMuzzleOffsetOverride`。
- 断言不存在 `SubHandMuzzleOffsetOverride`。

**Step 2：确认测试按预期失败**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
```

Expected（预期）: FAIL（失败），原因是当前主侧仍有 `0.04` 横向偏移，且副侧覆盖仍存在。

### Task 2：最小修改毒蛇齐射预设

**Files（文件）:**
- Modify（修改）: `../BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/ExpressionVisualPresetDefs_Test.xml`

**Step 1：修改配置**

将枪口片段收紧为：

```xml
<Muzzle>
  <IsRangedWeapon>true</IsRangedWeapon>
  <!-- 主副侧枪口共用前向 0.68 格，不添加横向偏移。 -->
  <MuzzleOffset>(0, 0, 0.68)</MuzzleOffset>
  <ExtraWorldOffset>(0, 0, 0)</ExtraWorldOffset>
</Muzzle>
```

**Step 2：确认聚焦测试通过**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessViperVolleyVisualDefaultsSmokeTests.ps1'
```

Expected（预期）: PASS（通过）。

### Task 3：回归验证与记录

**Files（文件）:**
- Modify（修改）: `../../日志/Agent工作日志/Agent日志09.md`

**Step 1：运行相关测试和 XML 解析检查**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessTrackingRangedModuleSmokeTests.ps1'
[xml](Get-Content -Raw -LiteralPath '..\BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml') | Out-Null
```

Expected（预期）: 新增或修改的断言通过；如测试被既有无关断言提前阻断，单独记录阻断位置。

**Step 2：检查差异**

只保留已确认的枪口横向偏移改动、对应断言、设计计划和工作日志，不修改其它视觉属性。
