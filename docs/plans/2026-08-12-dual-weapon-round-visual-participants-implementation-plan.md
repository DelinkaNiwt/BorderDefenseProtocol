# Dual Weapon Round Visual Participants Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让双武器整轮动作阶段读取计划中的全部实际参与武器，同时保留当前步骤与当前发射来源的既有逐发语义。

**Architecture:** 在现有视觉运行态增加独立的整轮参与结果集合，由最终 `RangedVerbEmissionPlan.StepSourceResultIds` 经攻击视觉桥接发布。阶段解析器只用该集合判断武器是否参与 `Warmup／Firing／Cooldown`，现有 cast 和 emit 集合继续服务执行焦点、枪口及逐发效果。

**Tech Stack:** RimWorld 1.6、C# 7.3、Harmony（方法补丁库）、PowerShell 烟雾回归测试、.NET Framework 4.8。

---

## 实施约束

- 直接使用当前工作区，不创建分支或 worktree（工作树）。
- 先写并运行失败测试，再修改生产代码。
- 不修改攻击节奏、原版姿态、枪口或后坐力逻辑。
- 不新增存档字段或逐 tick（游戏刻）视觉状态。
- 新增 C# 成员写中文 XML 文档注释，测试错误信息使用清晰英文。
- 不接触已退役的 `BorderDefenseProtocol.DevHarness（伴生测试模组）`。

### Task 1：锁定整轮参与者契约

**Files:**

- Create: `Source/BDP.Tests/DualWeaponRoundVisualParticipantsSmokeTests.ps1`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerVisualRuntimeState.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerVisualRuntimeStateOwner.cs`

**Step 1: 写失败测试**

测试断言：

- 运行态存在 `ActiveAttackParticipantResultIds`。
- `HasExecutionState` 包含该集合。
- 空状态初始化、执行状态发布和清理均处理该集合。
- 现有 `ActiveCastResultIds` 与 `ActiveEmitSourceResultIds` 不被改名或代用。

**Step 2: 运行测试并确认因新契约缺失而失败**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/DualWeaponRoundVisualParticipantsSmokeTests.ps1
```

**Step 3: 实现最小运行态契约**

在运行态与 owner 的发布入口增加只读列表参数和清理逻辑，不增加持久化。

**Step 4: 重跑测试并确认通过**

### Task 2：从整轮计划发布参与来源

**Files:**

- Modify: `Source/BDP.Tests/DualWeaponRoundVisualParticipantsSmokeTests.ps1`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionVisualRuntimeBridge.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: 扩展失败测试**

断言远程上下文先独立发布当前 cast 和 emit；`BdpVerb_Shoot.BindVerbEmissionPlan(...)` 在正式发射计划聚合完成后，再从 `StepSourceResultIds` 发布整轮参与者。

**Step 2: 运行并确认失败原因是桥接未发布最终发射参与者**

**Step 3: 实现发布接线**

新增独立发布方法，把最终发射计划来源作为整轮参与者传给 owner；近战桥接保持现有当前结果的最小集合。

**Step 4: 重跑测试并确认通过**

### Task 3：让动作阶段读取整轮参与者

**Files:**

- Modify: `Source/BDP.Tests/DualWeaponRoundVisualParticipantsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1`
- Modify: `Source/BDP/Core/Trigger/Visual/WeaponVisualStageResolver.cs`

**Step 1: 扩展失败测试**

断言：

- 正常路径读取 `ActiveAttackParticipantResultIds`。
- 不再读取 `ActiveCastResultIds` 判断动作阶段参与关系。
- 整轮参与集合缺失时，从 `HostSessionToken.ResultId` 展开作为读档恢复回退。

**Step 2: 运行并确认旧解析逻辑导致失败**

**Step 3: 最小修改来源根集合选择**

保持芯片实例匹配、复合来源展开及阶段优先级不变，只替换正常运行时的参与者来源。

**Step 4: 重跑定向测试并确认通过**

### Task 4：回归、构建、日志与提交

**Files:**

- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志*.md`

**Step 1: 运行视觉与执行定向回归**

运行新增测试及全部 `WeaponVisualStage*`、双武器执行、枪口、读档恢复相关测试。

**Step 2: 运行主工程全量 PowerShell 测试**

执行 `Source/BDP.Tests` 中全部测试脚本，记录任何与任务无关的既有失败。

**Step 3: 构建主模组**

使用工程既有构建方式生成 Core 和 Content，并确认无编译错误。

**Step 4: 写倒序工作日志**

记录根因、修复范围、验证结果和需要用户进行的游戏实测步骤；每个日志文件不超过 20 条。

**Step 5: 检查并提交**

只暂存本任务文件，避免夹带工作区中原有的无关改动。提交信息使用：

```text
fix: share weapon stages across round participants
```
