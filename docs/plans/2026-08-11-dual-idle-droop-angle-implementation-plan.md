# 双武器静默自然下垂角 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为双武器远程参考预设增加 6 度对称内收装饰角，并部署带红色握把方向箭头的新版参考贴图。

**Architecture:** 不修改视觉解析器；继续使用已存在的手侧镜像和网格侧装饰角符号裁决。只在双武器 XML 预设增加统一角度，并替换同一路径贴图，从而保证单武器、执行态和通用设施不变。

**Tech Stack:** RimWorld XML 配置、PowerShell 烟雾测试、PNG 贴图、.NET/C# 构建。

---

### Task 1: 锁定双武器装饰角配置

**Files:**
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: 写失败测试**

将双武器南北姿态断言改为：

```powershell
$dualPreset.SouthNorthPose.DefaultAngle -eq '6' -and
$null -eq $dualPreset.SouthNorthPose.SubHandAngleOffset -and
@($dualPreset.SouthNorthPose.ChildNodes | Where-Object NodeType -eq 'Element').Count -eq 3
```

继续断言基础单武器预设没有 `SouthNorthPose`。

**Step 2: 运行测试确认失败**

Run: `& Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

Expected: FAIL，因为双武器预设尚无 `DefaultAngle=6`。

**Step 3: 最小配置修改**

在 `BDP_Visual_RangedWeaponReference_Dual/SouthNorthPose` 中加入：

```xml
<DefaultAngle>6</DefaultAngle>
```

更新同一条目的中文注释；不添加 `SubHandAngleOffset`，不修改握持目标和镜像开关。

**Step 4: 运行测试确认通过**

Run: `& Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

Expected: PASS。

### Task 2: 部署红箭头参考贴图

**Files:**
- Source: `C:/NiwtDatas/Projects/RimworldModStudio/参考资源/通用资源/占位贴图/远程武器测试图.png`
- Replace: `1.6/Textures/Things/Trigger/Visual/RangedWeaponReference.png`

**Step 1: 核对输入**

确认源贴图为 `512×512`，并记录 SHA256 哈希值。

**Step 2: 替换贴图**

将源 PNG 原样复制到模组目标路径，不改名、不重新编码。

**Step 3: 核对输出**

确认源和目标的尺寸、字节数与 SHA256 哈希值完全一致。

### Task 3: 回归、部署与提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志43.md`
- Update by build: `1.6/Assemblies/BDP.Core.dll`
- Update by build: `1.6/Assemblies/BDP.Core.pdb`

**Step 1: 运行针对性回归**

运行参考预设、单武器默认姿态、镜像逻辑、握持锚点和点位可视化测试，并解析 `Visual.xml`。

Expected: 全部 PASS。

**Step 2: 构建部署**

Run: `dotnet build Source/BDP/BDP.csproj --no-restore -c Debug`

Expected: Core 隔离检查 PASS，0 警告、0 错误，并更新游戏目录中的程序集。

**Step 3: 更新工作日志**

在 `Agent日志43.md` 顶部新增本次条目，按倒序重新编号，确保总条目不超过 20。

**Step 4: 检查并提交**

精确暂存本计划涉及的测试、XML、PNG、程序集和日志；运行 `git diff --cached --check` 后提交：

```text
tune: add natural droop to dual idle pose
```

**Step 5: 提交后复验**

重新运行针对性测试和 XML 解析，确认工作区只剩用户原有未跟踪文件。
