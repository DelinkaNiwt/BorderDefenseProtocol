# Ability 与 Hediff 表达最小闭环 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 完成 Ability 与 Hediff 芯片表达的最小正式闭环，使新 BDP 在不引入额外平台复杂度的前提下，能够开始编写具体业务芯片逻辑并做游戏实测。

**Architecture:** 继续保留 `Trigger → Expression → HostSync → 原版宿主` 主链，不恢复旧版 `IChipEffect` 总控，也不新增来源中台或运行时镜像。独立 `Ability` 走原版 `Pawn_AbilityTracker`，独立 `Hediff` 走原版 `Pawn.health.hediffSet`，`Hediff` 首轮严格使用 BDP 专用 Def；`Hediff` 数量映射强度的规则统一命名为 `countToSeverity`。

**Tech Stack:** C#、RimWorld 原版宿主对象、BDP 表达系统、PowerShell smoke tests

---

## 范围护栏

本计划只负责完成以下闭环：

- `Ability` 独立表达结果
- `Hediff` 独立表达结果
- `HediffApplyModeKey` 命名统一
- `countToSeverity` 最小语义闭环
- 宿主同步与首轮 smoke test

本计划明确不包含：

- 形态切换正式闭环
- 通用来源追踪平台
- `Hediff` 附带 `Ability` 的 BDP 专门机制
- 多来源共用同一个 `HediffDef` 的复杂安全回收
- 更大范围的业务功能编写

## Task 1：锁定边界、命名和开测门槛

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AbilityHediffExpressionMinimalClosureSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`
- Reference: `模组工程/BorderDefenseProtocol/docs/plans/2026-04-06-Ability与Hediff表达最小闭环设计-第一版.md`

**Step 1：写失败的 smoke test**

新增 `AbilityHediffExpressionMinimalClosureSmokeTests.ps1`，至少锁定下面这些事实：

- 新 BDP 只把 `Ability` / `Hediff` 当表达结果，不引回旧版 `IChipEffect`
- 结果链上正式使用 `HediffApplyModeKey`
- `countToSeverity` 是唯一保留的数量映射强度语义
- `ExpressionService.SyncProjectedHosts(...)` 仍然是宿主同步入口
- `DefaultExpressionAbilityHostSynchronizer` 与 `DefaultExpressionHediffHostSynchronizer` 仍然参与主链

**Step 2：把现有投影 smoke test 补成边界保护**

在 `ExpressionPublishedProjectionSmokeTests.ps1` 里补断言，锁定：

- 发布后宿主同步必须吃“已构建快照”
- 普通读取路径不能偷偷重算
- `Ability` / `Hediff` 的最小主链依然挂在发布路径上

**Step 3：把 DevHarness 配置 smoke test 补成业务起步门槛**

在 `DevHarnessChipTrionConfigSmokeTests.ps1` 里补断言，锁定：

- `BDP_TestChipAbility`
- `BDP_TestChipHediff`

这两个测试芯片定义必须继续存在，作为首轮业务起步样本。

**Step 4：运行 smoke test，确认先失败**

Run:

```powershell
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessChipTrionConfigSmokeTests.ps1'
```

Expected:

- 新增测试先失败
- 其余测试在改动前后都能明确反映边界变化

---

## Task 2：统一 Hediff 相关命名，不留半旧半新口径

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Config/ChipExpressionEntryConfig.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Contract/ChipExpressionEntryContract.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Contract/DefaultChipExpressionContractInterpreter.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/ExpressionSourceDeclaration.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/ExpressionSourceMaterial.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/ExpressionInfoProjectionEntry.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/DefaultExpressionSourceDeclarationProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Combos/Config/ComboExpressionEntryConfig.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultExpressionInfoProjector.cs`

**Step 1：把字段名统一收口**

统一把：

- `ApplyModeKey`

改为：

- `HediffApplyModeKey`

要求：

- 配置层、契约层、声明链、结果链、信息投影链全部一起改
- 不允许新旧字段名长期并存

**Step 2：把诊断输出同步改名**

信息投影与说明文本里，把：

- `applyMode`

统一改为：

- `hediffApplyMode`

避免调试时继续出现旧口径。

**Step 3：运行相关 smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
```

Expected:

- 命名链 smoke test 通过
- 不再残留旧 `ApplyModeKey` 口径

---

## Task 3：把 `stack` 语义改名为 `countToSeverity`

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultExpressionHediffHostSynchronizer.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultExpressionInfoProjector.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AbilityHediffExpressionMinimalClosureSmokeTests.ps1`

**Step 1：重命名 Hediff 应用规则常量**

把内部常量：

- `StackApplyModeKey`

改为：

- `CountToSeverityApplyModeKey`

把字面值：

- `"stack"`

改为：

- `"countToSeverity"`

**Step 2：内部字段同步改名**

把 Hediff 聚合需求对象里的：

- `Count`

改为：

- `ResultCount`

明确它表达的是“当前成立结果数量”。

**Step 3：短期兼容旧配置**

首轮实现建议保留一小段兼容逻辑：

- 如果配置仍写 `"stack"`，也按 `countToSeverity` 处理

但：

- 所有文档
- 所有样例
- 所有新测试

统一只写 `countToSeverity`。

**Step 4：补 smoke test**

