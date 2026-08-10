# 新 BDP 剩余性能优化最终 Implementation Plan v2

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在 `CombatBodySession / CombatBody / Trigger / Trion` 薄接已经落地、且旧版 Task1 已经完成的前提下，一次性完成新 BDP 现阶段剩余的全部性能优化与边界收口工作。

**Architecture:** 继续以 `CompTriggerBody` 持有 Trigger 真值、`TriggerRuntimeCoordinator` 负责正式投影发布、`CombatBodySessionService` 负责跨 `CombatBody / Trigger / Trion` 的会话编排为唯一正式架构。本版计划不再重复已经完成的“主武器唯一 runtime owner”改造，而是直接从剩余热点推进：`formal host` 活跃 tick、owner 内部投影构建、表达运行时仓库、最终纯读化，以及基于当前真实代码的 CombatBodySession/Detach 回归保护。

**Tech Stack:** C#, RimWorld / Verse mod runtime, PowerShell smoke tests, `dotnet msbuild`

---

## 1. 当前真实基线

### 1.1 已完成，转为基线保护，不再列为施工任务

以下内容已经在真实代码中落地，后续任务只能保护，不能回退：

- 只有主武器上的 `CompTriggerBody` 会通过 `EquipmentTrackerTick` 推进 runtime。
- `AttackExecutionPlanRuntimeStore` 已删除。
- `TriggerRuntimeCoordinator` 已具备统一 `RuntimeTick()` 推进入口。
- `CombatBodySessionService` 已经成为真实的跨系统编排器，不再只是设计概念。
- Trigger 发布战斗投影前会经过 `CombatBodySessionPolicy.ShouldPublishCombatProjection(...)` 裁定。
- Trigger 卸下时已经有显式 `ForceTeardownOnDetach(...)`，会清理投影、chip drain、combat body drain、stale attack session。
- `TriggerInteractionInterpreter` 已把“战斗体未激活”解释成 `BattleModeUnavailable`。

### 1.2 当前仍未完成的核心问题

- `TriggerBodyVerbHostManager.Tick()` 仍然遍历全部 binding，`formal host` steady-state 仍有常驻 tick 税。
- `TriggerRuntimeCoordinator` 构建投影时仍调用 `BuildSelectedSnapshot(ownerPawn, owner.LoadoutReaderSurface)`，正式投影仍依赖公共 reader 反向读自己。
- 表达系统还没有运行时仓库，`combo` 匹配、芯片定义读取、契约解释仍有重复成本。
- `ExpressionFormalSurfaces` 普通热读仍会触发 `PreparePublishedReadState()`，纯读边界还没收干净。

### 1.3 当前必须写死的前提

- Trigger 的运行时 owner 是“当前装备在手上的那把触发体 thing”，不是 Pawn。
- 芯片配置、槽位状态、formal host、攻击入口都跟 Trigger 走，不跟 Pawn 走。
- 触发体离手后，战斗体/激活态/攻击入口都必须关闭退出。
- 当前版本下，`TrionExpressionConditionInterpreter` 和 `CombatBodyExpressionConditionInterpreter` 仍是占位条件分支，表达成立条件暂时**不**跟 Trion 实时数值变化联动失效。
- 但 `CombatBodySessionStateChanged` 已经是真实投影失效来源，不能再按旧文档把 CombatBodySession 当成“未来版本问题”。

---

## 2. 本版计划范围

### 2.1 做什么

- 完成剩余性能热点优化。
- 把当前真实代码已经引入的 CombatBodySession/Detach/装备态硬约束写入计划和回归合同。
- 重排任务顺序，让“下一步”直接落到真正还没做的任务上。

### 2.2 不做什么

- 不重做旧版 Task1。
- 不重新讨论 Trion 完整业务规则。
- 不把因子系统纳入本轮性能优化。
- 不做过渡态、兼容层、未来版预埋分支。

### 2.3 完成定义

全部任务完成后，必须同时满足：

