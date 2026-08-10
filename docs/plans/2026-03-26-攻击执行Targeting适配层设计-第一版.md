# 攻击执行 Targeting 适配层设计（第一版）

## 目标

这份设计文档用于把 BDP（Border Defense Protocol，边境防卫协议）当前“手动攻击只打一发就停”和“手动选目标图标反馈不对”这两个现象，
统一收口到同一个正式架构缺口上，
并给出一套不退回裸 `Verb` 执行边界的正式设计方案。

本文只做设计，不直接展开实施任务。

## 一、问题结论

### 1. 现象不是两个独立问题

当前表现：

- 远程、爆炸、近战 3 类芯片，手动攻击都只执行 1 次
- 手动选目标时，无论当前指向是否合法，鼠标附件都偏向通用红色十字准心

这两个现象的根因其实是同一个：

- BDP formal result（正式表达结果）还没有被正式适配成原版 `ITargetingSource`（目标选择源接口）
- `AttackExecution`（攻击执行边界）也还没有明确区分“立即施放一次”和“玩家下达正式强制攻击命令”

### 2. 当前错误链路

当前链路可以简化成：

```text
[手动按钮]
    -> BeginTargeting(TargetingParameters, callback 回调)
    -> 玩家点目标
    -> AttackExecution.TryExecute(...)
    -> verb.TryStartCastOn(...)
    -> 只执行 1 次
```

这里的问题有两个：

- UI 走的是普通 `callback`（回调式）目标选择路径，不是 `targetingSource`（目标选择源）路径
- 执行层拿到请求后，单发动作会自然落到“立即打一手”，而不是“正式持续攻击命令”

### 3. 正确链路应该是什么

```text
[手动按钮]
    -> BeginTargeting(ITargetingSource 目标选择源)
    -> 原版 Targeter（目标选择器）负责：
       - 合法目标：攻击图标
       - 非法目标：禁止图标
       - 点下目标：回调 OrderForceTarget(...)
    -> OrderForceTarget(...)
    -> AttackExecution.TryExecute(...)
    -> AttackExecution 下正式攻击命令
    -> Pawn 接正式 Job
    -> 持续攻击 / 正常追击 / 正常推进
```

## 二、设计总原则

### 1. 不退回裸 Verb 正式边界

这次设计的首要约束是：

- 可以借用真实 `Verb`（原版攻击动作对象）的原版 targeting（目标选择）能力
- 但不能把裸 `Verb` 重新抬回成正式执行边界

换句话说：

```text
UI/校验行为可以借 verb
正式下单必须回 AttackExecution
```

### 2. 适配层做“翻译”，不做“真值”

适配层负责的是：

- 把 formal result（正式表达结果）翻译成原版 `ITargetingSource`（目标选择源接口）能理解的对象

适配层不负责的是：

- 决定当前应该用哪个表达
- 决定攻击计划
- 决定近战/远程运行时策略
- 直接执行一次施放

### 3. 设计成通用件，不做手动入口特供件

第一批使用方虽然是手动入口，
但这层不应该被设计成“只服务手动按钮”的窄对象。

正式定位应是：

```text
任何需要把 BDP formal result（正式表达结果）接进原版 Targeter（目标选择器）/ targetingSource（目标选择源）体系的入口
都应复用这一层
```

## 三、核心对象设计

### 1. 新增通用适配层：`AttackExecutionTargetingSource`（攻击执行目标选择适配源）

建议新增一个正式对象：

- `AttackExecutionTargetingSource`（攻击执行目标选择适配源）

它实现：

- `ITargetingSource`（目标选择源接口）

它的职责是：

- 以 `Pawn + ResultId` 为稳定身份
- 把当前 formal result（正式表达结果）适配成原版 targeting source（目标选择源）
- 为 Targeter（目标选择器）提供 UI、校验、目标高亮和最终下单接口

### 2. 整体结构图

