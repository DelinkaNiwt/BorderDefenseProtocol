# AttackExecution 运行时动作步重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不推翻现有 `Expression -> VerbHosting -> AttackExecution -> Verb -> Projectile/Effect` 主链的前提下，为 AttackExecution 补齐“计划单位到运行时动作步单位”的正式映射层，使远程与近战都能稳定把高层编排翻译成引擎真正可消费的执行步。

**Architecture:** 保留现有 `AttackPlan` 作为高层逻辑编排模型，不再强迫 `Verb` 直接消费 `Group / Cast` 的高层结构。新增位于 `Core/AttackExecution/` 内部的 `AttackRuntimeStep` 与 `RuntimeStepBuilder`：前者描述“引擎下一次真正要消费的动作步”，后者负责把 `Plan` 翻译成远程或近战都可各自消费的运行时步。`BdpVerb_Shoot` 与近战执行链只消费 runtime step，不直接理解双侧调度策略名。

**Tech Stack:** C#, RimWorld Verb/JobDriver/Projectile 体系, BDP Expression/VerbHosting/AttackExecution/Semantics 分层

---

## 约束

- 不推翻现有 Expression、VerbHosting、Projectile 体系。
- 不回退到旧 BDP 的具体双持实现。
- 不新开大目录，只在 `Core/AttackExecution/` 内聚收口。
- 不写工程测试。
- 逐成员注释，中文文件保持 UTF-8。

### Task 1: 固化“计划单位”和“运行时动作步单位”的正式边界

**Files:**
- Create: `Source/BDP/Core/AttackExecution/AttackRuntimeStep.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionPlan.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionCast.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionEmit.cs`

**Step 1: 新增 AttackRuntimeStep**

至少包含以下成员：

- `AttackInstanceId`
- `GroupIndex`
- `StepIndex`
- `WeaponMode`
- `ExecutionKind`
- `HostResultId`
- `Target`
- `Casts`
- `Emits`
- `IntervalTicksAfter`
- `IsPrimarySelection`

要求：

- `AttackRuntimeStep` 只描述“运行时下一步要消费什么”。
- 它不是新的表达结果，也不是新的计划模型。

**Step 2: 回写注释边界**

要求：

- `AttackExecutionPlan` 明确是高层逻辑编排。
- `AttackExecutionCast` 明确是计划层动作，不等于运行时一步。
- `AttackExecutionEmit` 明确是最小发射载荷。
- `AttackRuntimeStep` 明确是运行时执行单位。

**Step 3: 不让 RuntimeStep 膨胀成总包**

要求：

- 不把 Pawn、Verb、Job 运行时对象塞进 step。
- 不把近战专属 `Tool / Maneuver` 强塞进远程 step。

### Task 2: 引入 RuntimeStepBuilder

**Files:**
- Create: `Source/BDP/Core/AttackExecution/IAttackRuntimeStepBuilder.cs`
- Create: `Source/BDP/Core/AttackExecution/DefaultAttackRuntimeStepBuilder.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackExecutionEntry.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionResolvedRequest.cs`

**Step 1: 增加正式 builder 接口**

要求：

- 输入：`AttackExecutionResolvedRequest`
- 输出：`IReadOnlyList<AttackRuntimeStep>`
- 只做映射，不直接执行

**Step 2: 在 ResolvedRequest 中挂接 RuntimeSteps**

要求：

- `ResolvedRequest` 同时保留 `Plan`
- 新增 `RuntimeSteps`
- 明确二者语义不同：
  - `Plan` = 逻辑编排
  - `RuntimeSteps` = 运行时执行单位

**Step 3: 在 Entry 中正式串起**

要求：

- `TryBuildPlan()` 成功后继续 build runtime steps
- 若 runtime steps 为空则拒绝执行
- 诊断日志补上 runtime step 摘要

