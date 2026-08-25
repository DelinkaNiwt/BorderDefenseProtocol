# Dual Pose Anchor Alignment Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 同步突击步枪与霰弹枪的双武器握持目标，并让枪口锚点严格跟随最终贴图旋转和镜像。

**Architecture:** 配置层只统一一个姿态偏移。Core.dll（核心程序集）复用现有 `TransformGraphicLocalOffset`（贴图局部偏移变换），移除枪口的平行旧公式，确保贴图、握持点和枪口点共享同一变换真值。

**Tech Stack:** C#、XML（可扩展标记语言）、PowerShell、MSBuild。

---

### Task 1: 添加失败回归测试

**Files:**
- Create: `Source/BDP.Tests/VisualMuzzleGraphicTransformSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**Step 1:** 检查枪口解析成员必须调用现有唯一的 `TransformGraphicLocalOffset`（贴图局部偏移变换）。

**Step 2:** 拒绝枪口继续调用旧瞄准角四元数或旧瞄准镜像分支，并检查作者注释与最终语义一致。

**Step 3:** 将突击步枪期望偏移改为 `(0.24, 0, 0.03)`，运行测试并确认当前实现失败。

### Task 2: 实施最小修正

**Files:**
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualMuzzleConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualSouthNorthPoseConfig.cs`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1:** 枪口使用 `TransformGraphicLocalOffset`，保留额外世界偏移。

**Step 2:** 修正仍描述旧公式的成员注释。

**Step 3:** 同步突击步枪双武器偏移。

**Step 4:** 构建并确认新增测试转绿。

### Task 3: 回归、日志与提交

**Files:**
- Modify: `1.6/Assemblies/BDP.Core.dll`
- Modify: `1.6/Assemblies/BDP.Core.pdb`
- Modify: `../../日志/Agent工作日志/Agent日志43.md`

**Step 1:** 运行相关视觉与锚点测试。

**Step 2:** 更新倒序工作日志并检查计划外差异。

**Step 3:** 提交源码、配置、测试、程序集与日志。
