# 变色龙自施放动作根因修正实施计划

## 1. 新增通用自施放 Verb

- 在 `BDP.Core.Abilities` 下新增 `BdpVerb_CastSelfAbility`。
- 复用现有 Trion 成本提交和 Ability 激活流程。
- 重写自施放目标校验与施放启动，跳过射击线、远程瞄准通知和攻击抖动。

## 2. 接入变色龙

- 将变色龙 AbilityDef 的 `verbClass` 切换为通用自施放 Verb。
- 保留现有前摇音效、40 秒 Ability 冷却、20 秒 Hediff、持续消耗和攻击后仅移除 Hediff 的行为。

## 3. 验证

- 增加静态边界测试，确认通用 Verb 不含变色龙业务耦合。
- 运行变色龙定向测试、XML 解析、Core/Content/Development Release 构建与程序集边界检查。
- 不纳入工作区既有的 PSD、PNG、DLL/PDB 修改。
