# Ranged Reference Idle Pose Tuning Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 调整双武器远程参考预设的南向静默握持位置和副手角度，形成更高、更外、轻微不对称的双持轮廓。

**Architecture:** 复用现有双武器专用视觉预设和握持点姿态原点换算，只调整 XML（可扩展标记语言）数据。基础单武器预设继续没有显式姿态，Core（核心程序集）和 Content（内容程序集）源码均不修改。

**Tech Stack:** RimWorld 1.6（边缘世界 1.6）、XML（可扩展标记语言）、PowerShell（微软命令行脚本）、Git（版本控制工具）。

---

### Task 1: 锁定双武器试验数据

**Files:**
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Test: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**Step 1: Write the failing test**

把双武器预设断言改为要求：

```powershell
($dualPreset.SouthNorthPose.DefaultOffset -eq '(0.16, 0, 0.06)') -and
($dualPreset.SouthNorthPose.SubHandAngleOffset -eq '6')
```

继续断言基础预设没有 `SouthNorthPose`。

**Step 2: Run test to verify it fails**

Run:

```powershell
& '.\Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
```

Expected: FAIL，因为双武器预设仍为 `(0.12, 0, 0)` 且没有副手角度差。

### Task 2: 写入静默双持试验姿态

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Write minimal implementation**

把 `BDP_Visual_RangedWeaponReference_Dual` 的南北姿态改为：

```xml
<SouthNorthPose>
  <DefaultOffset>(0.16, 0, 0.06)</DefaultOffset>
  <SubHandAngleOffset>6</SubHandAngleOffset>
</SouthNorthPose>
```

同步更新相邻中文注释，不改其它预设。

**Step 2: Run focused tests**

Run:

```powershell
& '.\Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\SingleWeaponTextureOnlyVisualSmokeTests.ps1'
& '.\Source\BDP.Tests\VisualGripAnchorSmokeTests.ps1'
```

Expected: 三项均输出 `PASS`。

### Task 3: 校验、记录并提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`

**Step 1: Verify data**

解析 `Visual.xml` 并运行 `git diff --check`，预期无错误。

**Step 2: Record work log**

在 `Agent日志43.md` 顶部增加本轮姿态调试记录，保持时间倒序。

**Step 3: Commit**

只暂存测试、`Visual.xml` 和工作日志，提交信息：

```text
tune: raise and spread dual reference pose
```
