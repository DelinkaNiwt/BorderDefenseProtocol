# DualPayload 与会话宿主架构修正 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把双侧远程攻击从“一个宿主替双方发射”的错误模型，修正为“统一会话宿主只负责调度，实际发射载荷各自携带真值并独立落地”的正式架构。

**Architecture:** 保留 `Expression -> VerbHosting -> AttackExecution -> Verb -> Projectile/Effect` 的主链，不回退到旧 BDP 的具体实现方式。新增正式的 `AttackPayload` 思路，但仍落在现有 `AttackExecutionCast / AttackExecutionEmit` 模型里：`DualPrimary` 只负责复合攻击身份与调度，不再承担双方发射真值；`Emit` 升级为完整发射载荷；远程持续会话始终由会话宿主承接，手动与自动统一走同一会话身份。

**Tech Stack:** C#, RimWorld Verb/JobDriver/Projectile 体系, BDP Expression/VerbHosting/AttackExecution/Semantics 分层

---

## 约束

- 不为“齐射 + 爆炸”写特判。
- 不新增兼容层，不做结果补丁。
- 不把旧 BDP 的双武器实现整块搬回新架构。
- 不写工程测试。
- 文档、代码注释按现有口径补全。

### Task 1: 固化双侧修正的正式边界

**Files:**
- Modify: `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionCast.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionEmit.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPlan.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionGroup.cs`

**Step 1: 明确 DualPrimary 的职责声明**

要求：

- `DualPrimary` 只表示“复合攻击入口”。
- `DualPrimary` 不再被视为“复合武器真值”。
- `FormalExpressionResult` 的注释与成员语义要明确这一点。

**Step 2: 明确 Cast 与 Emit 的职责声明**

要求：

- `Cast` 表示一次真实会话内的最小施放动作。
- `Emit` 表示该次施放动作内部的一个实际发射载荷。
- 不再把 `Emit` 当成轻量日志项。

**Step 3: 补计划模型注释**

要求：

- `Plan / Group / Cast / Emit` 各自成员注释写清楚“身份、调度、载荷、落地”的边界。

### Task 2: 把 Emit 升级为正式发射载荷

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionEmit.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanBuilder.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackEffectDiagnostics.cs`

**Step 1: 给 Emit 增加完整发射真值**

至少补入以下成员：

- `SourceResultId`
- `ProjectileDef`
- `SemanticContext`
- `OriginSide`

如当前架构自然需要，也可补入：

- `VerbLabel`
- `SoundCastDefName`

要求：

- 这些成员只服务“实际发射真值”。
- `Emit` 不得膨胀成万能攻击上下文包。
- 近战专属语义例如 `Tool / Maneuver` 继续留在近战攻击语义链，不并入远程发射载荷。
- 不把运行时对象本体塞进 Emit。

**Step 2: PlanBuilder 按源结果填充 Emit 载荷**

要求：

- 每个 `Emit` 的 `ProjectileDef` 来自它自己的源结果，而不是当前复合结果宿主。
- 每个 `Emit` 的 `SemanticContext` 来自它自己的源结果。
- 双侧复合时，主副侧各自生成自己的 emit 载荷。

**Step 3: 日志补载荷字段**

要求：

- `stage=emit` 至少能看到 `sourceResultId` 与 `projectileDef`。
- 让人工一眼看出“这发到底是谁发的、用的是什么投射物”。

### Task 3: 修正 DualPrimary 的复合结果构建策略

**Files:**
- Modify: `Source/BDP/Core/Expressions/Pipeline/ExpressionSnapshotBuilder.cs`
- Modify: `Source/BDP/Core/Expressions/Model/CompositeExpressionReference.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanBuilder.cs`

**Step 1: 保留 DualPrimary 的复合身份，但去掉其对单侧发射真值的占有**

要求：

- `DualPrimary` 继续保留 `CompositeKind = DualWeapon`。
- `DualPrimary` 继续保留调度风格与复合引用。
- `DualPrimary` 不再被下游当作唯一发射真值来源。

**Step 2: 强化复合引用**

要求：

- 复合引用能稳定找到主侧与副侧源结果。
- PlanBuilder 构建 Dual 计划时，所有 emit 都从源结果取值，不从复合结果猜值。

**Step 3: 保持现有自动选择规则**

要求：

- 自动攻击仍优先 `DualPrimary`。
- 但“优先选这个入口”不再等于“所有 emit 都沿用它的武器真值”。

### Task 4: 修正远程会话宿主语义

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: 区分“会话宿主结果”与“当前 cast 对应结果”**

要求：

- `RangedAttackExecutionContext` 正式区分：
  - 会话宿主是谁
  - 当前 cast / emit 属于谁
- 不再把 `cast.Result` 直接等同于“当前会话宿主”。

**Step 2: 手动持续链始终保持 Dual host 身份**

要求：

- 手动点 `DualPrimary` 后，持续 job 的宿主仍是 `DualPrimary` host。
- 不再因为 `PrimaryCast` 是主侧 cast，就把整个会话降成主侧 host。

**Step 3: 自动与手动统一到同一会话语义**

要求：

- 自动攻击和手动攻击都按“入口结果”创建远程会话。
- 一旦会话开始，后续 plan 准备都从同一个会话宿主结果继续。
- `VerbHosting` 只允许绑定会话宿主结果。
- `VerbHosting` 不得缓存、枚举或持有 emit 级发射真值。

### Task 5: 修正 BdpVerb_Shoot 的真实职责

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Check: `Source/BDP/Core/Verbs/BdpVerbBase.cs`

**Step 1: 让 BdpVerb_Shoot 只做会话消费，不再自带全局武器真值**

要求：

- `BdpVerb_Shoot` 是会话承接者。
- 它负责 warmup、burst、cooldown、TryCastShot 边界。
- 它不再统一用 `Projectile` 给所有 emit 发射。

**Step 2: 发射时按 Emit 载荷落地**

要求：

- `TryEmitCast()` 内部对每个 `emit` 使用 `emit.ProjectileDef`。
- `SemanticContext` 从 `emit` 载荷读取，不再默认沿用宿主整体上下文。
- 如果某些展示信息仍需要宿主 `verbProps`，必须明确只用于会话表面，不用于发射真值。

**Step 3: 兜底规则最小化**

要求：

- 只有在 emit 载荷结构非法时才允许兜底。
- 兜底必须诚实记录日志，不能悄悄回退成主侧 projectile。

### Task 6: 修正远程双侧调度的计划生成

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanBuilder.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionDiagnostics.cs`

