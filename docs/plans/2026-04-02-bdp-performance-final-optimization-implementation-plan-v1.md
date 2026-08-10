# 新 BDP 剩余性能优化最终 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在已完成发布式运行时投影主线重构的基础上，一次性收掉新 BDP 当前剩余的全部可确认性能优化项与架构脏边界，包括主武器唯一运行时 owner、formal host 活跃 tick、表达运行时仓库与索引缓存、读口纯读化，以及所有已失去意义的旧残留。

**Architecture:** 继续以 `CompTriggerBody` 持有真值、`TriggerRuntimeCoordinator` 负责运行时协调为唯一正式架构。本计划不再保留任何兼容层和过渡态，而是直接把剩余热点全部推进到 v2 目标态：只有主武器 Trigger 推进运行时，formal host 只 tick 活跃会话，表达构建只消费内部真值与运行时仓库，普通读口最终回归纯读。

**Tech Stack:** C#, RimWorld/Verse mod runtime, PowerShell smoke tests, `dotnet msbuild`

---

## 1. 计划范围与完成定义

### 1.1 本计划只做剩余未完成项

上一轮计划已经完成的内容不再重复施工：

- 已发布战斗投影 / 呈现投影骨架
- AttackExecution 从读时现算切到已发布投影
- UI / targeting 不再回头重建表达快照
- post-load 会话恢复与版本失效统一策略
- 旧 resolver 和无意义 targeting 缓存清理

本计划只覆盖当前仍未彻底完成的优化点：

- `P3` formal host 常驻 tick 税
- `P4` combo / 芯片定义 / 表达契约缺少运行时仓库与索引缓存
- `P6` Trigger 读口仍不是纯读
- v2 设计里仍未彻底落地的剩余边界：
  - 只允许主武器 Trigger 推进运行时
  - `TriggerRuntimeCoordinator` 不再通过公共 `LoadoutReaderSurface` 构建正式投影
  - 删除 `AttackExecutionPlanRuntimeStore`
  - `IExpressionReader` 与表达读表面收口到最终合同

### 1.2 本计划明确不做

- 不接 Trion 完整业务线
- 不实现统一因子计算规则
- 不预留兼容层
- 不分“当前版 / 未来版”两套实现
- 不做“先留个中间态以后再收”的过渡性设计

### 1.3 完成标准

全部任务完成后，必须同时满足：

- 自动战斗、手动攻击、burst、近战、dual、combo、切换、读档恢复等现有功能无损
- 普通表达读取、UI 读取、targeting 读取不再依赖读时状态推进
- 只有主武器上的 `CompTriggerBody` 会推进运行时
- formal host steady-state 只推进活跃会话
- combo 匹配、芯片定义读取、表达契约解释具备运行时仓库或索引复用
- `AttackExecutionPlanRuntimeStore` 删除
- 全套 smoke tests 和游戏回归清单通过

---

## 2. 施工原则

- 每个阶段结束后，优先验证“已有功能是否损坏、是否引入 bug”，不是中途性能体感。
- 每个阶段结束都必须能编译并跑过对应 smoke tests。
- 所有最终合同一次到位，不保留兼容入口。
- Trigger 的运行时语义以“当前装备中的 `CompTriggerBody` thing”为中心，不以 Pawn 为中心持有动态战斗态。
- 芯片配置、槽位状态、激活结果、formal host、攻击入口都属于这把 Trigger 的派生结果，不允许升级成 Pawn 级长期动态状态。
- 触发体未装备在手上时，视为战斗体不存在；相关激活结果、攻击入口、运行时推进都必须关闭退出，不允许保留“离手后继续挂着”的运行时。
- 不额外引入新的全局可变真值 owner。允许新增的长期复用对象仅限：
  - `TriggerRuntimeCoordinator`
  - `TriggerCombatProjectionBuilder`
  - `TriggerPresentationBuilder`
  - `ExpressionRuntimeRepository`
  - 只读索引 / 缓存对象
