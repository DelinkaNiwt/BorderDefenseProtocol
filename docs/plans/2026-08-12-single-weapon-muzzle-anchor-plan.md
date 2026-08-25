# Single Weapon Muzzle Anchor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为单武器原版姿态绘制增加可视化与实际发射共用的枪口锚点。

**Architecture:** 在 `VisualPoseResolver（视觉姿态解析器）` 内建立一条原版姿态解析入口，供单武器绘制、诊断和发射原点共同调用；完整姿态入口保持双武器行为。通过 `HostEquipmentRenderMode（宿主装备绘制模式）` 选择正确入口。

**Tech Stack:** C#、RimWorld/Verse、Harmony（补丁库）、PowerShell、XML（可扩展标记语言）、.NET

---

### Task 1: 添加失败回归测试

**Files:**
- Create: `Source/BDP.Tests/SingleWeaponMuzzleAnchorSmokeTests.ps1`
- Modify: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

1. 断言单武器绘制、诊断和发射原点共用原版姿态解析入口。
2. 断言单武器入口不调用双武器偏移和握持姿态原点。
3. 断言霰弹枪单武器声明枪口值。
4. 运行并确认旧实现失败。

### Task 2: 实现原版姿态枪口解析

**Files:**
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/TriggerVisualLaunchOriginResolver.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs`

1. 抽取原版贴图角度与网格镜像计算。
2. 增加只按原版姿态解析主贴图、附加层和枪口锚点的入口。
3. 单武器绘制改用该结果，再应用已有后坐。
4. 诊断与发射原点在只替换贴图模式下使用同一入口。

### Task 3: 补充霰弹枪单武器数据

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

1. 为 `BDP_Visual_Shotgun` 添加 `IsRangedWeapon=true` 和 `MuzzleOffset=(0, 0, 0.48828125)`。
2. 保留无显式姿态、无握持点，确保单武器仍走原版姿态。

### Task 4: 验证、日志与提交

**Files:**
- Modify: `1.6/Assemblies/BDP.Core.dll`
- Modify: `1.6/Assemblies/BDP.Core.pdb`
- Modify: `1.6/Assemblies/BDP.Content.dll`
- Modify: `1.6/Assemblies/BDP.Content.pdb`
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`

1. 运行单武器、双武器、枪口、诊断、后坐与混合武器定向测试。
2. 构建 Core 和 Content，要求 0 警告、0 错误且 Core 隔离检查通过。
3. 运行 `git diff --check`，倒序记录工作日志并只提交本次文件。
