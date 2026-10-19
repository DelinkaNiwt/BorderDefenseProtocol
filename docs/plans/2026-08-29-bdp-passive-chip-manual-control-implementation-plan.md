# BDP 被动芯片手动控制 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将被动类别 Main/Sub 芯片收口为玩家手动激活和关闭，同时保留同侧默认互斥豁免、身体部位禁用约束、显式互斥组和既定组合技准入规则。

**Architecture:** 继续复用现有 Trigger 正式激活、关闭、条件、延迟、Trion 和投影发布链。删除本次功能中专门为被动芯片增加的自动激活路径；保留一个中性的芯片类别判定扩展点，仅供同侧互斥和组合候选筛选使用。被动芯片的正式激活真值仍只有 `TriggerSlotState.IsActive`。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6、PowerShell 冒烟测试、`dotnet msbuild`。

---

## Task 1: 先更新失败测试，锁定手动控制语义

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\CombatBodyPrimarySlotAutoActivationSmokeTests.ps1`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\TriggerActiveSlotSelectionSmokeTests.ps1`

**Steps:**

1. 将测试目标从“被动自动激活”改为“被动按类别识别但不触发自动激活”。
2. 断言战斗体激活事务保留普通 Main[0]/Sub[0] 和 Special 原有自动路径，但不存在被动扫描、`RequestAutomaticActivate` 或被动自动写入路径。
3. 断言交互解释器不再把被动槽返回为 `InformationalOnly`（仅提示不可操作），而是进入普通槽位 Activate/Deactivate 解释流程。
4. 断言同侧默认互斥豁免依据 `IsPassiveChip`（被动类别判定），而不是依据“是否自动激活”。
5. 断言被动芯片仍受 `IsDisabled`、手部存在性和正常激活条件检查。
6. 断言同侧多个被动芯片可以作为多个正式 `IsActive` 槽位存在；侧级唯一槽位读取不再以“自动芯片回退”作为业务语义。
7. 断言显式 `ActivationExclusionGroups` 继续有效，组合技测试继续保留既定候选边界。
8. 先运行相关测试，确认它们因当前代码仍保留自动路径或禁止被动交互而失败；记录失败原因后再改生产代码。

**Verification:**

```powershell
pwsh -NoProfile -File Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyPrimarySlotAutoActivationSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\TriggerActiveSlotSelectionSmokeTests.ps1
```

## Task 2: 将类别判定从“自动激活策略”改成“被动类别判定”

**Files:**

- Delete: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\External\ITriggerActivationPolicyProvider.cs`
- Delete: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\External\TriggerActivationPolicyRegistry.cs`
- Delete: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Content\Trigger\Activation\PassiveTriggerActivationPolicyProvider.cs`
- Add: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\External\ITriggerChipClassificationProvider.cs`
- Add: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\External\TriggerChipClassificationRegistry.cs`
- Add: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Content\Trigger\Classification\PassiveTriggerChipClassificationProvider.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Content\ContentBootstrap.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs`

**Steps:**

1. Core 注册表只提供“芯片是否属于被动类别”的中性查询，不包含 `BDP_ChipCategory_Passive`。
2. Content 提供器继续按正式芯片画像的类别读取 `BDP_ChipCategory_Passive`，不增加新的激活标识字段。
3. 全部 `IsAutomaticallyActivated`、`ITriggerActivationPolicyProvider` 和“自动芯片”语义改为 `IsPassiveChip`/被动类别语义。
4. 组合候选继续按被动类别和正式 `IsActive` 筛选，不把类别判定解释成自动激活。
5. 先只完成命名和调用边界，不改变组合候选结果。

