# 光魂注视警戒朝向交还 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 目标暂时不可注视时保留手动锁定，同时把人物朝向控制权交还 RimWorld 原版。

**Architecture:** 保留现有非攻击 Job（作业）作为唯一必要的目标持有者。只把同一 Toil（作业步骤）的 `handlingFacing` 改为随正式 Verb（行为器）的 `CanHitTarget` 结果动态变化，不扩展全局 Harmony（运行时补丁）范围。

**Tech Stack:** RimWorld 1.6、C# 7.3、PowerShell 冒烟测试。

---

### Task 1: 建立失败回归测试

**Files:**
- Modify: `Source/BDP.Tests/LightSoulGuardWatchSmokeTests.ps1`

**Step 1: 写失败断言**

要求 Job 驱动先保存 `CanHitTarget(TargetA)` 结果，再把该结果赋给 `watchTarget.handlingFacing`；禁止保留固定 `handlingFacing = true`。

**Step 2: 运行测试确认失败**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulGuardWatchSmokeTests.ps1`

Expected: FAIL，原因是当前作业仍永久接管朝向。

### Task 2: 最小修正朝向接管

**Files:**
- Modify: `Source/BDP.Content/LightSoul/JobDriver_LightSoulGuardWatch.cs`

**Step 1: 动态计算朝向占用**

在现有 tickAction（逐刻动作）中执行：

```csharp
bool canWatchTarget = watchVerb.CanHitTarget(TargetA);
watchTarget.handlingFacing = canWatchTarget;
if (canWatchTarget)
{
    pawn.rotationTracker.FaceTarget(TargetA);
}
```

**Step 2: 删除永久接管**

移除 Toil 构造后的固定 `watchTarget.handlingFacing = true`，不修改其它文件。

**Step 3: 运行新增回归测试**

预期：`LightSoulGuardWatchSmokeTests PASS`。

### Task 3: 验证与提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志46.md`

**Step 1: 运行相关测试**

运行注视警戒、禁止暴力、光魂芯片和抵挡反馈测试。

**Step 2: 发布编译**

Run: `dotnet build Source/BDP.Content/BDP.Content.csproj -c Release --no-restore`

Expected: 0 warnings，0 errors（0 警告、0 错误）。

**Step 3: 更新日志并提交**

只提交本计划中的代码、测试、编译产物、文档和工作日志；保留工作区已有无关改动。
