# Trion储量 Gizmo 标题名称显示设计

> 本设计已获确认，用于在不改变 Trion 资源真值和 Gizmo 通用边界的前提下，为 Trion 资源 Gizmo 标题追加宿主小人的短名称。

## 目标

将 Trion 资源 Gizmo 的标题从当前的“Trion储量”扩展为：

`Trion储量（小人短名称）`

名称沿用 BDP 其它界面使用的 RimWorld `LabelShortCap`（短标签首字母大写形式），不显示 Pawn（小人）的完整正式名称。

## 现状与边界

- `TrionGeneGizmoBridge` 只为玩家 Pawn 创建 `Gizmo_TrionStatus`。
- `Gizmo_TrionStatus` 内部使用通用 `Thing owner`，当前唯一创建点传入 Pawn。
- 标题由 `Gizmo_TrionStatus.DrawTitleRow` 绘制，文案通过 `BDP_Trion_Status_Title` 语言键读取。
- `Core.dll` 继续只负责通用 Trion Gizmo 表现，不新增 Pawn 专属服务、状态或跨模块依赖。

## 方案

1. 将简体中文语言键 `BDP_Trion_Status_Title` 改为 `Trion储量（{0}）`。
2. 在 `DrawTitleRow` 中从 `owner.LabelShortCap` 取得名称，并作为语言键参数传入。
3. 保持现有标题矩形、徽标区域、卡片宽度和右侧扩展面板布局不变。
4. 保留空对象兜底：若 `owner` 为空，标题参数使用空字符串，不让 UI 绘制抛出异常。

## 数据流

```text
Pawn
  -> TrionGeneGizmoBridge
  -> Gizmo_TrionStatus(owner = Pawn)
  -> owner.LabelShortCap
  -> BDP_Trion_Status_Title.Translate(name)
  -> Trion储量（名称）
```

## 测试与验证

- 增加静态烟测断言：标题仍通过语言键绘制，语言键包含 `{0}`，生产代码使用 `owner.LabelShortCap`。
- 运行新增/更新的 Trion Gizmo 标题烟测。
- 运行现有 `TrionGizmoPanelLayoutSmokeTests`，确保标题改动不影响布局和扩展面板。
- 编译 Core/Content 程序集。
- 最终需要在游戏内选中带 Trion 的殖民者，确认短名称显示、长名称布局和标题中文案。

## 不做的事

- 不改 Trion 数值、读写口、资源条、tooltip 或扩展徽标。
- 不新增名称截断 helper，不拼接完整 `Pawn.Name`。
- 不修改旧测试中与本需求无关的 `CardWidth` / `BaseCardWidth` 基线问题。