- 运行时复杂度目标必须清晰：
  - 主武器运行时推进：steady-state 接近 `O(1 + 活跃 formal host 数)`
  - formal host tick：`O(活跃 formal host 数)`，不再是 `O(binding 总数)`
  - combo 匹配：平均接近 `O(1)`，不再线性扫全部 `ComboDef`
  - 芯片定义读取 / 表达契约解释：首次 `O(n)`，重复读取接近 `O(1)`
  - 普通已发布投影读取：接近 `O(1)`，不再顺带触发状态推进

---

## 3. 固定回归素材与命令

### 3.1 建议固定四份游戏回归存档

- 存档 A：单 Pawn，单 Trigger，主副侧各 1 到 2 个芯片。用于测装卸、切换、gizmo、tooltip、手动攻击。
- 存档 B：2 到 6 名 Pawn 持续交火。用于测自动攻击、burst、持续射击、近战。
- 存档 C：战斗进行中立即存档再读档。用于测 post-load 恢复、formal host 会话、持续攻击续接。
- 存档 D：Pawn 同时持有多把武器并频繁切换主武器。用于测主武器唯一运行时 owner。

### 3.2 基础命令

构建：

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

现有核心 smoke tests：

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

本计划新增 smoke tests：

```powershell
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
```

原则：

- 每个阶段至少跑本阶段新增或直接影响到的 smoke tests。
- 每个阶段结束都要进游戏做一轮功能回归。
- 最后一阶段必须跑完整 smoke suite。

---

## 4. 阶段总览

| 阶段 | 目标 | 主要收益 | 你中途重点测什么 |
| --- | --- | --- | --- |
| 阶段 1 | 主武器唯一运行时 owner 与死残留清理 | 消灭全装备 runtime 推进歧义，删除失效残留 | 主武器切换、装卸武器、读档后攻击是否仍正常 |
| 阶段 2 | formal host 活跃 tick 化 | 消灭全 binding 常驻 tick 税 | 连射、burst、近战连续攻击是否仍稳定 |
| 阶段 3 | 内部投影构建器与 owner build input 收口 | 正式投影构建不再依赖公共 reader | 切换、gizmo、手动攻击、读档恢复是否保持一致 |
| 阶段 4 | 表达运行时仓库与索引缓存 | 收掉 combo / 芯片定义 / 契约解释热点 | combo、dual、tooltip、实际攻击是否一致 |
| 阶段 5 | 最终纯读化与总验收 | 普通读取回归纯读，剩余旧边界彻底删除 | 全功能总回归，无新 bug、无旧逻辑回流 |

---

### Task 1: 主武器唯一 RuntimeTick 与死残留清理

**Files:**
- Create: `Source/BDP.Tests/PrimaryTriggerRuntimeOwnershipSmokeTests.ps1`
- Modify: `Source/BDP/Patches/Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- Modify: `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- Delete: `Source/BDP/Core/AttackExecution/AttackExecutionPlanRuntimeStore.cs`

**目标**

- 把“谁能推进 Trigger 运行时状态”彻底锁成主武器唯一入口。
- 删除已经失去意义的静态计划寄存残留。
- 给后续 formal host 活跃 tick 和纯读化提供稳定 runtime 边界。

**Step 1: 先写会失败的合同测试**

- 在 `PrimaryTriggerRuntimeOwnershipSmokeTests.ps1` 里增加断言：
  - `Patch_Pawn_EquipmentTracker_EquipmentTrackerTick` 不再使用 `AllEquipmentListForReading`
  - 只读取 `equipment.Primary`
  - `CompTriggerBody` 或 `TriggerRuntimeCoordinator` 存在明确 `RuntimeTick()` 推进入口
  - `AttackExecutionPlanRuntimeStore.cs` 文件不存在
