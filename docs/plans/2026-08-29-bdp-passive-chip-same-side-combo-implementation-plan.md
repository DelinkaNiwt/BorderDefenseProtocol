# BDP 被动芯片激活与同侧组合技 Implementation Plan

> 本计划的初版曾把被动芯片设计为自动激活。该方案已被 `2026-08-29-bdp-passive-chip-manual-control-design.md` 取代，不再作为执行依据；当前执行计划为 `2026-08-29-bdp-passive-chip-manual-control-implementation-plan.md`。本文件保留历史方案，便于追溯初始实现范围。

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不改变普通主副槽手动激活规则、特殊槽隔离规则、独立芯片表达和战斗体 Active（激活）期间禁止装卸规则的前提下，加入按被动类别自动激活的 Main/Sub（主/副）芯片，并让组合技只在规定的合法芯片激活状态之间匹配。

**Architecture:** Core（核心程序集）只提供中性的激活策略扩展口和按槽位过渡支撑，不直接认识 `BDP_ChipCategory_Passive`。Content（内容程序集）注册一个按 `ChipCategoryDef`（芯片类别定义）判断被动类别的提供器。被动芯片仍使用现有 Main/Sub 槽位、身体部位禁用同步、激活条件、Trion（特里恩）扣费、预留和延迟机制；仅把自动过渡上下文改成按槽位保存，避免同侧多个被动请求互相覆盖。表达层继续先生成每个激活来源的独立结果，再由组合解析器按合法候选对逐组匹配 ComboDef（组合技定义）。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6、PowerShell 冒烟测试、`dotnet msbuild`。

---

## 1. 固定不变项与验收边界

先把本次不允许被顺手改动的现状写成测试约束：

- 普通激活仍然最多 Main 一枚、Sub 一枚；普通芯片同侧继续互斥。
- 被动芯片仍然是真正的 Main/Sub 槽位芯片，仍受右手/左手对应身体部位存在性与当前禁用同步约束；自动扫描不能绕过 `IsDisabled`（已禁用）事实。
- 被动芯片由芯片类别是否为 `BDP_ChipCategory_Passive` 判定，不增加新的激活标识字段。
- 被动芯片不能通过玩家交互入口手动激活或关闭；玩家仍可看到槽位状态，但不能得到 Activate/Deactivate（激活/关闭）动作。
- 被动自动激活只由战斗体成功进入 Active 触发；战斗体退出或崩解时沿现有关闭链路清理。
- 激活条件、激活延迟、关闭延迟、激活 Trion 费用和装载预留继续走现有服务；被动只改变触发入口，不建立第二套扣费或条件系统。
- 组合技候选只有：普通 Main + 普通 Sub、普通 Main + 同侧被动 Main、普通 Sub + 同侧被动 Sub。被动 + 被动、跨侧普通 + 被动、Special（特殊槽）参与的组合全部排除。
- 每个合法候选对都独立调用组合匹配表；多个候选对或多个组合结果同时成立时全部生成、显示和保持可用。
- 独立芯片表达继续由现有来源收集器和单侧结果构建器生成，不因组合技成立而删除或替换。
- 装配台 Active 期间拒绝装卸的正常入口不改。底层 `TryLoadChip` 的阶段门禁缺口不在本功能中扩大成“Active 期间允许装载”的行为。

## 2. 先写失败测试，锁定行为形状

### Task 1: 增加被动激活与组合候选规则的冒烟测试

**Files:**

- Add: `Source/BDP.Tests/PassiveChipActivationAndComboAdmissionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyPrimarySlotAutoActivationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ComboDefinitionBoundarySmokeTests.ps1`

**步骤：**

1. 在新测试中断言 Core 存在中性被动判定扩展口，且 Core 源码不硬编码具体被动类别 DefName（定义名）。
2. 断言 Content 启动入口注册类别判定提供器，提供器只读取芯片画像的 `Category`（类别）。
3. 断言自动激活扫描只读取 Main/Sub，不读取 Special；扫描保留普通 Main[0]/Sub[0] 当前路径，并额外枚举同侧所有符合被动类别的槽位。
4. 断言自动激活请求仍调用正式 Trigger（触发体）命令，而不是直接写 `SetActive(true)`，以保留条件、延迟、Trion 和预留链。
5. 断言交互解释器对被动槽位不产生玩家 Activate/Deactivate 动作。
6. 断言同侧普通与被动不因默认控制范围互相阻挡，普通与普通仍阻挡；显式 `ActivationExclusionGroups`（激活互斥组）仍然有效。
7. 断言组合解析器枚举 `GetActiveSlots()`（全部激活槽位），而不是继续只调用主副侧各一个 `GetActiveSlot()`（单值读取）。
8. 断言组合候选包含三类允许组合，排除被动+被动、跨侧普通+被动和 Special。
9. 断言组合结果来源列表、第一/第二来源绑定、唯一结果标识和每条定义的全部表达条目均被保留。
10. 断言独立表达构建链和 Active 装卸入口没有被本功能改成新的分支。

