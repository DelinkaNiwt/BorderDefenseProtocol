# 2026-04-05 BDP 不必要成本清理重构总结与测试项概述

## 一、文档目的

- 这份文档用于给本轮“非必要结构成本清理重构”做一次交付总结。
- 重点不是重复实现细节，而是用人话说明：这轮到底改了什么、为什么现在比之前干净、以及后续该怎么验收。
- 文档同时附上本轮已完成的验证项概述，方便后续手测和复审直接照表走。

---

## 二、一句话结论

- 这轮重构没有改变 BDP 的核心玩法目标，也没有把 RimWorld 模组做成企业框架。
- 真正做掉的是几类“本来可以不付出”的成本：跨模块绕路、重复状态、过大的编排中心、运行时弱边界、反射式动态补丁。
- 重构完成后，`Trion / CombatBody / Trigger` 三个真值持有者仍然存在，但它们之间的协作关系已经从“互相渗透、彼此帮忙背责任”，收敛成“各自持有真值，通过明确事务和明确值对象协作”。

---

## 三、执行前后变化（人话版）

### 1. Expression 部分

- 执行前：`ExpressionSnapshotBuilder` 像一个大杂烩，既收集来源，又筛选，又拼单侧结果，又解组合结果，还顺手处理运行时 Verb 细节。
- 执行后：表达快照构建被拆成清晰阶段：
  - `ExpressionSourceCollector` 负责收集来源。
  - `SingleSideExpressionBuilder` 负责单侧构建。
  - `CompositeExpressionResolver` 负责组合结果归并。
  - `ExpressionSnapshotBuilder` 只保留总协调职责。
- 结果：表达系统后续再改时，不会再因为一个“大总管类”牵一发而动全身。

### 2. Trigger 部分

- 执行前：`CompTriggerBody` 除了做 Trigger 真值 owner，本身还扛着 Trion 绑定、拆卸清算、槽位停用、资源回收、发布收口等一串跨边界杂务。
- 执行后：`CompTriggerBody` 仍然是 Trigger 真值 owner，但资源绑定和拆卸清算已经抽到明确运行时事务对象里：
  - `TriggerTrionBindingService`
  - `TriggerDetachTeardownTransaction`
- 结果：现在 Trigger 的“拥有状态”和“执行事务”被分开了，类本身更像 owner，而不是一台全能施工机。

### 3. 远程攻击 Verb 部分

- 执行前：`BdpVerb_Shoot` 同时承担 Verse 射击桥接、轮次状态、发射游标、继续攻击规划等多种职责，读代码时很难一眼分出哪些是主流程，哪些是细节状态。
- 执行后：远程发射时序被拆成三个明确对象：
  - `RangedVerbRoundState`
  - `RangedVerbEmissionCursor`
  - `RangedVerbContinuationPlanner`
- `BdpVerb_Shoot` 自身只保留 Verse 桥接、上下文装配和关键调度入口。
- 结果：远程攻击主链更短，后续查“为什么继续攻击”“为什么 burst 不对”“为什么发射顺序不对”时，定位会更直接。

### 4. 发布与会话失效部分

- 执行前：投影发布、正式宿主刷新、攻击会话失效和读档恢复，分散在多个调用点里，各处都能补一刀，各处也都可能漏一刀。
- 执行后：`TriggerRuntimeCoordinator` 成为唯一 publish 收口点，统一负责：
  - projection version bump
  - projected hosts sync
  - formal host refresh
  - attack session invalidation / post-load recovery
- 结果：现在“发布完之后还要顺手做什么”已经不再靠外围调用点记忆，主链规则回到了一个地方。

### 5. 攻击会话身份部分

- 执行前：攻击会话身份在多个对象里以 `ProjectionVersion` 等碎片方式镜像保存，概念上是同一件事，代码里却分散成多份。
- 执行后：攻击会话统一改成显式 `AttackSessionToken`，不再让 `BdpVerb_Shoot`、`BdpVerb_MeleeAttackDamage` 等对象各自背着一份零散版本号。
- 结果：会话有效性判断回到单一身份对象，读档恢复和继续攻击链也更容易统一。

