# 毒蛇 targeting 边界纠偏记录（第一版）

## 目的

记录本轮对毒蛇手动 targeting、dual 必要 LOS 适配和组级手动入口的边界纠偏。

这次不是给毒蛇写特例补丁，而是把原版 `Targeter` 门面、BDP 模块确认链、dual 必要 LOS 裁定三者重新分工。

## 一、保留的正确修复

- 非法锚点不再被宿主层当成“整个目标无效”。
- 当前候选点是否合法，继续交给模块产生当前候选点真值。
- 宿主层只在 `OnGUI` 把非法候选点表现成禁止图标，不让原版 `Targeter` 清掉悬停目标和预览会话。

## 二、局部回收的错误做法

- `AttackExecutionTargetingSource.CanHitTarget(...)` 不再回落到 `context.Verb.CanHitTarget(target)`。
- `AttackExecutionTargetingSource.ValidateTarget(...)` 不再回落到 `context.Verb.ValidateTarget(target, showMessages)`。
- dual 非必要直射侧不再由 dual 适配层调用 `sourceVerb.CanHitTarget/ValidateTarget` 预筛。
- 组级手动入口不再在逐成员下单前调用 `source.ValidateTarget(target, false)` 预筛成员。

## 三、职责边界

- `Targeter` 适配层只负责让交互流程继续进入 BDP 的正式输入和确认链。
- 毒蛇模块只负责自己的分段相邻点合法性，包括“射手到第一锚点”“锚点到锚点”“最后锚点到真实目标”。
- dual 层只负责“某侧是否必须做射手到真实目标的直接 LOS 裁定”。
- 非必要直射侧由自己的模块确认链继续裁定，不由 dual 层替它理解业务规则。

## 四、dual 必要 LOS 组合

- 必要 LOS + 必要 LOS：dual 层按两侧必要直射分别裁定；两侧都失败时 dual 禁止。
- 必要 LOS + 非必要 LOS：必要侧失败时被剪掉，非必要侧继续进入自己的模块/确认链。
- 非必要 LOS + 非必要 LOS：dual 层不拦截，两个侧都继续进入各自逻辑。

## 五、验证覆盖

本轮新增或收紧了以下 smoke tests：

- `ViperPathLatchFirstAnchorContinuitySmokeTests.ps1`
- `ViperPathLatchSegmentLosBoundarySmokeTests.ps1`
- `DualRangedManualTargetingLegalitySmokeTests.ps1`

它们分别锁住：

- 非法第一个锚点不能触发宿主层原版直射回落。
- 毒蛇最终目标确认必须保留分段相邻点 LOS 语义。
- dual/grouped 手动入口不能在适配层提前筛掉非必要直射侧。
