# [组合技来源形态姿态筛选] Implementation Plan
> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

## Goal

让组合技来源可以按来源芯片的运行时形态和姿态进行可选匹配：

- 未填写形态：该来源芯片的任意形态都可匹配。
- 填写形态、未填写姿态：该形态下的任意姿态都可匹配。
- 同时填写形态和姿态：只匹配指定组合。
- 只填写姿态：定义校验失败，避免姿态脱离形态产生歧义。

将 `BDP_Combo_LightSoulPropulsion` 限制为光魂“大盾”形态，盾形态的 `mobile` 和 `guard` 两个姿态均可匹配，`heavy_blade`（重刃）不匹配。

## Architecture

### 配置与契约

新增中性的 `ComboSourceScopeConfig`（配置侧）和 `ComboSourceScopeContract`（运行侧契约），分别挂在组合技的 `FirstSourceScope`、`SecondSourceScope`。

范围筛选与已有 `ComboSourceAdmission`（来源准入）保持独立。准入继续负责芯片身份/变体/标签等条件，范围负责当前模式与姿态。

### 运行时

复用现有 `ITriggerLoadoutReader.GetChipModeKey`、`GetChipStanceKey`，由表达式解析器在组合候选匹配时读取当前状态，并通过组合技服务/表面访问层传入 `ComboRuntimeIndex`。保持现有无序芯片来源索引、正反来源尝试和多定义歧义保护，不引入优先级。

### 数据定义

只给光魂推进组合技增加：

```xml
<FirstSourceScope>
  <ModeKey>shield</ModeKey>
</FirstSourceScope>
```

不写 `StanceKey`，因此盾形态下的所有姿态都允许。

## Tech Stack

- C#（C#语言）/.NET：BDP.Core 组合技定义、契约和运行时匹配。
- XML（可扩展标记语言）：组合技 Def 配置。
- PowerShell（Windows脚本）：现有边界、定义和运行时烟雾测试。
- MSBuild：Core 与 Content 构建验证。

## Implementation Tasks

### Task 1: 建立失败测试（TDD）

**Files:**
- Add `Source/BDP.Tests/ComboSourceScopeDefinitionSmokeTests.ps1`
- Add `Source/BDP.Tests/ComboSourceScopeRuntimeSmokeTests.ps1`
- Modify `Source/BDP.Tests/LightSoulPropulsionDefinitionSmokeTests.ps1`

**Checks:**
- 新增配置/契约类型和组合定义字段存在。
- 姿态必须依赖形态的定义校验入口存在。
- 运行时匹配能区分 `shield` 与 `heavy_blade`，且不填写姿态时允许 `mobile`、`guard`。
- 光魂推进 XML 明确声明 `ModeKey=shield`，不声明姿态限制。

**Verification:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ComboSourceScopeDefinitionSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ComboSourceScopeRuntimeSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
```

Expected result before production changes: the new scope tests fail because the scope configuration, contract, and runtime plumbing do not yet exist; the LightSoul test fails because its Def has no scope.

### Task 2: 实现组合技来源范围配置与契约

**Files:**
- Add `Source/BDP/Core/Combos/Config/ComboSourceScopeConfig.cs`
- Add `Source/BDP/Core/Combos/Contract/ComboSourceScopeContract.cs`
- Modify `Source/BDP/Core/Combos/Defs/ComboDef.cs`
- Modify `Source/BDP/Core/Combos/Contract/ComboDefinitionConfig.cs`
- Modify `Source/BDP/Core/Combos/Contract/ComboDefinitionContract.cs`
- Modify `Source/BDP/Core/Combos/Contract/ComboDefinitionContractResolver.cs`
- Modify `Source/BDP/Core/Combos/Validation/ComboDefinitionValidator.cs`

**Details:**
- 两侧来源独立配置，保持来源顺序语义与已有 admission 逻辑一致。
- 契约解析时保留空范围为 `null`/通配语义。
- 校验非空 `StanceKey` 必须同时有非空 `ModeKey`。
- 成员逐项添加简体中文注释，不能把新的玩家文案硬编码进逻辑。

### Task 3: 传递运行时形态/姿态并参与匹配

**Files:**
- Modify `Source/BDP/Core/Combos/Runtime/ComboSourceAdmissionSnapshot.cs`
- Modify `Source/BDP/Core/Combos/Runtime/ComboSourceAdmissionEvaluator.cs`
- Modify `Source/BDP/Core/Expressions/Runtime/ComboRuntimeIndex.cs`
- Modify `Source/BDP/Core/Combos/Access/ComboDefinitionService.cs`
- Modify `Source/BDP/Core/Expressions/Access/ComboSurfaceAccess.cs`
- Modify `Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`

**Details:**
- 在已有来源快照中携带当前 `ModeKey`、`StanceKey`，保留旧入口的无状态兼容行为。
- 新匹配入口接收双方当前状态；旧入口继续支持未提供状态的调用方。
- 正向/反向来源尝试均使用对应来源侧的 scope。
- 先做准入，再做范围；任一条件不满足即淘汰候选。
- 保持同一芯片来源对多个定义同时命中时返回歧义，不新增优先级机制。

### Task 4: 更新光魂推进 Def 并定向验证

**Files:**
- Modify `1.6/Content/Defs/ComboDef/LightSoulPropulsion.xml`

**Verification:**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source/BDP.Tests\ComboSourceScopeDefinitionSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source/BDP.Tests\ComboSourceScopeRuntimeSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source/BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
dotnet msbuild .\Source\BDP\BDP.csproj -p:Configuration=Release -p:UseSharedCompilation=false -t:Build -v:minimal
dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Release -p:UseSharedCompilation=false -t:Build -v:minimal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source\Build\Verify-BdpCoreIsolation.ps1 -CoreProjectPath .\Source\BDP\BDP.csproj -CoreAssemblyPath .\1.6\Assemblies\BDP.Core.dll -ModSourceRoot .\Source -CoreSourceRoot .\Source\BDP -CoreDefsRoot .\1.6\Defs
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Source\Build\Verify-BdpDevelopmentBoundary.ps1 -ModRoot (Get-Location)
```

### Task 5: 收尾

- 运行 `git diff --check`，确认无空白错误。
- 检查 `git status --short`，只暂存本次组合技功能涉及的文件，不触碰用户已有贴图改动。
- 在 `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志` 写入倒序工作日志；单个日志文件不超过20条。
- 提交本次实现，提交信息使用简体中文/英文简短说明。

## TDD 顺序

每个行为先增加会失败的烟雾测试并运行确认失败，再写最小生产改动使测试通过，最后运行完整构建和边界检查；不以测试脚本替代实际组合运行时的准入与范围逻辑。

## Commit

建议拆为两个提交：

1. `test: add combo source scope smoke tests`
2. `feat: support combo source mode and stance scopes`

如测试与实现在当前工作区一次性完成，也至少保持计划/文档、测试、生产实现的文件边界清晰。