```text
+-----------------------------+
| BDP formal world            |
| （BDP 正式表达世界）        |
| - Pawn                      |
| - ResultId                  |
| - ExpressionSnapshot        |
| - FormalExpressionResult    |
+--------------+--------------+
               |
               | 适配
               v
+-----------------------------+
| AttackExecutionTargetingSource |
| （攻击执行目标选择适配源）   |
| - targetParams              |
| - ValidateTarget            |
| - DrawHighlight             |
| - OnGUI                     |
| - OrderForceTarget          |
+--------------+--------------+
               |
               | 正式接单
               v
+-----------------------------+
| AttackExecution             |
| （攻击执行边界）            |
| - Resolve                   |
| - Plan                      |
| - Dispatch                  |
+--------------+--------------+
               |
               v
+-----------------------------+
| Pawn Job / JobDriver        |
+-----------------------------+
```

### 3. 与手动入口的关系

手动入口不是这层的拥有者，只是这层的第一批调用方。

关系应是：

```text
[Command_BdpManualEntryTarget
 （BDP 手动入口目标选择命令）]
    -> BeginTargeting(new AttackExecutionTargetingSource(...))
```

而不是：

```text
[Manual 专属 source]
    -> 以后别的入口再复制一层类似对象
```

## 四、稳定输入与短生命周期缓存

### 1. 稳定输入

`AttackExecutionTargetingSource`（攻击执行目标选择适配源）建议只长期持有这些稳定输入：

- `Pawn`
- `ResultId`
- `IAttackExecutionEntry`（攻击执行正式入口）
- `IExpressionVerbHostResolver`（表达式 Verb 宿主解析器）或等价 formal-result-to-verb（正式结果到 Verb）解析器
- `AttackDispatchIntent`（攻击派单意图）

其中最关键的稳定身份是：

```text
Pawn + ResultId + DispatchIntent
```

### 2. 不缓存 formal 真值为长期状态

适配层不应该在创建时就把：

- `FormalExpressionResult`
- `Verb`
- `ExpressionSnapshot`

永久缓存下来并假定它们始终正确。

原因：

- targeting（目标选择）过程中 Pawn 状态可能变化
- formal result（正式表达结果）可用性可能变化
- 当前可用 `Verb` 宿主也可能变化

### 3. 采用会话型轻量缓存

为了避免 UI 高频查询时每次都重走全链路，
适配层内部采用“会话型短缓存（同一轮目标选择会话内的轻量缓存）”。

其原则是：

```text
同一 targeting 会话中尽量复用
只有明显状态变化才失效重建
```

建议内部结构：

```text
ResolvedTargetingContext（已解析的目标选择上下文）
- FormalExpressionResult Result
- Verb Verb
- bool IsValid

AttackExecutionTargetingSource
- Pawn
- ResultId
- DispatchIntent
- cachedContext
- cacheStateKey
- cacheTick
```

### 4. 缓存失效条件

第一版只做必要失效，不做过度复杂优化。

建议失效条件包括：

- `Pawn` 已空、已死、已 despawn
- 当前 snapshot 中找不到 `ResultId`
- `Result.IsAvailable == false`
- 无法重新解析出当前 `Verb`
- 宿主关键状态变化导致 `cacheStateKey` 不一致

第一版 `cacheStateKey` 可只覆盖明显关键项：

- `pawn.Spawned`
- `pawn.Dead`
- `pawn.Drafted`
- 当前主装备引用

这意味着：

```text
正常鼠标移动 -> 读缓存
状态真的变化 -> 重新解析
```

## 五、`ITargetingSource` 成员设计

### 1. 统一入口：`GetOrRefreshResolvedContext()`

所有 UI 和校验接口都不应各自重新解析一遍。

建议由 `AttackExecutionTargetingSource`（攻击执行目标选择适配源）内部先统一提供：

- `GetOrRefreshResolvedContext()`（获取或刷新已解析上下文）

然后所有 `ITargetingSource` 成员都基于这一个结果工作。

结构图：

