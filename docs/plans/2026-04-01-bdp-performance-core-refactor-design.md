# 新 BDP 主线性能重构目标态设计

> 日期：2026-04-01
> 范围：P1 / P2 / P3 / P4 主线性能问题
> 原则：一步到位的目标态改造，不保留兼容层，不设计渐进迁移态，不允许旧新双轨长期并存

---

## 1. 一页结论

这次重构要解决的核心，不是某个局部 `if` 或某个小循环，而是**整条战斗判断链的职责分布错了**：

- 表达快照现在是“读的时候临时现算”
- 攻击执行现在是“入口算一次，执行再算一次”
- formal host 现在是“常驻全量 tick”
- Trigger 读口现在不是纯读，而是在读的时候偷偷推进状态

目标态必须改成下面这一套：

1. `CompTriggerBody` 成为唯一的战斗投影真值 owner
2. 表达快照、主攻击选择、result 索引、formal host 绑定都只在**显式刷新点**重建
3. 自动战斗、手动瞄准、UI、攻击执行全部只读当前已发布的投影状态
4. attack execution 内部不再接受“只给一个 `ResultId` 然后自己回头重解”的调用方式
5. formal host 只推进活跃会话，不再全量常驻 tick
6. Trigger 读口彻底纯化，禁止读时做状态准备

这不是“在现有实现上补几层缓存”，而是把当前链条改成**明确 owner、明确刷新、明确只读消费**的结构。

---

## 2. 本次设计覆盖范围

本设计直接覆盖以下问题：

- `P1` 自动战斗热路径重复重建表达快照
- `P2` 同一次攻击请求里快照可能被重复解析
- `P3` 每把 BDP 武器都有固定 formal host tick 税
- `P4` 组合技匹配与表达解释缺少运行期索引/缓存

以下问题不单独立项，而是作为本设计的自然结果被一并收掉：

- `P5` UI / targeting 重复走表达构建
- `P6` Trigger 读取口不是纯读

原因很简单：

- 只要表达正式投影变成 owner-owned cache，UI / targeting 就天然不再需要重建快照
- 只要状态推进统一移到显式刷新点，读口就天然可以变成纯读

---

## 3. 不接受的方案

### 方案 A：继续在热点处打补丁

做法：

- 给自动战斗入口加局部缓存
- 给攻击执行加单请求缓存
- 给 UI 再加一层 tick 缓存
- 给 formal host 再做若干短路

不接受原因：

- 这种做法只会把“重复重算”从大块拆成小块
- 每条链都会长出自己的局部缓存和失效规则
- 最后会形成自动战斗、手动瞄准、UI、formal host 各自维护缓存的烂摊子

结论：

- 可以作为临时止血
- 不符合这次“完整、干净、彻底”的目标

### 方案 B：保留当前读时构建，只在 ExpressionService 外面包一层缓存

做法：

- 保留 `BuildSelectedSnapshot()` 的现有职责
- 在外层按 `pawn + version` 缓一层结果

不接受原因：

- 真正的 owner 还是不清楚
- `BuildSelectedSnapshot()` 仍然会依赖 Trigger 读口的隐式准备动作
- 读时构建和写时刷新会混在一起，边界仍然脏

结论：

- 比方案 A 稍微整齐
- 但仍然是“把脏逻辑包起来”，不是改对

### 方案 C：直接重构为“Trigger 持有正式战斗投影状态”的目标态

做法：

- `CompTriggerBody` 直接持有当前正式战斗投影
- 状态变化时显式重建
- 所有下游只读这份已发布投影

推荐原因：

- 真值 owner 清楚
- 刷新点清楚
- 读写边界清楚
- 自动战斗、formal host、UI、targeting 可以共享同一份结果

结论：

- **本设计采用方案 C**

---

## 4. 目标态总原则

### 4.1 单一 owner

当前时刻“BDP 认为这把武器能怎么打”的正式结果，只允许有一个 owner：

