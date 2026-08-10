# 新 BDP 主线性能重构目标态设计 v2

> 日期：2026-04-02
> 输入：v1 设计稿 + Claude 二审报告 + 当前源码自查
> 目标：形成可直接进入实施拆解的终版目标态设计
> 原则：不保留兼容层，不设计渐进迁移态，不允许旧新双轨长期并存

---

## 1. 一页结论

v1 的方向是对的，根因判断也对，但还不够收口。  
本版修正后，最终目标态明确为：

1. `CompTriggerBody` 不再自己内联承担所有运行时协调，而是持有一个内部 `TriggerRuntimeCoordinator`
2. `TriggerRuntimeCoordinator` 是唯一的运行时协调者，负责 dirty 收口、投影发布、formal host 同步、运行时 tick
3. 核心战斗投影与 UI 呈现投影彻底分离
4. `AttackExecution` 删除“按 `ResultId` 回头现算”的 resolver 语义，但保留 `TryPreparePlan` / `TryPrepareCast` 这类计划准备入口，改为纯校验 + 纯编排
5. `AttackExecutionResolvedRequest` 被新的内部准备态对象替代，不再承载“重新解析结果”的含义
6. `AttackExecutionPlanRuntimeStore` 直接删除，不保留静态全局计划寄存
7. `AttackExecutionTargetingSource` 也必须切到已发布投影，不允许继续调用 `TryGetSelectedResult()` 做读时解析
8. `TriggerCombatProjectionState` 只保存战斗核心数据和必要索引，不混入 `ManualProjection` / `VisualProjection`
9. 当前版本下，投影失效不依赖 Trion 运行时资源变化；如果未来让表达成立条件真正依赖 Trion，可用量变化必须单独立项改设计
10. 版本失效后的统一策略选择为：**结束当前 BDP 攻击会话，交还上层重新选取**

这版的目标不是“继续补缓存”，而是把当前系统改成一套真正闭环的发布式运行时架构。

---

## 2. 本版修订结论

## 2.1 接受的二审意见

以下意见直接进入 v2：

- `CompositeReferences` 的下游依赖必须保留，而且应同时补 `CompositeReferenceIndex`
- `TryPreparePlan()` / `TryPrepareCast()` 的目标态必须明确，不能只改 `TryExecute()`
- `AttackExecutionPlanRuntimeStore` 的命运必须写死
- `ManualProjection` / `VisualProjection` 不应混入核心战斗投影
- `CompTriggerBody` 不应继续膨胀，必须引入内部协调者
- `TriggerCombatProjectionState` 必须明确为非序列化运行时对象
- 版本失效后的恢复策略必须闭环

## 2.2 部分接受的二审意见

关于 Trion 运行时条件：

- 二审提出“Trion 可用量变化可能导致投影失效”，这个提醒是正确的
- 但按当前源码事实，`TrionExpressionConditionInterpreter` 仍是预留分支，返回 `IsSatisfied = false`，并未把 Trion 实时资源接入正式表达成立条件
- `ExpressionSourceTrionConfig.MinimumRequired` 当前也没有进入表达构建判定

因此 v2 的最终判断是：

- **当前版本投影失效矩阵不纳入 Trion 资源变化**
- 但文档必须显式写明这是建立在“当前代码未启用 Trion 运行时表达 gating”这一前提上

## 2.3 我自己的自查补充

除了二审意见，本版还补上以下自查结果：

- `TriggerCombatProjectionState` 在 v1 里字段重复，`PrimaryRanged` / `PrimaryMelee` 与 `Snapshot` 重复表达同一事实，v2 去掉重复字段
- 核心投影构建器不应依赖公共 `ITriggerLoadoutReader`，而应直接消费 `CompTriggerBody` 的内部真值快照，避免公共读口反向参与正式投影构建
- `AttackExecutionTargetingSource` 现在自己维护一套按 tick 刷新的解析上下文，这与目标态的“统一只读已发布投影”冲突，v2 明确重构
- “主装备是唯一活跃 Trigger”必须补齐首轮投影发布时间，否则会留下“卸下时清了，装备上时谁来发布”的时序漏洞

---

## 3. 不可动摇的终局原则

### 3.1 单一 owner

当前时刻“BDP 认为这把武器能怎么打”的正式战斗结果，只允许有一个 owner：

- `CompTriggerBody` 持有的当前已发布运行时投影

更准确地说：

- 真值 owner 是 `CompTriggerBody`
- 运行时协调 owner 是 `TriggerRuntimeCoordinator`
- 当前已发布战斗投影 owner 仍然属于这把 `CompTriggerBody`