### Task 3: 定义远程与近战的运行时动作步映射规则

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackRuntimeStepBuilder.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackGroupExecutionKind.cs`

**Step 1: 远程映射规则**

要求：

- 单侧单发 cast -> 1 step
- 单侧齐射 cast(多 emits) -> 1 step
- 同组并列的多个远程 cast -> 可归并成 1 step
- step 内最终消费的是合并后的 emit 列表，而不是 plan.casts 原样直通

**Step 2: 近战映射规则**

要求：

- 近战一次命中推进 -> 1 step
- 近战暂不强行套远程会话语义
- 但同样要通过 RuntimeStepBuilder 正式映射

**Step 3: 明确“统一边界，不统一细节”**

要求：

- 统一的是“都先映射成 runtime step”
- 不统一“远近执行细节必须完全一样”

### Task 4: 重构远程执行链只消费 RuntimeStep

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultRangedAttackExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/JobDriver_BdpRangedAttackExecution.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: 远程上下文改挂当前 step**

要求：

- `RangedAttackExecutionContext` 增加 `Step`
- 区分：
  - `SessionResult`
  - `Step`
  - `Cast`

**Step 2: 执行器绑定 RuntimeSteps**

要求：

- 不再把 `Plan.Casts` 直接绑进 `BdpVerb_Shoot`
- 改为绑定 `RuntimeSteps`

**Step 3: JobDriver 按 step 推进**

要求：

- 每轮持续推进准备下一次 runtime step
- 不再直接把 `Plan.PrimaryCast` 当成下一步运行时单位

### Task 5: 重构 BdpVerb_Shoot 只消费运行时动作步

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`

**Step 1: 把 pendingBurstCasts 改成 pendingRuntimeSteps**

要求：

- 宿主内部等待队列应表达“待消费的运行时动作步”
- 不再直接表达“待消费的高层 cast”

**Step 2: 每次 TryCastShot 只消费一个 runtime step**

要求：

- step 内可包含多个 emit
- step 内可来自一个或多个 cast
- 但对 Verb 来说，这都只算一次真正开枪动作

**Step 3: WarmupComplete 按 step 数驱动**

要求：

- 原版 burst 会话推进按 runtime step 数推进
- 不再按 plan.casts 数量直接推进

### Task 6: 收口双侧同组并列的正式语义

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackRuntimeStepBuilder.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionDiagnostics.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackEffectDiagnostics.cs`

**Step 1: 让 Dual 同组并列在运行时归并为一个 step**

例如：

```text
group0:
  cast1 = 齐射(5 emits)
  cast2 = 爆炸(1 emit)
```

映射成：

```text
step0:
  emits = cast1.emits + cast2.emits
  hostResultId = dual result id
```

**Step 2: 保持 emit 真值不变**

要求：

- 归并只合并“运行时步”
- 不得改写 emit 自己的 `ProjectileDef / SemanticContext / SourceResultId`

**Step 3: 诊断日志补足 step 维度**

至少输出：

- `hostResultId`
- `stepIndex`
- `castCount`
- `emitCount`
- `sourceResultId`
- `projectileDef`

### Task 7: 保持近战执行边界与即时效果边界稳定

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackPlanExecutor.cs`
- Modify: `Source/BDP/Core/AttackExecution/DefaultAttackEffectEmitter.cs`
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`

**Step 1: 近战执行器也接 RuntimeStep**

要求：

- 近战不绕开新边界
- 但仍保留当前 direct effect / chase / continuous 的既有能力

**Step 2: 不让新层误伤近战**

要求：

- 近战目前的即时效果路径仍可工作
- 不因统一加 step 层而强迫近战全部进入远程式会话

### Task 8: 同步 DefHarness 注释与日志口径

**Files:**
- Modify: `BorderDefenseProtocol.DevHarness/1.6/Defs/Trigger/ThingDefs_BDP_TestChips.xml`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionDiagnostics.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackEffectDiagnostics.cs`

**Step 1: Def 注释同步为新口径**

要求：

- 明确 `Plan` 是逻辑编排
- 明确真正落地是 runtime step
- 明确 dual 统一入口不等于统一投射物

**Step 2: 日志让人工看出三层关系**

要求：

- `plan`
- `runtimeStep`
- `emit payload`

三层信息都能看出来，不混在一起。

### Task 9: 最小编译验证

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/*.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Expressions/*.cs`

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

---

## 预期重构后结构

```text
ExpressionResult
-> 攻击身份

AttackPlan
-> 高层逻辑编排

AttackRuntimeStep
-> 运行时动作步

Ranged / Melee Executor
-> 消费运行时动作步

BdpVerb_Shoot / BdpVerb_MeleeAttackDamage
-> 真正落地当前动作步

AttackExecutionEmit
-> 发射真值
```

## 本计划解决的正式缺口

- 解决 `Plan` 单位与运行时消费单位不一致的问题。
- 解决 dual 同组并列在远程会话里被错误串行化的问题。
- 解决远近模式都缺统一“运行时动作步映射层”的问题。
- 继续保持 emit payload 不被宿主吞并。

## 本计划刻意不做的事

- 不重写表达系统。
- 不重写 VerbHosting 注册体系。
- 不构建大而全的 SessionFramework。
- 不回退到旧 BDP 的 DualVerb 直接实现。
