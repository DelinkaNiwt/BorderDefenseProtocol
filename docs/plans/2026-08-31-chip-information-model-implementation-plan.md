# BDP 芯片信息解析模型实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在 BDP Core.dll 中建立独立、通用、中性的芯片信息解析模型，使表达系统、芯片制造台和触发体装配台共同消费同一份只读信息模型；彻底清理早期废弃的 `ExpressionInfoProjection` 信息层，不做兼容迁移。

**Architecture:** 以有效芯片定义、正式表达事实和运行状态组成中性 `ChipInformationParseInput`，由无状态 `ChipInformationParser` 通过通用规则发现公开信息块，生成同时包含 `Schema`、解析值、状态、来源链和诊断的 `ChipInformationModel`。Core.dll 只提供模型、规则契约、通用规则和注册机制；Content.dll 只负责把自己的成型业务结果转换成中性输入或注册脱离单一芯片的扩展规则；三个消费者不进入解析器依赖关系。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 `Assembly-CSharp`、现有 PowerShell 冒烟测试、MSBuild。

**Working rules:** 不创建 worktree，不创建新分支；直接在当前项目执行。每个实现任务先增加或修改失败测试，运行并确认失败，再写生产代码。只暂存本计划涉及的文件，不暂存已有 PSD/PNG 贴图改动。

---

## Task 1: 建立红灯契约测试与旧层清理清单

**Files:**

- Create: `Source/BDP.Tests/ChipInformationModelContractSmokeTests.ps1`
- Create: `Source/BDP.Tests/ChipInformationGenericRuleSmokeTests.ps1`
- Create: `Source/BDP.Tests/ChipInformationProvenanceSmokeTests.ps1`
- Create: `Source/BDP.Tests/ChipInformationConsumerBoundarySmokeTests.ps1`
- Inspect: `Source/BDP/Core/Expressions/Model/ExpressionInfoProjection.cs`
- Inspect: `Source/BDP/Core/Expressions/Model/ExpressionInfoProjectionEntry.cs`
- Inspect: `Source/BDP/Core/Expressions/Projection/DefaultExpressionInfoProjector.cs`
- Inspect: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Inspect: `Source/BDP/Core/Trigger/Runtime/TriggerPresentationState.cs`
- Inspect: `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs`

### Step 1: 先写失败测试

测试先要求新模型的核心文件、类型和规则入口存在：

- `ChipInformationModel`、`ChipInformationParseInput`、`ChipInformationBlock`、`ChipInformationField`；
- `ChipInformationSchema` 与字段类型 / 状态；
- `ChipInformationProvenance` 能表示基础定义、武装构型、运行结果和最终值；
- `IChipInformationRule`、规则注册表和统一解析入口；
- 新模型源码不出现芯片身份特判；
- 新模型不引用制造台、装配台窗口或 UI 绘制类型。

此时新类型尚不存在，运行测试必须红灯，记录真实失败原因后才能进入下一步。

### Step 2: 固化旧层清理清单

确认以下内容属于待删除或替换范围：

- `ExpressionInfoProjection` 及其条目模型；
- `DefaultExpressionInfoProjector`；
- `IExpressionReader.GetInfoProjection` 和对应实现；
- `TriggerPresentationState.InfoProjection` 及构建调用；
- 只为旧信息投影服务的空模型、复制和诊断辅助；
- 仅断言旧信息层存在的测试。

保留与战斗正式结果有关的 `ExpressionPublishedProjectionSnapshot`、`ExpressionPublishedResultSnapshot` 和 `RangedProjectionSeed.InfoProjectionTags`；它们不是新的芯片信息模型，不能误删。

### Step 3: 运行红灯测试

