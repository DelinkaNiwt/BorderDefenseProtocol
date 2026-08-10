# Combat Body Collapse Smoke Parameter Adjustment Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将战斗体被动崩解烟雾调整为半径 `2.0` 格，并仅为这次烟雾指定初始气体浓度 `8`。

**Architecture:** 保留现有 `CombatBodyExitTransaction.ReleaseCollapseSmoke`（战斗体退出事务.释放崩解烟雾）结构与调用时序，只调整同一个原版 `GenExplosion.DoExplosion`（爆炸生成接口）的局部实参。使用 `postExplosionGasAmount: 8`（爆炸后气体浓度）命名参数，不修改任何全局 Def（定义）或气体网格规则。

**Tech Stack:** C#（C# 语言）7.3、.NET Framework 4.8、RimWorld 1.6 API（应用程序接口）、PowerShell（脚本）冒烟测试

---

### Task 1: 锁定新的局部烟雾参数

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`

**Step 1: Write the failing test**

把烟雾方法断言中的半径改为 `2.0f`，并要求 `GasType.BlindSmoke`（致盲烟雾）后明确传入：

```csharp
postExplosionGasAmount: 8
```

另加否定断言，禁止生产代码继续保留 `2.4f` 或使用全局气体修改入口。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
```

Expected: FAIL（失败），提示崩解烟雾缺少半径 `2.0` 和局部浓度 `8`。

**Step 3: Commit the failing test**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
git commit -m "test(bdp): 锁定崩解烟雾局部参数"
```

### Task 2: 调整唯一的原版烟雾调用

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/CombatBodyCollapseSmokeReleaseSmokeTests.ps1`

**Step 1: Write minimal implementation**

把调用尾部调整为：

```csharp
GenExplosion.DoExplosion(
    ownerPawn.Position,
    ownerPawn.Map,
    2.0f,
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
    GasType.BlindSmoke,
    postExplosionGasAmount: 8);
```

不修改其他方法、参数、原版 Def（定义）或气体网格。

**Step 2: Run focused test**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\CombatBodyCollapseSmokeReleaseSmokeTests.ps1"
```

Expected: PASS（通过）。

**Step 3: Run CombatBody regression tests**

Run:

```powershell
$tests = Get-ChildItem "模组工程\BorderDefenseProtocol\Source\BDP.Tests" -Filter "CombatBody*SmokeTests.ps1"
foreach ($test in $tests) {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File $test.FullName
    if ($LASTEXITCODE -ne 0) { throw "测试失败：$($test.Name)" }
}
```

Expected: 全部通过。

**Step 4: Run Release builds**

Run:

```powershell
dotnet build "模组工程\BorderDefenseProtocol\Source\BDP\BDP.csproj" -c Release
dotnet build "模组工程\BorderDefenseProtocol\Source\BDP.Content\BDP.Content.csproj" -c Release
```

Expected: 两个程序集均为 0 warnings（零警告）、0 errors（零错误）。

**Step 5: Commit implementation**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs"
git commit -m "tune(bdp): 缩短崩解烟雾范围与寿命"
```

### Task 3: 更新工作日志

**Files:**
- Modify: `日志/Agent工作日志/Agent日志14.md`

**Step 1: Add newest entry**

在日志最上方记录：

- 半径最终调整为 `2.0`；
- 本次调用初始浓度为 `8`；
- 参数只影响崩解烟雾，不修改全局气体规则；
- 测试与编译结果。

**Step 2: Commit work log**

```powershell
git add -- "日志/Agent工作日志/Agent日志14.md"
git commit -m "docs(log): 记录崩解烟雾参数调整"
```
