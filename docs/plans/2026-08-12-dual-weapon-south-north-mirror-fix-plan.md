# Dual Weapon South/North Mirror Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修正双武器南北朝向下位置已正确但贴图手侧镜像相反的问题。

**Architecture:** 继续由 `VisualPoseResolver（视觉姿态解析器）` 统一裁定屏幕侧与镜像；只反转南北分支的手侧镜像布尔条件，不改任何业务预设参数。通过现有 PowerShell 冒烟测试锁定公式和相关视觉边界。

**Tech Stack:** C#、RimWorld/Verse、PowerShell、XML（可扩展标记语言）、.NET

---

### Task 1: 锁定南北向镜像规则

**Files:**
- Modify: `Source/BDP.Tests/DualWeaponHandProjectionSmokeTests.ps1`

1. 增加断言，要求 `handMirror` 在朝南选择主手、朝北选择副手。
2. 运行测试并确认它因当前旧公式而失败。

### Task 2: 最小修正镜像裁决

**Files:**
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`

1. 将旧式 `isSubHand ^ North` 改为与新屏幕侧映射一致的 `isSubHand ^ South`。
2. 更新相邻中文注释，明确镜像跟随屏幕左侧，不改变其他姿态数据。
3. 运行定向视觉与来源隔离测试。

### Task 3: 构建、记录与提交

**Files:**
- Modify: `1.6/Assemblies/BDP.Core.dll`
- Modify: `1.6/Assemblies/BDP.Core.pdb`
- Modify: `1.6/Assemblies/BDP.Content.dll`
- Modify: `1.6/Assemblies/BDP.Content.pdb`
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`

1. 构建 Core 与 Content，要求 0 警告、0 错误且 Core 隔离检查通过。
2. 运行 `git diff --check` 和最终定向测试。
3. 倒序写入工作日志并只提交本次文件。