运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationModelContractSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationGenericRuleSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationProvenanceSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationConsumerBoundarySmokeTests.ps1
```

预期：因为生产类型尚未创建而失败，不允许跳过红灯直接实现。

## Task 2: 建立 Core 中性信息模型基础类型

**Files:**

- Create: `Source/BDP/Core/Information/Model/ChipInformationModel.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationParseInput.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationSchema.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationBlockSchema.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationFieldSchema.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationBlock.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationField.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationContext.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationValue.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationValueKind.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationValueState.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationProvenance.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationSourceKind.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationContributionKind.cs`
- Create: `Source/BDP/Core/Information/Model/ChipInformationDiagnostic.cs`

### Step 1: 实现最小只读结构

模型使用只读集合和冻结后的值快照，不暴露可变 `Thing`、`VerbProperties`、`Def` 或 UI 对象。

必须支持：

- 公开 Schema 与当前解析值同时存在；
- 信息块和字段按稳定语义键枚举 / 查询；
- 明确的值类型、单位键和值状态；
- 形态 / 姿态和 Ability 子结果的嵌套上下文；
- 诊断独立于玩家信息模型。

### Step 2: 实现类型安全的值容器

初始值类型为文本、布尔、整数、数值、百分比、持续时间、枚举和列表。禁止用一个无约束的 `Dictionary<string, object>` 代替字段模型。

解析器输出语言包键和标准单位，不输出已本地化的长文本。

### Step 3: 运行契约测试

此时契约测试应从“类型缺失”推进到模型结构和字段类型检查。若失败，只修模型边界，不提前实现规则。

## Task 3: 建立规则契约、注册表和合并引擎

**Files:**

- Create: `Source/BDP/Core/Information/Rules/IChipInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/ChipInformationRuleRegistry.cs`
- Create: `Source/BDP/Core/Information/Rules/ChipInformationRuleContext.cs`
- Create: `Source/BDP/Core/Information/Rules/ChipInformationContribution.cs`
- Create: `Source/BDP/Core/Information/Rules/ChipInformationMergeService.cs`
- Create: `Source/BDP/Core/Information/ChipInformationParser.cs`
- Create: `Source/BDP/Core/Information/ChipInformationService.cs`

### Step 1: 先补规则红灯用例

在 `ChipInformationGenericRuleSmokeTests.ps1` 中加入通用行为断言：

- 同一远程结构被两个不同身份输入复用同一规则；
- 无远程结构时不生成远程块；
- 同一字段的不同来源按固定优先级合并；
- 未知扩展只进入诊断；
- 规则源码不读取 `DefName`、名称或 `ThingID` 做结构分支。

### Step 2: 实现规则接口和注册表

`IChipInformationRule` 只提供：

```text
CanApply(input)
DescribeSchema(input)
Resolve(input, schema)
```

注册表检查规则 ID 唯一性、核心规则不可静默替换和稳定排序。Content 扩展规则只能针对中性契约注册。

### Step 3: 实现贡献合并

合并服务按 `BlockKey` / `FieldKey` 收拢贡献：

- 相同键和值：合并来源；
- 有优先级差异：使用高优先级值并记录被覆盖贡献；
- 无法裁定：标记 `Conflict` 并写诊断；
- 不允许依赖规则注册顺序静默覆盖。

固定来源优先级：

```text
RuntimeResult > EffectiveDefinition > DeclaredDefinition > DerivedFact
```

### Step 4: 实现安全解析流程

解析器按“能力发现 → 上下文发现 → 规则匹配 → Schema → 值与来源 → 合并校验 → 输出模型”执行。

加入稳定排序、递归深度限制、循环引用检测和单规则失败隔离。信息解析失败不能影响正式战斗结果。

### Step 5: 运行通用规则测试

预期：通用输入结构测试通过；芯片身份变更不会改变规则选择；缺失数据不会制造空字段或假值。

## Task 4: 建立中性输入适配层

**Files:**

- Create: `Source/BDP/Core/Information/Input/ChipInformationDefinitionInputAdapter.cs`
- Create: `Source/BDP/Core/Information/Input/ChipInformationExpressionInputAdapter.cs`
- Create: `Source/BDP/Core/Information/Input/ChipInformationRuntimeInputAdapter.cs`
- Modify: `Source/BDP/Core/Chips/External/IChipInstanceDefinitionProvider.cs` only if新模型需要补充中性来源接口
- Modify: `Source/BDP/Core/Chips/External/ChipInstanceSurfaceAccess.cs` only if需要把有效定义转换成信息输入
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionPublishedSnapshotBuilder.cs` only if需要补足公开表达事实

### Step 1: 先写输入边界测试

增加断言：

- 普通芯片走静态有效定义；
- 成品芯片走 `CompManufacturedChip` 提供的有效配置；
- 激活表达输入只来自正式激活槽位；
- 适配器不把制造台 / 装配台窗口对象传入 Core；
- 输入保留芯片身份用于输出追踪，但规则看不到身份匹配入口。

### Step 2: 实现定义输入适配

制造台和装配台均使用有效芯片定义输入。制造业务的组合解析类型由 Content 转成 Core 能识别的有效定义和来源记录，Core 不引用 `ChipArmamentFormDef`。

