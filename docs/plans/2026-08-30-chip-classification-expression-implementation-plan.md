# 芯片分类与表达类型去歧义实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 BDP 的芯片业务分类收敛为四类，消除表达条目 `Kind`（类型）与运行时输出通道的命名混淆，统一 `GunClass`（枪械类别）为 `ArmamentForm`（武装构型），并增加通用的能力按钮控制 Hediff 及实际 Hediff 持有态持续消耗机制。

**Architecture:** `Category`（分类）继续表示芯片制造分类；表达配置使用 `EntryType`（表达条目类型）；运行时使用 `ExpressionOutputChannel`（表达输出通道）；发布快照保留独立的发布通道类型。Hediff 控制由 Core 提供中性的 `Automatic`/`AbilityButton` 两种模式，Content 通过 AbilityDef（能力定义）组件声明具体目标；持续消耗在宿主同步之后按实际表达宿主 Hediff 是否存在对账。变色龙继续使用原 Hediff、隐身组件和攻击后停用服务，不增加变色龙专用底层分支。

**Tech Stack:** C#（C#语言）、RimWorld 1.6 原版 Def/XML（定义/配置）、PowerShell 静态烟测、现有 BDP Core/Content/Development 构建脚本。

---

## 1. 固定变更边界

- 不考虑旧存档、旧 DefName（定义名）或旧 API（程序接口）兼容。
- 不改 `ChipCategoryDef`、`Profile.Category` 和 `TriggerCategoryDef` 的 `Category` 命名。
- 武装、防护分类 DefName 保持不变；状态并入通用技能；能力改为通用技能；被动改为配件。
- 配件只绕过默认同侧激活控制；显式激活互斥组仍然有效。
- Core 不写入变色龙名称、隐身逻辑或具体业务 DefName。
- 不把 `SourceVariantKey`（来源变体键）改名为武装构型键，因为它必须继续支持非武装来源变体。

## 2. 先写边界测试

修改或新增现有 `Source/BDP.Tests` PowerShell 烟测，使其先表达目标契约，再实现代码：

- 四类芯片分类顺序、语言键、旧分类 DefName 不残留。
- `ChipExpressionEntryTypeConfig`、`ChipExpressionEntryType`、`ExpressionOutputChannel` 与 `OutputChannel` 字段命名完整，旧表达专用 `Kind`/`ResultKind` 不残留。
- `ArmamentForm` 和 `BDP_ArmamentForm_*` 不残留；所有武装构型引用使用 `ArmamentForm`。
- 配件激活控制使用中性 Core 接口/注册表，Content 只负责把配件分类映射到独立激活控制能力；显式互斥组检查仍在切换主链中。
- Hediff 的 `Automatic` 与 `AbilityButton` 模式边界完整；按钮模式不由 Hediff 同步器自动添加。
- 能力按钮可配置目标表达宿主 Hediff，切换组件不包含变色龙专用名称。
- 持续消耗服务在 Hediff 宿主同步之后运行，并对 Hediff 结果检查实际表达宿主存在。
- 变色龙仍使用原 Hediff Def、原隐身组件和原攻击后自动关闭服务，仅增加通用能力入口。

## 3. 实施分类与表达命名

### 3.1 分类 Def 和内容引用

修改 `1.6/Content/Defs/ChipDef/Classification.xml`、分类语言包、制造窗口固定分类列表及全部芯片功能 XML：

- `BDP_ChipCategory_Ability` → `BDP_ChipCategory_GeneralSkill`，显示名“通用技能”。
- `BDP_ChipCategory_Status` → 不再定义、不再引用；变色龙改用通用技能。
- `BDP_ChipCategory_Passive` → `BDP_ChipCategory_Accessory`，显示名“配件”。
- 保持 `BDP_ChipCategory_Weapon`、`BDP_ChipCategory_Defense`。
- 更新分类筛选、制造预览和测试中的顺序与语言键。

### 3.2 表达配置类型与运行时输出

在 Core 的表达配置、契约、解释器、克隆器、解析器、快照、投影和读取面中统一：