**Step 1: 保持双侧调度只负责“排布”**

要求：

- `Alternating` 只决定先后。
- `Simultaneous` 只决定同组并列。
- `MixedRhythm` 只决定组间组合关系。
- 调度层不再吞并主副侧载荷真值。

**Step 2: Dual 同组并列时，每个 cast 都要携带自己的 emit 集**

要求：

- 齐射侧的 cast 内可以有多个 emit。
- 单发侧的 cast 内可以只有一个 emit。
- 二者并列时只是 group 关系并列，不做真值合并。

**Step 3: 诊断日志补足复合计划信息**

要求：

- `stage=plan` 或 `stage=group_dispatch` 能看出这是 dual 哪种 schedule。
- `stage=cast_emit` 能看出每个 cast 实际属于哪条源结果。

### Task 7: 统一远程即时组与近战即时组的执行语义

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackGroupExecutionKind.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackEffectEmitter.cs`

**Step 1: 保持“组内并列”和“执行边界”分离**

要求：

- `TimingMode` 只描述编排。
- `ExecutionKind` 只描述走 `DirectEffect` 还是 `VerbSession`。

**Step 2: 远程组统一走 VerbSession**

要求：

- 即使是 dual simultaneous 远程组，也不得直接效果直发。
- 所有远程组都必须通过远程会话宿主边界落地。

**Step 3: 近战组维持现有即时效果能力**

要求：

- 不因这次 dual ranged 修正，误伤近战当前可直接派发的架构边界。

### Task 8: 补 DefHarness 与诊断口径

**Files:**
- Modify: `BorderDefenseProtocol.DevHarness/1.6/Defs/Trigger/ThingDefs_BDP_TestChips.xml`
- Modify: `Source/BDP/Core/AttackExecution/AttackEffectDiagnostics.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionDiagnostics.cs`

**Step 1: 把测试芯片注释补成新架构口径**

要求：

- 明确“Dual 是统一入口，不是统一投射物”。
- 明确“齐射/爆炸各自有自己的发射载荷真值”。

**Step 2: 日志口径改成可人工判断**

至少输出：

- attackId
- hostResultId
- cast.resultId
- emit.sourceResultId
- projectileDef
- target

**Step 3: 不新增测试工程**

要求：

- 只维持 DevHarness 的样本表达清晰度。

### Task 9: 最小编译与交付整理

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/*.cs`
- Modify: `Source/BDP/Core/Expressions/*.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: 主模组编译**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; $env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; dotnet msbuild BDP.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected:

- `BDP.dll` build success

**Step 2: DevHarness 编译**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; $env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'; dotnet msbuild BDP.DevHarness.csproj -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- `BDP.DevHarness.dll` build success

**Step 3: 交付检查**

要求：

- 所有新增成员有逐成员注释。
- 中文文档回读无乱码。
- 日志字段命名与新架构口径一致。

---

## 预期完成后的正式架构

```text
ExpressionResult
-> 只回答“有哪些攻击入口、有哪些复合关系”

DualPrimary
-> 只回答“这是双侧入口，怎么调度”

AttackPlan
-> 只回答“这一轮怎么排 group / cast / emit”

AttackEmit
-> 只回答“这一次实际发射用谁的真值”

VerbHosting
-> 只回答“哪个宿主承接原版会话”

BdpVerb_Shoot
-> 只消费当前会话内的 cast / emit

Projectile / Effect
-> 只落地效果
```

## 本计划解决的根因

- 解决 `DualPrimary` 被错误当成统一武器真值宿主的问题。
- 解决手动 dual 会话被降成主侧 host 的问题。
- 解决副侧 emit 被主侧 projectile 覆盖的问题。
- 解决自动与手动会话语义不一致的问题。

## 本计划刻意不做的事

- 不处理伤口来源名展示问题。
- 不扩展爆炸链额外业务规则。
- 不为某个具体芯片组合写专门映射。
- 不引入结果导向的兼容回退逻辑。
