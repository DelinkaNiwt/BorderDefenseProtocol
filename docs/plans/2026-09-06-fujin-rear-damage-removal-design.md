# 风刃后方追加伤害回收设计

## 目标

取消风刃命中后沿飞行方向对后方两格单位追加伤害的需求，使风刃只保留投射物对主命中目标的原版 `Cut`（切割）伤害。

## 已确认事实

- `BDP_Fujin_DirectionalSlash` 仅由风刃入口挂载，当前没有其它 Def 或运行时模块引用。
- `DirectionalSlashModule` 仅负责读取最终飞行方向并生成后方单位的额外 `DamagePlan`（伤害计划）。
- `ImpactStageContext.FinalFlightDirection`、`ImpactStageContext.DamageFactor` 和 `DamagePlan.TargetThing` 均没有其它当前消费者，分别服务于方向斩击的方向计算、伤害倍率和额外目标绑定。
- `ExtraDamages`、`IImpactStageModule`、`HitCell` 等远程协议设施在风刃之前已经存在，并仍被通用 Impact（命中阶段）流程或其它正式模块使用，不随本次需求删除。
- 风刃的主目标命中、分段距离重定向、障碍物放行、齐射、散布和长拖尾不属于本次回收范围。

## 设计决定

采用最小回收方案：

1. 删除风刃的方向斩击 Def、挂载、语言键和实现文件。
2. 从 Core Impact 上下文删除只服务方向斩击的最终飞行方向和伤害倍率字段。
3. 从 Core `DamagePlan` 删除仅用于额外目标绑定的 `TargetThing` 字段；额外伤害容器本身保留，并恢复其原有的主命中目标回退执行方式。
4. 保留障碍物交互策略等风刃仍需要的 Core 改动，不回退无关的 Ability 远程攻击链路。
5. 同步风刃玩家说明和契约测试，确保风刃只配置距离重定向模块。

## 验证

- 风刃 Def 和语言 XML 解析通过。
- 风刃作者契约测试、Core 契约测试通过。
- BDP.Content Release 构建通过，确认删除方向斩击实现后无程序集引用残留。
- 发布目录和架构边界检查通过；后方两格伤害的游戏内行为不再作为验证目标。