不允许以下模块再各自推导一份“我认为当前该怎么打”：

- 自动战斗入口
- `AttackExecutionTargetingSource`
- `AttackExecutionService`
- UI / gizmo / visual projection
- formal host manager

### 3.2 发布式读取

所有消费方都只能读取**已经发布**的运行时投影，不允许读时触发：

- 表达重建
- Trigger 状态准备
- formal host 修正
- 结果补查

### 3.3 纯读 / 纯构建 / 纯推进

三类行为必须彻底拆开：

- 纯读：只读当前发布状态
- 纯构建：只根据当前真值构造下一份完整投影
- 纯推进：只推进运行时状态与 dirty 检测

### 3.4 删除旧语义，不保留兼容

下列旧语义在目标态中必须消失：

- `BuildSelectedSnapshot(pawn)` 的按需现算语义
- `TryGetSelectedResult(pawn, resultId, ...)` 的按需现算语义
- `PrepareReadState()` 的读时状态准备语义
- `SnapshotAttackExecutionResolver` 的“最小请求 -> 回头解析”语义
- formal host 的全 binding 常驻 tick
- targeting source 的独立解析缓存真值链

### 3.5 Trigger 装备态硬约束

- Trigger 的正式运行时语义以“当前装备中的 `CompTriggerBody` thing”为中心，不以 Pawn 为中心保存动态战斗态。
- 芯片配置、槽位真值、激活结果、formal host、攻击入口都属于这把 Trigger 派生出的运行时结果，而不是 Pawn 自身的长期状态。
- “触发体被装备在手上”是战斗体存在的前提；触发体离手、卸下、被其它武器替换后，相关战斗入口与运行时必须整体关闭退出。
- 因此目标态不允许引入 Pawn 级的“当前激活芯片”“当前攻击入口”“当前 formal host 会话”这类长期动态缓存；外部系统只能通过“当前主武器上的 Trigger”读取正式结果。

---

## 4. 最终架构总览

目标态核心结构如下：

1. `CompTriggerBody`
   - 仍是真值 owner
   - 持有 `TriggerRuntimeCoordinator`
   - 对外只暴露纯读表面和真值写入口

2. `TriggerRuntimeCoordinator`
   - 运行时唯一协调者
   - 负责 dirty 标记、运行时 tick、投影发布、presentation 发布、formal host 同步

3. `TriggerCombatProjectionState`
   - 当前已发布的核心战斗投影
   - 非序列化、发布后只读、整包替换

4. `TriggerPresentationState`
   - 当前已发布的 UI / 说明 / 手动入口投影
   - 版本号必须与核心战斗投影一致

5. `AttackExecutionRequest`
   - 外部正式请求
   - 只携带“选择了哪条结果、当时是哪个版本”

6. `AttackExecutionPreparedContext`
   - 内部准备态对象
   - 替代 `AttackExecutionResolvedRequest`
   - 只承载已验证版本下的编排上下文

7. `TriggerBodyVerbHostManager`
   - 只消费已发布核心战斗投影
   - 维护 binding、索引和活跃 tick 列表

8. `ExpressionRuntimeRepository`
   - 持有 combo 索引、芯片定义缓存、表达契约缓存
   - 只服务投影构建阶段

---

## 5. 运行时对象模型

## 5.1 `TriggerRuntimeCoordinator`

`TriggerRuntimeCoordinator` 是本次重构后的核心运行时对象。

建议职责：

- 持有当前 `TriggerCombatProjectionState`
- 持有当前 `TriggerPresentationState`
- 持有当前 `ProjectionVersion`
- 持有 dirty 标志与 dirty 原因
- 处理 `RuntimeTick()`
- 负责 `RebuildAndPublish()`
- 协调 `Expression projected hosts` 与 `TriggerBodyVerbHostManager`

建议字段：

- `CompTriggerBody owner`
- `int currentProjectionVersion`
- `TriggerCombatProjectionState currentCombatProjection`
- `TriggerPresentationState currentPresentation`
- `bool projectionDirty`
- `ProjectionDirtyReason dirtyReason`
- `bool pendingPostLoadFinalize`
- `string lastPrimaryOwnerKey`

关键规则：

- 协调者是纯运行时对象，不参与存档
- 所有发布动作都由它统一完成
- `CompTriggerBody` 不再内联大段刷新与同步流程

## 5.2 `TriggerCombatProjectionState`

