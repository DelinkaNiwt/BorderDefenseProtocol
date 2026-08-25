# 霰弹枪双武器下移外扩试调 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 只将霰弹枪朝南静默双武器姿态略向外并向下移动，改善角色头部附近的视觉拥堵。

**Architecture:** 不改 C# 解析器、握持锚点或贴图。更新霰弹枪专用回归测试，使其允许 `DefaultOffset` 与通用参考不同，但继续逐项验证角度、镜像、握持和枪口数据相同；随后只改一处 XML 数值。

**Tech Stack:** RimWorld XML、PowerShell 烟雾测试。

---

### Task 1: 锁定霰弹枪独立位置

**Files:**
- Modify: `Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: 修改测试期望**

将霰弹枪双武器 `DefaultOffset` 断言改为 `(0.24, 0, 0.03)`；继续要求 `DefaultAngle`、`ForceHandMirrorWhenInactive`、`Grip` 和 `Muzzle` 与参考双武器相同。

**Step 2: 运行测试确认失败**

Run: `& Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

Expected: FAIL，因为 XML 仍为 `(0.20, 0, 0.12)`。

**Step 3: 最小 XML 修改**

将 `BDP_Visual_Shotgun_Dual/SouthNorthPose/DefaultOffset` 改为：

```xml
<DefaultOffset>(0.24, 0, 0.03)</DefaultOffset>
```

同步更新该条目的中文注释，其它字段不动。

**Step 4: 运行测试确认通过**

Run: `& Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

Expected: PASS。

### Task 2: 回归与提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志43.md`

**Step 1: 回归验证**

运行霰弹枪视觉、远程基准、镜像、单武器默认姿态、握持点和点位可视化测试，并解析 `Visual.xml`。

**Step 2: 更新日志**

在 `Agent日志43.md` 顶部新增倒序记录，总条目保持不超过 20。

**Step 3: 精确提交**

只暂存测试、`Visual.xml` 和工作日志，执行差异检查后提交：

```text
tune: lower and spread shotgun dual pose
```

**Step 4: 提交后复验**

重跑相关测试并确认只剩用户既有未跟踪文件。