- `CompTriggerBody`

不再允许以下系统各自暗中推导一份“我理解的当前战斗结果”：

- 自动战斗入口
- 攻击执行入口
- UI 投影口
- targeting source
- formal host manager

### 4.2 发布式读取

所有外部系统只允许读取**已经发布**的正式投影，不允许触发重算。

换句话说：

- 读取是读取
- 刷新是刷新
- 状态推进是状态推进

三者必须分开。

### 4.3 全量替换，不做兼容

本次重构不是“先保留旧 API，再慢慢替换”。

直接采用以下策略：

- 删除旧的按需构建式读取路径
- 删除旧的请求内二次解析路径
- 删除旧的读时状态准备路径
- 删除旧的 formal host 全量 tick 语义

如果某条链还依赖旧语义，就说明它没有完成重构，而不是说明系统需要兼容。

---

## 5. 目标态架构

## 5.1 核心新对象：`TriggerCombatProjectionState`

新增一个由 `CompTriggerBody` 持有、只整包替换、不做发布后原地改写的运行时对象：

`TriggerCombatProjectionState`

建议字段：

- `int ProjectionVersion`
- `ExpressionSnapshot Snapshot`
- `IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex`
- `FormalExpressionResult PrimaryRanged`
- `FormalExpressionResult PrimaryMelee`
- `ManualEntryProjection ManualProjection`
- `VisualExpressionProjection VisualProjection`
- `IReadOnlyDictionary<string, BdpFormalVerbHostSlot> ResultIdToFormalSlot`
- `bool IsEmpty`

设计要求：

- 发布后视为只读对象
- 一次刷新失败，不允许发布半成品
- 永远用“整包替换”代替“局部热修”

这里的关键不是“有没有缓存”，而是：

- 这份对象就是当前正式战斗投影本身
- 下游只读它，不再自己重新拼

## 5.2 `CompTriggerBody` 成为运行时协调者

`CompTriggerBody` 新增统一职责：

1. 维护 Trigger 真值
2. 检测运行时状态变化
3. 在显式刷新点重建 `TriggerCombatProjectionState`
4. 把重建结果同步到 projected hosts 和 formal host manager

`CompTriggerBody` 不再允许把“读口内部偷偷准备状态”作为常规运行时策略。

目标态下，`CompTriggerBody` 只保留两类核心入口：

- 真值写入口
- 正式投影刷新入口

## 5.3 `ExpressionService` 改为纯读 + 纯构建器集合

目标态下，`ExpressionService` 不再负责“收到一个 pawn 就临时帮你构建当前快照”。

它只承担两类职责：

1. 提供共享的表达构建依赖
2. 读取 `CompTriggerBody` 已发布的战斗投影

直接调整为两层语义：

- `ExpressionRuntimeBuilder`
  - 只服务刷新阶段
  - 输入：`CompTriggerBody` 当前正式 Trigger 真值
  - 输出：`TriggerCombatProjectionState`
- `ExpressionReadService`
  - 只服务读阶段
  - 输入：`Pawn`
  - 输出：当前已发布的 `TriggerCombatProjectionState`

也就是说，当前这种：

- `BuildSelectedSnapshot(pawn)`
- `TryGetSelectedResult(pawn, resultId, ...)`

内部临时重建快照的模式，目标态下应直接废弃。

## 5.4 `AttackExecution` 只吃已解析请求

目标态下，攻击执行系统内部不再接受“最小请求 + 自己回头解析”的模式。

当前这套模式的问题是：

- 入口先拿到 snapshot/result
- `AttackExecutionService` 又按 `ResultId` 重新解析一次

这是不允许保留的。

目标态下改成：

- `AttackExecutionRequest` 直接携带已命中的 `FormalExpressionResult`
- 同时携带命中时刻的 `ProjectionVersion`
- 必要时携带命中的 `TriggerCombatProjectionState`