这是一份纯核心战斗投影对象，不允许混入 UI 表现数据。

最终字段建议：

- `int ProjectionVersion`
- `ExpressionSnapshot Snapshot`
- `IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex`
- `IReadOnlyDictionary<string, CompositeExpressionReference> CompositeReferenceIndex`
- `IReadOnlyDictionary<string, BdpFormalVerbHostSlot> ResultIdToFormalSlot`
- `bool IsEmpty`

明确不放入：

- `ManualEntryProjection`
- `VisualExpressionProjection`
- `ExpressionInfoProjection`
- 与 `Snapshot` 重复的 `PrimaryRanged` / `PrimaryMelee`

原因：

- `Snapshot` 已经是核心表达总表
- `Snapshot.PrimaryRanged` / `PrimaryMelee` / `CompositeReferences` 已经是正式事实
- 投影状态需要的是**索引能力**，不是把同一事实再复制一遍

规则：

- `Snapshot` 发布后视为只读
- `ResultIndex` 与 `CompositeReferenceIndex` 都从 `Snapshot` 一次性构建
- 任何消费者都只读，不得写回

## 5.3 `TriggerPresentationState`

这是与核心战斗投影并行发布的呈现层状态，不属于核心战斗真值。

最终字段建议：

- `int ProjectionVersion`
- `ExpressionInfoProjection InfoProjection`
- `ManualEntryProjection ManualProjection`
- `VisualExpressionProjection VisualProjection`

规则：

- `TriggerPresentationState` 由协调者在核心战斗投影发布后同步构建
- 它与 `TriggerCombatProjectionState` 是同版本的 sibling state
- UI 读取只读 `TriggerPresentationState`
- 战斗执行永远不依赖它

这样既保留性能收益，又不把 UI 结构污染到核心投影中。

## 5.4 `AttackExecutionRequest`

外部正式执行请求最终字段建议：

- `string AttackInstanceId`
- `Pawn Pawn`
- `string ResultId`
- `int ProjectionVersion`
- `LocalTargetInfo Target`
- `AttackExecutionReason Reason`
- `AttackDispatchIntent DispatchIntent`

注意：

- 外部请求不直接携带 `ExpressionSnapshot`
- 外部请求不直接携带 `FormalExpressionResult`
- 外部请求也不直接携带 `TriggerCombatProjectionState`

原因：

- 外部调用方只需要表达“在版本 N 的投影里，我选择了 resultId”
- 真正的当前已发布投影仍由 `CompTriggerBody` owner 持有
- 这样可以保证入口总是向 owner 读取一次纯读状态，再做版本校验

## 5.5 `AttackExecutionPreparedContext`

这是内部准备态对象，替代当前的 `AttackExecutionResolvedRequest`。

最终字段建议：

- `AttackExecutionRequest Request`
- `TriggerCombatProjectionState Projection`
- `FormalExpressionResult Result`
- `AttackExecutionPlan Plan`
- `IReadOnlyList<AttackRuntimeStep> RuntimeSteps`
- `AttackExecutionCursor Cursor`

设计规则：

- 它不再表示“resolver 刚解析完”
- 它表示“请求已经通过版本校验，并拿到了当前投影中的正式结果”
- 所有执行器、协议构建器、上下文创建器都改为消费这个对象

---

## 6. 构建器与读取器分离

## 6.1 核心构建器只消费内部真值

v2 明确规定：

- 正式战斗投影构建器不允许再依赖公共 `ITriggerLoadoutReader`

原因：

- 公共 reader 是对外读表面
- 它的职责是被消费，而不是被正式投影反向用来构建 owner 自己的真值投影

因此新增：

- `TriggerProjectionBuildInput`

由 `TriggerRuntimeCoordinator` 从 `CompTriggerBody` 当前内部真值收集：

- 当前槽位真值
- 当前激活侧
- 当前切换上下文
- 当前禁用状态
- 当前容器关系

然后交给：

- `TriggerCombatProjectionBuilder`

产出：

- `TriggerCombatProjectionState`

## 6.2 表达只读服务的最终职责

`ExpressionReadService` 只做纯读：

- `GetCombatProjection(Pawn pawn)`
- `GetPresentationProjection(Pawn pawn)`
- `TryGetCurrentResult(Pawn pawn, string resultId, out FormalExpressionResult result, out int projectionVersion)`

旧接口的命运：

- `GetSnapshot(Pawn pawn)` 删除
- `BuildSelectedSnapshot(pawn)` 删除
- `TryGetSelectedResult(pawn, resultId, ...)` 删除

