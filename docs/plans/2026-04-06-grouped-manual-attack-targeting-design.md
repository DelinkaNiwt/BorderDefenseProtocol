# Grouped Manual Attack Targeting 设计

> 本设计用于修正多选 Pawn 时 BDP 手动攻击入口“按钮已聚合、但实际只给一个 Pawn 下单”的问题。目标是在不改动 `AttackExecution` 正式执行边界的前提下，为手动入口补齐正确的组级 targeting 语义。

## 1. 目标

- 多选多个拥有同一攻击入口的 Pawn 时，下方只显示一个聚合按钮。
- 点击该按钮后，只开启一次 targeting。
- 选定目标后：
  - 能命中的成员都会下攻击单；
  - 不能命中的成员跳过；
  - 如果一个都不能命中，则不允许确认。

## 2. 非目标

- 不改 `AttackExecutionRequest`、`AttackSessionToken`、`AttackExecutionService` 的正式执行契约。
- 不改原版 `Targeter`、`Command_VerbTarget` 或 Harmony patch 原版多选攻击逻辑。
- 不引入新的全局会话缓存或复杂的多选状态服务。

## 3. 聚合规则

- 聚合单位定义为“同攻击入口”。
- 在当前 BDP 体量下，直接复用 `ManualEntryProjectionGroup.GroupId` 作为入口语义键。
- `Command_BdpManualEntryTarget` 不再依赖 `Label/icon` 的偶然相等来合并显示，而是显式写入 `groupKey`，让原版 gizmo 分组按入口键工作。

这意味着：
- 同一个芯片/同一个手动入口组，会稳定聚合；
- 只是文案恰好相同、但不是同一入口的按钮，不会误聚合。

## 4. 方案选择

### 方案 A：继续让每个按钮各自 `BeginTargeting`

- 优点：改动小。
- 缺点：本质问题不变，仍然会被全局单例 `Targeter` 覆盖。

### 方案 B：为聚合按钮补一个组级 targeting source（推荐）

- 优点：语义正确，保留 BDP 自己的 `resultId + projectionVersion + sessionToken` 正式执行模型。
- 缺点：需要增加一个很小的组级适配类。

最终采用 **方案 B**。

## 5. 核心设计

### 5.1 继续复用原版 gizmo 分组外壳

- 每个 Pawn 仍然各自产出自己的 `Command_BdpManualEntryTarget`。
- 原版 `GizmoGridDrawer` 仍负责把同组按钮合并成一个显示项。
- 但 BDP 命令不再在 `ProcessInput()` 中直接启动 targeting，而是把真正行为收口到 `ProcessGroupInput()`。

这样可以直接利用原版分组调用链，而不需要自己重写底部 gizmo 网格。

### 5.2 单击行为改为“组处理优先”

- `ProcessInput()` 只保留最小基础行为，不再 `BeginTargeting(...)`。
- `ProcessGroupInput(Event ev, List<Gizmo> group)` 成为唯一入口：
  - 若组内只有 1 个命令，启动单体 `AttackExecutionTargetingSource`；
  - 若组内有多个命令，收集组内所有 `AttackExecutionTargetingSource`，构造一个新的 `GroupedAttackExecutionTargetingSource`，只启动一次 targeting。

这样既兼容单选，也正确支持多选。

### 5.3 新增轻量组级 targeting source

新增一个很小的 `GroupedAttackExecutionTargetingSource : ITargetingSource`，内部只持有一组底层 `AttackExecutionTargetingSource`。

它不重建攻击计划，不持久化状态，只做 targeting 适配：

- `targetParams`：取代表成员的参数。
- `UIIcon` / `GetVerb` / `IsMeleeAttack`：取第一个有效成员作为展示代表。
- `CanHitTarget(target)`：只要任一成员可命中即返回 `true`。
- `ValidateTarget(target, showMessages)`：只要任一成员校验通过即返回 `true`；若全部失败，可回退到代表成员输出拒绝原因。
- `OrderForceTarget(target)`：遍历成员，对通过校验的成员逐个调用 `OrderForceTarget(target)`。

这保持了“targeting 是组级的，执行仍是成员级的”。

## 6. 数据与边界

- 不新增新的正式领域对象。
- 不把“组攻击”灌进 `AttackExecutionService` 内部。
- 组级逻辑只停留在 `Expressions/Projection` 到 `AttackExecutionTargetingSource` 这一层。

边界保持为：
- `Projection` 决定有哪些入口；
- `Command` 决定按钮如何聚合、点击后走单体还是组级 targeting；
- `AttackExecutionTargetingSource` 仍只负责单 Pawn 正式下单；
- `GroupedAttackExecutionTargetingSource` 只是一个薄适配器。

## 7. 最小改动点

- `Source/BDP/Core/Expressions/Projection/DefaultManualEntryGizmoResolver.cs`
  - 生成命令时传入入口键。
- `Source/BDP/Core/Expressions/Projection/Command_BdpManualEntryTarget.cs`
  - 增加入口键；
  - 显式设置 `groupKey`；
  - 把 targeting 启动逻辑迁移到 `ProcessGroupInput()`。
- `Source/BDP/Core/AttackExecution/GroupedAttackExecutionTargetingSource.cs`
  - 新增组级 targeting 适配器。

## 8. 验收标准

- 多选两个拥有相同攻击入口的 Pawn，只显示一个按钮。
- 点该按钮后，不会出现“只剩最后一个 Pawn 的 targetingSource 生效”的现象。
- 在两人都能命中的目标上确认后，两人都进入攻击动作。
- 在只有一人能命中的目标上确认后，只有那一人攻击，另一人被安全跳过。
- 在无人能命中的目标上，不能确认。

## 9. 为什么这个方案足够

- 它没有去 patch 原版 `Targeter`。
- 它没有把 BDP 攻击系统改造成新的组会话系统。
- 它只补齐了缺失的“组级 targeting 适配层”。

对当前模组体量来说，这就是最直接、最稳、最符合现有架构边界的修法。