建议字段形态：

- `Pawn Pawn`
- `LocalTargetInfo Target`
- `FormalExpressionResult Result`
- `TriggerCombatProjectionState ProjectionState`
- `int ProjectionVersion`
- `AttackExecutionReason Reason`
- `AttackDispatchIntent DispatchIntent`
- `string AttackInstanceId`

目标态下：

- `AttackExecutionResolvedRequest` 可以删除
- `AttackExecutionService` 不再拥有“按 `ResultId` 重新找结果”的 resolver 阶段

执行前只做一件事：

- 校验当前 `CompTriggerBody.CurrentProjectionVersion == request.ProjectionVersion`

如果版本不一致：

- 当前请求直接失效
- 调用方必须重新准备

这才是干净的边界。

## 5.5 `TriggerBodyVerbHostManager` 改为纯消费投影状态

formal host manager 的目标态职责只有三件事：

1. 按 projection state 刷新 binding
2. 按 projection state 维护 `resultId -> binding` 索引
3. 只 tick 当前活跃 formal host 会话

它不再承担：

- 懒初始化兜底
- 读路径补绑
- 查询时临时修正
- 用“全量遍历 + 空值判断”维持日常运行

目标态数据：

- 固定 slot binding 数组或固定顺序表
- `Dictionary<string, BdpFormalVerbBinding> bindingsByResultId`
- `List<Verb> activeVerbsForTick`

刷新时一次性完成：

1. 重置固定槽位状态
2. 由 `ProjectionState` 映射出当前绑定
3. 同步 formal host 壳
4. 建立 `bindingsByResultId`
5. 建立 `activeVerbsForTick`

运行时 tick 只做：

- 遍历 `activeVerbsForTick`

## 5.6 Combo / 芯片解释改为共享运行时仓库

当前组合技和芯片解释的问题，不只是线性扫描，还包括：

- 每次构建都重新走定义读取和解释链
- 构建器依赖按调用临时创建

目标态下增加共享运行时仓库：

- `ComboRuntimeIndex`
- `ChipDefinitionCache`
- `ExpressionContractCache`

明确规则：

- Def 级数据在模组启动后建立一次
- 模式级表达契约解释结果在首次命中后缓存
- 表达快照构建阶段只消费仓库，不再自己构造读取器链

---

## 6. 真值、刷新、读取三层分工

### 6.1 真值层

真值层只回答：

- 哪些槽位装了什么
- 哪些槽位当前激活
- 哪些切换在进行中
- 哪些槽位被禁用

真值层属于 `CompTriggerBody` 当前已有的 slot / switch / disable 状态。

### 6.2 投影层

投影层只回答：

- 当前表达总表是什么
- 当前远程/近战主攻击是谁
- 当前 formal host 该怎么绑定
- 当前 UI 应该展示什么

投影层就是 `TriggerCombatProjectionState`。

### 6.3 消费层

消费层包括：

- 自动战斗入口
- 手动瞄准入口
- UI gizmo / projection
- 攻击执行服务
- formal host tick

消费层一律不得修改真值，也不得触发重建。

---

## 7. 刷新与失效矩阵

目标态下，以下事件会导致 `TriggerCombatProjectionState` 失效并重建：

| 事件 | 是否立即重建 | 说明 |
| --- | --- | --- |
| 装入芯片 | 是 | 真值已变化 |
| 卸下芯片 | 是 | 真值已变化 |
| 激活正式提交 | 是 | 主攻击、结果表、formal host 都可能变 |
| 停用正式提交 | 是 | 同上 |
| 禁用状态变化 | 是 | 当前激活、主攻击、可用结果都可能变 |
| 读档恢复完成 | 是 | 必须发布一份完整正式投影 |
| 装备解除 | 是 | 清空 projected hosts 与 formal hosts |
| 运行时到期切换被结算 | 是 | 属于正式真值变化 |

目标态下，以下事件**不允许**触发重建：

