# 黑触发器特性适应权重实施计划

> **For Codex:** 按测试先行完成本计划；不改动玩家形成决策面板的显示语义。

**目标：** 让黑触发器定义可以声明一项或多项适应特性，并在同一战斗侧重的最终候选抽取中按 `1 + 3 × 命中数量` 提高权重。

**架构：** 在 Content 形成扩展中增加可选特性声明；在死亡前冻结人物自身特性，排除被压制特性和 `Trait_BlackTriggerInherited`（黑触发器临时继承特性）；候选计划保存命中特性与最终权重；最终确认时使用稳定整数加权抽取。技能、生涯路线、侧重分配和玩家面板保持原语义。

**技术栈：** C# 7.3、RimWorld 1.6 Def/XML、PowerShell 冒烟测试。

---

### Task 1：建立失败测试

**文件：**

- 新建：`Source/BDP.Tests/BlackTriggerTraitWeightSmokeTests.ps1`

覆盖：适应特性定义、可选 degree、来源特性快照、压制和临时继承排除、线性权重、稳定加权抽取、最终化调用、玩家窗口不显示权重。

### Task 2：增加 Def 数据模型和来源快照

**文件：**

- 新建：`Source/BDP.Content/Trigger/Black/Formation/Defs/BlackTriggerAdaptedTrait.cs`
- 新建：`Source/BDP.Content/Trigger/Black/Formation/Model/BlackTriggerTraitSnapshot.cs`
- 修改：`Defs/BlackTriggerFormationExtension.cs`
- 修改：`Model/BlackTriggerMakerSnapshot.cs`
- 修改：`Snapshot/BlackTriggerMakerSnapshotFactory.cs`

人物自身特性在死亡前完整冻结；临时继承特性不进入快照。继承特性选择仍只使用现有最多三项选项。

### Task 3：增加特性匹配与候选权重

**文件：**

- 新建：`Source/BDP.Content/Trigger/Black/Formation/Matching/BlackTriggerTraitWeightCalculator.cs`
- 修改：`Matching/BlackTriggerFormationMatcher.cs`
- 修改：`Model/BlackTriggerCandidatePlan.cs`
- 修改：`Validation/BlackTriggerFormationDefValidator.cs`

每个实际人物特性最多命中一次；非压制命中每项 `+3`，无命中保持 `1`。无效特性定义或 degree 使所属 ThingDef 不能进入形成池。

### Task 4：接入稳定加权最终抽取

**文件：**

- 修改：`Matching/BlackTriggerStableRandom.cs`
- 修改：`Transactions/BlackTriggerFormationFinalizationService.cs`

按候选保存的正整数权重做稳定抽取，保留事件延迟决定、读档一致性和世界唯一检查。

### Task 5：补充开发测试定义与静态验证

**文件：**

- 修改：`1.6/Development/Defs/ThingDef/BlackTriggerTest.xml`
- 修改：`Source/BDP.Tests/BlackTriggerFormationDefinitionSmokeTests.ps1`
- 新建：`日志/Agent工作日志/Agent工作日志-20260922-黑触发器特性适应权重.md`

不擅自给正式风刃或卵之冠选择具体适应特性；开发测试黑触发器声明 degree 精确项与无 degree 项验证配置加载路径。

### Task 6：验证与提交

运行 Content Release 编译、黑触发器全部相关冒烟测试和边界验证脚本；确认玩家决策窗口未出现权重或适应特性字段后，仅提交本需求文件。
