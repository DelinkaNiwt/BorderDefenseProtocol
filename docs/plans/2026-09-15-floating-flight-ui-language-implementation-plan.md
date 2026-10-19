# 浮空芯片名称与按钮文案 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复浮空芯片自定义界面的中文名称，并将模式切换按钮改为简短可读标签。

**Architecture:** 保留现有 `ChipFunctionDef.ResolvedLabel` 和浮空 Gizmo 逻辑；只补充 Keyed 语言键，并让按钮标签使用独立短模式名，详细说明继续由现有描述和右键菜单提供。影子绘制不参与本次改动。

**Tech Stack:** RimWorld 1.6、C# 7.3、PowerShell 烟测、XML 语言包、Harmony 现有 Content 工程。

---

### Task 1: 添加回归测试

**Files:**
- Modify: `Source/BDP.Tests/FloatingFlightDefinitionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FloatingFlightGizmoSmokeTests.ps1`

**Step 1:** 增加 Keyed 芯片名称与三条短模式键断言，并把按钮标签断言改为只使用 `ShortModeLabel(current)`。

**Step 2:** 运行两个烟测，确认它们因语言键和短标签实现尚不存在而失败。

### Task 2: 修复语言键与按钮标签

**Files:**
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Gameplay.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Commands.xml`
- Modify: `Source/BDP.Content/FloatingFlight/UI/FloatingFlightGizmoProvider.cs`

**Step 1:** 增加 `BDP_ChipFunction_Floating`、`BDP_FloatingFlight_ModeShort_Off`、`BDP_FloatingFlight_ModeShort_MoveOnly`、`BDP_FloatingFlight_ModeShort_Always`。

**Step 2:** 将按钮标签改为 `浮空：{短模式名}`，新增 `ShortModeLabel`；描述仍使用完整模式名和当前/下一模式参数。

**Step 3:** 重新运行两个烟测，确认通过。

### Task 3: 编译与回归验证

**Files:**
- Build: `Source/BDP.Content/BDP.Content.csproj`

**Step 1:** 运行全部 `FloatingFlight*SmokeTests.ps1`。

**Step 2:** 编译 Core、Content、Development Release 工程，确认无错误。

**Step 3:** 检查 Git 差异，只保留本次语言、按钮、测试、计划和工作日志变更后提交。