如果需要对外保留“读快照”的能力，也只能是：

- 从 `GetCombatProjection(pawn)` 里读取 `Snapshot`

而不是重新构建。

---

## 7. 发布与失效模型

## 7.1 Dirty 来源

以下事件会标记 projection dirty：

- 装入芯片成功
- 卸下芯片成功
- 激活正式提交
- 停用正式提交
- 禁用状态变化
- 到期切换被正式结算
- 读档恢复完成
- 当前主装备发生变化
- 当前武器从已装备变为未装备
- 当前 projection 尚未发布，但 owner 已进入运行时

## 7.2 发布时机

最终规则分两类：

### 写入口后的即时发布

以下成功写操作在结束前直接调用协调者发布：

- `TryLoadChip`
- `TryUnloadChip`
- `NotifySlotActivationCommitted`
- `NotifySlotDeactivated`

### 运行时 tick 内发布

以下变化只能在 `RuntimeTick()` 中发现并结算，因此在 tick 内收口：

- disable sync 导致的正式禁用变化
- due switch transition 到期结算
- 主装备切换后的首轮投影发布
- post-load finalize

## 7.3 `RuntimeTick()` 的最终流程

`CompTriggerBody.RuntimeTick()` 最终只做以下顺序：

1. 若当前 owner 不是 `pawn.equipment.Primary`，直接返回
2. 若 pending post-load finalize，先完成恢复与首轮发布
3. 同步 disable 状态；若真值变化则标 dirty
4. 结算 due switch transitions；若真值变化则标 dirty
5. 如果 dirty，则调用 `RebuildAndPublish()`
6. tick `activeVerbsForTick`

重要边界：

- `RuntimeTick()` 是推进入口
- 不是读口兜底入口
- 也不是 UI / targeting 的后门刷新入口

## 7.4 主装备切换规则

v2 明确规定：

- 一个 pawn 同一时刻只有主装备上的 Trigger 可以发布正式运行时投影

对应规则：

- `Patch_Pawn_EquipmentTracker_EquipmentTrackerTick` 不再扫描 `AllEquipmentListForReading`
- 它只读取 `equipment.Primary`
- 若主装备有 `CompTriggerBody`，调用其 `RuntimeTick()`
- `Notify_Unequipped()` 立即清空当前发布投影、presentation 和 formal host 状态
- 新装备成为 primary 后，由其第一次 `RuntimeTick()` 负责首轮发布

这样能彻底消灭“全装备 TryGetComp 扫描”和“多把武器同时拥有活跃投影”的语义歧义。

## 7.5 非序列化规则

以下对象全部是纯运行时对象，不参与存档：

- `TriggerRuntimeCoordinator`
- `TriggerCombatProjectionState`
- `TriggerPresentationState`
- `ResultIndex`
- `CompositeReferenceIndex`
- `activeVerbsForTick`

读档规则：

- 只恢复 Trigger 真值、容器、slot truth、formal host 壳本体
- post-load finalize 完成后，由协调者重建并发布运行时投影

## 7.6 Trion 边界说明

当前代码事实下：

- `TrionExpressionConditionInterpreter` 仍是占位实现
- `ExpressionSourceTrionConfig.MinimumRequired` 尚未参与正式表达成立判定
- 当前投影的可用性不依赖 Trion 实时资源变化

因此 v2 明确规定：

- 当前投影失效矩阵**不监听** `ITrionEvents`

但同时明确保留一条边界说明：

- 如果未来让 Trion 可用量真实参与表达成立条件，那将直接改变投影失效模型，必须单独重做本设计的 Section 7

---

## 8. 攻击执行最终模型

## 8.1 入口校验模型

`AttackExecutionService` 的第一步变成：

1. 读取当前主装备 `CompTriggerBody` 的 `CurrentCombatProjection`
2. 校验 `request.ProjectionVersion == currentProjection.ProjectionVersion`
3. 从 `currentProjection.ResultIndex[request.ResultId]` 命中结果
4. 组装 `AttackExecutionPreparedContext`

这一步只允许做：

- 纯读
- 版本校验
- O(1) 索引查找

不允许做：

- 表达重建
- 重新读 Trigger 表面
- 重新解释芯片契约
- 重新扫描 snapshot

## 8.2 `TryPreparePlan()` / `TryPrepareCast()` 的最终命运

这两个接口**保留**，但语义彻底变化。

### `TryPreparePlan(AttackExecutionRequest request, out AttackExecutionPreparedContext prepared)`

职责：

