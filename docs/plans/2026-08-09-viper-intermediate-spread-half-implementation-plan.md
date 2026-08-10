# 毒蛇中间续段散布减半 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将毒蛇路线引导模块中间续段的基准散布半径从 1.25 格降至 0.625 格，同时保持首段、最终段和原版精度影响链不变。

**Architecture:** 只调整 Content（内容层）RoutePath 的配置默认值与 XML Def（定义）默认值，不改 Core（核心层）精度快照、不改 RouteSegmentResolver（路线段解析器）的随机/安全算法。同步修改状态对象和模块缺省回退，确保新建路线、存档快照和缺失配置都使用同一数值。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 XML Def、PowerShell 静态烟测、dotnet build（构建）。

---

### Task 1: 更新失败期望并确认旧实现会被拦截

**Files:**
- Modify: `Source/BDP.Tests/RoutePathOrderedSpreadSmokeTests.ps1:38,104`

**Step 1: 修改静态检查期望**

将 `IntermediateSpreadRadius` 的 C# 与 XML 期望从 `1.25` 改为 `0.625`；最终段与其他精度参数期望保持不变。

**Step 2: 运行检查确认尚未实现时失败**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\RoutePathOrderedSpreadSmokeTests.ps1
```

Expected: FAIL，原因是当前配置仍为 `1.25`。

### Task 2: 同步四处运行时默认值与 XML

**Files:**
- Modify: `Source/BDP.Content/RangedModules/RoutePath/RoutePathConfig.cs:23`
- Modify: `Source/BDP.Content/RangedModules/RoutePath/RoutePathState.cs:218,246`
- Modify: `Source/BDP.Content/RangedModules/RoutePath/RoutePathModule.cs:668`
- Modify: `1.6/Content/Defs/RangedModuleDef/RoutePath.xml:22`

**Step 1: 修改配置默认值**

将上述四处中间段默认/回退值统一改为 `0.625f` 或 `0.625`。

**Step 2: 保持其他参数不变**

确认 `FinalSpreadRadius=0.30`、`HighAccuracySpreadScale=0.25`、`SpreadSafetyShrinkSteps=4`、安全检查与随机采样代码没有改动。

**Step 3: 运行静态检查确认通过**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\RoutePathOrderedSpreadSmokeTests.ps1
```

Expected: `RoutePathOrderedSpreadSmokeTests PASS`。

### Task 3: Release 构建与变更边界核验

**Files:**
- Verify only: `1.6/Assemblies/BDP.Core.dll`
- Verify only: `1.6/Assemblies/BDP.Content.dll`

**Step 1: 编译 Core**

Run:

```powershell
dotnet build .\Source\BDP\BDP.csproj -c Release --no-restore
```

Expected: 0 warnings（警告）、0 errors（错误），并输出 Core 隔离门禁 PASS。

**Step 2: 编译 Content**

Run:

```powershell
dotnet build .\Source\BDP.Content\BDP.Content.csproj -c Release --no-restore
```

Expected: 0 warnings、0 errors，Core 隔离门禁仍为 PASS。

**Step 3: 检查 Git 变更边界**

确认本次只提交路线配置、XML、静态检查、发布产物和工作日志；保留用户现有 BeamTrail 改动，不执行还原或覆盖。

**Step 4: 提交**

```powershell
git add -- Source/BDP.Content/RangedModules/RoutePath Source/BDP.Tests/RoutePathOrderedSpreadSmokeTests.ps1 1.6/Content/Defs/RangedModuleDef/RoutePath.xml 1.6/Assemblies/BDP.Core.dll 1.6/Assemblies/BDP.Core.pdb 1.6/Assemblies/BDP.Content.dll 1.6/Assemblies/BDP.Content.pdb
git commit -m "tune: 降低毒蛇中间续段散布"
```