- 在 `TriggerSingleTruthSmokeTests.ps1` 里补断言：
  - `TriggerRuntimeCoordinator` 负责统一运行时推进，不只是发布
- 在 `PostLoadAttackSessionRecoverySmokeTests.ps1` 里补断言：
  - post-load finalize 仍走统一 runtime owner，不因主武器唯一入口丢失恢复时机

**Step 2: 跑测试确认红灯**

Run:

```powershell
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- 新增合同先失败，明确指出当前仍存在全装备扫描或无唯一 runtime tick 入口。

**Step 3: 实现主武器唯一 runtime 推进**

- 在 `Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs` 中改成：
  - 只读取 `__instance.Primary`
  - 只对主武器上的 `CompTriggerBody` 调用统一 runtime tick
- 在 `CompTriggerBody` / `TriggerRuntimeCoordinator` 中补上最终 `RuntimeTick()` 顺序：
  1. 验证 owner 仍是当前主武器
  2. 处理 post-load finalize
  3. 同步 disable 状态并标 dirty
  4. 结算到期 switch transition 并标 dirty
  5. 如 dirty 则 `RebuildAndPublish()`
  6. 推进 active formal host

**Step 4: 删除失效残留**

- 删除 `AttackExecutionPlanRuntimeStore.cs`
- 用 `rg` 确认仓库里无任何剩余引用

**Step 5: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected:

- 编译通过
- ownership / single truth / post-load / projection version 相关 smoke tests 通过

**阶段停点：你可以立刻回游戏测**

- 多把武器切主武器后，BDP 攻击、gizmo、formal host 都只跟当前主武器走
- 卸下主武器、重新装备主武器后，自动攻击和手动攻击不坏
- 战斗中读档后，攻击会话仍能正确恢复或正确中断

**这阶段如果出问题，优先怀疑**

- `Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs`
- `CompTriggerBody.Lifecycle.cs`
- `TriggerRuntimeCoordinator.cs`

---

### Task 2: formal host 改成只 tick 活跃会话

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

**目标**

- 把 formal host steady-state 从“全 binding 全壳 tick”改成“只推进活跃会话”。
- 保持 burst、持续射击、近战连续攻击、读档后续接不损坏。

**Step 1: 先写会失败的合同测试**

- 在 `FormalHostActiveTickSmokeTests.ps1` 中增加断言：
  - `TriggerBodyVerbHostManager` 持有 `activeVerbsForTick` 或等价活跃列表
  - `Tick()` 不再遍历全部 binding 并无差别调用 `VerbTick()`
  - formal host manager 的 tick 源来自活跃会话，不来自“所有已绑定结果”
- 在 `FormalHostVerbSmokeTests.ps1` 中补断言：
  - manager 只消费已发布投影与活跃队列

**Step 2: 跑测试确认红灯**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- 新合同先失败，指出当前仍是全 binding tick。

**Step 3: 实现活跃 formal host 队列**

- 在 `TriggerBodyVerbHostManager` 中引入最终数据形态：
  - 固定 binding 表
  - `bindingsByResultId`
  - `activeVerbsForTick`
- 定义 formal host 活跃判定：
  - 正在持续执行
  - 正在 burst / warmup / 近战推进
  - 读档恢复后仍持有合法会话
- `Refresh(...)` 只负责同步 binding，不负责给全部壳强行常驻 tick
- `Tick()` 只遍历 `activeVerbsForTick`

**Step 4: 让 formal host 壳暴露最小活跃态**

- 在 `BdpVerb_FormalHostShoot` / `BdpVerb_FormalHostMelee` / `BdpVerb_Shoot` / `BdpVerb_MeleeAttackDamage` 中补最小活跃查询口
- 保证 manager 不需要知道攻击协议内部细节，也不靠猜测主攻来决定活跃性

**Step 5: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- formal host active tick 合同通过
- ranged / burst / post-load 回归通过

**阶段停点：你可以立刻回游戏测**

- 自动远程持续射击还能稳定打完
- burst 不会莫名少打一发或卡住
- 近战连续攻击不丢
- 战斗中切换/停用/激活芯片时，不出现 formal host 幽灵攻击或假死

**这阶段如果出问题，优先怀疑**

- `TriggerBodyVerbHostManager.cs`
- `BdpVerb_FormalHostShoot.cs`
- `BdpVerb_Shoot.cs`

---

### Task 3: 投影构建改成 owner 内部 build input，不再依赖公共 reader

**Files:**
- Create: `Source/BDP/Core/Trigger/Projection/TriggerProjectionBuildInput.cs`
- Create: `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionBuilder.cs`
- Create: `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Modify: `Source/BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `Source/BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1`

**目标**

- 正式投影构建器只消费 `CompTriggerBody` 内部真值，不再通过公共 `LoadoutReaderSurface` 反向构建 owner 自己的正式投影。
- 给最终纯读化提前铺平边界。

**Step 1: 先写会失败的合同测试**

- 在 `TriggerSingleTruthSmokeTests.ps1` 中增加断言：
  - `TriggerProjectionBuildInput`、`TriggerCombatProjectionBuilder`、`TriggerPresentationBuilder` 存在
  - `TriggerRuntimeCoordinator` 不再调用 `BuildSelectedSnapshot(ownerPawn, owner.LoadoutReaderSurface)`
- 在 `ExpressionPublishedProjectionSmokeTests.ps1` 中补断言：
  - published projection 构建不再依赖公共 reader 反向重入

**Step 2: 跑测试确认红灯**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 新增合同先失败，明确指出 coordinator 仍依赖公共 reader。

**Step 3: 实现 owner 内部 build input**

- 在 `CompTriggerBody` 上新增或整理只对内部可见的真值抓取口，收集：
  - 当前槽位真值
  - 当前激活侧
  - 当前切换上下文
  - 当前禁用状态
  - 当前容器一致性相关信息
- 用 `TriggerProjectionBuildInput` 作为 builder 输入，不让外部 reader 反向参与正式投影构建

**Step 4: 实现独立 projection builders**

- `TriggerCombatProjectionBuilder` 负责战斗投影构建与索引装配
- `TriggerPresentationBuilder` 负责 info / manual / visual projection 构建
- `TriggerRuntimeCoordinator` 只负责协调，不再内联快照构建细节

**Step 5: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
```