- `ChipExpressionEntryKindConfig` → `ChipExpressionEntryTypeConfig`。
- `ChipExpressionEntryKind` → `ChipExpressionEntryType`。
- 配置/契约字段 `Kind` → `EntryType`。
- `ExpressionResultKind` → `ExpressionOutputChannel`。
- 结果字段 `ResultKind` → `OutputChannel`。
- `ExpressionPublishedChannelKind` 只有在确认与输出通道完全重复时才合并；若仍承担“公开发布快照类别”语义则保留。
- 其它有明确局部语义的 `*Kind` 不机械改名。
- XML 表达条目 `<Kind>` → `<EntryType>`，所有 ComboDef（组合技定义）和 ChipFunctionDef（芯片功能定义）同步更新。
- `PassiveKey` → `PassiveIdentityKey`，相关公开查询口和结果索引同步改名。

## 4. 实施武装构型与激活控制命名

- `BDP_GunClass_Revolver` 等 DefName 统一改为 `BDP_ArmamentForm_*`。
- 语言键、制造窗口、定义校验、组合解析和测试同步改名。
- 玩家可见“武装型”改为“武装构型”，文案全部走简体中文语言包。
- `ITriggerChipClassificationProvider.IsPassiveChip` 改为中性的独立激活控制接口 `ITriggerChipActivationControlProvider.HasIndependentActivationControl`。
- `TriggerChipClassificationRegistry` 改为对应中性注册表 `TriggerChipActivationControlRegistry`；Core 不出现配件分类 DefName。
- Content 的提供器改为配件激活控制提供器，只比较 `BDP_ChipCategory_Accessory`。
- 保留所有 `hasExplicitConflict`（显式冲突）判定在默认同侧豁免之前。

## 5. 实施通用 Hediff 按钮控制

### 5.1 Core 设施

新增 Core 的通用 `CompProperties_AbilityEffect_ToggleExpressionHediff` 和对应 `CompAbilityEffect`：

- 通过 AbilityDef 组件配置 `hediffDefName`，不硬编码业务名称。
- 只允许目标 Def 的 `hediffClass`（Hediff 类）属于 `BdpExpressionHostHediff`。
- 按当前 Pawn 上是否存在目标表达宿主 Hediff 执行添加/移除。
- 使用现有 BDP Ability Verb 和宿主同步链，不新增 Trigger 侧副作用。

在表达条目中增加 Hediff 控制模式，默认 `Automatic`；按钮模式使用 `AbilityButton`：

- 自动模式沿用现有添加/移除语义。
- 按钮模式由 Ability 组件直接切换，Hediff 同步器只同步已存在宿主的表达结果。
- 来源表达失效时，通用同步器清理按钮模式的表达宿主，避免来源消失后遗留状态。

### 5.2 持续消耗

修改 `ExpressionSustainDrainService`：

- 宿主同步完成后再进行持续费用对账。
- 对 `ExpressionOutputChannel.Hediff` 结果，只有 Pawn 实际存在对应 `BdpExpressionHostHediff` 时才建立费用组。
- 能力、被动、攻击表达继续沿用正式结果可用性规则。
- 保持现有效果身份聚合和中央 Trion 账本注册/注销机制。

### 5.3 变色龙接入

- `BDP_ChipFunction_Chameleon` 改为通用技能分类。
- 保留 Hediff 表达条目，声明 `AbilityButton` 控制模式和原 `BDP_Hediff_Chameleon`。
- 增加一个 Ability 表达条目，绑定新 AbilityDef。
- 新 AbilityDef 使用 Core 通用切换组件，配置目标 `BDP_Hediff_Chameleon`，不配置独立使用费用和冷却。
- 保留 `HediffComp_BdpInvisibility`、`ChameleonAttackShutdownService`、Hediff Def 和原有玩家文案语义。

## 6. 清理与验证

- 用旧标识扫描清理源码、XML、语言包、测试名、注释和相关文档；保留历史交接文档中必要的事实记录，不篡改无关历史。
- 运行全部相关 PowerShell 烟测，特别是分类、表达四通道、Ability/Hediff、持续消耗、变色龙和配件激活控制测试。
- 运行 Core 隔离、Development 边界、Content 构建和发布就绪检查。
- 不覆盖工作区中预先存在的 DLL、PDB 和 PSD 修改。
- 更新工作日志；日志文件超过 20 条时新建日志文件。
- 仅在静态检查和构建成功后提交最终实现；提交中不包含预先存在的二进制和设计素材改动。
- 最终说明未进行游戏内实际运行验证的部分，不能用编译通过替代游戏内验证。
