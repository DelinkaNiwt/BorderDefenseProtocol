# Trion Gizmo UI 重绘设计

> 本稿用于落实已确认的 Trion 资源 gizmo 视觉改版，不调整 `Trion / CombatBody / Trigger` 真值边界，只改 `Gizmo_TrionStatus` 的表现层。

**目标：**
- 把当前 `Trion` gizmo 改成更接近目标草图的短卡片样式
- 保留现有正式 `Trion` 读口和扩展徽标机制，只重构布局、文案、图标和分隔线画法
- 让状态表达更稳定：数值统一 1 位小数，状态图标用固定 4 槽位的亮/暗切换表达

## 1. 布局目标

新的 gizmo 采用三段式布局：
- 顶部：仅保留标题 `Trion能量`
- 中部：更粗的主能量条，整体位置水平居中并略偏下
- 底部：左侧小号提示文本，右侧最多 4 个状态图标槽位

卡片整体要比当前实现更短，避免现在“顶部文案过满、下方扩展行另起一排”的拥挤感。

## 2. 文本策略

标题区：
- 只显示 `Trion能量`
- 使用标准 `Small` 字号，不再在标题行塞入“可用”“恢复/消耗”等信息

底部说明区：
- 使用比标题更小的视觉层级
- 左侧保留现阶段已有的资源提示信息，预计包含：
  - `可用: x.x / y.y`
  - `消耗: z.z/秒` 或在未激活状态下显示 `恢复: z.z/天`
- 所有数值统一使用 `F1`

## 3. 主条策略

主条仍然表达以下正式资源语义：
- `Cur / Max`：当前总填充范围
- `Allocated / Max`：正式锁定边界
- `Reserved / Max`：预测锁定边界

显示规则维持现有语义：
- `Allocated > 0` 时：显示正式锁定段与可用段
- `Allocated <= 0` 时：显示当前总量，并在需要时显示预测锁定边界

但表现要调整为：
- 条更粗
- 条内文案移除，不再在条中央写大号数字
- 分隔线不再是一条贯穿全高的竖线，而是改成“上短线 + 下短线，中间留空”的断开式标记

## 4. 状态图标策略

状态图标位于底部右侧，固定最多 4 个槽位。

本次确认的规则：
- 不是永远显示空槽
- 只有有状态时才显示对应图标
- 一旦显示，最多只放 4 个
- 超过 4 个的状态不再继续加入 gizmo

状态切换方式：
- 同一状态使用同一图标/示意贴图
- 通过高亮与灰暗表达开/关，而不是靠显示/隐藏切换语义

这意味着：
- `Frozen` 不再用“有时出现一个字块、没时完全没位置”的方式处理
- 扩展 provider 返回的状态也应尽量稳定使用图标 + tint，而不是文字块临时拼接

## 5. 图标来源与边界

本次不在 `CompTrion` 中引入 CombatBody/Trigger 内部依赖。

边界保持：
- `Gizmo_TrionStatus` 继续只依赖 `Thing owner`、`ITrionReader` 和 Trion 扩展注册表
- CombatBody 等外部系统继续通过 `ITrionGizmoExtensionProvider` 提供状态徽标
- 若后续需要更正式的示意图形，应通过贴图资源或统一图标 helper 注入，不把系统判断逻辑塞回 Trion 核心

## 6. 引擎绘制手段

原版 RimWorld/Verse 当前足够支持这次改版，主要使用：
- `Widgets.DrawWindowBackground`：卡片背景与边框
- `Widgets.DrawBoxSolid` / `GUI.DrawTexture`：主条、图标槽、亮暗遮罩
- `Widgets.Label` + `Text.Font` + `Text.Anchor`：标题与底部提示文本
- `Widgets.DrawLineVertical` 或小矩形手绘：断开式分隔线
- `TooltipHandler.TipRegion`：保留整体 tooltip

本次不需要引入新的窗口、滚动区或复杂交互控件。

## 7. 改动落点

核心改动文件预计为：
- `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`

可能需要的小范围配套调整：
- `Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs`
- `Source/BDP/Core/Trion/External/TrionGizmoExtensionBadge.cs`
- 相关 smoke tests

## 8. 验收标准

完成后应满足：
- gizmo 比当前更短
- 标题区只有 `Trion能量`
- 主条更粗、更靠近卡片中下部
- 底部信息是小号文本，数值统一 1 位小数
- 图标区最多 4 个状态位，超过不再追加
- 状态切换通过图标亮/暗表达
- 分隔线为上下断开式，不再贯穿整条高度
