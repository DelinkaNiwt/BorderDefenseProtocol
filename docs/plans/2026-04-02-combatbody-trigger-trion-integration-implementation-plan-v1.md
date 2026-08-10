# 新 BDP 战斗体-触发器-Trion 接线 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不破坏现有 Trigger 发布式架构的前提下，把 `CombatBody`、`Trigger`、`Trion` 按旧 BDP 已实现语义接成一条可编译、可回归、可继续扩展的正式主链。

**Architecture:** 保持三套真值 owner 不变: `CombatBody` 只管 phase 和宿主变换，`Trigger` 只管槽位/切换/发布真值，`Trion` 只管资源账本。跨系统事务只新增一个很薄的 `CombatBodySessionService`，继续复用现有 `*SurfaceAccess` 作为统一出入口，不引入事件总线、额外 owner、通用框架或接口塔。

**Tech Stack:** C#, RimWorld/Verse, PowerShell smoke tests, `dotnet msbuild`

---

## 1. 本计划的硬边界

### 1.1 本次必须做到

- 开战斗体前必须检查当前主武器触发体是否存在。
- 装卸芯片、装备、卸下时，`Trigger -> Trion.Reserved` 必须同步。
- 开战斗体时正式锁定的是“当前已装芯片总占用”，不是基础固定费。
- 战斗体开启后，芯片激活、攻击入口、自动攻击才成立。
- 触发体卸下时，如果战斗体仍开着，必须直接解除战斗体。
- `AvailableDepleted -> Collapsing -> 90 ticks -> EmergencyDeactivate` 必须打通。

### 1.2 本次明确不做

- 不引入新的总控层，不引入事件总线。
- 不新增“卸下触发体后战斗继续、装回恢复”的玩法。
- 不新增“战斗中禁止装卸芯片”的硬规则。
- 不重画 `CombatBody / Trigger / Trion / AttackExecution / Expressions` 的边界。
- 不补一个长期兼容层让新旧两套会话逻辑并存。

### 1.3 旧 BDP 依据总表

- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyActivationChecker.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyOrchestrator.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/Hediff_CombatBodyCollapsing.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Core/Comps/CompTrion.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Core/Genes/Gene_TrionGland.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.Activation.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.CombatBodySupport.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.Lifecycle.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.SlotManagement.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Patches/Patch_Pawn_TryGetAttackVerb.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Patches/Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`

---

## 2. 施工原则

- 先把统一事务入口立起来，再挪业务，不要一边挪一边散改调用点。
- 优先复用现有 surface、service、runtime coordinator，不平地起新层。
- `CombatBodySessionService` 只做跨系统事务，不持有新的长期真值。
- `CombatBodyService` 收缩成原始 phase/service，不再直接摸 `Trigger/Trion`。
- `TriggerRuntimeCoordinator` 继续做唯一发布 owner，战斗体门控也落在它这里。
- 所有跨系统交互都走已有正式出入口:
- `CombatBodySurfaceAccess`
- `TriggerSurfaceAccess`
- `TrionSurfaceAccess`
- 不为“以后可能还会有更多联动”提前做通用框架。

---

## 3. 固定验证命令

构建:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

现有回归脚本:

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

本计划新增脚本:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
```

---

## 4. 里程碑总览

| 里程碑 | 目标 | 停点 |
| --- | --- | --- |
| M1 | 立起 `CombatBodySessionService`，把统一出入口改接过去 | 只换入口，不改旧行为 |
| M2 | 把激活/手动关闭主链从 `CombatBodyService` 挪到 `CombatBodySessionService` | `CombatBodyService` 不再直接碰 `Trigger/Trion` |
| M3 | Trigger 补齐 `Reserved / activationCost / drain / battle mode gate` | 芯片与资源链路成立 |
| M4 | 发布层补齐 phase 门控，接通 `unequipped -> deactivate` | 攻击入口/自动攻击只在 Active 成立 |
| M5 | 接通崩解、紧急关闭、读档恢复订阅 | 旧 BDP 崩解主链闭合 |