- UI 读取
- targeting 读取
- auto attack 读取
- attack execution 查询
- formal host 查询

换句话说：

- 读路径不改状态
- 读路径不刷投影
- 读路径不补齐宿主

---

## 8. 新的运行时流程

## 8.1 Trigger 运行时 Tick

新增一个统一的 Trigger 运行时推进入口，语义上由 `CompTriggerBody` 持有，例如：

- `RuntimeTick()`

由装备 tick 桥直接调用当前主装备上的 `CompTriggerBody.RuntimeTick()`。

这条入口负责：

1. 处理 post-load 待完成刷新
2. 同步 owner pawn 禁用状态
3. 结算到期切换
4. 如果真值变化，则重建 `TriggerCombatProjectionState`
5. tick `activeVerbsForTick`

重要约束：

- 运行时 tick 是**状态推进入口**
- 不是“给读口擦屁股”的入口

## 8.2 自动战斗入口

自动战斗目标态流程：

1. 读取 `CompTriggerBody.CurrentProjectionState`
2. 取 `PrimaryRanged` 或 `PrimaryMelee`
3. 通过 `bindingsByResultId` 直接拿 formal host binding
4. 构造已解析 `AttackExecutionRequest`
5. 直接进入执行

自动战斗入口不再做：

- 重建 snapshot
- 再按 `ResultId` 查一遍 snapshot
- 再按 `ResultId` 查一遍 binding 后交给执行器重算

## 8.3 手动瞄准与 UI

UI / targeting 目标态流程：

1. 直接读取 `CurrentProjectionState`
2. 手动条目使用 `ManualProjection`
3. visual / gizmo 读取缓存投影
4. 若玩家点选某条结果，按 `ResultIndex[resultId]` 拿命中结果

UI / targeting 不再保留各自独立的同 tick 缓存策略。

原因：

- 当前正式投影本身已经是缓存
- 局部再包一层只会制造第二套失效规则

## 8.4 攻击执行

攻击执行目标态流程：

1. 接收已解析请求
2. 校验 projection version
3. 基于 request 内的 `Result` 和 `ProjectionState` 直接编排
4. 执行过程中一律使用 request 内的结果图，不回头请求 ExpressionService 现算

任何时候只要发现 version 不一致：

- 当前请求废弃
- 上层重新准备

这样可以彻底消灭“命中时刻和执行时刻看到不同表达结果”的隐患。

---

## 9. 关键接口调整

## 9.1 必须删除或改名的旧入口

以下语义必须消失：

- `ExpressionService.BuildSelectedSnapshot(pawn)` 的按需现算语义
- `ExpressionService.TryGetSelectedResult(pawn, resultId, ...)` 的按需现算语义
- `CompTriggerBody.Reads.PrepareReadState()` 的读时准备语义
- `AttackExecutionService` 内部的 `ResultId -> snapshot/result` 重新解析语义
- `TriggerBodyVerbHostManager.Tick()` 的全 binding 常驻 tick 语义

### 9.2 新入口语义

- `CompTriggerBody.GetCurrentProjectionState()`
  - 纯读
- `CompTriggerBody.RebuildProjectionState()`
  - 只在显式刷新点调用
- `CompTriggerBody.RuntimeTick()`
  - 统一运行时推进入口
- `ExpressionReadService.GetCurrentProjection(pawn)`
  - 纯读
- `AttackExecutionService.TryExecute(AttackExecutionRequest request)`
  - 只接受已解析请求

---

## 10. 核心结构性决定

### D1. 一名 pawn 只允许存在一套活跃 Trigger 战斗投影

目标态下明确规定：

- 当前活跃的 `CompTriggerBody` 只来自 `pawn.equipment.Primary`

这条规则必须写死。

理由：

- `TriggerSurfaceAccess` 当前本来就按主装备定位 Trigger
- `EquipmentTrackerTick` 却在扫描全部装备，这本身就是语义不统一

