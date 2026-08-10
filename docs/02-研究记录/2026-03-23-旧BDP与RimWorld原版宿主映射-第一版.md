# 旧 BDP 与 RimWorld 原版宿主映射（第一版）

## 目的

这份记录只回答一个问题：

旧 BDP 的关键能力，到底应该优先贴 RimWorld 的哪种原版通道。

二次重构必须先从这里出发，不能先幻想一套悬浮在游戏之上的体系。

## 核心原则

- 原版已有稳定通道的地方，优先贴原版
- 只有原版明显缺位的地方，才允许 BDP 自建规则层
- 自建规则层也必须能清楚映射回原版宿主

## 映射总表

| 领域 | 旧 BDP 主要载体 | RimWorld 更自然的宿主 | 说明 |
|---|---|---|---|
| Trion 资源 | `CompTrion` | `ThingComp` 挂在 Pawn | 资源本身适合作为 Pawn 上的长期状态 |
| 战斗体相位 | `CombatBodyRuntime + Hediff + Orchestrator` | `Hediff/HediffComp` 为主 | 战斗体本质是 Pawn 身体与状态相位 |
| 装载与切换 | `CompTriggerBody` | `CompEquippable/ThingComp` | 这是装备侧问题 |
| 手动/自动攻击入口 | `Verb + Patch + JobDriver` | `Verb/JobDriver` | 必须贴原版战斗入口 |
| 投射物飞行与命中 | `Bullet_BDP` | `Projectile` | 原版已经提供天然宿主 |
| 可见信息与操作入口 | Gizmo/UI/Patch | `Gizmo/InspectString/StatDrawEntry` | 应尽量做只读投影与桥接 |

## 1. Trion 资源

### 原版映射建议

优先继续贴 `ThingComp`，并挂在 Pawn 上。

### 原因

- 资源是长期状态
- 需要存档
- 需要在多个系统之间共享
- 不应该因为装备卸下、窗口关闭、动作结束而失去真值

### 不建议的宿主

- 不建议挂在触发体装备上当正式真值
- 不建议只做临时运行时对象

## 2. 战斗体相位

### 原版映射建议

优先贴 `Hediff/HediffComp`。

### 原因

- 战斗体本质是 Pawn 身体所处的一种特殊状态
- 原版 Hediff 天然支持：
  - 持续状态
  - Tick
  - 伤害后通知
  - Spawn/Death 通知
  - 序列化
  - Gizmo 与说明文字

### 结论

二次重构里，战斗体不该主要寄存在触发体里，也不该只是外部流程对象。

它可以有“编排器”，但正式相位状态应尽量落在 Pawn 身体语义上。

## 3. 装载与切换

### 原版映射建议

优先贴 `CompEquippable` / 装备侧 `ThingComp`。

### 原因

- 芯片槽、左右手、特殊槽，本质上是装备载荷
- 被装备、卸下、掉落、切主武器，都属于装备生命周期

### 关键限制

装备在 Pawn 身上时，原版稳定入口不是任意 `CompTick`，而是：

- `Pawn_EquipmentTracker.EquipmentTrackerTick()`
- `CompEquippable.verbTracker.VerbsTick()`

这意味着：

- 切换与 verb 驱动必须尊重装备通道
- 不能把触发体当成一个脱离原版装备系统的独立游戏对象

## 4. 行动入口

### 原版映射建议

优先贴 `Verb` 与 `JobDriver`。

### 原因

- 手动攻击、自动攻击、近战、远程，本来就是原版战斗系统的核心入口
- `Verb` 负责玩家点击和战斗系统接入
- `JobDriver` 负责持续动作、站姿、打断、Toil 推进

### 结论

二次重构中，统一行动规则层必须最终回落到：

- 哪个 `Verb` 暴露给原版
- 触发后走什么 `JobDriver`
- 发射或攻击结果如何落到原版 projectile / damage

## 5. 投射物飞行与命中

### 原版映射建议

优先贴 `Projectile`。

### 原因

原版 `Projectile` 已经天然负责：

- 飞行推进
- `ticksToImpact`
- 中途拦截
- 到点 impact

### 结论

BDP 要扩展的是：

- 飞行意图
- 轨迹修正
- 命中解析
- 到达后效果

而不是重做一个脱离 projectile 的完整飞行系统。

## 6. 显示与信息投影

### 原版映射建议

优先贴：

- `Gizmo`
- `InspectString`
- `StatDrawEntry`
- 合理的 Harmony bridge

### 原因

- 玩家最终是在 RimWorld UI 中操作和理解模组
- 模组显示层应该努力融入原版界面，而不是自造一套平行交互系统

## 对二次重构的硬约束启发

### 应该优先贴原版的部分

- 资源正式宿主：Pawn 上的长期状态对象
- 战斗体相位：Hediff 语义
- 装备装载与切换：CompEquippable 语义
- 行动入口：Verb / JobDriver
- 投射物：Projectile
- 显示层：Gizmo / Inspect / Stat

### 允许自建规则层的部分

- 能力声明与装载规则
- 行动计划的统一格式
- 投递与效果协议
- 跨领域的只读查询与决策辅助

### 明确不该自建成“另一个游戏”的部分

- 不自建平行生命周期系统取代 Pawn / Hediff / Equipment / Projectile
- 不自建平行攻击主通道绕开 Verb / Job
- 不自建大量影子真值替代原版宿主对象

## 当前结论

二次重构的方向应该是：

- 从旧 BDP 提炼统一规则层
- 但规则层必须严格寄生在 RimWorld 的原版宿主之上
- 真正的目标不是“把系统做得像一个平台”
- 而是“让 BDP 的复杂能力，最后仍像 RimWorld 自己长出来的一部分”
