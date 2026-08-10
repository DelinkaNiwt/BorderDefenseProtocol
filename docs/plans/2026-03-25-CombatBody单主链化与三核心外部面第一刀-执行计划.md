# CombatBody 单主链化与三核心外部面第一刀 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把 `Cleanup` 从战斗体平行层切回离开事务内部步骤，并为 `CompTrion / CombatBody / Trigger` 中已进入主链的部分补上明确的正式外部读写面。

**Architecture:** 本轮只做第一刀，不扩散到表达、投影、Trigger 以外的其它系统。`CombatBodyCoordinator` 收成唯一主链；`Cleanup` 退出系统级位置，改为离开事务步骤；`CompTrion` 与 `CombatBody` 补 `Reader / Commands / Events` 正式外部面，同时保留兼容接口，避免当前阶段改动扩散失控。

**Tech Stack:** RimWorld 1.6、C# 7.3、ThingComp、CompEquippable、正式只读接口、正式请求接口、后续游戏内验证。

---

### Task 1: 落正式外部面接口

**Files:**
- Create: `Source/BDP/Core/Trion/ITrionReader.cs`
- Create: `Source/BDP/Core/Trion/ITrionCommands.cs`
- Create: `Source/BDP/Core/CombatBody/ICombatBodyReader.cs`
- Create: `Source/BDP/Core/CombatBody/ICombatBodyCommands.cs`
- Modify: `Source/BDP/Core/Trion/ITrionState.cs`
- Modify: `Source/BDP/Core/CombatBody/ICombatBodyCoordinator.cs`
- Modify: `Source/BDP/Core/CombatBody/ICombatBodyPhaseState.cs`

**Step 1:** 为 Trion 与 CombatBody 各自新增正式外部读写面接口。  
**Step 2:** 让旧兼容接口改为继承新外部面，而不是继续单独承担全部责任。  
**Step 3:** 保持 `Trigger` 现有 `Reader / Commands / Events` 结构不变，不在这轮继续扩散。

### Task 2: 切掉 Cleanup 平行层

**Files:**
- Delete: `Source/BDP/Core/Cleanup/ICollapseCleanupCoordinator.cs`
- Delete: `Source/BDP/Core/Cleanup/CombatBodyCleanupCoordinator.cs`
- Create: `Source/BDP/Core/CombatBody/CombatBodyExitPipeline.cs`

**Step 1:** 删除 `Cleanup` 平行层接口与协调者。  
**Step 2:** 新建 `CombatBodyExitPipeline`，只负责离开侧事务步骤，不持有相位真值。  
**Step 3:** 把“释放占用 / 关闭 Trigger / 恢复宿主”压进该离开步骤对象。

### Task 3: 收束 CombatBody 主链

**Files:**
- Modify: `Source/BDP/Core/CombatBody/CombatBodyCoordinator.cs`
- Modify: `Source/BDP/Core/CombatBody/CombatBodyState.cs`
- Modify: `Source/BDP/Core/CombatBody/PawnCombatBodyHost.cs`

**Step 1:** 让 `CombatBodyCoordinator` 明确实现正式请求面与事件面。  
**Step 2:** 让 `CombatBodyState` 明确实现正式只读面。  
**Step 3:** 让 `CombatBodyCoordinator` 直接依赖 `CombatBodyExitPipeline`，退出事务不再通过平行 cleanup 层。  
**Step 4:** 让 `PawnCombatBodyHost` 的注释与定位收束为“已进入主链的正式宿主挂接点”，不再表述成游离空桥。
**Step 5:** 补 `CombatBodyEnterPipeline`，让进入侧与离开侧都回到同一条主链内部的步骤簇结构。

### Task 4: 收束 Trion owner 对外定位

**Files:**
- Modify: `Source/BDP/Core/Trion/CompTrion.cs`

**Step 1:** 让 `CompTrion` 实现 `ITrionReader / ITrionCommands / ITrionEvents`。  
**Step 2:** 不新增额外 facade，不扩大资源系统层级。  
**Step 3:** 保持当前行为不变，只校正对外接口边界。

### Task 5: 编译与记录

**Files:**
- Modify: `docs/99-会话交接/2026-03-24-工作推进-卷02.md`

**Step 1:** 重新编译主模组。  
**Step 2:** 重新编译 `DevHarness`。  
**Step 3:** 做一轮架构复审，重点检查：
- 是否新增了新的 owner
- 是否把 `Cleanup` 换名后继续平行存在
- 是否因为“统一接口”制造了新的空抽象
**Step 4:** 补写日志，记录这一刀的边界变化与后续游戏测试口。
