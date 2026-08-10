# Combat Body Collapse Smoke Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 被动崩解正式收尾时，在可选紧急脱离开始前于 Pawn（角色）原地释放一次原版烟雾。

**Architecture:** 直接在 `CombatBodyExitTransaction`（战斗体退出事务）的 Collapse（被动崩解）分支调用一个私有烟雾方法。该方法只复用 RimWorld（边缘世界）原版 `GenExplosion.DoExplosion`（爆炸生成接口），不新增抽象、配置或跨程序集接线。

**Tech Stack:** C#（C# 语言）7.3、.NET Framework 4.8、RimWorld 1.6 API（应用程序接口）、PowerShell（脚本）冒烟测试

---

### Task 1: 锁定被动崩解烟雾顺序

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`

**Step 1: Write the failing test**

新增 UTF-8（编码）PowerShell（脚本）测试，读取：

```powershell
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$exitTransactionText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8
```

测试必须断言：

```powershell
$exitTransactionText -match 'if \(exitMode == CombatBodySessionExitMode\.Collapse\)[\s\S]*RemoveCollapsePendingHediff\(ownerPawn\);[\s\S]*ReleaseCollapseSmoke\(ownerPawn\);[\s\S]*emergencyEscapeService\.ExecuteEmergencyEscapeIfAvailable'
```

并断言私有方法包含：

```powershell
GenExplosion.DoExplosion(
    ownerPawn.Position,
    ownerPawn.Map,
    2.4f,
    DamageDefOf.Smoke,
    null,
    -1,
    -1f,
    null,
    null,
    null,
    null,
    null,
    0f,
    1,
    GasType.BlindSmoke);
```

同时断言该方法先检查 `ownerPawn == null || !ownerPawn.Spawned || ownerPawn.Map == null`。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
```

Expected: FAIL（失败），提示缺少 `ReleaseCollapseSmoke(ownerPawn)`（释放崩解烟雾）调用或方法。

**Step 3: Commit the failing test**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
git commit -m "test(bdp): 锁定战斗体崩解烟雾顺序"
```

### Task 2: 实现原版烟雾释放

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`

**Step 1: Write minimal implementation**

引入 `RimWorld` 命名空间：

```csharp
using RimWorld;
```

在 Collapse（被动崩解）分支中插入：

```csharp
RemoveCollapsePendingHediff(ownerPawn);
ReleaseCollapseSmoke(ownerPawn);
emergencyEscapeService.ExecuteEmergencyEscapeIfAvailable(
    ownerPawn,
    owner.HostState.CachedCollapseEmergencyEscape);
```

新增逐成员中文注释的私有方法：

```csharp
/// <summary>
/// 在被动崩解正式收尾时，于紧急脱离前在原地释放一次原版烟雾。
/// </summary>
private static void ReleaseCollapseSmoke(Pawn ownerPawn)
{
    if (ownerPawn == null || !ownerPawn.Spawned || ownerPawn.Map == null)
    {
        return;
    }

    GenExplosion.DoExplosion(
        ownerPawn.Position,
        ownerPawn.Map,
        2.4f,
        DamageDefOf.Smoke,
        null,
        -1,
        -1f,
        null,
        null,
        null,
        null,
        null,
        0f,
        1,
        GasType.BlindSmoke);
}
```

**Step 2: Run focused test to verify it passes**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
```

Expected: PASS（通过）。

**Step 3: Run combat-body regression tests**

Run:

```powershell
Get-ChildItem "模组工程\BorderDefenseProtocol\Source\BDP.Tests" -Filter "CombatBody*SmokeTests.ps1" |
    ForEach-Object {
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File $_.FullName
        if ($LASTEXITCODE -ne 0) { throw "测试失败：$($_.Name)" }
    }
```

Expected: 所有 CombatBody（战斗体）冒烟测试通过。

**Step 4: Run Release build**

Run:

```powershell
dotnet build "模组工程\BorderDefenseProtocol\Source\BDP\BDP.csproj" -c Release
```

Expected: Build succeeded（编译成功），0 errors（零错误）。

**Step 5: Commit implementation**

```powershell
git add -- `
  "模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs" `
  "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
git commit -m "feat(bdp): 被动崩解前释放原版烟雾"
```

### Task 3: 记录工作并核对提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志NN.md`（选择当前未满 20 条且没有用户未提交改动的最新日志；若无法安全追加则新建下一编号）

**Step 1: Write work log**

按时间倒序记录：

- 仅被动崩解释放烟雾；
- 原版半径 `2.4` 格 `BlindSmoke`（致盲烟雾）；
- 调用发生在可选紧急脱离之前；
- 主动解除不受影响；
- 已运行的测试与编译结果。

**Step 2: Verify diff scope**

Run:

```powershell
git status --short
git diff --check
git log -4 --oneline
```

Expected: 本任务只涉及计划、测试、退出事务和独立工作日志；既有其他改动保持不变。

**Step 3: Commit work log**

```powershell
git add -- "日志/Agent工作日志/<本次日志文件>"
git commit -m "docs(log): 记录战斗体崩解烟雾实现"
```
