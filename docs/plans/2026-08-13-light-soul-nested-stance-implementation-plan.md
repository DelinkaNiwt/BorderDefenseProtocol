# Light Soul Nested Stance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 新增“光魂”芯片预设，并以中性的嵌套姿态设施支持“大形态内切换小姿态”；切换大形态时，小姿态重置为目标形态的默认值。

**Architecture:** `BDP.Core`（核心程序集）只新增通用姿态状态、解析、切换事务与攻击约束支持；`BDP.Content`（内容程序集）承载光魂业务、姿态按钮、跳跃耗能分支及护盾扩展。形态有效条目由“形态公共条目 + 当前姿态条目”组成，姿态只属于一个形态，切换形态时不跨形态继承。

**Tech Stack:** RimWorld 1.6、C#、XML（可扩展标记语言）、Harmony（补丁库）、PowerShell 冒烟测试、MSBuild（微软构建工具）

---

## Task 1: 嵌套姿态定义与表达解析

**Files:**
- Modify: `Source/BDP/Core/Definitions/ChipActionPresetDef.cs`
- Modify: `Source/BDP/Core/Definitions/ChipActionPresetValidator.cs`
- Modify: `Source/BDP/Core/Expression/ChipExpressionContract.cs`
- Modify: `Source/BDP/Core/Expression/ChipExpressionContractInterpreter.cs`
- Test: `Source/BDP.Tests/ChipNestedStanceDefinitionSmokeTests.ps1`
- Test: `Source/BDP.Tests/ChipNestedStanceResolutionSmokeTests.ps1`

1. 先写定义、校验和“公共条目 + 姿态条目”解析的失败测试。
2. 运行新测试，确认因姿态设施缺失而失败。
3. 新增中性的 `ChipExpressionStanceConfig`（芯片表达姿态配置）和姿态契约。
4. 校验姿态键、默认姿态、显示文本、条目引用、重复条目和最终父子顺序。
5. 解析时读取当前姿态；无效或空值回落到该形态默认姿态。
6. 运行测试并提交。

## Task 2: 姿态运行时状态、事务与按钮

**Files:**
- Modify: `Source/BDP/Core/Runtime/TriggerSlotState.cs`
- Modify: `Source/BDP/Core/Runtime/TriggerChipModeService.cs`
- Modify: `Source/BDP/Core/Runtime/ITriggerBodyStateReader.cs`
- Modify: `Source/BDP/Core/Runtime/ITriggerBodyCommandPort.cs`
- Modify: `Source/BDP/Core/Runtime/CompTriggerBody.Reads.cs`
- Modify: `Source/BDP/Core/Runtime/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Runtime/CompTriggerBody.Integrity.cs`
- Modify: `Source/BDP.Content/Runtime/ContentBootstrap.cs`
- Add: `Source/BDP.Content/Runtime/ChipStanceGizmoProvider.cs`
- Modify: `1.6/Languages/ChineseSimplified/Keyed/BDP_Content.xml`
- Test: `Source/BDP.Tests/TriggerChipStanceRuntimeSmokeTests.ps1`
- Test: `Source/BDP.Tests/ChipStanceGizmoContentSmokeTests.ps1`

1. 先写姿态保存、恢复、默认重置、事务回滚和按钮注册的失败测试。
2. 新增槽位姿态键和只读快照；保存读档时规范化无效姿态。
3. 扩展现有形态服务：切换形态时原子地设置目标默认姿态；失败时同时回滚形态与姿态。
4. 新增姿态切换/轮换命令；切换成功后发布投影更新。
5. Content 中新增通用姿态 Gizmo（游戏操作按钮）并注册。
6. 运行测试并提交。

## Task 3: 跳跃能力 Trion（触力能）扣费扩展点

**Files:**
- Modify: `Source/BDP/Core/Abilities/BdpVerb_CastAbility.cs`
- Add: `Source/BDP/Core/Abilities/IBdpExpressionAbilityVerb.cs`
- Add: `Source/BDP/Core/Abilities/BdpAbilityTrionCostCommitter.cs`
- Add: `Source/BDP/Core/Abilities/BdpVerb_CastAbilityJump.cs`
- Modify: `Source/BDP/Core/Expression/DefaultExpressionAbilityHostSynchronizer.cs`
- Modify: `Source/BDP.Content/CombatBody/ShortJump/Verb_CastAbilityCombatBodyShortJump.cs`
- Test: `Source/BDP.Tests/TrionJumpAbilityHostSmokeTests.ps1`

