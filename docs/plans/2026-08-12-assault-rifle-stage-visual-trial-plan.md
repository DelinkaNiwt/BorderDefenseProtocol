# Assault Rifle Stage Visual Trial Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为突击步枪枪壳的单武器与双武器预设配置预热换用霰弹枪贴图、射击及最终冷却隐藏的试验表现。

**Architecture:** 复用已经完成的 `StageVisuals`（阶段视觉）设施，只在正式 Content（内容层）的两个突击步枪独占预设中声明业务规则。Core（核心程序集）、攻击逻辑和霰弹枪预设保持不变。

**Tech Stack:** RimWorld 1.6、XML（可扩展标记语言）Def、PowerShell 回归测试。

---

### Task 1：写入并验证突击步枪阶段配置

**Files:**

- Create: `Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `../../日志/Agent工作日志/Agent日志43.md`

**Step 1：写失败测试**

测试按 `defName` 找到 `BDP_Visual_RangedWeaponReference` 与 `BDP_Visual_RangedWeaponReference_Dual`，断言两者均只声明 `Warmup`、`Firing`、`Cooldown`：预热贴图为 `Things/Trigger/Visual/ShotgunReferenceLan`，射击和冷却 `Visible=false`，不声明 `Idle`。

**Step 2：确认测试失败**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1`

Expected: 因两个预设尚无 `StageVisuals` 而失败。

**Step 3：写最小 XML 配置**

在两个预设的默认 `GraphicData` 后加入相同的三个阶段条目；每条写中文注释。不修改姿态、握持点、枪口或霰弹枪预设。

**Step 4：验证**

运行新测试、阶段配置／绘制／内容边界测试，并用 PowerShell 解析 `Visual.xml`。Expected: 全部通过。

**Step 5：提交配置与日志**

只暂存本任务测试、`Visual.xml`、设计计划文档和工作日志，避免夹带工作区其它改动。