- `formal host` steady-state tick 从 `O(binding 总数)` 收到 `O(活跃 formal host 数)`。
- 正式投影构建只消费 owner 内部 build input，不再经由公共 reader 反向读取。
- `combo` 匹配平均接近 `O(1)`，芯片定义/契约解释重复读取接近 `O(1)`。
- 普通已发布投影读取接近 `O(1)`，不再顺带推进状态。
- CombatBodySession 发布裁定、Detach teardown、触发体装备态硬约束在所有后续任务中都不被破坏。
- 自动攻击、手动攻击、burst、近战、dual、combo、切换、读档恢复无功能回退。

---

## 3. 固定回归保护

### 3.1 每个任务结束至少要过的基线 smoke tests

```powershell
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
```

### 3.2 施工常用命令

构建：

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

核心战斗/表达回归：

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

---

## 4. 阶段总览

| 新任务 | 目标 | 主要收益 | 结束后你重点实测什么 |
| --- | --- | --- | --- |
| Task 1 | `formal host` 活跃 tick 化 | 去掉全 binding 常驻 tick 税 | 自动远程持续射击、burst、近战连续攻击 |
| Task 2 | owner 内部投影构建器收口 | 正式投影不再经由公共 reader 反向构建 | 切换、gizmo、tooltip、手动攻击、读档首次状态一致性 |
| Task 3 | 表达运行时仓库与缓存 | 去掉 `combo / 定义 / 契约` 重复热点 | combo、dual、tooltip、攻击结果一致性 |
| Task 4 | 最终纯读化 | 普通读路径不再顺带修状态 | UI 读取、gizmo、自动/手动攻击一致性 |
| Task 5 | 总验收与游戏回归矩阵 | 把最终边界全部写死 | 全功能总回归 |

说明：

- 旧版 Task1 已经完成，在本版计划中不再占一个施工阶段。
- 当前“下一步”就是新 Task 1，也就是 `formal host` 活跃 tick 化。

---

### Task 1: `formal host` 活跃 tick 化