- 校验版本
- 命中结果
- 构建 plan
- 构建 runtime steps
- 返回内部准备态上下文

### `TryPrepareCast(AttackExecutionRequest request, out AttackExecutionPreparedContext prepared, out AttackExecutionCast cast)`

职责：

- 调用 `TryPreparePlan`
- 取首个 cast

理由：

- 当前 `JobDriver_BdpRangedAttackExecution.PrepareNextCast()` 真实需要的是“为下一轮持续推进准备 plan / step / cast”
- 这不是兼容遗留，而是目标架构下仍然合理的正式能力

因此 v2 的决定不是删除这两个口，而是：

- **保留能力，删除旧语义**

## 8.3 `TryExecute()` 的最终语义

`TryExecute(AttackExecutionRequest request)` 仅作为：

- `TryPreparePlan()` + `TryExecutePrepared()` 的快捷入口

内部实现可以继续调用准备阶段，但不再经过 resolver。

## 8.4 `AttackExecutionPreparedContext` 的下游消费

以下模块统一改为消费 `AttackExecutionPreparedContext`：

- `AttackExecutionService.Stages`
- `DefaultRangedAttackExecutor`
- `DefaultMeleeAttackExecutor`
- `DefaultAttackEffectEmitter`
- `RangedAttackExecutionContext`
- `MeleeAttackExecutionContext`
- `RangedAttackProtocolService`
- `RangedBurstEmissionAssembler`

这样：

- `FindCompositeReference()` 改读 `prepared.Projection.CompositeReferenceIndex`
- `FindSourceResult()` 改读 `prepared.Projection.ResultIndex`
- 不再线性扫描 `prepared.Snapshot.CompositeReferences`
- 不再线性扫描 `prepared.Snapshot.Results`

## 8.5 版本失效后的恢复策略

v2 明确选择：

- **结束当前 BDP 会话，交还上层重新选取**

具体表现：

- 自动战斗：当前准备失败，返回上层；由下一轮自动攻击选择重新进入
- 手动 targeting：确认目标时若版本失效，本次命令取消，提示目标已过期
- 远程持续攻击 job：`PrepareNextCast()` 返回 false 时结束当前 BDP job，由上层攻击系统重新决策

不采用自动重试的原因：

- 版本失效意味着正式战斗投影已变化
- 表达变化可能同时改变主攻击、武器模式、formal host、Trion 消耗语义
- 自动重试会把“应该重选”的语义偷偷变成“沿用旧意图继续打”，边界不干净

## 8.6 `AttackExecutionPlanRuntimeStore` 的最终命运

直接删除。

理由：

- 当前代码中它已经是静态全局残留，实际上没有形成必要的唯一运行时数据源
- 新模型下，持续推进所需的数据分别绑定在：
  - `AttackExecutionPreparedContext`
  - `RangedAttackExecutionContext`
  - `RangedVerbEmissionPlan`
  - verb 当前绑定的运行时上下文
- 再保留一个静态全局计划表只会重新引入“谁是真正 owner”的歧义

最终规则：

- per-attack 运行时数据只允许挂在当前攻击会话对象上
- 不允许再走静态全局 store

---

## 9. `AttackExecutionTargetingSource` 的最终模型

这是本次自查后必须补齐的一项。

当前问题：

- 它内部仍通过 `TryGetSelectedResult()` 读时解析 snapshot/result
- 自己维护一套按 tick + stateKey 的局部缓存

这与目标态冲突。

最终设计：

1. targeting source 只保留：
   - `Pawn`
   - `ResultId`
   - `ProjectionVersion`
   - `Reason`
   - `DispatchIntent`
2. UI 展示、命中校验、Verb 获取，全部通过当前 `CurrentCombatProjection` + `VerbHostManager` 的纯读能力完成
3. targeting source 可以保留一个**同版本视图缓存**
   - 但缓存键只能是 `ProjectionVersion`
   - 不再使用 tick/stateKey 组合缓存

说明：

- 这不是新真值 owner
- 只是“同一已发布版本下的只读视图缓存”
- 一旦版本变化，targeting source 立即失效

---

## 10. formal host 最终模型

`TriggerBodyVerbHostManager` 最终职责限定为：

1. 根据当前 `TriggerCombatProjectionState` 刷新 binding
2. 维护 `resultId -> binding` 索引
3. 维护 `activeVerbsForTick`
4. 在 `TickActiveVerbs()` 中只推进当前活跃 formal host 会话

最终数据建议：