Expected:

- 投影构建 owner 边界合同通过
- post-load 恢复不回退

**阶段停点：你可以立刻回游戏测**

- 装卸芯片、切换、gizmo、tooltip、手动攻击都仍正常
- 读档后首次 UI 与首次攻击结果一致
- 不出现“显示已切换，但攻击结果还是旧的”这类真值分裂

**这阶段如果出问题，优先怀疑**

- `TriggerRuntimeCoordinator.cs`
- `CompTriggerBody.Contexts.cs`
- `TriggerCombatProjectionBuilder.cs`

---

### Task 4: 建立表达运行时仓库，收掉 combo / 定义 / 契约热点

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

**目标**

- 建立只服务投影构建阶段的运行时仓库。
- 把 combo 匹配、芯片定义读取、表达契约解释从“战斗时重复现做”改成“启动后长期复用”。

**Step 1: 先写会失败的合同测试**

- 在 `ExpressionRuntimeRepositorySmokeTests.ps1` 中增加断言：
  - `ExpressionRuntimeRepository`、`ComboRuntimeIndex`、`ChipDefinitionCache`、`ExpressionContractCache` 存在
  - `ComboDefinitionReaderSurface.FindMatch(...)` 不再直接线性扫 `DefDatabase<ComboDef>.AllDefsListForReading`
  - `ExpressionSnapshotBuilder` 不再每次 new 完整解释链
- 在 `ComboDefinitionBoundarySmokeTests.ps1` 中补断言：
  - combo 仍支持无序匹配