**Files:**
- Create: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/VerbHosting/BdpFormalVerbBinding.cs`
- Modify: `Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Modify: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`

**Step 1: 写失败合同测试**

- `FormalHostActiveTickSmokeTests.ps1` 断言：
  - `TriggerBodyVerbHostManager` 不再在 `Tick()` 中遍历全部 `bindings`
  - manager 具备 `activeVerbsForTick` 或等价活跃集合
  - tick 来源是“当前活跃会话”，不是“当前全部绑定壳”
- `FormalHostVerbSmokeTests.ps1` 补断言：
  - formal host manager 仍只消费已发布投影
  - 读档恢复后仍能把合法持续会话加入活跃队列

**Step 2: 先跑到红灯**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- 明确指出当前仍是“全 binding tick”。

**Step 3: 最小实现**

- `TriggerBodyVerbHostManager`
  - 增加固定 binding 表与 result 索引
  - 增加 `activeVerbsForTick`
  - `Refresh(...)` 只同步 binding，不顺手全量 tick
  - `Tick()` 只遍历活跃集合
- formal host / 实际 verb
  - 对 manager 暴露“是否仍需 tick”的最小状态
  - manager 不知道攻击协议细节，只消费活跃态

**Step 4: 回归验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

**Step 5: 游戏实测停点**

- 自动远程持续射击能稳定打完
- burst 不少发、不多发、不假死
- 近战连续攻击不丢
- 芯片激活/停用/切换后，不出现幽灵 formal host

**Step 6: 提交**

```bash
git add Source/BDP/Core/VerbHosting Source/BDP/Core/Verbs Source/BDP.Tests
git commit -m "perf: tick formal hosts only while active"
```

---

### Task 2: owner 内部投影构建器收口

**Files:**
- Create: `Source/BDP/Core/Trigger/Projection/TriggerProjectionBuildInput.cs`
- Create: `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionBuilder.cs`
- Create: `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`

**Step 1: 写失败合同测试**

- `TriggerSingleTruthSmokeTests.ps1` 断言：
  - `TriggerProjectionBuildInput`、`TriggerCombatProjectionBuilder`、`TriggerPresentationBuilder` 存在
  - `TriggerRuntimeCoordinator` 不再直接调用 `BuildSelectedSnapshot(ownerPawn, owner.LoadoutReaderSurface)`
- `ExpressionPublishedProjectionSmokeTests.ps1` 断言：
  - 已发布投影构建不再依赖公共 reader 反向参与 owner 自己的正式投影构建

**Step 2: 先跑到红灯**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

**Step 3: 最小实现**

- 从 `CompTriggerBody` 内部抓取 build input：
  - 槽位真值
  - 当前激活状态
  - 当前切换上下文
  - 当前禁用状态
  - 当前容器一致性信息
- `TriggerCombatProjectionBuilder` 只负责战斗投影与索引装配
- `TriggerPresentationBuilder` 只负责 presentation / manual / info / visual
- `TriggerRuntimeCoordinator` 只负责 dirty、发布和 CombatBodySession 发布裁定

**Step 4: 回归验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

**Step 5: 游戏实测停点**

- 装卸芯片、切换、gizmo、tooltip、手动攻击都正常
- 读档后首次 UI 与首次攻击结果一致
- 不出现“显示已切换但实际攻击还是旧结果”的真值分裂

**Step 6: 提交**

```bash
git add Source/BDP/Core/Trigger Source/BDP.Tests
git commit -m "refactor: build trigger projections from owner input"
```

---

### Task 3: 表达运行时仓库与缓存

**Files:**
- Create: `Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs`
- Create: `Source/BDP/Core/Expressions/Runtime/ComboRuntimeIndex.cs`
- Create: `Source/BDP/Core/Expressions/Runtime/ChipDefinitionCache.cs`
- Create: `Source/BDP/Core/Expressions/Runtime/ExpressionContractCache.cs`
- Create: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/DefaultExpressionSourceDeclarationProvider.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP/Core/Chips/Access/ChipDefinitionReaderSurface.cs`
- Modify: `Source/BDP/Core/Combos/Access/ComboDefinitionReaderSurface.cs`
- Modify: `Source/BDP.Tests/ComboDefinitionBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`

**Step 1: 写失败合同测试**

- `ExpressionRuntimeRepositorySmokeTests.ps1` 断言：
  - 运行时仓库和三类缓存存在
  - `ComboDefinitionReaderSurface.FindMatch(...)` 不再线性扫 `DefDatabase<ComboDef>.AllDefsListForReading`
  - `ExpressionSnapshotBuilder` 不再每次重建整套静态依赖
- `ComboDefinitionBoundarySmokeTests.ps1` 保持无序匹配合同不变