**验证：**

```powershell
pwsh -NoProfile -File Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyPrimarySlotAutoActivationSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1
```

测试在实现前应因缺少新接口、候选枚举和来源绑定而失败；记录失败点后再进入实现。

## 3. 建立 Core 中性的芯片激活策略扩展口

### Task 2: 提供被动类别判定的可插拔边界

**Files:**

- Add: `Source/BDP/Core/Trigger/External/ITriggerActivationPolicyProvider.cs`
- Add: `Source/BDP/Core/Trigger/External/TriggerActivationPolicyRegistry.cs`
- Add: `Source/BDP.Content/Trigger/Activation/PassiveTriggerActivationPolicyProvider.cs`
- Modify: `Source/BDP.Content/ContentBootstrap.cs`

**步骤：**

1. Core 定义只回答“某枚芯片是否属于自动激活类别”的提供器接口和注册表；注册表没有提供器时返回旧行为所需的默认值。
2. Content 提供器通过 `ChipSurfaceAccess`（芯片读取表面）读取正式芯片画像，并比较 `Profile.Category.defName` 与现有被动类别 DefName；不新增配置字段、不新增激活标识。
3. Content 启动时一次注册提供器，注册逻辑保持幂等，避免静态启动重复注册。
4. Core 的切换、交互、战斗体自动扫描和组合候选筛选统一从注册表取判定，不在 Core 内写具体内容类别。

## 4. 让被动芯片可以在同侧并存，同时保留普通互斥

### Task 3: 把自动过渡上下文从“每侧一份”补成“自动槽位各自一份”

**Files:**

- Modify: `Source/BDP/Core/Trigger/Switching/Model/SwitchContext.cs`
- Modify: `Source/BDP/Core/Trigger/State/TriggerSlotState.cs`
- Modify: `Source/BDP/Core/Trigger/Switching/Flow/TriggerSwitchService.cs`
- Modify: `Source/BDP/Core/Trigger/Switching/Flow/TriggerSwitchTransitionService.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`

**步骤：**

1. 在 `TriggerSlotState` 增加可存档的自动过渡上下文引用，明确它是 Waiting/Activating/Deactivating（等待/激活中/关闭中）的过程数据，不是新的激活状态；正式激活真值仍只有 `IsActive`。
2. 保留现有 `mainSwitchContext/subSwitchContext/specialSwitchContext`，它们继续服务普通玩家切换、成对主副手同步和特殊槽交互，不把普通控制模型改成多选。
3. 从现有过渡服务抽出可复用的“单槽自动激活/单槽自动关闭”推进路径：
   - 先使用现有 `EvaluateActivationRequirements`（激活条件求值）；
   - 再使用现有阻挡者与显式激活互斥组判断；
   - 无阻挡时按现有激活延迟进入正式 `SetActive` 提交；
   - 正式提交仍通过 `NotifySlotActivationCommitted`，由现有 Trion 扣费与投影发布链处理；
   - 关闭按现有关闭延迟和 `NotifySlotDeactivated` 发布。
4. 自动过渡读取和写回按 `TriggerSide + slotIndex` 定位，确保同侧多个被动各自持有上下文，不会互相覆盖。
5. 普通目标与普通目标仍使用原来的每侧上下文和控制范围；只有当同侧两者至少一枚由策略判定为被动时，才跳过默认“同侧控制范围互斥”。显式 `ActivationExclusionGroups` 冲突仍优先成立。
6. 被动目标不参与成对普通切换的“让位”语义；若未来某个被动定义形成绑定槽，继续沿用绑定槽的现有原子规则，不为本需求另造绑定语义。
7. 读取 `GetActiveSlot(side)` 的普通控制用途改为优先返回该侧可手动控制的普通激活槽；全部激活事实继续只通过 `GetActiveSlots()` 暴露，避免被动占据低索引后遮蔽普通槽。

### Task 4: 接通被动自动激活、统一关闭和持续条件复查

**Files:**

- Modify: `Source/BDP/Core/CombatBodySession/CombatBodyActivationTransaction.cs`
- Modify: `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.ActivationRequirements.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerActivationRequirementMonitor.cs`
- Modify: `Source/BDP/Core/Trigger/Switching/Flow/TriggerDisableSync.cs`
- Modify: `Source/BDP/Core/Trigger/Access/Contracts/ITriggerLoadoutCommands.cs`
- Modify: `Source/BDP/Core/Trigger/Access/Surfaces/TriggerFormalSurfaces.cs`