---

### Task 1: 立起 CombatBodySession 薄接线层，并把 CombatBody surface 改接过去

**Files:**
- Create: `Source/BDP/Core/CombatBodySession/CombatBodySessionExitMode.cs`
- Create: `Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs`
- Create: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Create: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Access/Surfaces/CombatBodySurfaceAccess.cs`

**旧 BDP 依据:**
- `CombatBodyOrchestrator.cs`
- `Gene_TrionGland.cs`

**Step 1: 写失败的架构脚本**

- 新增 `CombatBodySessionContractsSmokeTests.ps1`，先断言以下目标态:
- `Core/CombatBodySession/` 三个文件存在。
- `CompCombatBodyHost` 同时持有“原始战斗体服务”和“CombatBodySessionService”。
- `CombatBodySurfaceAccess.ResolveReader/ResolveCommands/ResolveEvents` 都返回 `CombatBodySessionService`，不是原始 `CombatBodyService`。

**Step 2: 跑脚本确认当前失败**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
```

Expected:

- 失败，提示当前还没有 `CombatBodySessionService`，surface 还没改接。

**Step 3: 新建最小 CombatBodySession 三件套**

- `CombatBodySessionExitMode` 只保留:
- `Manual`
- `Emergency`
- `CombatBodySessionPolicy` 只先放纯判断:
- `TryResolvePrimaryTrigger(Pawn pawn, out CompTriggerBody trigger)`
- `IsBattleModeActive(Pawn pawn)`
- `ShouldPublishCombatProjection(Pawn pawn, CompTriggerBody trigger)`
- `CombatBodySessionService` 先实现:
- `ICombatBodyReader`
- `ICombatBodyCommands`
- `ICombatBodyEvents`
- 第一版先只做转发，不立刻改行为。

**Step 4: 改宿主 Comp**

- `CompCombatBodyHost` 增加:
- `private CombatBodyService rawCombatBodyService;`
- `private CombatBodySessionService combatBodySessionService;`
- `Service` 改为返回 `combatBodySessionService`
- 补一个 `RawService` 内部口，供 `CombatBodySessionService` 调用。

**Step 5: 改统一 surface 入口**

- `CombatBodySurfaceAccess.ResolveReader/ResolveCommands/ResolveEvents` 继续维持原签名。
- 只改底层返回值，统一转到 `CombatBodySessionService`。

**Step 6: 构建并回归**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
```

Expected:

- 编译通过。
- contracts 脚本通过。
- 此阶段不要求游戏行为改变。

**Step 7: Commit**

```powershell
git add Source/BDP/Core/CombatBodySession Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs Source/BDP/Core/CombatBody/Access/Surfaces/CombatBodySurfaceAccess.cs Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1
git commit -m "refactor: route combat body surfaces through battle session"
```

---

### Task 2: 把激活/手动关闭主链迁到 CombatBodySessionService

**Files:**
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs`
- Modify: `Source/BDP/Core/CombatBody/Flow/CombatBodyCoordinator.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Create: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**旧 BDP 依据:**
- `CombatBodyActivationChecker.cs`
- `CombatBodyOrchestrator.cs`
- `CompTriggerBody.CombatBodySupport.cs`

**Step 1: 写失败的流程脚本**

- 在 `CombatBodyTriggerTrionIntegrationSmokeTests.ps1` 先断言:
- `CombatBodyService` 不再直接引用 `TrionSurfaceAccess.ResolveCommands(...)`
- `CombatBodyService` 不再直接引用 `TriggerSurfaceAccess.ResolveLoadoutCommands(...)`
- `CombatBodySessionService.TryActivate()` 内存在固定顺序:
- 找主武器 Trigger
- 检查 Trion
- `Allocate`
- 原始进入 Active
- 自动激活 `Special`
- 冻结 / 注册维持 / 订阅 `AvailableDepleted`
- `CombatBodySessionService.RequestDeactivate(Manual)` 内存在固定顺序:
- 关槽位
- 注销维持 / 取消订阅
- `Release`
- `SetFrozen(false)`
- 原始退出

**Step 2: 跑脚本确认失败**

Run:

```powershell
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

