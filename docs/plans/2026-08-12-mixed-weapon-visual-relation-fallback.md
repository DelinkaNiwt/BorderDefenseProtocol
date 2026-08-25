# Mixed Weapon Visual Relation Fallback Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让任意两枚已接入视觉系统的武器芯片按各自双武器预设绘制，并让未声明视觉的 Combo（组合）继续回退。

**Architecture:** 只修改 Core.dll（核心程序集）的默认视觉投影决策，不触碰攻击复合解析。视觉关系先识别显式 Combo 视觉，再以不同武器芯片实例数量补足 DualWeapon（双武器）关系。

**Tech Stack:** C#、PowerShell 回归测试、MSBuild。

---

### Task 1: 增加视觉关系回归测试

**Files:**
- Create: `Source/BDP.Tests/VisualMixedWeaponRelationFallbackSmokeTests.ps1`

**Step 1:** 用当前已部署 `BDP.Core.dll` 的真实内部类型构造单武器、混合双武器、无视觉 Combo 和有视觉 Combo 快照。

**Step 2:** 运行测试，确认混合双武器及无视觉 Combo 用例因当前逻辑返回 `SingleSide` 或 `Combo` 而失败。

### Task 2: 最小修改视觉关系决策

**Files:**
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs`

**Step 1:** 将已统计的武器芯片实例数传给视觉关系解析。

**Step 2:** 仅允许显式声明视觉预设的可用 Combo 优先接管视觉。

**Step 3:** Combo 未声明视觉时继续回退；正式 DualWeapon 或至少两个武器芯片实例均返回 DualWeapon。

**Step 4:** 运行新增测试，确认由失败转为通过。

### Task 3: 构建、回归与交付

**Files:**
- Modify: `1.6/Assemblies/BDP.Core.dll`
- Modify: `1.6/Assemblies/BDP.Core.pdb`
- Modify: `../../日志/Agent工作日志/Agent日志43.md`

**Step 1:** 构建 Debug 配置并更新部署程序集。

**Step 2:** 运行新增测试和现有单武器、双武器视觉边界测试。

**Step 3:** 确认工作区只包含计划内改动，写入倒序工作日志并提交。

