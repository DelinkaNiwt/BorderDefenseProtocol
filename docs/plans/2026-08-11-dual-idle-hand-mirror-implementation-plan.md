# Dual Idle Hand Mirror Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为双武器朝南静默姿态增加默认关闭的手侧强制镜像，使倒 V 由正确贴图方向形成，而不是由大角度旋转伪造。

**Architecture:** 在南北姿态配置增加一个非执行态专用开关，并复用现有手侧裁决、网格翻转和角度取反路径。只有双武器远程参考预设启用；单武器、旧预设、瞄准镜像和投射物链保持不变。

**Tech Stack:** C#（C#语言）、RimWorld 1.6（边缘世界 1.6）、Unity、XML（可扩展标记语言）、PowerShell（微软命令行脚本）、Git（版本控制工具）。

---

### Task 1: 建立失败契约

**Files:**
- Create: `Source/BDP.Tests/VisualInactiveHandMirrorSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**Step 1: Write failing tests**

新测试要求：

```text
ExpressionVisualSouthNorthPoseConfig.ForceHandMirrorWhenInactive 默认 false
ResolveSouthNorthOffset 读取 !request.IsExecutionActive
ResolveDrawAngle 使用 forceHandMirror || IsNearSouthNorthAim(aimAngle)
EastWest 姿态不强制手侧镜像
```

参考预设测试要求：

```text
DefaultOffset = (0.20, 0, 0.12)
ForceHandMirrorWhenInactive = true
DefaultAngle 和 SubHandAngleOffset 均省略
基础单武器预设不含该字段
```

**Step 2: Verify RED**

```powershell
& '.\Source\BDP.Tests\VisualInactiveHandMirrorSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedWeaponReferenceVisualSmokeTests.ps1'
```

Expected: 两项均因新字段和新解算条件不存在而失败。

### Task 2: 实现默认关闭的非执行态镜像

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualSouthNorthPoseConfig.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`

**Step 1: Add config member**

增加带中文注释的：

```csharp
public bool ForceHandMirrorWhenInactive = false;
```

**Step 2: Thread the decision through pose calculation**

在南北姿态解算中计算：

```csharp
ForceHandMirror = pose.ForceHandMirrorWhenInactive && !request.IsExecutionActive
```

东西姿态固定为 `false`。给 `PoseOffset` 增加 `ForceHandMirror`，并传给 `ResolveDrawAngle`。

**Step 3: Relax only the inactive gate**

把手侧镜像条件改为：

```csharp
if (handMirrorAllowed
    && handMirror
    && (forceHandMirror || IsNearSouthNorthAim(aimAngle)))
```

不改网格翻转和角度取反内容。

**Step 4: Verify Core test GREEN**

运行 `VisualInactiveHandMirrorSmokeTests.ps1`，预期通过；参考预设测试仍因 XML 未更新而失败。

### Task 3: 启用双武器静默镜像并撤销无效角度

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Update the dual preset**

双武器南北姿态改为：

```xml
<SouthNorthPose>
  <DefaultOffset>(0.20, 0, 0.12)</DefaultOffset>
  <ForceHandMirrorWhenInactive>true</ForceHandMirrorWhenInactive>
</SouthNorthPose>
```

删除 `DefaultAngle` 与 `SubHandAngleOffset`。

**Step 2: Verify focused GREEN**

运行新测试、参考预设、单武器、握持锚点和瞄准镜像测试，预期全部通过。

### Task 4: 构建、记录并提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志43.md`
- Build output: `1.6/Assemblies/BDP.Core.dll`
- Build output: `1.6/Assemblies/BDP.Core.pdb`

**Step 1: Build and deploy**

先隔离构建 Core，再以 Debug（调试）配置构建到游戏加载目录；预期 0 警告、0 错误，并通过 Core 依赖隔离检查。

**Step 2: Final verification**

运行相关测试、解析 `Visual.xml`、执行 `git diff --check`。

**Step 3: Record work log**

在 `Agent日志43.md` 顶部增加本次静默镜像记录，保持时间倒序。

**Step 4: Commit**

只暂存本计划列出的源码、配置、测试、构建输出和工作日志，提交信息：

```text
feat: mirror dual weapon at idle
```