```text
Targeter 高频回调
    |   |   |   |
    v   v   v   v
+-----------------------------+
| GetOrRefreshResolvedContext |
+-----------------------------+
              |
      cache hit? ---- yes ---> 直接复用
              |
              no
              v
      重新解析 result + verb
```

### 2. UI/校验成员实现原则

这些成员都走“借用当前 resolved verb（当前已解析 Verb）”的策略：

- `targetParams`
- `GetVerb`（获取当前原版 Verb）
- `UIIcon`
- `CanHitTarget(...)`
- `ValidateTarget(...)`
- `DrawHighlight(...)`
- `OnGUI(...)`

推荐含义如下：

#### `targetParams`（目标选择参数）

- 来自当前 resolved `Verb.targetParams`

#### `GetVerb`（获取当前原版 Verb）

- 返回当前 resolved `Verb`
- 仅用于原版 Targeter（目标选择器）所需的 targeting（目标选择）语义
- 不代表正式执行边界回到了 `Verb`

#### `UIIcon`（界面图标）

- 优先返回 `Verb.UIIcon`
- 如无正式图标，再按当前既有图标策略兜底

#### `CanHitTarget(...)`（能否命中目标）

- 转发给当前 `Verb`

#### `ValidateTarget(...)`（校验目标是否合法）

- 转发给当前 `Verb.ValidateTarget(...)`
- 保持原版合法性提示语义

#### `DrawHighlight(...)`（绘制目标高亮）

- 转发给当前 `Verb.DrawHighlight(...)`

#### `OnGUI(...)`（绘制鼠标附件图标与界面反馈）

- 转发给当前 `Verb.OnGUI(...)`
- 这样原版“合法目标显示攻击图标，非法目标显示禁止图标”的反馈才能自然回来

### 3. 明确一条红线

上面这些 UI/校验成员虽然借用了 `Verb`，
但它们的职责仍然只是：

- 原版 targeting（目标选择）反馈
- 合法目标判定

它们不负责：

- 正式执行
- 正式下单

## 六、正式下单设计

### 1. `OrderForceTarget(...)`（强制下达目标命令）的唯一职责

`OrderForceTarget(...)` 的职责不是：

- 直接 `verb.TryStartCastOn(target)`

它的唯一职责应该是：

- 把“玩家刚刚指定了这个目标”翻译成一张正式攻击订单

建议行为：

```text
OrderForceTarget(target)
    -> AttackExecution.TryExecute(request)
```

其中请求语义必须明确包含：

- 谁下单：`Pawn`
- 用哪个 formal result：`ResultId`
- 打谁：`Target`
- 来源：`Manual`
- 派单意图：`ForceTargetOrder`

### 2. 为什么不能继续直接打一次

如果 `OrderForceTarget(...)` 内部继续直接执行：

```text
verb.TryStartCastOn(target)
```

就会回到旧问题：

- 只打一发
- 不是正式攻击命令
- UI 和执行边界重新耦合回裸 `Verb`

因此：

```text
OrderForceTarget = 下单
不是 = 当场打一手
```

## 七、AttackExecution 请求模型补强

### 1. 当前缺口

当前 `AttackExecutionRequest` 只有：

- `Pawn`
- `ResultId`
- `Target`
- `Reason`

其中 `Reason` 只说明：

- 这单从哪来

但它不说明：

- 这单应该怎样进入执行系统

### 2. 新增派单意图：`AttackDispatchIntent`（攻击派单意图）

建议新增正式枚举：

- `AttackDispatchIntent`（攻击派单意图）

第一版至少包含：

- `ImmediateCast`（立即施放）
- `ForceTargetOrder`（正式强制攻击下单）

含义：

```text
Reason
= 这单从哪来

DispatchIntent
= 这单怎么进入执行系统
```

这两个维度必须拆开。

### 3. 请求模型更新后含义

```text
AttackExecutionRequest
- Pawn
- ResultId
- Target
- Reason
- DispatchIntent
```

