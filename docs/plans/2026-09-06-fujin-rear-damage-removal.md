# 风刃后方追加伤害回收 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 移除风刃命中后对后方两格单位追加伤害的业务逻辑及其专用 Core 扩展，使风刃只对主命中目标执行原版 `Cut`（切割）伤害。

**Architecture:** 删除仅被方向斩击消费的 `FinalFlightDirection`、`DamageFactor` 和显式 `DamagePlan.TargetThing` 扩展；保留远程协议的通用 Impact（命中阶段）和额外伤害容器。额外伤害执行恢复为旧的主命中目标回退行为，避免影响未来其它通用模块。

**Tech Stack:** RimWorld 1.6、C#、XML Def、PowerShell 冒烟测试、旧式 MSBuild/.NET Release 构建。

---

### Task 1: 锁定 Core 专用扩展的删除边界

**Files:**
- Modify: `Source/BDP.Tests/FujinCoreContractSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FujinAuthoringSmokeTests.ps1`

**Step 1: Write the failing test**

要求 Core 契约测试确认 `ImpactStageContext` 不再暴露 `FinalFlightDirection`，`DamagePlan` 不再暴露 `TargetThing`；作者测试确认风刃仍只有距离重定向模块。

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\FujinCoreContractSmokeTests.ps1'
& '.\Source\BDP.Tests\FujinAuthoringSmokeTests.ps1'
```

Expected: 当前源码中仍有上述 Core 扩展，Core 契约测试失败；作者测试在方向斩击 Def 已移除的当前工作区通过。

### Task 2: 删除方向斩击专用 Core 扩展

**Files:**
- Modify: `Source/BDP/Core/Projectiles/RangedFlightProtocol/Impact/ImpactStageContext.cs`
- Modify: `Source/BDP/Core/Projectiles/RangedFlightProtocol/Model/DamagePlan.cs`
- Modify: `Source/BDP/Core/Projectiles/BdpProjectile.cs`

**Step 1: Implement the minimal code**

- 删除 Impact 上下文中最终飞行方向和命中阶段伤害倍率字段及其解析方法。
- 删除 `DamagePlan.TargetThing`。
- 额外伤害循环恢复为使用当前主命中目标，不再读取显式额外目标。
- 保留 `ExtraDamages`、`IImpactStageModule`、`HitCell` 和其它原有通用远程协议设施。

**Step 2: Run the focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\FujinCoreContractSmokeTests.ps1'
& '.\Source\BDP.Tests\FujinAuthoringSmokeTests.ps1'
```

Expected: 两项测试均输出 `PASS`。

### Task 3: 完成构建和边界验证

**Files:**
- Verify: `1.6/Assemblies/BDP.Content.dll`
- Verify: `1.6/Assemblies/BDP.Content.pdb`

**Step 1: Build**

Run:

```powershell
dotnet build '.\Source\BDP.Content\BDP.Content.csproj' -c Release --no-restore
```

Expected: 0 warnings, 0 errors。

**Step 2: Run architecture checks**

Run relevant XML, release-directory and Core-isolation checks. Expected: all pass;不检查后方两格游戏内伤害行为。

### Task 4: Record and commit

**Files:**
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志\Agent日志173.md`

**Step 1: Update the work log**

记录本次取消后方两格伤害、回收 Core 专用扩展、保留通用远程协议、测试和构建结果。

**Step 2: Commit**

```powershell
git add -- <本次实现文件>
git commit -m "refactor: 回收风刃后方伤害专用设施"
```