**步骤：**

1. 保留现有战斗体进入 Active 后的 Special 自动激活和普通 Main[0]/Sub[0] 自动激活顺序。
2. 在普通自动激活之后，扫描正式 Main/Sub 槽位中的全部被动根槽；跳过空槽、镜像槽、已激活槽和 `IsDisabled` 槽；每个候选都提交正式激活请求。
3. 自动激活请求必须经过现有相位检查；不把“Active 期间底层可直接装载”纳入新功能。
4. 战斗体 Release（主动退出）时，普通和 Special 仍沿用原有侧级关闭；所有已激活被动按精确槽位提交关闭，避免每侧只关闭第一枚。
5. 战斗体 Collapse（崩解）进入不可用状态时，现有禁用同步继续立即关闭全部激活槽，并额外清理各被动槽的自动过渡上下文，防止到期后又被提交激活。
6. 持续激活条件复查按真实激活根槽逐枚处理；失效的普通或被动槽都按精确槽位关闭，不能因同侧存在多个激活槽而误关另一枚。
7. 为精确槽位关闭补充内部正式命令入口，但不把它接入玩家按钮；原有玩家侧级关闭入口保持不变。
8. 禁用同步、读档恢复、卸载清理和投影刷新均继续以 `IsActive` 为唯一正式激活真值，不保存或读取额外“被动已激活”标识。

## 5. 禁止被动芯片进入玩家手动交互

### Task 5: 收紧交互解释器而不改变普通槽按钮

**Files:**

- Modify: `Source/BDP/Core/Trigger/Interaction/TriggerInteractionInterpreter.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Contexts.cs`

**步骤：**

1. 交互解释器在读取到被动类别芯片后返回信息性状态，不产生可用的 Activate/Deactivate/ SwitchTo（激活/关闭/切换）动作。
2. 侧级交互读取只把普通激活槽作为玩家控制槽；若一侧只有被动激活，不能伪造一个“可关闭当前芯片”的侧级动作。
3. 普通槽位的空槽、禁用、条件不足、切换中和成对绑定提示继续沿原逻辑输出。
4. 不新增玩家文案；继续使用现有原因码和语言键，避免把内部激活策略泄露成新的 UI 业务分支。

## 6. 组合技改为合法候选对枚举，并传递 ComboDef 来源顺序

### Task 6: 让匹配器返回第一/第二来源的实际绑定

**Files:**

- Add: `Source/BDP/Core/Combos/Access/ComboSourceBinding.cs`
- Modify: `Source/BDP/Core/Combos/Access/ComboDefinitionReadResult.cs`
- Modify: `Source/BDP/Core/Expressions/Runtime/ComboRuntimeIndex.cs`

**步骤：**

1. 给一次具体匹配结果增加 `FirstSourceChip/SecondSourceChip`（第一/第二来源芯片）绑定。
2. `ComboRuntimeIndex` 保留现有正向、反向准入判断；正向命中时绑定原始参数顺序，反向命中时交换绑定顺序。
3. 组合定义读取 `Read(ComboDef)` 的普通路径不构造实例来源绑定；只有 `FindMatch` 的运行时匹配结果附带本次具体来源绑定。
4. 保留同一来源键组合多份定义同时通过时的既有歧义拒绝，不把“全部合法候选对都生成”误解成“同一候选对绕过定义歧义”。

### Task 7: 重写组合候选与来源结果构建

**Files:**

- Modify: `Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ComboFormalExpressionResolution.cs`
- Modify: `Source/BDP/Core/Expressions/Pipeline/ComboFormalExpressionResultFactory.cs`
- Modify: `Source/BDP/Core/Expressions/Model/CompositeExpressionReference.cs`
- Modify: `Source/BDP/Core/Expressions/Model/ExpressionPublishedCompositeReference.cs` only if公开投影需要同步注释或字段，否则不改

**步骤：**

1. 组合解析器从 `GetActiveSlots()` 收集三类候选：普通 Main、普通 Sub、被动 Main、被动 Sub；Special 直接丢弃。
2. 只生成以下候选对，并保持实际传入顺序：
   - 普通 Main → 普通 Sub；
   - 普通 Main → 被动 Main；
   - 普通 Sub → 被动 Sub。