1. 先写“跳跃能力也能成为表达宿主并只在成功施放入口扣费”的失败测试。
2. 抽取通用扣费提交器和能力动词标记接口。
3. 新增继承原版 `Verb_CastAbilityJump`（跳跃能力动词）的 BDP 基类，在原版跳跃执行之前提交 Trion 扣费。
4. 让短距跳跃继承新基类；原能力不配置扣费组件，因此保持免费。
5. 运行测试并提交。

## Task 4: 护盾近战准入、禁攻和非攻击型视觉投影

**Files:**
- Modify: `Source/BDP.Content/Shields/EnergyShieldBlockPolicy.cs`
- Modify: `Source/BDP.Content/Shields/HediffComp_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shields/HediffCompProperties_EnergyShield.cs`
- Modify: `Source/BDP/Core/Combat/AttackExecutionService.cs`
- Modify: `Source/BDP/Core/Visuals/DefaultVisualProjectionBuilder.cs`
- Test: `Source/BDP.Tests/EnergyShieldMeleeAdmissionSmokeTests.ps1`
- Test: `Source/BDP.Tests/ViolenceDisabledAttackGateSmokeTests.ps1`
- Test: `Source/BDP.Tests/NonVerbResidentVisualSmokeTests.ps1`

1. 先分别写三组失败测试。
2. 护盾配置增加“是否允许近战伤害”开关，默认关闭；依据原版 `DamageInfo.Tool`（伤害工具）和近战武器语义识别近战。
3. BDP 攻击接入点尊重 `WorkTags.Violent`（暴力工作类型）禁用，举盾姿态因此不能发起攻击。
4. 视觉投影允许带视觉预设的非复合、非攻击表达条目成为常驻视觉。
5. 运行测试并提交。

## Task 5: 光魂正式内容

**Files:**
- Modify: `1.6/Content/Defs/ChipActionPresetDef/Presets.xml`
- Add: `1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml`
- Add: `1.6/Content/Defs/HediffDef/LightSoul.xml`
- Modify: `1.6/Content/Defs/WeaponVisualPresetDef/Visual.xml`
- Modify: `1.6/Languages/ChineseSimplified/DefInjected/BDP.Core.ChipActionPresetDef/Presets.xml`
- Modify: `1.6/Languages/ChineseSimplified/DefInjected/AbilityDef/Abilities.xml`
- Modify: `1.6/Languages/ChineseSimplified/DefInjected/HediffDef/Hediffs.xml`
- Modify: `1.6/Languages/ChineseSimplified/Keyed/BDP_Content.xml`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingListModel.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/Window_ChipManufacturing.cs`
- Test: `Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- Test: `Source/BDP.Tests/CrossCategoryProfessionManufacturingSmokeTests.ps1`

1. 先写覆盖分类、职业、形态、姿态、跳跃扣费、盾牌参数、禁攻减速及双伤害权重的失败测试。
2. 新增光魂推进能力，复用短距跳跃飞行物与逻辑，配置 Trion 扣费。
3. 新增灵活/举盾两个护盾 Hediff（健康状态），分别配置 180°/50%/禁近战与 120°/98%/允许近战；举盾阶段设置移动速度 ×0.6 和禁用暴力行为。
4. 新增光魂预设：大盾形态公共持有推进能力，两个姿态各启用对应护盾；重刃形态启用推进能力与近战条目。
5. 重刃使用两个互斥 `Tool`（攻击工具）：钝伤 20、穿透 0、权重 1.7；切割 15、穿透 0.10、权重 1.0，约为 75%/25%。
6. 配置三套独立视觉预设；在没有最终美术资源时仅复用现有正式资源，不虚构缺失贴图路径。
7. 所有新增玩家文本进入语言包。
8. 职业筛选只属于武装分类；防护等非武装分类直接显示该分类全部预设。光魂仍保留“攻击手”定义语义，但该字段不参与防护页筛选。
9. 运行测试并提交。

## Task 6: 集成验证、审查与日志

**Files:**
- Modify: `日志/Agent工作日志/Agent日志*.md`

1. 运行全部相关 PowerShell 测试和现有回归测试。
2. 编译 `BDP.Core`、`BDP.Content` 与主解决方案，记录完整命令结果。
3. 检查 XML 加载约束、仓库差异和无关文件，确认只保留计划内改动。
4. 做一次自审：形态切换默认重置、读档恢复、事务回滚、扣费时机、禁攻时序、双伤害互斥权重。
5. 按时间倒序写简洁工作日志（单文件不超过 20 条）。
6. 最终验证后提交，并给出游戏内实测清单；不能由自动测试证明的交互明确标注。