- 固定顺序 `BdpFormalVerbBinding[] bindingsBySlot`
- `Dictionary<string, BdpFormalVerbBinding> bindingsByResultId`
- `List<Verb> activeVerbsForTick`

最终规则：

- formal host 不参与表达选择
- formal host 不参与结果推导
- formal host 不提供“猜主攻”的附加能力
- formal host 只消费 `TriggerCombatProjectionState.ResultIdToFormalSlot`

---

## 11. Combo / 芯片解释最终模型

建立统一运行时仓库：

- `ExpressionRuntimeRepository`
- `ComboRuntimeIndex`
- `ChipDefinitionCache`
- `ExpressionContractCache`

规则：

- `ComboRuntimeIndex` 在 Def 装载后建立
- `ChipDefinitionCache` 以 `ThingDef` 为键缓存
- `ExpressionContractCache` 以“芯片定义 + 模式键”为键缓存
- 正式投影构建阶段只消费仓库，不再每次 new 解释器链

这样 P4 会和 P1 一起被收掉，而不是作为独立补丁存在。

---

## 12. 明确拒绝的设计选择

以下做法在 v2 中明确拒绝：

- 在 `ExpressionService` 外面再包一层 `pawn + version` 局部缓存
- 让 UI 层继续拥有独立 tick 缓存真值
- 让 targeting source 继续维护自己的状态键解析链
- 保留 `AttackExecutionResolvedRequest` 但只把 resolver 藏起来
- 保留 `AttackExecutionPlanRuntimeStore` 作为“以后也许会用”的静态寄存
- 继续用公共 `LoadoutReaderSurface` 回头给 owner 自己构建正式投影
- 在版本失效时自动重试并静默沿用当前目标

---

## 13. 模块落点建议

新增文件建议：

- `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- `Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionState.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerPresentationState.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionBuilder.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerProjectionBuildInput.cs`
- `Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs`
- `Source/BDP/Core/Expressions/Runtime/ComboRuntimeIndex.cs`
- `Source/BDP/Core/Expressions/Runtime/ExpressionContractCache.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionPreparedContext.cs`

重点重写文件：

- `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- `Source/BDP/Core/Expressions/Access/Contracts/IExpressionReader.cs`
- `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionService.Stages.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- `Source/BDP/Core/AttackExecution/RangedProtocol/RangedAttackProtocolService.cs`
- `Source/BDP/Core/AttackExecution/RangedBurstEmissionAssembler.cs`
- `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- `Source/BDP/Patches/Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs`

直接删除文件：

- `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionPlanRuntimeStore.cs`

---

## 14. 最终验收标准

### 架构标准

- `CompTriggerBody` 不再自己内联承担全部运行时协调，必须通过 `TriggerRuntimeCoordinator`
- 核心战斗投影与 UI 呈现投影完全分离
- 公共读表面全部为纯读
- 构建器不再依赖公共 reader
- `AttackExecution` 不再存在 resolver 现算阶段
- `AttackExecutionResolvedRequest` 与 `AttackExecutionPlanRuntimeStore` 均被删除

### 行为标准

- 自动战斗、手动 targeting、UI、formal host 对同一时刻只看到同一份已发布投影
- 版本失效时不会继续沿用旧结果执行
- dual / combo 编排仍能从投影中 O(1) 取到来源引用与来源结果
- 主装备切换后只存在一份活跃 Trigger 投影
- formal host 只 tick 活跃会话

### 性能标准

- 自动战斗入口不再直接构建 snapshot
- targeting source 不再调用 `TryGetSelectedResult()`
- `TryPreparePlan()` / `TryPrepareCast()` 不再触发表达现算
- formal host tick 不再遍历全 binding
- combo / 芯片契约不再每次构建都重新走定义解释链

---

## 15. 最终决策

这份 v2 是定稿方向，不再继续用“局部缓存补丁”方式迭代。  
最终采用的目标态是：

- `CompTriggerBody` 持有真值
- `TriggerRuntimeCoordinator` 负责运行时协调
- `TriggerCombatProjectionState` 负责核心战斗投影
- `TriggerPresentationState` 负责 UI 呈现投影
- `AttackExecutionRequest` 只描述“版本 N 下选择了哪条结果”
- `AttackExecutionPreparedContext` 作为内部编排上下文
- 版本失效统一结束当前 BDP 会话并交还上层重选
- formal host 只承接、只消费、只 tick 活跃会话

如果按这版执行，v1 的主线方向会保留，但此前的关键遗漏、职责混杂和若干潜在脏边界会一并收干净。
