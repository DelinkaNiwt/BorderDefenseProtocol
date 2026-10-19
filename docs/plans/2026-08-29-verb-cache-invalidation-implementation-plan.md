# 正式远程宿主参数缓存失效修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让正式远程宿主在切换武装构型后重新解析原版连发参数，消除单双持射击间隔继承旧值的问题。

**Architecture:** 保留 BDP 现有的单一正式宿主和双持交替调度，只在 `verbProps` 绑定变化时清理原版 `Verb` 的两个私有惰性缓存。通过既有 Harmony 字段访问能力集中处理原版兼容边界。

**Tech Stack:** C# 7.3、RimWorld 1.6 `Assembly-CSharp`、Harmony、PowerShell 冒烟测试。

---

### Task 1: 添加缓存失效边界测试

**Files:**
- Create: `Source/BDP.Tests/FormalHostVerbCacheInvalidationSmokeTests.ps1`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`

**Step 1: Write the failing test**

检查正式宿主源码必须声明两个原版缓存字段访问器，并在 `SyncFormalBinding` 的绑定重置后、重新赋值 `verbProps` 前调用缓存清理方法。

**Step 2: Run test to verify it fails**

运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/FormalHostVerbCacheInvalidationSmokeTests.ps1
```

预期：FAIL，因为当前正式宿主没有缓存清理实现。

**Step 3: Write minimal implementation**

在 `BdpVerb_FormalHostShoot` 内集中封装原版 `cachedTicksBetweenBurstShots` 和 `cachedBurstShotCount` 的清理，并在 `resetApplied` 分支中调用；不触碰攻击计划、投射物或 Tick 逻辑。

**Step 4: Run test to verify it passes**

再次运行同一 PowerShell 测试，预期 PASS。

### Task 2: 构建与行为验证

**Files:**
- Build: `Source/BDP/BDP.csproj`
- Build: `Source/BDP.Content/BDP.Content.csproj`
- Log: `日志/Agent工作日志/Agent日志91.md`

**Step 1: Run focused tests**

运行缓存失效测试、攻击日志测试和诊断接收器测试，预期全部 PASS。

**Step 2: Build assemblies**

运行 Release 构建，预期 Core 隔离检查和两个项目构建均成功。

**Step 3: Review diff**

确认只保留缓存失效修复、测试、计划/日志及构建产物，不引入计时公式或双持调度改动。

**Step 4: Commit**

提交本次相关文件，保留工作区中其他已有用户改动不纳入提交。