3. 对每个候选对调用一次 `ComboSurfaceAccess.FindMatch`；匹配失败只影响该候选对，不影响其它候选对和独立表达。
4. 根据 `ComboSourceBinding` 把第一/第二来源材料和正式结果精确回溯到对应槽位；不再用整侧第一条材料、整侧第一条 PrimaryVerb（主动作结果）代替实际来源。
5. `ComboFormalExpressionResolution` 内部字段改为 First/Second 语义，组合属性、执行节奏、TrionResolve（特里恩费用求值）和 `FollowFirstSource/FollowSecondSource` 都只消费这两个实际来源。
6. 组合引用的 `SourceResultIds` 按第一、第二来源顺序写入；现有 `MainSourceResultId/SubSourceResultId` 对 Combo 继续作为顺序别名，DualWeapon（双持）仍保持原本的主副侧语义，避免扩大公开投影结构。
7. 组合结果 ID（标识）增加稳定来源对标识，至少能区分同一个 ComboDef/条目由不同被动芯片候选对生成的多个结果；否则投影索引会发生覆盖。来源对标识使用芯片 ThingID（实例标识），不使用显示名称。
8. 每个 ComboDef 的全部合法表达条目都逐条生成；不因同侧来源重复而只保留第一条。
9. 不修改 `ExpressionSourceCollector`、`SingleSideExpressionBuilder`、`ExpressionSnapshotBuilder` 的独立来源生成职责；组合成立后独立结果仍保留。

## 7. 补充测试并构建验证

### Task 8: 为状态组合和结果引用补测试

**Files:**

- Modify: `Source/BDP.Tests/PassiveChipActivationAndComboAdmissionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyExitSemanticsSmokeTests.ps1` if exact-slot close assertion needs to be added
- Add or modify: `Source/BDP.Tests/ComboSourceBindingSmokeTests.ps1`
- Add or modify: `Source/BDP.Tests/TriggerActiveSlotSelectionSmokeTests.ps1`

**必须覆盖：**

- 普通 Main + 普通 Sub：组合成立。
- 普通 Main + 同侧被动 Main：组合成立。
- 普通 Sub + 同侧被动 Sub：组合成立。
- 被动 + 被动：不匹配。
- 普通 + 对侧被动：不匹配。
- Special + 任意：不匹配。
- 同侧多个被动全部通过各自激活条件时，不覆盖彼此的自动过渡上下文，最终全部 `IsActive`。
- 战斗体退出时同侧多个被动全部关闭；条件失效时关闭精确失效槽。
- 被动只要对应手部缺失/槽位已禁用，就不自动激活。
- 被动槽不生成玩家可用 Activate/Deactivate 动作。
- 同一个 ComboDef/条目由两组不同来源对命中时，结果 ID、来源列表和手动入口 ID 不冲突。
- 组合条目中的 `FollowFirstSource`、`FollowSecondSource`、显式属性覆盖、TrionResolve 仍按组合表第一/第二来源绑定。
- 独立芯片结果仍然存在，Combo 结果只是附加结果。

### Task 9: 运行验证命令

```powershell
pwsh -NoProfile -File Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ComboSourceBindingSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyPrimarySlotAutoActivationSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyExitSemanticsSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ChipCombinationSelectionRulesSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ComboSourceAdmissionRuntimeSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ExpressionActivationCostOwnershipSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1
dotnet msbuild Source\BDP\BDP.csproj -p:Configuration=Release -t:Build -v:minimal
dotnet msbuild Source\BDP.Content\BDP.Content.csproj -p:Configuration=Release -t:Build -v:minimal
git diff --check
```

构建后需要检查：

- Core 未新增对 Content 的程序集依赖；
- Development 未被正式程序集依赖；
- 普通手动切换与特殊槽自动激活原有测试仍通过；
- 工作区已有的无关 DLL、日志和测试脚本修改不被覆盖或回退。

## 8. 日志与版本控制

### Task 10: 记录本次实现并提交

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志\Agent日志94.md`；若超过20条则新建下一个日志文件

**步骤：**

1. 记录实际改动文件、测试命令、构建结果、未改动边界和任何剩余风险。
2. 只暂存本次功能实际创建/修改的源文件、测试和计划/日志文件；保留用户已有的 DLL、PDB、日志和未跟踪脚本变更。
3. `git diff --check` 和相关测试通过后，创建一个单独提交，便于回退；不创建新分支。

## 9. 明确不做的事情

- 不把所有激活芯片都丢进组合匹配。
- 不让 Special 槽参与组合技。
- 不让被动+被动或跨侧普通+被动形成组合技。
- 不新增 `IsPassiveActive` 之类的第二套激活真值。
- 不把被动芯片改成 Special 槽，也不绕过 Main/Sub 身体部位禁用。
- 不改变独立表达的保留策略。
- 不把 Active 期间底层装载缺口扩展成正式支持路径；若后续确认需要修复，该问题单独按装配入口根因处理。
- 不修改与本功能无关的武装型、视觉、攻击执行或语言文案逻辑。
