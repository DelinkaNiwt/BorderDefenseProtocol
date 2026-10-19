# BDP 表达条目分层 VerbProperties 覆盖 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让普通芯片表达条目使用字段级可空的 VerbProperties 声明，并实现“表达条目显式值/倍率优先于武装构型显式值/倍率”的分层解析规则。

**Architecture:** 将组合技当前使用的 `VerbPropsOverlay` 提升为 Core 表达层的通用声明类型，普通芯片条目与组合技条目共同使用。Content 只负责把武装构型字段作为较低优先级层合入副本；Core 负责把声明层正规化为原版 `VerbProperties`，并在最终规格生成时应用选定倍率。

**Tech Stack:** RimWorld 1.6、C# 7.3、.NET Framework 4.8、PowerShell 冒烟测试。

---

### Task 1: 写出分层优先级失败测试

**Files:**
- Create: `Source/BDP.Tests/ExpressionVerbPropertiesLayeredOverrideSmokeTests.ps1`

**Step 1: Write the failing test**

测试通过反射和源文件边界同时验证：普通表达条目的 `VerbProps` 使用通用字段级覆盖类型；覆盖层拥有绝对值和倍率字段；表达条目绝对值优先于构型绝对值；表达条目倍率优先于构型倍率；缺省字段仍能使用构型绝对值；构型倍率只在表达条目未声明同名倍率时生效。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ExpressionVerbPropertiesLayeredOverrideSmokeTests.ps1
```

Expected: FAIL，因为当前普通条目仍声明为原始 `VerbProperties`，且通用覆盖层尚未包含表达条目倍率与分层合并实现。

### Task 2: 将字段级 VerbProperties 声明提升为 Core 通用模型

**Files:**
- Create: `Source/BDP/Core/Expressions/Config/VerbPropsOverlay.cs`
- Delete: `Source/BDP/Core/Combos/Config/VerbPropsOverlay.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ChipExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Combos/Config/ComboExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Combos/Config/ComboExpressionEntryCloneService.cs`

**Step 1: Add the shared declaration type**

迁移现有组合技字段级类型到 `BDP.Core.Expressions`，补充 `rangeMultiplier`、`accuracyMultiplier`、`warmupMultiplier`、`cooldownMultiplier`、`burstShotCountMultiplier` 等与构型现有倍率一一对应的字段。所有可覆盖字段使用可空类型表达“是否显式声明”。

**Step 2: Switch both authoring configs to the shared type**

把普通 `ChipExpressionEntryConfig.VerbProps` 改成通用覆盖层；组合技条目继续使用同一类型，不改变组合技的来源求值声明。

### Task 3: 先实现普通表达条目的最小正规化

**Files:**
- Modify: `Source/BDP/Core/Expressions/Contract/ChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP/Core/Chips/Validation/ChipDefinitionValidator.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ResolvedVerbSpecFactory.cs`

**Step 1: Materialize the authoring overlay**

增加从 `VerbPropsOverlay` 到原版 `VerbProperties` 的正规化入口；仅把显式字段写入运行时对象，省略字段保留原版默认值。远程条目仍要求显式 `verbClass`，近战条目保持现有 Tool 推导规则。

**Step 2: Apply expression multipliers in the final spec**

在创建正式规格前，按“绝对值先选定、倍率后计算”的顺序处理；倍率使用 `HasValue` 判断显式性，不能用大于 0 判断。

### Task 4: 修改 Content 构型合并为分层选择

**Files:**
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Resolution/ChipArmamentFormExpressionService.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewBuilder.cs`

**Step 1: Merge ordinary entries without overwriting explicit entry fields**

构型绝对值只填入普通表达条目缺省字段；构型倍率只填入表达条目未声明的倍率字段。不得把构型层直接乘进表达条目显式倍率后的结果。

**Step 2: Apply the same rule to combo entry copies**

组合技条目显式字段保持优先；构型层只补缺省绝对值和缺省倍率。组合技既有 `VerbPropsResolve` 来源求值仍保留，不把普通芯片逻辑混入组合技来源协议。

### Task 5: 补齐测试、构建并核查边界

**Files:**
- Modify: `Source/BDP.Tests/ComboArmamentFormPropagationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipArmamentFormApplicationBoundarySmokeTests.ps1`（仅在需要同步类型契约时）
- Modify: `日志/Agent工作日志/Agent日志86.md`

**Step 1: Run the new test and existing focused tests**

Run the new layered override test, ordinary armament boundary test, combo propagation test, and Core isolation/build-boundary tests.

**Step 2: Build Core and Content**

使用工作区现有构建方式构建 `BDP.csproj` 与 `BDP.Content.csproj`，确认程序集输出和 Core 隔离检查通过。

**Step 3: Review the diff**

确认没有修改无关资源；确认 XML 字段名保持一致，普通表达条目的缺省 `burstShotCount` 仍按运行时 1 发处理；最后写入倒序工作日志并提交本次变更。