### 6. Trion 边界部分

- 执行前：`Trion` 的 drain 注册还带着字符串键协议，Trigger 语义会渗进 Trion 层，边界不够干净。
- 执行后：
  - 字符串键被替换为 `TrionDrainKey` 值对象。
  - Trigger 侧语义转换外移到 `TriggerDrainKeyFactory`。
  - `Trion` 不再反向知道 Trigger 的具体语义。
- 结果：Trion 回到“资源系统”本位，不再偷偷承担 Trigger 侧语义翻译。

### 7. Verb 运行时解析部分

- 执行前：表达结果到 Verb 运行时行为之间，还存在反射改 `VerbProperties` 私有细节的做法。
- 执行后：改为显式 `ResolvedVerbSpec` 模型，运行时消费 typed 结果，不再靠反射式补丁硬改 Verse 私有字段。
- 结果：行为来源更明确，后续复审时也不会再把这块判成高风险动态魔法。

---

## 四、本轮落地的核心结构调整

### 1. 新增的表达流水线对象

- `Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- `Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs`
- `Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`

### 2. 新增的 Trigger 运行时事务对象

- `Source/BDP/Core/Trigger/Runtime/TriggerTrionBindingService.cs`
- `Source/BDP/Core/Trigger/Runtime/TriggerDetachTeardownTransaction.cs`
- `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeServices.cs`

### 3. 新增的远程 Verb 细分对象

- `Source/BDP/Core/Verbs/RangedVerbRoundState.cs`
- `Source/BDP/Core/Verbs/RangedVerbEmissionCursor.cs`
- `Source/BDP/Core/Verbs/RangedVerbContinuationPlanner.cs`

### 4. 新增的关键值对象与事务对象

- `Source/BDP/Core/AttackExecution/AttackSessionToken.cs`
- `Source/BDP/Core/Trion/TrionDrainKey.cs`
- `Source/BDP/Core/CombatBodySession/CombatBodyActivationTransaction.cs`
- `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`
- `Source/BDP/Core/CombatBodySession/CombatBodySessionTrionBinding.cs`
- `Source/BDP/Core/Expressions/Model/ResolvedVerbSpec.cs`

### 5. 被明显瘦身的旧中心类

- `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs`
- `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- `Source/BDP/Core/CombatBodySession/CombatBodySessionService.cs`

---

## 五、这轮重构直接消掉了哪些不必要成本

- 不再需要靠 runtime singleton / service locator 到处偷拿服务。
- 不再需要靠字符串 key 和隐式命名约定串联资源 drain。
- 不再需要在多个对象里镜像保存同一份攻击会话身份。
- 不再需要把 publish 之后的附带动作散落在多个调用点里。
- 不再需要在一个 owner comp 里同时塞满“真值持有 + 跨系统事务 + 收尾编排”。
- 不再需要靠反射去改 Verse 私有运行时细节。

---

## 六、测试项概述（本轮已覆盖）

### 1. 架构边界烟雾测试

- `Source/BDP.Tests/RuntimeServiceLocatorBoundarySmokeTests.ps1`
- `Source/BDP.Tests/TrionDrainKeyBoundarySmokeTests.ps1`
- `Source/BDP.Tests/AttackSessionTokenBoundarySmokeTests.ps1`
- `Source/BDP.Tests/ExpressionResolvedVerbSpecBoundarySmokeTests.ps1`
- `Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1`

这组测试主要锁死本轮重构的核心边界，确保下面这些问题不会悄悄回流：

- runtime service locator 重新长回来
- `Trion` 再次耦合 `Trigger`
- attack session 身份重新碎片化
- 表达系统重新回到反射补丁
- `CombatBodySessionService` 再次膨胀成隐藏总控中心

### 2. Trigger / CombatBody / Trion 集成回归