**Step 2: 先跑到红灯**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
```

**Step 3: 最小实现**

- `ComboRuntimeIndex`
  - 以无序双芯片键建索引
  - 运行期直接命中
- `ChipDefinitionCache`
  - 以 `ThingDef` 为键缓存芯片定义读取结果
- `ExpressionContractCache`
  - 以“芯片定义 + 模式键”缓存契约解释结果
- `ExpressionRuntimeRepository`
  - 统一持有上面三类仓库
- 注意：
  - 只缓存静态定义/契约
  - 不缓存依赖当前 Trigger 真值的最终表达成立结果
  - 不把当前占位的 Trion/CombatBody 条件解释错误地升级成实时失效监听

**Step 4: 回归验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

**Step 5: 游戏实测停点**

- combo、dual、主副手协同攻击仍能正常成立
- tooltip / gizmo / 实际攻击结果一致
- 切换芯片后 combo 成立和失效逻辑不出错

**Step 6: 提交**

```bash
git add Source/BDP/Core/Expressions Source/BDP/Core/Chips Source/BDP/Core/Combos Source/BDP.Tests
git commit -m "perf: cache expression static runtime dependencies"
```

---

### Task 4: 最终纯读化

**Files:**
- Create: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`
- Modify: `Source/BDP/Core/Expressions/Access/Contracts/IExpressionReader.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/ExpressionManualGizmoBridge.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/External/TriggerEquippedGizmoService.cs`
- Modify: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`

**Step 1: 写失败合同测试**

- `TriggerPureReadBoundarySmokeTests.ps1` 断言：
  - `PreparePublishedReadState()` 不再挂在表达/UI 普通热读链上
  - `PrepareReadState()` 不再作为普通 published projection 读取前置
  - `IExpressionReader` 不再把 `GetSnapshot(Pawn)` 当作正式主合同
- `ExpressionPublishedProjectionSmokeTests.ps1` 断言：
  - 表达正式读口改为直接读取 combat / presentation projection

**Step 2: 先跑到红灯**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

**Step 3: 最小实现**

- `IExpressionReader` 收口为最终纯读合同
- `ExpressionFormalSurfaces` 直接读取已发布投影
- `CompTriggerBody.Reads.cs`
  - 普通读取不再触发 `PrepareReadState()`
  - runtime 推进只允许留在 `RuntimeTick()`、写入口、post-load finalize
- 保留 command path 的必要状态刷新，但不污染普通读路径

**Step 4: 回归验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
```

**Step 5: 游戏实测停点**

- 选中单位出 gizmo、tooltip、手动瞄准都正常
- 自动攻击与手动攻击结果一致
- 切换、激活、停用、读档恢复都不依赖“读一下顺手修状态”

**Step 6: 提交**

```bash
git add Source/BDP/Core/Expressions Source/BDP/Core/Trigger Source/BDP.Tests
git commit -m "refactor: make trigger expression reads pure"
```

---

### Task 5: 最终总验收

**Files:**
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Modify: `Source/BDP.Tests/AutoAttackSeparationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ComboDefinitionBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`
- Finalize: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Finalize: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- Finalize: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`

**Step 1: 收紧最终合同**

- 不允许回退到：
  - 全 binding `formal host` tick
  - 公共 reader 反向构建 owner 投影
  - `combo` 线性扫 DefDatabase
  - 普通已发布读取顺手推进状态
  - 破坏 CombatBodySession 发布裁定
  - 破坏 Detach teardown

**Step 2: 完整构建与脚本验收**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerDetachTeardownSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
```

**Step 3: 游戏内总回归矩阵**

1. 单 Pawn 存档
   - 装卸芯片
   - 激活 / 停用 / 切换
   - gizmo / tooltip / 手动 targeting
2. 持续交火存档
   - 自动远程
   - 自动近战
   - burst / 持续射击
   - dual / combo
3. 战斗中读档存档
   - formal host 会话恢复
   - stale session 中断
   - 首次 UI / 首次攻击一致
4. 多武器切换存档
   - 切主武器后 runtime owner 正确切换
   - 卸下触发体后战斗入口及时关闭

**Step 4: 提交**

```bash
git add Source/BDP.Tests
git commit -m "test: lock final performance optimization contracts"
```

---

## 5. 执行顺序

1. Task 1: `formal host` 活跃 tick 化
2. Task 2: owner 内部投影构建器收口
3. Task 3: 表达运行时仓库与缓存
4. Task 4: 最终纯读化
5. Task 5: 最终总验收

原因：

- `formal host` 常驻 tick 仍是最直接、最确定的剩余 steady-state 热点。
- 投影 owner 收口是最终纯读化的前置条件。
- 运行时仓库必须建在稳定的投影构建边界上。
- 纯读化必须放在后面，避免过早切掉读时兜底导致调试面过窄。

---

## 6. 现在就怎么做

如果按本版计划继续执行：

1. 不再重做旧版 Task1。
2. 先进入本版 Task1，也就是 `formal host` 活跃 tick 化。
3. 每次实现完一小阶段，优先看功能是否损坏，不需要中途感受性能。


