# Ranged Reference Soft Inverted V Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把双武器远程参考预设调整为更高、更外且不完全对称的柔和倒 V 静默姿态。

**Architecture:** 复用现有双武器专用预设、握持点姿态原点和南北角度字段，只修改 XML（可扩展标记语言）配置。单武器和 Core（核心程序集）行为保持不变。

**Tech Stack:** RimWorld 1.6（边缘世界 1.6）、XML（可扩展标记语言）、PowerShell（微软命令行脚本）、Git（版本控制工具）。

---

### Task 1: 锁定新姿态数据

**Files:**
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**Step 1: Write the failing test**

要求双武器预设满足：

```powershell
$dualPreset.SouthNorthPose.DefaultOffset -eq '(0.20, 0, 0.12)'
$dualPreset.SouthNorthPose.DefaultAngle -eq '-8'
$dualPreset.SouthNorthPose.SubHandAngleOffset -eq '20'
```

同时要求南北姿态只有这三个元素，基础单武器预设仍没有姿态配置。

**Step 2: Run test to verify it fails**

```powershell
& '.\Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
```

Expected: FAIL，因为当前仍为 `(0.16, 0, 0.06)`、主手 `0` 度、副手最终 `6` 度。

### Task 2: 写入柔和倒 V 数据

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Write minimal implementation**

把双武器预设的 `SouthNorthPose` 改为：

```xml
<SouthNorthPose>
  <DefaultOffset>(0.20, 0, 0.12)</DefaultOffset>
  <DefaultAngle>-8</DefaultAngle>
  <SubHandAngleOffset>20</SubHandAngleOffset>
</SouthNorthPose>
```

同步更新相邻中文注释。

**Step 2: Run focused tests**

```powershell
& '.\Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\SingleWeaponTextureOnlyVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\VisualGripAnchorSmokeTests.ps1'
```

Expected: 三项均输出 `PASS`。

### Task 3: 校验、记录并提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`

**Step 1: Verify**

解析 `Visual.xml`，运行 5 项相关测试与 `git diff --check`。

**Step 2: Record log**

在日志顶部增加柔和倒 V 试验记录，保持时间倒序。

**Step 3: Commit**

只暂存 XML、测试和工作日志，提交信息：

```text
tune: try soft inverted v dual pose
```