Expected:

- 失败，提示 `CombatBodyService` 仍直接碰 `Trigger/Trion`。

**Step 3: 收缩 CombatBodyService**

- `CombatBodyService` 改成只实现:
- `ICombatBodyReader`
- `ICombatBodyEvents`
- 删除对外 `TryActivate()/RequestDeactivate()` 主流程职责。
- 增加最小内部方法:
- `internal bool TryEnterActive(float allocatedTrion)`
- `internal void EnterCooldown(int cooldownTicks, string reason)`
- `internal void EnterCollapsing(string reason)`
- 原始服务仍只负责:
- phase 切换
- 宿主变换
- phase event 广播

**Step 4: 在 CombatBodySessionService 中重写激活主链**

- `TryActivate()` 固定顺序:

```text
1. rawCombatBody.CanActivate()
2. 解析当前主武器 CompTriggerBody
3. 读取 Trigger 当前正式占用总量 allocateAmount
4. Trion.CanAfford(allocateAmount)
5. Trion.Allocate(allocateAmount)
6. rawCombatBody.TryEnterActive(allocateAmount)
7. Trigger 尝试自动激活 Special 侧
8. Register combat-body drain（若配置 > 0）
9. SetFrozen(true)
10. 订阅 AvailableDepleted
11. 请求 Trigger 刷新 battle projection
```

- 失败回滚只保留最小集:
- `Allocate` 成功但原始进入失败时，立即 `Release(allocateAmount)`
- 不做多余的补偿对象

**Step 5: 在 CombatBodySessionService 中重写手动关闭主链**

- `RequestDeactivate(Manual)` 固定顺序:

```text
1. 让 Trigger 关闭 Main / Sub / Special
2. 注销战斗体维持 drain
3. 取消 AvailableDepleted 订阅
4. Trion.Release(rawCombatBody.AllocatedTrion)
5. Trion.SetFrozen(false)
6. rawCombatBody.EnterCooldown(0, "ManualDeactivate")
7. 请求 Trigger 刷新 battle projection
```

**Step 6: 保留旧 BDP 语义，不额外补玩法**

- 不引入 `sessionTriggerThingId`
- 不让战斗体在“没 Trigger 的情况下继续挂着”
- `Special` 自动激活失败时不回滚整个战斗体
- 不把 `CompProperties_CombatBodyHost.activationCost` 当战斗体固定占用费继续用下去

**Step 7: 构建并回归**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

Expected:

- 编译通过。
- `CombatBodyService` 已退回原始 phase service。
- `CombatBodySessionService` 已成为唯一跨系统事务入口。

**Step 8: Commit**

```powershell
git add Source/BDP/Core/CombatBodySession Source/BDP/Core/CombatBody/Flow/CombatBodyCoordinator.cs Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1
git commit -m "refactor: move combat body session flow into battle session"
```

---

### Task 3: 在 Trigger 内补齐 Reserved、芯片激活费、持续消耗和战斗体门控

**Files:**
- Create: `Source/BDP/Core/Trion/TrionDrainKeys.cs`
- Modify: `Source/BDP/Core/Chips/Config/ChipTrionConfig.cs`
- Modify: `Source/BDP/Core/Chips/Contract/ChipTrionContract.cs`
- Modify: `Source/BDP/Core/Chips/Contract/DefaultChipDefinitionContractResolver.cs`
- Modify: `Source/BDP/Core/Trigger/Switching/Flow/TriggerSwitchService.cs`
- Modify: `Source/BDP/Core/Trigger/Interaction/TriggerInteractionReason.cs`
- Modify: `Source/BDP/Core/Trigger/Interaction/TriggerInteractionInterpreter.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`

**旧 BDP 依据:**
- `CompTriggerBody.SlotManagement.cs`
- `CompTriggerBody.Activation.cs`
- `CompTriggerBody.Lifecycle.cs`