### Step 3: 实现表达与运行输入适配

表达系统提供正式表达事实、激活状态、条件结果、形态 / 姿态状态和运行来源。信息模型不能替代 `ExpressionSnapshot` 的战斗真值，也不能参与攻击计算。

### Step 4: 运行输入边界测试

预期：定义预览和运行信息可以使用同一解析器，但不会互相携带窗口或消费者专属状态。

## Task 5: 实现第一版通用规则

**Files:**

- Create: `Source/BDP/Core/Information/Rules/ProfileInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/LoadoutInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/TrionInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/ActivationRequirementInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/RangedVerbInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/MeleeToolInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/AbilityInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/HediffInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/PassiveInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/ContextInformationRule.cs`
- Create: `Source/BDP/Core/Information/Rules/VariantProvenanceRule.cs`
- Create or modify: neutral defense / extension rule files only after确认对应中性事实契约
- Modify: `Languages/ChineseSimplified (简体中文)/LanguageInfo.xml` or对应语言文件，加入所有新增 `LabelKey`、单位键和状态文本

### Step 1: 先补每条规则的失败用例

测试使用匿名的结构化输入，不使用“小行星”“光魂”等真实芯片名称，不建立样本专属断言。

覆盖：

- 共同身份、装载、Trion 和激活条件；
- 远程 Verb 的确认字段白名单；
- 近战 Tool 列表；
- Ability 父块和递归子结果；
- Hediff 持续时间与效果汇总；
- Passive 的已注册通用字段；
- 形态 / 姿态嵌套上下文；
- 缺失字段省略；
- 未知字段诊断。

### Step 2: 逐条实现规则

规则只读取中性契约和事实，不读取原始私有字段，不将整个 Def 对象倾倒进模型。

### Step 3: 接入武装构型来源链

最终字段只输出最终值，但附带：

```text
基础值 → 构型操作 → 有效值 → 运行结果
```

支持替换、加法、倍率和派生贡献。构型来源记录使用通用 `SourceKind` / `ContributionKind`，不写武装类型专属分支。

### Step 4: 运行规则测试

预期：不同结构输入自动生成不同信息块；同一结构的芯片无需新增规则；构型基础值和构型值可以被消费者查询但不重复污染默认最终字段。

## Task 6: 建立公开读取面并接入表达系统

**Files:**

- Create: `Source/BDP/Core/Information/Access/IChipInformationReader.cs`
- Create: `Source/BDP/Core/Information/Access/ChipInformationSurfaceAccess.cs`
- Create: `Source/BDP/Core/Information/Access/ChipInformationPublication.cs` if运行时发布需要独立快照
- Modify: `Source/BDP/Core/Expressions/Access/Contracts/IExpressionReader.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/Expressions/Runtime/ExpressionRuntimeRepository.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerPresentationState.cs` only to移除旧 `InfoProjection`
- Modify: `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs` only to移除旧信息投影构建

### Step 1: 先写公开读取边界测试

断言：

- 外部消费者只能得到只读模型；
- 可以枚举 Schema、信息块、字段、上下文和来源链；
- 不暴露原始 `VerbProperties` / `Thing` 可变引用；
- 没有按消费者命名的解析入口；
- 信息模型不会进入攻击判定或正式状态同步路径。

### Step 2: 实现通用读取面

公开读取面只提供统一的 `Read(ChipInformationParseInput)` 或等价中性入口。必要时提供按有效输入指纹缓存，但缓存属于信息服务，不属于窗口。

### Step 3: 替换表达系统旧信息出口

表达系统使用新模型发布当前激活芯片的信息集合。运行时战斗投影和信息模型保持并行：

```text
正式表达事实 → 战斗 / 视觉 / 宿主投影
             ↘ 芯片信息解析模型
```

删除旧 `ExpressionInfoProjection` 依赖，不把新模型塞回旧类名或旧字段语义。

### Step 4: 运行 Core 编译与表达边界测试

先验证 Core.dll 可以独立编译，并通过 Core 依赖隔离检查；再验证表达系统仍能生成正式战斗结果。

## Task 7: 让制造台和装配台成为模型消费者

**Files:**

- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewBuilder.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewModel.cs` only to适配通用字段 / 来源模型
- Modify: `Source/BDP.Content/Assembly/Window/Panel_ChipDetail.cs`
- Modify: `Source/BDP.Content/Assembly/Window/TriggerAssemblyPreviewService.cs` if其承担芯片详情拼装
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewPanel.cs` only to消费转换后的信息模型

