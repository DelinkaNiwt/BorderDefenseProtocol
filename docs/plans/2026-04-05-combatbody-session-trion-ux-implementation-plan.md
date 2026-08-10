# CombatBody Session Trion UX Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 全量把 `CombatBodySession` 收敛为 `CombatBodySession` 命名，并同时落地远程 Trion 提示锁存与 Trion 调试 gizmo。

**Architecture:** 保持现有三套真值 owner 不变：`CompTrion` 继续只持有资源真值，`CombatBody` 继续只持相位真值，`Trigger` 继续只持槽位与投影真值。本次只修正命名、补充提示锁存、补充正式 Trion 调试写入口，不引入新的 owner 或通用总控。远程提示节流仍留在 `BdpVerb_Shoot` UI 出口层，Trion 调试按钮仍留在 gene gizmo bridge 层。

**Tech Stack:** C#, RimWorld/Verse, PowerShell smoke tests

---

### Task 1: 锁定新命名与新行为的测试

**Files:**
- Modify: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- Modify: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/TrionGeneGizmoSmokeTests.ps1`
- Create/Modify: `Source/BDP.Tests/...` as needed for prompt latch and debug commands

**Step 1: Write the failing tests**
- 把所有对 `CombatBodySession*` 的硬编码断言改成 `CombatBodySession*`
- 增加断言：远程提示锁存存在“首次提示、恢复重置”的正式帮助方法/状态
- 增加断言：`ITrionCommands` 暴露调试写入口，且语义区分“夹到占用下界”和“置 0 被拒绝”
- 增加断言：Pawn 侧 Trion gizmo 在开发模式下追加 `+50/-50/MAX/0`

**Step 2: Run tests to verify they fail**

Run: `& '.\\Source\\BDP.Tests\\CombatBodySessionContractsSmokeTests.ps1'`
Expected: FAIL because current code still uses `CombatBodySession*`

Run: `& '.\\Source\\BDP.Tests\\RangedAttackTrionConsumptionSmokeTests.ps1'`
Expected: FAIL because current code has no prompt latch behavior

Run: `& '.\\Source\\BDP.Tests\\TrionGeneGizmoSmokeTests.ps1'`
Expected: FAIL because current code has no debug buttons / no formal write surface

### Task 2: 全量改名为 CombatBodySession

**Files:**
- Rename/Modify: `Source/BDP/Core/CombatBodySession/*`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Access/Surfaces/CombatBodySurfaceAccess.cs`
- Modify: related tests/docs references

**Step 1: Write minimal implementation**
- 把 `CombatBodySession` 命名空间和类型全量改为 `CombatBodySession`
- 保持 surface 契约与行为顺序不变
- 仅同步外露常量/原因名中真正需要改名的部分

**Step 2: Run tests to verify the rename passes**

Run: `& '.\\Source\\BDP.Tests\\CombatBodySessionContractsSmokeTests.ps1'`
Expected: PASS

### Task 3: 实现远程不足提示锁存

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: helper file if extraction is needed
- Test: `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`

**Step 1: Write minimal implementation**
- 在 `BdpVerb_Shoot` 中加入按 Pawn 维度的不足提示锁存
- 失败时：若未提示过则弹一次并置位
- 成功 admission / success commit 时清除锁存

**Step 2: Run tests**

Run: `& '.\\Source\\BDP.Tests\\RangedAttackTrionConsumptionSmokeTests.ps1'`
Expected: PASS

### Task 4: 补 Trion 正式调试写接口与 gizmo

**Files:**
- Modify: `Source/BDP/Core/Trion/ITrionCommands.cs`
- Modify: `Source/BDP/Core/Trion/TrionService.cs`
- Modify: `Source/BDP/Core/Trion/CompTrion.cs`
- Modify: `Source/BDP/Core/Genes/TrionGeneGizmoBridge.cs`
- Test: `Source/BDP.Tests/TrionGeneGizmoSmokeTests.ps1`
- Test: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`

**Step 1: Write minimal implementation**
- 新增正式调试写入口
- 在 `CompTrion` 内统一做 max / allocated 下界校验
- 开发模式下追加 `+50/-50/MAX/0` 按钮
- 负向写入与置 0 按你的语义分别做“夹住 + 提示”与“拒绝 + 提示”

**Step 2: Run tests**

Run: `& '.\\Source\\BDP.Tests\\TrionGeneGizmoSmokeTests.ps1'`
Expected: PASS

Run: `& '.\\Source\\BDP.Tests\\TrionGeneGuiContractsSmokeTests.ps1'`
Expected: PASS

### Task 5: 做针对性回归验证

**Files:**
- Test: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**Step 1: Run tests**

Run: `& '.\\Source\\BDP.Tests\\CombatBodyCollapseEmergencySmokeTests.ps1'`
Expected: PASS

Run: `& '.\\Source\\BDP.Tests\\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'`
Expected: PASS

**Step 2: Review docs references**
- 同步必要文档命名
- 不扩散修改到无关历史归档