**Step 1: 先让测试卡住缺口**

- 在 `CombatBodyTriggerTrionIntegrationSmokeTests.ps1` 继续加断言:
- `ChipTrionConfig/ChipTrionContract` 都有 `DrainPerDay`
- `CompTriggerBody` 存在 `SyncReserved...` 或等价入口
- `Notify_Equipped` 存在并同步 `Reserved`
- `RequestActivate` 前有战斗体 phase 门控
- `TriggerInteractionReason` 存在 `BattleModeUnavailable`
- `NotifySlotActivationCommitted` 会处理 activation cost / chip drain

**Step 2: 补芯片 Trion 契约**

- `ChipTrionConfig` 增加:
- `public float DrainPerDay;`
- `ChipTrionContract` 增加:
- `public float DrainPerDay;`
- `DefaultChipDefinitionContractResolver` 把 XML/Def 配置映射进契约。

**Step 3: 给 TriggerService 加芯片 Trion 读取**

- 在 `TriggerService` 增加:
- `GetChipTrionContract(Thing chip)`
- 仍复用 `ChipSurfaceAccess.Read(...)`
- 不新增新的 chips surface

**Step 4: 在 CompTriggerBody 内补齐 Reserved 同步**

- 新增最小方法:
- `CalculateReservedTrionCost()`
- `SyncReservedTrion()`
- 计算时只按控制根槽或 `ThingID` 去重，避免双持镜像重复收费。
- 在以下位置调用:
- `TryLoadChip()` 成功后
- `TryUnloadChip()` 成功后
- `Notify_Equipped()` 时
- `Notify_Unequipped()` 里清成 `0`

**Step 5: 给芯片激活加战斗体硬门**

- `CompTriggerBody.RequestActivate(...)` 前先检查:

```text
CombatBodySurfaceAccess.ResolveReader(OwnerPawn)?.Phase == CombatBodyPhase.Active
```

- 不成立直接拒绝。
- `TriggerInteractionInterpreter` 对“已装芯片但战斗体未开”的槽位，返回:
- `Blocked`
- `BattleModeUnavailable`

**Step 6: 在正式提交激活/停用回调里接通 Trion**

- `NotifySlotActivationCommitted(...)` 内:
- 读取 `ChipTrionContract.ActivationCost`
- `TryConsume(activationCost)`
- `RegisterDrain(TrionDrainKeys.Chip(side, slotIndex), drainPerDay)`
- 若提交时支付失败，立刻撤销该槽位并重新发布，避免出现“免费激活”
- `NotifySlotDeactivated(...)` 内:
- `UnregisterDrain(TrionDrainKeys.Chip(side, slotIndex))`

**Step 7: 构建并回归**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
```

Expected:

- 编译通过。
- Trigger 能独立维持 `Reserved / activation cost / chip drain`。
- 业务门控只认 `CombatBody.Phase == Active`，不再复制布尔镜像真值。

**Step 8: Commit**

```powershell
git add Source/BDP/Core/Trion/TrionDrainKeys.cs Source/BDP/Core/Chips/Config/ChipTrionConfig.cs Source/BDP/Core/Chips/Contract/ChipTrionContract.cs Source/BDP/Core/Chips/Contract/DefaultChipDefinitionContractResolver.cs Source/BDP/Core/Trigger/Switching/Flow/TriggerSwitchService.cs Source/BDP/Core/Trigger/Interaction/TriggerInteractionReason.cs Source/BDP/Core/Trigger/Interaction/TriggerInteractionInterpreter.cs Source/BDP/Core/Trigger/State/CompTriggerBody.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Integrity.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1
git commit -m "feat: wire trigger trion costs through battle session rules"
```

---

### Task 4: 用战斗体 phase 门控正式发布层，并接通触发体卸下即解除战斗体

**Files:**
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Modify: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**旧 BDP 依据:**
- `CompTriggerBody.Lifecycle.cs`
- `Patch_Pawn_TryGetAttackVerb.cs`
- `Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`

**Step 1: 先补失败断言**

- 继续在 `CombatBodyTriggerTrionIntegrationSmokeTests.ps1` 断言:
- `ProjectionDirtyReason` 新增 `CombatBodySessionStateChanged`
- `TriggerRuntimeCoordinator` 在发布前检查:
- 当前主武器是不是自己
- `CombatBody.Phase == Active`
- `Notify_Unequipped(pawn)` 在清投影前会请求 `CombatBodySurfaceAccess.ResolveCommands(pawn)?.RequestDeactivate(...)`

**Step 2: 给运行时发布层加统一门控**

- `TriggerRuntimeCoordinator.RebuildAndPublish()` 改成:

```text
if 不是当前主武器 owner:
    发布空 projection