- `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`
- `Source/BDP.Tests/TriggerDetachTeardownSmokeTests.ps1`
- `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- `Source/BDP.Tests/TriggerSwitchTimingSmokeTests.ps1`

这组测试主要覆盖：

- Trigger 与 CombatBody 的契约边界
- 坍塌、紧急退出、切换时序
- Trigger 装配 / 拆卸后的 Trion 资源结算
- Trigger 单一真值是否仍成立

### 3. Expression / Publish / FormalHost 回归

- `Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- `Source/BDP.Tests/ExpressionRuntimeRepositorySmokeTests.ps1`
- `Source/BDP.Tests/FormalHostActiveTickSmokeTests.ps1`
- `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- `Source/BDP.Tests/PrimaryTriggerRuntimeOwnershipSmokeTests.ps1`

这组测试主要覆盖：

- 表达投影发布链
- FormalHost 刷新与 active tick
- 读档后的攻击会话恢复
- Trigger 运行时 owner 归属是否清晰

### 4. 远程攻击与继续攻击回归

- `Source/BDP.Tests/RangedAttackTrionConsumptionSmokeTests.ps1`
- `Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- `Source/BDP.Tests/AttackExecutionProjectionVersionSmokeTests.ps1`
- `Source/BDP.Tests/DefaultBurstParitySmokeTests.ps1`

这组测试主要覆盖：

- 远程攻击的 Trion 消耗
- 远程协议边界
- 攻击投影版本与继续攻击链
- 默认 burst 语义是否保持一致

### 5. 编译验证

- `dotnet msbuild 'Source/BDP/BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal`

结果：主工程编译通过，说明这轮清理不仅是“测试脚本过了”，而是模组主程序集本身也成功重新构建。

---

## 七、建议你优先手测的地方

如果你要用最少时间判断这轮重构有没有把模组弄坏，建议优先测下面几类：

### 1. Trigger 装卸与切换

- 让主 Trigger / 副 Trigger / 特殊 Trigger 分别装上、切换、拆下、销毁。
- 观察：
  - 是否能正常生效和失效
  - 是否出现残留效果
  - 是否出现 Trion drain 没有回收或重复注册

### 2. 存档 / 读档 / 读档后继续战斗

- 战斗中存档，再立刻读档。
- 观察：
  - FormalHost 是否正常恢复
  - 自动攻击 / 继续攻击是否恢复到正确状态
  - 是否出现会话失效后仍继续打一半、或者读档后直接断链

### 3. 远程 burst 与继续攻击

- 测单发、连发、强制目标、自动攻击四种入口。
- 观察：
  - burst 发射数是否正确
  - 发射节奏是否正确
  - 继续攻击是否仍能延续
  - Trion 是否按预期消耗

### 4. Trigger 拆卸时的清算

- 在战斗中途拆卸或移除 Trigger。
- 观察：
  - 资源绑定是否解除
  - 已发布 projection 是否清掉
  - 正在进行的攻击会话是否被正确中断

### 5. 表达变更后的正式宿主刷新

- 切换影响表达的芯片、组合技、宿主条件。
- 观察：
  - FormalHost verb 是否跟着刷新
  - gizmo / 手动入口是否仍正确
  - 不应出现“旧投影还挂着、新投影没接上”的状态

### 6. CombatBody 进出战斗状态

- 触发进入战斗、退出战斗、异常中断、坍塌恢复等流程。
- 观察：
  - Trigger 激活与停用是否跟随正确
  - CombatBodySession 是否正确收口
  - 不应出现退出后仍保留运行时绑定

---

## 八、当前复审结论

- 从这轮代码落地结果看，原先被指出的几类“不必要成本”已经不是当前代码的有效问题。
- 更准确地说，现在的 BDP 仍然是一个有体量、有规则链的 RimWorld 模组，但它已经不再靠“多点镜像状态 + 大中心类 + 弱边界约定”硬撑。
- 若后续继续开发，新代码应沿用本轮确立的原则：真值留在 owner，事务抽成明确对象，跨系统协作优先用明确值对象和明确边界，不要再回到隐式串联和临时补丁式扩展。

