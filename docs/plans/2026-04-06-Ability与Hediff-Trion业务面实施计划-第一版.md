# Ability 与 Hediff Trion 业务面实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为新 BDP 补齐可直接写业务的 `Ability` 施法扣 `Trion` 与 `Hediff` 持续耗 `Trion` 正式业务面，并让 DevHarness 两个样例 Def 直接用起来。

**Architecture:** `Ability` 的施法成本继续挂在原版 `AbilityDef.comps`，但正式扣费放到自定义 `Verb_CastAbility` 子类最前面提交，避免“校验时够、落地时不够”漏扣。`Hediff` 的持续耗蓝继续挂在原版 `HediffDef.comps`，由一个很薄的 `HediffComp` 按当前 `Severity` 发布到 `Trion` 持续消耗表，不回流污染表达层和 Trigger。

**Tech Stack:** RimWorld C# 扩展点、主模组 `Trion` 正式请求面、DevHarness XML Def、PowerShell 烟雾测试。

---

### 任务 1：锁定业务样例新要求

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessAbilityHediffBusinessSmokeTests.ps1`

**Step 1: 写失败测试**

- 断言 `BDP_TestAbility_ExpressionOnly` 使用 BDP 自定义施法 Verb。
- 断言 `BDP_TestAbility_ExpressionOnly` 声明 `TrionCost=50` 的能力组件。
- 断言 `BDP_TestHediff_ExpressionOnly` 声明 BDP 自定义持续耗蓝 HediffComp。
- 断言 `BDP_TestHediff_ExpressionOnly` 的耗蓝阶段配置为 `1/s` 与 `3/s`。

**Step 2: 运行测试确认它先失败**

Run: `& '.\Source\BDP.Tests\DevHarnessAbilityHediffBusinessSmokeTests.ps1'`

Expected: FAIL，因为当前样例还没有这些业务配置。

**Step 3: 最小实现通过测试**

- 只补这次业务面真正需要的最小 C# 与 XML。

**Step 4: 重新运行测试确认转绿**

Run: `& '.\Source\BDP.Tests\DevHarnessAbilityHediffBusinessSmokeTests.ps1'`

Expected: PASS。

### 任务 2：补齐 Ability 施法扣费正式面

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Abilities/BdpVerb_CastAbility.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Abilities/CompAbilityEffect_BdpTrionCost.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Abilities/CompProperties_AbilityEffect_BdpTrionCost.cs`

**Step 1: 先让按钮与目标校验会拒绝**

- 在能力组件里提供：
  - `CanCast`
  - `GizmoDisabled`
  - `Valid`
- 不够 `Trion` 时使用固定中文提示拒绝施法。

**Step 2: 补真正施法时的正式提交**

- 在 `BdpVerb_CastAbility.TryCastShot()` 里先找 `CompAbilityEffect_BdpTrionCost`。
- 成功扣费后才继续 `base.TryCastShot()`。
- 扣费失败时直接提示并返回 `false`。

**Step 3: 保持实现简单**

- 不引入额外平台。
- 不做来源账本。
- 不做补丁。
- 不把施法成本塞回芯片表达层。

### 任务 3：补齐 Hediff 持续耗蓝正式面

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Hediffs/HediffComp_BdpTrionDrain.cs`
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Hediffs/HediffCompProperties_BdpTrionDrain.cs`

**Step 1: 定义最小 XML 契约**

- `DrainStages`
  - `MinSeverity`
  - `DrainPerSecond`

**Step 2: 在 HediffComp 内发布持续消耗**

- 解析当前 `Severity` 对应的 `DrainPerSecond`。
- 转成 `perDay` 后注册到 `ITrionCommands.RegisterDrain(...)`。
- `Severity` 变化时覆盖旧值。
- `Hediff` 移除时注销。

**Step 3: 保持业务边界干净**

- `Hediff` 只关心自己存在时要不要耗蓝。
- 不把持续耗蓝写回 Trigger。
- `Trion` 不够时只扣到 0，不移除 Hediff。

### 任务 4：把 DevHarness 样例 Def 接到新业务面

**Files:**
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Core/AbilityDefs/AbilityDefs_BDP_TestExpressionOnly.xml`
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Core/HediffDefs/HediffDefs_BDP_TestExpressionOnly.xml`

**Step 1: Ability 样例**

- 把 `verbClass` 改成 `BDP.Core.Abilities.BdpVerb_CastAbility`。
- 增加 `TrionCost=50` 的能力组件。

**Step 2: Hediff 样例**

- 增加持续耗蓝 HediffComp。
- 阶段配置：
  - `MinSeverity=0.1` -> `1/s`
  - `MinSeverity=2` -> `3/s`

### 任务 5：补说明并做最小验证

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/docs/需求说明/2026-04-06-芯片表达使用说明-第一版.md`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessChipTrionConfigSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessAbilityHediffBusinessSmokeTests.ps1`

**Step 1: 补使用说明**

- 明确：
  - 芯片表达层不写施法成本与状态耗蓝
  - 施法成本写在 `AbilityDef.comps`
  - 状态耗蓝写在 `HediffDef.comps`

**Step 2: 运行聚焦验证**

Run:

- `& '.\Source\BDP.Tests\DevHarnessAbilityHediffBusinessSmokeTests.ps1'`
- `& '.\Source\BDP.Tests\DevHarnessChipTrionConfigSmokeTests.ps1'`

Expected: PASS。

**Step 3: 运行主模组构建**

Run: `dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal`

Expected: Build succeeded。
