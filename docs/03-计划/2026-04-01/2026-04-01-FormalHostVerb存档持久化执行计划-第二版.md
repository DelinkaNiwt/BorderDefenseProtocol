---
标题：FormalHostVerb 存档持久化执行计划
版本号: v2.0
更新日期: 2026-04-01
最后修改者: Codex GPT-5
标签: [文档][用户已确认方向][已完成][未锁定]
摘要: 基于第二版设计分析整理的可施工执行计划。重点修正 v1.1 中“只要 deep-save 壳对象并在消费点重建 plan 就能完整续接”的过度乐观判断，新增“首次 post-load 重绑保态”和“burst cursor 持久化”两个正式任务。
---

# FormalHostVerb 存档持久化执行计划

> 参考设计文档：`docs/04-架构评估/2026-04-01/2026-04-01-FormalHostVerb存档持久化设计分析-第二版.md`

## 实施目标

本计划的目标不是只修日志警告，而是把 FormalHostVerb 存档续接拆成三个可验证阶段：

1. 壳对象真正进入存档树
2. 读档后的第一次表达重绑不清空已恢复会话状态
3. burst 中段依靠持久化 cursor 恢复到正确消费位置

只有三个阶段都完成，才能说“像原版一样跨档连续”。

---

## 任务总览

| 任务 | 文件 | 目标 |
|------|------|------|
| T1 | `Core/Verbs/BdpVerb_Shoot.cs` | 为 `HostResultId` 与 burst cursor 添加持久化字段 |
| T2 | `Core/VerbHosting/TriggerBodyVerbHostManager.cs` | 让 formal host 壳进入 deep-save 树，并在 PostLoad 后按槽位恢复 |
| T3 | `Core/Trigger/State/CompTriggerBody.Lifecycle.cs` | 集成 T2 的存读档入口，并保证恢复顺序正确 |
| T4 | `Core/Verbs/BdpVerb_FormalHostShoot.cs` / `Core/Verbs/BdpVerb_FormalHostMelee.cs` | 增加“首次 post-load 重绑保态”机制 |
| T5 | `Core/Verbs/BdpVerb_Shoot.cs` | 暖机完成与 burst 续接时惰性重建 emission plan，并恢复 cursor |
| T6 | `Core/AttackExecution/AttackExecutionPostLoadRecovery.cs` | 将 recovery 收缩为 safety net，只终止无效旧会话 |
| T7 | `Source/BDP.Tests/*.ps1` | 补 smoke tests，覆盖三层目标 |

---

## T1：BdpVerb_Shoot 持久化最小会话身份与 burst cursor

### 文件

- `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

### 必做内容

#### 1. `HostResultId` 改成 backing field + `ExposeData()`

这是 v1.1 已识别出的必要项，继续保留。

#### 2. 新增 burst cursor 持久化字段

当前至少需要：

- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

必要时可一并持久化：

- `pendingEmissionConsumedCount`

但这项更多是诊断值，可选。

#### 3. `ExposeData()` 中同时持久化

`BdpVerb_Shoot.ExposeData()` 需要在 `base.ExposeData()` 之后补充：

- `hostResultId`
- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

### 目标

让读档后的 verb 不只知道“我是哪个结果”，还知道“我打到第几个发射位置”。

### 风险说明

不能把 `pendingVerbEmissionPlan` 或 `pendingEmissionWindows` 整体序列化。  
这些是可重建派生对象，不应该直接进存档。

---

## T2：TriggerBodyVerbHostManager 暴露 formal host 壳的 deep-save 链

### 文件

- `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`

### 必做内容

#### 1. 增加 `ExposeVerbShells()`

在 `Saving` 和 `LoadingVars` 两个阶段对称执行：

- 按 `FormalHostSlots` 固定顺序导出远程壳列表
- 按 `FormalHostSlots` 固定顺序导出近战壳列表
- 使用 `Scribe_Collections.Look(..., LookMode.Deep)` deep-save

#### 2. 增加 `RestoreShellsPostLoad()`

职责：

- 读取 `LoadingVars` 阶段加载出来的壳对象
- 按固定槽位重新挂回 `bindings`
- 调用 `InitializeFormalHost(owner, slot)` 重新注入运行时引用

#### 3. 保持槽位顺序唯一

恢复时必须按 `FormalHostSlots` 顺序，而不是按 loadID 做自由匹配。  
这里恢复的是“固定槽位拥有的壳”，不是“任意 verb 池里的对象”。

### 目标

解决：

- `not deep-saved`
- `Could not resolve reference to object with loadID`

### 风险说明

这里只负责“把对象读回来”，不负责“保证它续上正确会话”。  
不要在这一层混入 session 重建逻辑。

---

## T3：CompTriggerBody.Lifecycle 集成存读档入口

### 文件

- `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`

### 必做内容

#### 1. 在任意 Scribe 阶段前确保内部状态存在

`EnsureInternalState()` 不能再只放在 `Saving` 分支里。  
`LoadingVars` 阶段也必须保证 `verbHostManager` 已存在。

#### 2. 在 `PostExposeData()` 的主 Scribe 区段中接入 `ExposeVerbShells()`

顺序要求：

1. 基础 owner 真值字段
2. `verbHostManager.ExposeVerbShells()`
3. `PostLoadInit` 恢复

#### 3. 在 `PostLoadInit` 中于 `RefreshProjectedOutputs()` 之前接入 `RestoreShellsPostLoad()`

推荐顺序：

1. `EnsureInternalState()`
2. `EnsureChipContainer()`
3. `EnsureSlots()`
4. `verbHostManager.RestoreShellsPostLoad(this)`
5. `RestoreSlotTruth()`
6. `RebuildContainerFromSlotTruth()`
7. `RefreshProjectedOutputs()`

### 目标

确保 cross-ref 解析依赖的壳对象已经在存档树里，并且 post-load 阶段能把它们重新挂回正式槽位。

---

## T4：formal host 首次 post-load 重绑保态

### 文件

- `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`

### 这是 v2 新增的关键任务

v1.1 最大缺口就在这里。

### 必做内容

#### 1. 增加“读档后首次重绑保态”标记

建议在 formal host 壳内部新增一次性标记，例如：

- `preserveLoadedStateOnce`

它的语义是：

- 这个壳刚从存档恢复回来
- 下一次 `SyncFormalBinding()` 是同会话恢复，不是新会话切换

#### 2. 在 `RestoreShellsPostLoad()` 后为已加载壳打上该标记

只有从存档读回来的壳需要。  
新建的降级壳不需要。

#### 3. 调整 `SyncFormalBinding()` / `ShouldResetForBindingChange()` 判定

规则必须改成：

- 如果是“同槽位、同 `HostResultId`、同会话”的首次 post-load rebind，则只重注入 `verbProps/tool/maneuver/caster/verbTracker`，不调用 `Reset()`
- 如果结果身份真的变了，或 binding 已失效，才允许 `Reset()`

### 目标

保住原版 `Verb.ExposeData()` 恢复出来的这些值：

- `state`
- `currentTarget`
- `burstShotsLeft`
- `ticksToNextBurstShot`

### 风险说明

这一步是整个 v2 的生死线。  
如果没做好，即使壳对象能被读回来，也会在第一次 `Refresh()` 被我们自己清空状态。

---

## T5：BdpVerb_Shoot 惰性重建 emission plan，并恢复 cursor

### 文件

- `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