### Step 1: 先写消费者边界测试

断言：

- 制造台和装配台调用统一信息读取面；
- 两者不直接解析 `VerbProperties`、`ChipArmamentFormDef` 或表达条目字段；
- 视图模型只负责把通用模型转换为已有界面需要的结构；
- 信息解析器源码不出现窗口类名；
- 预览消费者可以读取最终值和来源链。

### Step 2: 迁移制造台预览

制造台只把当前有效成型配置适配为输入，再消费模型中的 Schema、最终字段和构型来源。保留现有 UI 的必要视觉转换，但删除 `ChipManufacturingPreviewBuilder` 内对具体字段的重复解析。

### Step 3: 迁移装配台详情

装配台选中成品芯片后，通过统一读取面获得模型；详情绘制器只枚举块和字段，不按芯片身份改变布局。装卸事务、槽位操作和真实状态仍由装配系统负责。

### Step 4: 运行 Content 消费者测试

预期：同一芯片信息模型可以被两个预览消费者读取；改变消费者名称或界面实现不会影响解析结果。

## Task 8: 删除旧信息层并迁移测试 / 文档

**Files:**

- Delete after新读取面通过测试：
  - `Source/BDP/Core/Expressions/Model/ExpressionInfoProjection.cs`
  - `Source/BDP/Core/Expressions/Model/ExpressionInfoProjectionEntry.cs`
  - `Source/BDP/Core/Expressions/Projection/DefaultExpressionInfoProjector.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Contracts/IExpressionReader.cs`
- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerPresentationState.cs`
- Modify: `Source/BDP/Core/Trigger/Projection/TriggerPresentationBuilder.cs`
- Modify or retire tests that only验证旧信息层存在
- Modify: relevant architecture docs if旧层路径仍被描述为现行入口

### Step 1: 先运行全局旧符号检查

使用：

```powershell
rg -n -S "ExpressionInfoProjection|DefaultExpressionInfoProjector|GetInfoProjection|InfoProjection" Source\BDP Source\BDP.Content Source\BDP.Development Source\BDP.Tests -g '*.cs' -g '*.ps1'
```

区分必须保留的 `InfoProjectionTags` 战斗字段和必须删除的旧信息层符号，禁止用大范围替换误删正式结果代码。

### Step 2: 删除旧层

只有新公开读取面、Core 编译和表达边界测试通过后，才删除旧文件和旧调用链。

### Step 3: 更新测试与语言包边界

旧测试改为验证新模型契约，不保留“旧投影字段必须存在”的断言。所有新增玩家可见文本必须来自语言包，代码只保存语言键。

## Task 9: 完整验证、日志与提交

**Files:**

- Modify: `日志/Agent工作日志/Agent日志123.md`
- No changes: existing PSD/PNG texture files

### Step 1: 运行测试

至少运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationModelContractSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationGenericRuleSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationProvenanceSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipInformationConsumerBoundarySmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\CoreModAssemblyIsolationVerifierSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ExpressionPublishedProjectionSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChipManufacturingPreviewSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\TriggerAssemblyContentBoundarySmokeTests.ps1
```

### Step 2: 编译三套程序集

按项目现有环境执行 Core、Content、Development 的 Release 或 Debug 编译，并确认：

- Core.dll 不依赖 Content.dll / Development.dll；
- Content.dll 可以引用 Core 新模型；
- Development.dll 不被其它程序集依赖；
- 输出目录只出现本次编译结果。

### Step 3: 做边界检查

验证：

- 新信息规则中没有芯片身份分支；
- 新模型没有窗口 / UI 引用；
- 未知字段只进入诊断；
- 构型基础值和构型来源可以读取；
- 最终值默认不重复显示基础值和构型值；
- `git diff --check` 通过；
- 贴图工程改动保持未暂存。

### Step 4: 写倒序工作日志

记录本次完成的模型、规则、来源链、三方消费者接入、旧层删除和验证结果。日志文件超过20条时新建下一份日志，不改写历史记录。

### Step 5: 分阶段提交

至少保留以下可回退提交边界：

1. Core 信息模型、规则和测试；
2. 表达系统读取面与旧层替换；
3. Content 两个消费者接入；
4. 最终清理、验证、语言包和日志。

每次提交前检查暂存区，只包含当前阶段文件。