锁定以下事实：

- `countToSeverity` 是正式名字
- `"stack"` 最多只允许作为过渡兼容
- 语义仍然是“把结果数量写进同一 Hediff 的 Severity”

**Step 5：运行 smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
```

Expected:

- `countToSeverity` 命名 smoke test 通过

---

## Task 4：收口 Ability 最小闭环，保证独立 Ability 可正式起业务

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultExpressionAbilityHostSynchronizer.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AbilityHediffExpressionMinimalClosureSmokeTests.ps1`

**Step 1：锁定独立 Ability 的唯一语义**

本轮只支持：

- 当前结果里有 `AbilityDefName` → 补入该 `Ability`
- 当前结果里没有 → 回收该 `Ability`

不要在这轮引入：

- 额外冷却状态镜像
- 额外充能状态镜像
- 额外能力运行时容器

**Step 2：检查同步器回收边界**

确保 `DefaultExpressionAbilityHostSynchronizer` 只回收：

- 本轮由表达系统补进去的 `Ability`

而不是原版或其他来源已有的能力。

**Step 3：补 smoke test**

至少锁定：

- `GainAbility`
- `RemoveAbility`
- 基于已记录集合回收
- 不在 `Trigger` 里直接做宿主副作用

**Step 4：运行 smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
```

Expected:

- Ability 最小闭环 smoke test 通过

---

## Task 5：收口 Hediff 最小闭环，明确首轮业务护栏

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultExpressionHediffHostSynchronizer.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Chips/Validation/DefaultChipDefinitionValidator.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AbilityHediffExpressionMinimalClosureSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`

**Step 1：锁定首轮 Hediff 业务边界**

在代码注释、验证器或 smoke test 中明确首轮规则：

- 表达系统接管的 `Hediff` 必须使用 BDP 专用 `HediffDef`

这里不一定要做成硬错误，但至少要：

- 在验证器里给出清晰提示，或
- 在 smoke test 和设计注释里把边界钉死

**Step 2：锁定当前唯一保留的 Hediff 应用规则**

首轮只保留两种语义：

- 空 `HediffApplyModeKey`：只保证存在
- `countToSeverity`：把结果数量写入 `Severity`

不要在本轮引入更多 Hediff 应用规则。

**Step 3：检查回收逻辑**

确认当前回收逻辑与首轮边界一致：

- 只要业务使用 BDP 专用 `HediffDef`
- 当前按 `def` 回收就足够开测

此处不额外引入复杂来源索引。

**Step 4：补 smoke test**

至少锁定：

- `RemoveAllHediffsOfDef(...)` 的存在是建立在“首轮专用 Def”边界上的
- `countToSeverity` 时会把 `ResultCount` 写回 `Severity`
- 当前没有额外形态型、模式型 `Hediff` 分类

**Step 5：运行 smoke test**

Run:

```powershell
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessChipTrionConfigSmokeTests.ps1'
```

Expected:

- Hediff 最小闭环 smoke test 通过
- 首轮业务边界 smoke test 通过

---

## Task 6：补最小业务样本和开测门槛

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`
- Reference: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ExpressionPublishedProjectionSmokeTests.ps1`
- Reference: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AbilityHediffExpressionMinimalClosureSmokeTests.ps1`

**Step 1：把业务起步样本定义清楚**

首轮至少约定两个业务样本：

- 一个独立 `Ability` 芯片表达样本
- 一个独立 `Hediff` 芯片表达样本

这两个样本不是为了玩法完整，而是为了证明：

- 作者已经可以开始写具体业务芯片定义

**Step 2：定义开测门槛**

只有下面几条全部满足，才允许进入下一阶段业务编写：

- Ability 最小闭环 smoke test 全通过
- Hediff 最小闭环 smoke test 全通过
- 命名统一 smoke test 全通过
- 现有表达发布 smoke test 不回退
- 能成功构建主模组

**Step 3：运行最终检查**

Run:

```powershell
& '.\Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1'
& '.\Source\BDP.Tests\AbilityHediffExpressionMinimalClosureSmokeTests.ps1'
& '.\Source\BDP.Tests\DevHarnessChipTrionConfigSmokeTests.ps1'
dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- smoke tests 全 PASS
- 主模组构建成功

---

## 实施完成标志

只有当下面这些条件全部满足时，才算本计划完成：

- `ApplyModeKey` 已统一改为 `HediffApplyModeKey`
- `stack` 已统一改为 `countToSeverity`
- 独立 `Ability` 表达闭环已通过 smoke test
- 独立 `Hediff` 表达闭环已通过 smoke test
- 首轮 `Hediff` 专用 Def 边界已被明确锁定
- 文档和测试口径已统一
- 构建通过

到这一步为止，就可以开始下一阶段工作：

- 正式编写具体的 `Ability` 芯片表达业务逻辑
- 正式编写具体的 `Hediff` 芯片表达业务逻辑
- 进入游戏实测与数值调试

## 备注

本计划完成后，不应再先去扩平台，而应优先写真实业务样本。

下一阶段的正确顺序应是：

1. 先写一批最简单的 Ability 芯片业务表达
2. 再写一批最简单的 Hediff 芯片业务表达
3. 用真实游戏实测反馈，再决定是否有必要扩更复杂机制