### 必做内容

#### 1. 暖机续接：在 `WarmupComplete()` 前置惰性补 plan

如果：

- `HasPendingEmissionPlan()` 为 false
- `currentTarget` 有效
- `HostResultId` 有效

则先尝试重建 plan，再走现有 `WarmupComplete()` 逻辑。

注意：  
不能简单替换成 `base.WarmupComplete()`，必须保留 BDP 当前自己的暖机收口逻辑。

#### 2. burst 续接：在 `TryCastShot()` 开始处惰性补 plan

如果：

- 当前 `state == Bursting`
- 但没有 pending plan

则先按 `HostResultId + currentTarget` 重建 plan。

#### 3. 恢复 cursor，而不是只重建 plan

重建 plan 后必须把消费位置推进到读档前的 cursor：

- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

否则会从头重放当前 burst。

#### 4. 不重置 `burstShotsLeft`

`burstShotsLeft` 已由原版基类持久化，续接时只作为剩余发数上限继续消耗。

### 目标

让：

- 暖机中存档 → 读档后继续暖机
- burst 中段存档 → 读档后从正确发射位置继续

### 风险说明

如果当前表达结果变化导致新 plan 结构与旧 cursor 不兼容，必须降级终止该旧会话。  
不要强行套用旧 cursor。

---

## T6：PostLoadRecovery 收缩为 safety net

### 文件

- `Source/BDP/Core/AttackExecution/AttackExecutionPostLoadRecovery.cs`

### 必做内容

恢复链成型后，recovery 不再承担“正常续接”的主逻辑，只做兜底：

- `verb == null` 的旧存档兼容
- `verb.Available() == false` 的失效会话终止
- BDP job 还在，但对应宿主壳或表达结果已失效时终止

不能再像当前临时方案那样，把所有 BDP busy stance 一刀切清掉。

### 目标

让 recovery 成为：

- 旧档兼容层
- 异常降级层

而不是正常路径。

---

## T7：测试补齐

### 文件

- `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`
- 新增或扩展相关 smoke tests

### 必测场景

#### A. 对象持久化

- 暖机中存档时，不再出现 `not deep-saved`
- 读档时，不再出现 formal host `loadID` 解析失败

#### B. 暖机续接

- 自动攻击进入 `Stance_Warmup`
- 存档
- 读档
- 继续倒计时并正常发射

#### C. burst 中段续接

- burst 打到中段
- 存档
- 读档
- 只继续剩余部分，不重放已打部分

#### D. 降级终止

- 结果失效
- `HostResultId` 无法命中
- cursor 超界

都必须安全终止旧会话，不刷异常。

---

## 推荐实施顺序

1. T1：先补 `HostResultId` 与 cursor 持久化字段
2. T2：让壳对象进入存档树
3. T3：接入 `CompTriggerBody` 生命周期
4. 写第一轮 failing smoke test，验证当前仍会在首次 `Refresh()` 清状态
5. T4：补首次 post-load 重绑保态
6. T5：补暖机与 burst 的惰性 plan 重建 + cursor 恢复
7. T6：收缩 recovery 为 safety net
8. T7：完整回归测试与游戏内验证

---

## v2 的完成标准

只有同时满足以下条件，才算真正完成：

1. 存档时无 `not deep-saved`
2. 读档时无 formal host 引用解析失败
3. 暖机中存档后可无缝续接
4. burst 中段存档后从正确游标续接，而不是从头重放
5. 表达结果失效时能安全降级终止，不刷异常

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-04-01 | 初版，完整覆盖 T1-T4 的文件位置、修改内容和验证点 | Claude Sonnet 4.6 |
| v1.1 | 2026-04-01 | 新增 T5（惰性重建 emission plan），修正 T4 角色 | Claude Sonnet 4.6 |
| v2.0 | 2026-04-01 | 新增“首次 post-load 重绑保态”与“burst cursor 持久化”两项关键任务，修正实施边界 | Codex GPT-5 |