**Step 2: 跑测试确认红灯**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
```

Expected:

- 新增合同先失败，指出线性扫 DefDatabase 或重复 new 解释链仍存在。

**Step 3: 实现运行时仓库**

- `ComboRuntimeIndex`
  - 在初始化时把 combo 按无序键建立索引
  - 运行期按两枚芯片 Def 直接命中
- `ChipDefinitionCache`
  - 以 `ThingDef` 为键缓存芯片定义读取结果
- `ExpressionContractCache`
  - 以“芯片定义 + 模式键”作为缓存键
- `ExpressionRuntimeRepository`
  - 统一持有并暴露上述三类只读仓库

**Step 4: 改造投影构建路径消费仓库**

- `ExpressionSurfaceAccess` 统一持有仓库
- `ExpressionSnapshotBuilder` 与相关 provider / interpreter 改为消费仓库，不再临时 new 全套依赖
- 明确缓存边界只覆盖静态定义和契约解释，不缓存依赖当前 Trigger 真值的最终表达成立结果

**Step 5: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 运行时仓库和 combo 合同通过
- 表达发布投影回归通过

**阶段停点：你可以立刻回游戏测**

- combo、dual、主副手协同攻击仍能正常成立
- tooltip / gizmo / 实际攻击结果一致
- 切换芯片后 combo 成立和失效逻辑不出错

**这阶段如果出问题，优先怀疑**

- `ExpressionRuntimeRepository.cs`
- `ComboRuntimeIndex.cs`
- `ExpressionSnapshotBuilder.cs`

---

### Task 5: 最终纯读化与表达读表面合同收口

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

**目标**

- 把普通热读路径最终收口成纯读。
- 把 `IExpressionReader` 拉到最终合同，不再保留为了过渡而留下的旧读取语义。

**Step 1: 先写会失败的合同测试**

- 在 `TriggerPureReadBoundarySmokeTests.ps1` 中增加断言：
  - `PreparePublishedReadState()` 不再作为表达/UI 热读兜底
  - `PrepareReadState()` 不再挂在普通 published projection 读取链上
  - `IExpressionReader` 不再把 `GetSnapshot(Pawn)` 作为主要读合同
  - `DefaultManualEntryGizmoResolver` 不再依赖 `reader.GetSnapshot(pawn)`
- 在 `ExpressionPublishedProjectionSmokeTests.ps1` 中补断言：
  - 表达只读表面改为直接读 combat / presentation projection

**Step 2: 跑测试确认红灯**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 新增合同先失败，指出普通热读路径仍带状态准备副作用。

**Step 3: 收口最终读合同**

- `IExpressionReader` 改成最终纯读形态，建议至少提供：
  - `GetCombatProjection(Pawn pawn)`
  - `GetPresentationProjection(Pawn pawn)`
  - `TryGetCurrentResult(Pawn pawn, string resultId, out FormalExpressionResult result, out int projectionVersion)`
- 同步修改全部调用点：
  - gizmo
  - manual projection
  - 说明投影
  - 任何仍依赖 `GetSnapshot(...)` 的消费者
- 删除或内收只为过渡存在的旧读取入口

**Step 4: 把普通读口彻底拉回纯读**

- `CompTriggerBody.Reads.cs` 中普通读取不再调用 `PrepareReadState()`
- 运行时推进只留在：
  - `RuntimeTick()`
  - 写入口
  - post-load finalize
- 如某些业务读口仍需要“命令前刷新”，单独保留 command path，不允许污染普通 published 读取

**Step 5: 构建并验证**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
```

Expected:

- pure-read 边界合同通过
- 表达、single truth、attack execution 回归通过

**阶段停点：你可以立刻回游戏测**

- 选中单位出 gizmo、tooltip、手动瞄准都正常
- 自动攻击与手动攻击结果一致
- 切换、激活、停用、读档恢复都不依赖“读一下就顺手修状态”