## Task 3: 删除被动自动激活与自动槽位过渡路径

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Access\Contracts\ITriggerLoadoutCommands.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Access\Surfaces\TriggerFormalSurfaces.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\TriggerSlotState.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchTransitionService.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Switching\Flow\TriggerDisableSync.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Runtime\TriggerActivationRequirementMonitor.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.ActivationRequirements.cs`

**Steps:**

1. 从战斗体进入 Active 顺序中移除 `TryAutoActivatePassiveSlots`；普通 Main[0]/Sub[0] 与 Special 的现有路径保持不变。
2. 删除被动专用 `RequestAutomaticActivate` 命令和正式表面入口。
3. 删除 `TriggerSlotState` 的自动过渡命名和自动业务语义，将现有按槽位上下文收口为通用独立切换上下文；正式激活真值仍只有 `IsActive`。
4. 删除被动自动扫描和自动业务入口，但保留按槽位独立过渡所需的通用懒结算、精确关闭、禁用同步和条件监控接线。
5. 战斗体退出仍必须关闭全部实际激活的 Main/Sub/Special 槽；当同侧有多个被动时，使用精确槽位关闭入口逐枚处理，不能只关闭 `GetActiveSlot(side)` 返回的第一枚。
6. 精确槽位关闭入口改成通用手动/生命周期关闭入口，不再以“自动芯片”作为路由条件；仍复用关闭延迟、绑定槽同步和正式停用通知。
7. `GetActiveSlot(side)` 恢复为普通侧级控制读取；全部正式激活事实继续由 `GetActiveSlots()` 提供。

## Task 4: 让被动槽走普通手动交互，同时放宽默认同侧互斥

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Interaction\TriggerInteractionInterpreter.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs`

**Steps:**

1. 删除交互解释器中“被动/自动芯片返回 `None + InformationalOnly`”的早期分支，使被动槽按普通槽位生成 Activate/Deactivate。
2. 保留空槽、镜像槽、禁用槽、战斗体不可用、条件不足和切换中的既有提示。
3. 在 `FindActivationBlockers` 中，仅当双方都属于普通控制芯片且共享默认控制范围时产生默认同侧阻挡；目标或活动芯片属于被动类别时跳过默认同侧阻挡。
4. 显式 `ActivationExclusionGroups` 判断独立于上述豁免，仍然产生阻挡。
5. 同侧多个被动的激活和关闭都通过精确槽位按钮；侧级唯一槽位接口不随机代表某一枚被动。

## Task 5: 保持组合、独立表达和身体约束边界

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs` only if the classification rename requires it
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs` only if classification names appear in public comments
- Modify: relevant smoke tests under `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests`

**Steps:**

1. 不改变已确认的三类组合候选：普通 Main + 普通 Sub、普通 Main + 同侧被动 Main、普通 Sub + 同侧被动 Sub。
2. 不让被动+被动、跨侧普通+被动和 Special 进入组合匹配。
3. 组合成立后独立芯片表达继续存在，多个合法组合继续全部生成。
4. 被动类别判定只参与候选分类，不绕过 `IsActive`、身体部位禁用或激活条件。
5. 不改 `ActivationExclusionGroups` 定义和切换语义；组合表只声明来源匹配，不覆盖运行时互斥状态。

## Task 6: 按 TDD 顺序验证并构建

**Commands:**

```powershell
pwsh -NoProfile -File Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyPrimarySlotAutoActivationSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\TriggerActiveSlotSelectionSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ComboSourceBindingSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\CombatBodyExitSemanticsSmokeTests.ps1
pwsh -NoProfile -File Source\BDP.Tests\ChipCombinationSelectionRulesSmokeTests.ps1
dotnet msbuild Source\BDP\BDP.csproj -p:Configuration=Release -t:Build -v:minimal
dotnet msbuild Source\BDP.Content\BDP.Content.csproj -p:Configuration=Release -t:Build -v:minimal
git diff --check
```

验证重点：

- 战斗体激活不会自动开启被动芯片；
- 被动槽能正常显示并执行手动 Activate/Deactivate；
- 同侧普通+被动、被动+被动不因默认控制范围互斥；
- 显式互斥组仍然严格生效；
- 身体部位缺失或槽位禁用时被动不能激活；
- 战斗体退出时同侧多个被动全部关闭；
- 组合来源、独立表达和 Special 隔离不回归；
- Core 不依赖 Content，Development 不被正式程序集依赖。

## Task 7: 记录并提交

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志\Agent日志94.md`；若超过20条则创建下一个日志文件

只提交本次手动控制修正涉及的源代码、测试、计划和日志文件，保留当前工作区已有的 DLL/PDB 修改。提交前再次执行 `git diff --check` 和完整相关测试。

## 明确不做

- 不自动激活被动芯片。
- 不因互斥芯片关闭而自动恢复被动芯片。
- 不增加“被动已激活”或“希望恢复”的第二套状态。
- 不把所有激活芯片放入组合匹配。
- 不让 Special 参与组合技。
- 不绕过战斗体、手部存在性、禁用状态、激活条件、Trion 或现有延迟。
