# Targeting 段合法性查询口裁定（第一版）

## 1. 问题

当前主架构已有：

- `caster（施法者） -> target（目标）` 的合法性判断

当前主架构缺少：

- `origin（任意起点） -> candidate target（候选目标）` 的中性合法性判断

这会导致需要“中间路径控制点”的业务模块，无法在不污染主架构的前提下查询每一段是否合法。

## 2. 裁定

主架构补一个最小中性查询口：

- `TargetingSegmentLegalityRequest（Targeting 段合法性请求）`
- `TargetingSegmentLegalityResult（Targeting 段合法性结果）`
- `ITargetingSegmentLegalityService（Targeting 段合法性服务）`
- `DefaultTargetingSegmentLegalityService（默认 Targeting 段合法性服务）`

## 3. 原则

- 只补查询能力
- 不补业务语义
- 不补毒蛇、锚点、路径、终点概念
- 不重写原版命中、遮挡、视线规则

## 4. 默认实现职责

默认实现只做三件事：

- 复用当前 `TargetingParameters（目标参数）` 校验候选目标是否允许被选中
- 复用当前 `Verb（攻击动作）` 的 `CanHitTargetFrom（从指定起点命中目标）`
- 返回：
  - 合法
  - 或中性拒绝原因

默认实现不做：

- 追加锚点
- 写上下文
- 画预览
- 决定继续、拒绝、完成

## 5. 边界

- 主架构负责：
  - 提供查询口
- 业务模块负责：
  - 决定什么时候调用
  - 决定非法时如何反馈
- 原版 / 现有 BDP 负责：
  - 具体合法性判定细节

## 6. 结果

补完后，业务模块可以在 `Targeting（瞄准阶段）` 内，以中性方式查询：

- 从任意地图格到候选目标这一段是否成立

而主架构仍然保持中性，不带任何路径业务名词。