**这阶段如果出问题，优先怀疑**

- `CompTriggerBody.Reads.cs`
- `ExpressionFormalSurfaces.cs`
- `DefaultManualEntryGizmoResolver.cs`

---

### Task 6: 最终总验收与游戏回归矩阵

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
- Create or Finalize: `Source/BDP.Tests/PrimaryTriggerRuntimeOwnershipSmokeTests.ps1`
- Create or Finalize: `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- Create or Finalize: `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- Create or Finalize: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`

**目标**

- 把所有“最终目标态合同”写进 smoke tests。
- 完成一次构建、测试、游戏功能总回归，不再留后续收口任务。

**Step 1: 统一收紧 smoke tests**

- 所有 smoke tests 一律按最终目标态写死，不允许再保留“暂时允许”的旧语义：
  - 不允许全装备 runtime 扫描
  - 不允许 formal host 全 binding tick
  - 不允许 published read 触发状态推进
  - 不允许 combo 线性扫 DefDatabase
  - 不允许 `AttackExecutionPlanRuntimeStore`
  - 不允许 `IExpressionReader.GetSnapshot(...)` 作为正式主读口

**Step 2: 完整构建与脚本验收**

Run:

```powershell
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\PostLoadAttackSessionRecoverySmokeTests.ps1'
& '.\Source\BDP.Tests\AutoAttackSeparationSmokeTests.ps1'
& '.\Source\BDP.Tests\AttackExecutionProjectionVersionSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\PrimaryTriggerRuntimeOwnershipSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostActiveTickSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionRuntimeRepositorySmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'
```

Expected:

- 全部 PASS

**Step 3: 游戏内总回归清单**

按固定存档逐项验证：

1. 存档 A
- 装卸芯片
- 激活 / 停用 / 切换
- gizmo / tooltip / 手动 targeting

2. 存档 B
- 自动远程
- 自动近战
- burst / 持续射击
- dual / combo

3. 存档 C
- 战斗中读档
- formal host 会话恢复
- stale session 正确中断

4. 存档 D
- 多武器切主武器
- 卸下主武器
- 非主武器不干扰当前运行时

**Step 4: 最终收口判断**

只有满足以下全部条件，才算本计划完成：

- 所有 smoke tests 通过
- 以上四份存档回归无明显功能损坏
- 无新增“自动攻击不工作”“第一枪异常”“读档后失效”“切换后动作不断开”一类功能性 bug

---

## 5. 实施顺序裁决

本计划采用 `B. 架构层次优先` 的顺序，但每个阶段的停点只看功能回归，不看中途性能体感。

执行顺序固定为：

1. `Task 1`
2. `Task 2`
3. `Task 3`
4. `Task 4`
5. `Task 5`
6. `Task 6`

不要并行跳阶段。原因：

- `Task 1` 决定运行时 owner 边界，是后面全部优化的前置条件
- `Task 2` 依赖主武器唯一 runtime 推进入口
- `Task 3` 决定 projection build 的最终 owner 边界
- `Task 4` 的运行时仓库必须在最终 builder 边界上接线
- `Task 5` 必须最后做，避免过早切掉读时兜底导致调试面过窄

---

## 6. 计划结论

这份计划就是剩余性能优化项的一次性终局实施计划，不再预留后续“下一版再收”。

最终目标态写死为：

- 只有主武器 Trigger 推进运行时
- `TriggerRuntimeCoordinator` 统一负责 runtime tick 与发布
- formal host 只 tick 活跃会话
- 正式投影构建只消费 owner 内部真值与运行时仓库
- combo / 芯片定义 / 表达契约走长期复用仓库
- 普通 published 读取回归纯读
- 旧残留与过渡接口全部删除

Plan complete and saved to `docs/plans/2026-04-02-bdp-performance-final-optimization-implementation-plan-v1.md`.