这样“手动入口”不再天然等于“只打一手”。

## 八、执行层分流设计

### 1. 新分流原则

执行层不应继续只靠：

- `ShotCount`
- `BurstShotCountHint`
- `DriveMode`

来猜测这是“立即施放”还是“正式命令”。

应该先看：

- `DispatchIntent`（派单意图）

### 2. 正式分流语义

推荐分流：

```text
if DispatchIntent == ForceTargetOrder
    -> 下正式攻击 Job

if DispatchIntent == ImmediateCast
    -> 走现有 immediate / continuous cast 运行链
```

### 3. `DriveMode` 与 `DispatchIntent` 的关系

两者不是一回事。

```text
DispatchIntent
= 命令是怎么下进系统的

DriveMode
= 进入系统后，运行时怎么推进
```

必须拆开，否则手动强制攻击还会继续被误翻译成单次施放。

## 九、手动入口接线设计

### 1. 新按钮对象

建议新增：

- `Command_BdpManualEntryTarget`（BDP 手动入口目标选择命令）

职责：

- 负责显示按钮
- 在点击时启动 `Find.Targeter.BeginTargeting(source, ...)`（让原版目标选择器接管后续目标选择流程）

它不负责：

- 正式执行
- 攻击计划
- 直接打一发

### 2. `DefaultManualEntryGizmoResolver` 的新角色

它仍然负责：

- 把 formal manual projection（正式手动入口投影）翻译成 gizmo（按钮对象）

但它不再负责：

- 用普通 callback 直接接收目标并触发执行

新的关系应是：

```text
DefaultManualEntryGizmoResolver
    -> 构建 Command_BdpManualEntryTarget
    -> Command 内部持有 AttackExecutionTargetingSource
```

## 十、为什么这是通用层，而不是手动特供补丁

因为这层解决的是一个更底层的问题：

```text
formal result（正式表达结果）
如何进入原版 targetingSource（目标选择源）世界
且不破坏 AttackExecution（攻击执行边界）正式边界
```

这不是手动按钮私有问题，
只是这次首先在手动入口暴露出来。

因此第一批接入者虽然是手动入口，
但对象设计本身必须保持通用。

## 十一、第一版实施边界

第一版落地范围建议只做：

### 1. 新增

- `AttackExecutionTargetingSource`（攻击执行目标选择适配源）
- `Command_BdpManualEntryTarget`（BDP 手动入口目标选择命令）
- `AttackDispatchIntent`（攻击派单意图）

### 2. 修改

- `AttackExecutionRequest`
- `DefaultAttackExecutionEntry`
- `DefaultManualEntryGizmoResolver`
- 必要时补齐 planner / dispatcher 对 `DispatchIntent` 的消费

### 3. 第一版暂不做

- 不扩展到多目标、多段目标选择
- 不提前展开高阶并列攻击编排
- 不为了本问题顺手重做整个 UI 系统

## 十二、设计结论

这次设计的正式结论是：

```text
新增一个通用的 `AttackExecutionTargetingSource`（攻击执行目标选择适配源），
把 BDP formal result（正式表达结果）正式适配为原版 `ITargetingSource`（目标选择源接口）。

UI / 校验行为借用当前 resolved verb（当前已解析 Verb），
但 `OrderForceTarget(...)`（强制下达目标命令）必须回到 `AttackExecution`（攻击执行边界）正式边界，
并通过新增的 `AttackDispatchIntent.ForceTargetOrder`（正式强制攻击下单）
把“玩家正式强制攻击命令”和“立即施放一次”彻底拆开。
```

两个问题会由此自然收口：

- 有 targetingSource，图标反馈自然恢复原版语义
- 有 ForceTargetOrder，手动攻击自然不再只打一发就停

## 十三、文档约束

- 本设计文档是实施计划前的正式拍板版本
- 后续若要做计划外扩展，必须先更新设计或计划
- 实施阶段所有新增代码成员继续逐成员注释