else if CombatBody.Phase != Active:
    发布空 projection

else:
    正常构建 snapshot -> combat projection -> presentation projection
```

- 不在很多消费者里散落写 `if phase != Active return`。

**Step 3: 给 CombatBodySessionService 一个明确的刷新入口**

- 增加最小方法:
- `NotifyCombatBodySessionStateChanged()`
- 里面只做:
- 找当前主武器 Trigger
- `trigger.PublishCombatProjection(ProjectionDirtyReason.CombatBodySessionStateChanged)` 或等价正式入口
- 该方法只做通知，不承载业务真值。

**Step 4: 改触发体卸下逻辑**

- `Notify_Unequipped(pawn)` 顺序固定:

```text
1. 若 CombatBody.Phase == Active，先 RequestDeactivate(Manual)
2. Trion.SetReserved(0)
3. 清空当前已发布 projection
4. 中断失效的攻击会话恢复逻辑
```

- 不保留“等同一把装回就恢复”的会话想象。

**Step 5: 构建并回归**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
```

Expected:

- 手动攻击入口只在 `Active` 时出现。
- 自动攻击只在 `Active` 时能拿到有效入口。
- 触发体卸下时，战斗体直接解除。

**Step 6: Commit**

```powershell
git add Source/BDP/Core/CombatBodySession/CombatBodySessionPolicy.cs Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs Source/BDP/Core/Trigger/State/CompTriggerBody.cs Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1
git commit -m "feat: gate trigger projection by combat body phase"
```

---

### Task 5: 接通崩解、紧急关闭、90 ticks 收尾和读档恢复订阅