目标态下应统一为：

- 所有正式读取、刷新、tick 推进都围绕主装备 Trigger 展开

这样 `EquipmentTrackerTick` 不需要再全装备扫描。

### D2. `ExpressionSnapshot` 发布后视为只读

当前 `ExpressionSnapshot` 和 `FormalExpressionResult` 是可写对象。  
目标态下可以保留对象形态，但必须执行下面的发布规则：

- 构建阶段允许写
- 发布后禁止原地改写
- 一旦状态变化，直接生成下一份完整对象

这能避免：

- UI、targeting、attack execution 读到同一对象时被中途改写

### D3. Disable / Switch 变化必须进入正式刷新链

当前禁用状态变化会广播事件，但没有保证一定先刷新正式投影。  
目标态下必须改成：

- disable state 变化 = 正式真值变化
- 必须进入统一重建链

否则会出现：

- slot 禁用了，但主攻击和 formal host 还在看旧结果

### D4. formal host 只承接，不推导

formal host 的定位必须锁死：

- 它只承接已发布结果
- 它不参与表达选择
- 它不参与真值推导
- 它不提供“猜测当前主攻”的服务

这条边界不允许再松。

---

## 11. 模块落点建议

建议目标模块如下：

- `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionState.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionBuilder.cs`
- `Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionIndex.cs`
- `Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs`
- `Source/BDP/Core/Expressions/Runtime/ComboRuntimeIndex.cs`
- `Source/BDP/Core/Expressions/Runtime/ExpressionContractCache.cs`

重点重写模块：

- `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionRequest.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`
- `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- `Source/BDP/Patches/Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs`

其中：

- `AttackExecutionResolvedRequest.cs` 在目标态下应考虑删除
- `Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs` 应改成只驱动主装备 Trigger

---

## 12. 风险判断

这套设计的主要风险不在“能不能提速”，而在“能不能保证时序一致”。

最高风险点：

- post-load 恢复
- 持续 burst 会话续接
- disable 变化引发的即时重建
- 攻击请求版本失效后的重新准备

但这些风险都属于**可定义、可验证**的边界风险，不是无从收口的系统性风险。

反过来说，如果继续保留旧设计：

- 各处局部缓存
- 读时准备
- 入口重算
- 执行再重算

那问题只会越来越难界定。

所以这次应该接受“短期内重写边界”的成本，换长期结构清晰。

---

## 13. 验收标准

完成后，必须满足以下标准：

### 性能标准

- 自动战斗入口不再直接构建 `ExpressionSnapshot`
- 攻击执行入口不再按 `ResultId` 重新解析 snapshot
- formal host tick 不再遍历全量固定槽位
- 组合技匹配不再线性扫描全部 `ComboDef`

### 架构标准

- `CompTriggerBody` 是唯一正式战斗投影 owner
- Expression 读取口全部为纯读
- Trigger 读取口全部为纯读
- `AttackExecutionService` 只吃已解析请求
- formal host manager 只消费已发布投影

### 行为标准

- 主攻击切换后，auto / manual / UI / formal host 看到的是同一份结果
- disable / activate / deactivate 后，无旧结果残留
- post-load 恢复后，formal host 与当前投影一致
- 版本失效的旧攻击请求不会带着旧结果继续执行

---

## 14. 最终决策

这次主线性能优化，不采用“哪里慢补哪里”的修修补补策略。  
直接采用下面这套最终架构：

- `CompTriggerBody` 持有唯一正式战斗投影
- 状态变化时显式重建
- 所有读链只读已发布投影
- attack execution 只接受已解析请求
- formal host 只推进活跃会话
- combo / 芯片解释进入共享运行时仓库

这是一次**收口边界**的重构，不是一次“多加几个缓存”的优化。

如果后续实现严格按这份设计执行，P1-P6 里的大部分主问题都会一起消失，而不是一条一条靠补丁压住。
