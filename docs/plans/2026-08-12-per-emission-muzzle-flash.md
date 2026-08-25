# Per-Emission Muzzle Flash Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让 BDP 每个成功发射计划在自己的武器枪口调用原版 `ShotFlash（射击闪光）`，并消除小人中心的重复闪光。

**Architecture:** 将来源武器的 `muzzleFlashScale（枪口闪光尺寸）` 纳入正式运行时规格，并在构建每发 `ProjectileInitPlan（投射物初始化计划）` 时冻结。发射成功后直接以现有枪口根坐标调用原版 `FleckMaker.Static`；BDP 宿主表面的尺寸设为零，阻止原版宿主层再次在射手格中心生成闪光。

**Tech Stack:** C#、RimWorld `Verb/Fleck`（动作/特效）接口、PowerShell 静态冒烟测试、MSBuild（微软构建工具）

---

### Task 1: 添加失败的行为边界测试

**Files:**
- Create: `Source/BDP.Tests/RangedMuzzleFlashOwnershipSmokeTests.ps1`

**Step 1:** 断言正式规格和每发计划保存枪焰尺寸。

**Step 2:** 断言 BDP 宿主表面的原版中心枪焰尺寸为零。

**Step 3:** 断言 `TryEmitPlan` 仅在发射成功后，以 `rootOrigin` 调用原版 `ShotFlash`。

**Step 4:** 运行测试并确认因功能尚未存在而失败。

### Task 2: 冻结每发枪焰尺寸

**Files:**
- Modify: `Source/BDP/Core/Expressions/Model/ResolvedVerbSpec.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ResolvedVerbSpecFactory.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/Model/ProjectileInitPlan.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`

**Step 1:** 在正式规格中保存作者声明的枪焰尺寸。

**Step 2:** 在每发计划中冻结来源结果自己的枪焰尺寸并参与存档。

**Step 3:** 把 BDP 宿主表面的枪焰尺寸设为零，避免中心重复闪光。

### Task 3: 在实际枪口播放原版闪光

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1:** 在投射物成功发射后调用原版 `FleckMaker.Static`。

**Step 2:** 使用 `rootOrigin` 而非随机散布后的 `launchOrigin`。

**Step 3:** 尺寸小于等于原版阈值时不生成闪光。

### Task 4: 验证与提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志*.md`

**Step 1:** 运行新增测试及相关枪口锚点、双持发射测试。

**Step 2:** 构建主模组并检查变更范围。

**Step 3:** 写入倒序工作日志，提交功能改动。