**Files:**
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompProperties_CombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Flow/CombatBodyCoordinator.cs`
- Create: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**旧 BDP 依据:**
- `CombatBodyOrchestrator.cs`
- `Hediff_CombatBodyCollapsing.cs`
- `Gene_TrionGland.cs`
- `CompTrion.cs`

**Step 1: 写失败的崩解脚本**

- 新增 `CombatBodyCollapseEmergencySmokeTests.ps1`，先断言:
- `CompCombatBodyHost` 有 `CompTick()`
- `CompTick()` 或等价 runtime tick 在 `Collapsing` 到时后调用 `RequestDeactivate(Emergency)`
- `CombatBodySessionService` 订阅 `AvailableDepleted`
- `CombatBodySessionService.TriggerCollapse(...)` 会进入 `Collapsing`
- `RequestDeactivate(Emergency)` 使用 `Release + ConsumeUntilDepleted` 得到旧 `ForceDeplete()` 等价结果

**Step 2: 给 host comp 加轻量 runtime tick**

- `CompCombatBodyHost.CompTick()` 只做两件事:
- 若 phase 为 `Collapsing` 且剩余 `<= 0`，请求 `Emergency` 关闭
- 其余 session runtime 收尾交给 `CombatBodySessionService`
- 不把 `CompCombatBodyHost` 变成新 owner

**Step 3: 在 CombatBodySessionService 中接通崩解链**

- 激活成功后:
- 若 `maintenanceDrainPerDay > 0`，注册 `TrionDrainKeys.CombatBody`
- 订阅 `AvailableDepleted`
- 读档后若 phase 已是 `Active`，恢复该订阅
- `TriggerCollapse(reason)` 顺序:

```text
1. rawCombatBody.EnterCollapsing(reason)
2. 打断 Pawn 当前 job
3. 请求 Trigger 刷新 projection（此时应为空）
```

**Step 4: 接通紧急关闭**

- `RequestDeactivate(Emergency)` 顺序:

```text
1. 关闭 Main / Sub / Special
2. 注销战斗体维持 drain
3. 取消 AvailableDepleted 订阅
4. Trion.Release(rawCombatBody.AllocatedTrion)
5. Trion.ConsumeUntilDepleted(当前可用量)
6. Trion.SetFrozen(false)
7. rawCombatBody.EnterCooldown(emergencyCooldownTicks, "EmergencyDeactivate")
8. 请求 Trigger 刷新 projection
```

- 这一步不新增 `ForceDeplete()` 正式接口，复用现有 `Release + ConsumeUntilDepleted`。

**Step 5: 补最小配置位**

- `CompProperties_CombatBodyHost` 增加:
- `public float maintenanceDrainPerDay = 0f;`
- 默认 `0`，不破坏当前注入式宿主。
- 本次不额外做“从 Gene/Def 动态灌值”的新配置系统。

**Step 6: 构建并回归**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- `AvailableDepleted` 能进入 `Collapsing`
- 90 ticks 后自动紧急关闭
- 读档后 Active 状态能恢复订阅
- 紧急关闭后资源不返还

**Step 7: Commit**

```powershell
git add Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs Source/BDP/Core/CombatBody/Bridge/CompProperties_CombatBodyHost.cs Source/BDP/Core/CombatBody/Flow/CombatBodyCoordinator.cs Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1 Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1
git commit -m "feat: complete combat body collapse and emergency session flow"
```

---

## 5. 全量回归与游戏内验收

### 5.1 最终命令

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

### 5.2 游戏内重点实测

1. 装 Trigger，装芯片，观察 `Reserved` 是否跟着变。
2. 开战斗体，确认只自动激活 `Special`，不自动激活左右手。
3. 战斗体关闭前后，芯片按钮、攻击按钮、自动攻击是否同步开/关。
4. 卸下 Trigger，确认战斗体直接解除，不留半活状态。
5. 把 Trion 可用量耗尽，确认进入 `Collapsing`，90 ticks 后紧急关闭。
6. 在 `Active` 和 `Collapsing` 状态分别存档再读档，确认入口、投影、订阅都不丢。

---

## 6. 本计划刻意不纳入主线的内容

- 旧 BDP 的伤口 drain / 受伤扣能链，不并入本次主线。
- 战斗体玩家按钮或正式 UI 入口，不并入本次主线。
- 真身快照、外观表现、紧急脱离演出等表现层细节，不并入本次主线。

原因:

- 这些内容不是把 `CombatBody / Trigger / Trion` 三系统接通的最小闭环。
- 现在一起做，会明显增加施工面和回归面。
- 等本计划完成后，可以另开一份小计划逐项接回，不需要推翻本次架构。

---

## 7. 推荐执行顺序

- 严格按 `Task 1 -> Task 2 -> Task 3 -> Task 4 -> Task 5` 执行。
- 不建议并行改 `CombatBodyService` 和 `TriggerRuntimeCoordinator`。
- 不建议在 `Task 3` 之前先写 `unequipped -> deactivate`，否则很难分辨是资源链错还是发布链错。

---

## 8. 可选附加任务

如果需要在游戏里更方便地人工实测，但又不想现在做正式 UI，可加一个开发模式小任务:

- 新增一个 dev-only gizmo 或 debug action
- 它只做一件事: 调 `CombatBodySurfaceAccess.ResolveCommands(pawn)`
- 不引入新的系统层
- 做完后随时可以删，不影响本次正式接线主链

这个任务不属于主线交付，不要抢在 `Task 1-5` 前面做。

