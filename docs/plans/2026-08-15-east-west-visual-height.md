# BDP 武器东西姿态高度 Implementation Plan（实施计划）

> **For Claude（供执行者）：** REQUIRED SUB-SKILL（必需子技能）：使用 `superpowers:test-driven-development（测试驱动开发）` 逐项执行，并在结束前使用 `superpowers:verification-before-completion（完成前验证）`。

**Goal（目标）：** 为显式东西武器姿态增加共同屏幕纵向基准，使其中心与南向一致，同时保留双武器前低后高的透视分离。

**Architecture（架构）：** Core（核心层）在 `ExpressionVisualEastWestPoseConfig` 中提供中性的 `SideBaseZ` 字段，解析器以 `SideBaseZ ± SideDeltaZ` 计算最终纵向偏移，并让现有诊断快照完整公开该值。Content（内容层）只为七个正式显式姿态写入各自的共同高度；纯贴图替换路径不变。

**Tech Stack（技术栈）：** C#（C井语言）、RimWorld Def XML（边缘世界定义配置）、PowerShell（微软命令行脚本）冒烟测试、MSBuild（微软构建工具）。

---

### Task 1（任务一）：建立 Core 共同高度契约

**Files（文件）：**

- Modify（修改）：`Source/BDP.Tests/DualWeaponHandProjectionSmokeTests.ps1`
- Modify（修改）：`Source/BDP.Tests/TriggerVisualPoseDiagnosticsSmokeTests.ps1`
- Modify（修改）：`Source/BDP/Core/Expressions/Config/ExpressionVisualEastWestPoseConfig.cs`
- Modify（修改）：`Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify（修改）：`Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsSnapshot.cs`
- Modify（修改）：`Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs`

**Step 1：写入失败断言**

在手部投影测试中要求解析器包含：

```powershell
Assert-True (
    $poseResolverText -match 'float finalZ = pose\.SideBaseZ \+ \(isFront \? -pose\.SideDeltaZ : pose\.SideDeltaZ\);'
) '东西朝向必须在共同 Z 基准上叠加前后手透视差。'
```

在诊断测试中要求配置类、诊断快照和复制链均包含 `SideBaseZ`。

**Step 2：确认测试因字段缺失而失败**

Run（运行）：

```powershell
& 'Source\BDP.Tests\DualWeaponHandProjectionSmokeTests.ps1'
& 'Source\BDP.Tests\TriggerVisualPoseDiagnosticsSmokeTests.ps1'
```

Expected（预期）：FAIL（失败），指出缺少共同 Z 基准或诊断字段。

**Step 3：实现最小 Core 契约**

在东西姿态配置中增加逐成员中文注释和：

```csharp
public float SideBaseZ = 0f;
```

解析器改为：

```csharp
float finalZ = pose.SideBaseZ + (isFront ? -pose.SideDeltaZ : pose.SideDeltaZ);
```

诊断快照新增 `public float SideBaseZ { get; set; }`，诊断访问层从配置复制该值。

**Step 4：验证 Core 契约转绿并编译**

Run（运行）：

```powershell
& 'Source\BDP.Tests\DualWeaponHandProjectionSmokeTests.ps1'
& 'Source\BDP.Tests\TriggerVisualPoseDiagnosticsSmokeTests.ps1'
dotnet build 'Source\BDP\BDP.csproj' -c Release
```

Expected（预期）：两个测试 PASS（通过），Release（发布）编译零错误。

### Task 2（任务二）：建立并实现正式 Content 高度关系

**Files（文件）：**

- Create（新建）：`Source/BDP.Tests/VisualEastWestElevationParitySmokeTests.ps1`
- Modify（修改）：`Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`
- Modify（修改）：`Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Modify（修改）：`1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1：新增配置行为测试**

测试读取所有七个显式四向预设，并对每个预设计算：

```powershell
$southCommonZ = $defaultOffsetZ + $southZAdjust
$eastFrontZ = $sideBaseZ - $sideDeltaZ
$eastBackZ = $sideBaseZ + $sideDeltaZ
$eastCenterZ = ($eastFrontZ + $eastBackZ) / 2
Assert-True ([Math]::Abs($eastCenterZ - $southCommonZ) -lt 0.0001) '东西姿态中心必须与南向共同偏移一致。'
```

同时断言中型远程双武器仍为 `SideDeltaZ = 0.10`，东向主手／西向副手是前景，另两侧是背景；纯贴图替换单武器没有显式姿态。

在两项既有中型远程测试中增加 `SideBaseZ = 0.03` 的明确断言。

**Step 2：确认旧 XML 配置失败**

Run（运行）：

```powershell
& 'Source\BDP.Tests\VisualEastWestElevationParitySmokeTests.ps1'
& 'Source\BDP.Tests\MediumRangedVisualPresetInheritanceSmokeTests.ps1'
& 'Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
```

Expected（预期）：FAIL（失败），指出七个预设缺少 `SideBaseZ`。

**Step 3：写入正式 XML 数值**

为七个 `EastWestPose` 条目逐项增加中文注释和：

```text
弧月                         0.05
光魂灵活盾单／双             0.10
光魂举盾单／双               0.18
光魂重刃                     0.12
中型远程双武器基准           0.03
```

保留中型双武器 `SideDeltaZ = 0.10` 及全部其它姿态字段。

**Step 4：验证 Content 配置转绿**

Run（运行）：

```powershell
& 'Source\BDP.Tests\VisualEastWestElevationParitySmokeTests.ps1'
& 'Source\BDP.Tests\MediumRangedVisualPresetInheritanceSmokeTests.ps1'
& 'Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
```

Expected（预期）：全部 PASS（通过）。

### Task 3（任务三）：完整验证、日志与提交

**Files（文件）：**

- Create（新建）：`C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志47.md`

**Step 1：运行相关回归**

运行新东西高度测试、既有南北高度测试、光魂测试、中型远程测试、单武器原版姿态测试、手部投影与诊断测试。

**Step 2：运行构建和格式检查**

```powershell
dotnet build 'Source\BDP\BDP.csproj' -c Release
git diff --check
```

Expected（预期）：零编译错误，差异格式检查通过。

**Step 3：写工作日志**

`Agent日志46.md` 已满 20 条，因此新建 `Agent日志47.md`，按时间倒序记录本次根因、字段、配置值、验证结果和未触及范围。

**Step 4：限定暂存并提交**

只暂存本计划所列源码、测试、正式 XML、计划文档和工作日志，不纳入工作区既有无关改动。

```powershell
git commit -m "fix: 修正武器东西姿态共同高度"
```
