# Trion Gizmo 芯片面板合并重构设计（第一版）

## 1. 目标

把当前分离的两个 Gizmo（小控件）合并为一个正式 UI（界面）：

- 左侧保留现有 `Trion` 能量条。
- 右侧从 `Trion` 能量条扩展出触发体芯片槽面板。
- 主槽、副槽上下两行显示，槽位数量按真实触发体配置向右延伸。
- 芯片激活、关闭、切换、禁用、切换中状态，全部在该 Gizmo 内完成预览和交互。
- 特殊侧芯片不进入主/副槽位行，继续使用 `Trion` 顶部扩展图标区展示。

这个 UI 是玩家正式 UI，不限制在 `DevMode`（开发者模式）。诊断按钮、装入测试芯片等开发能力仍然只属于 `DevHarness`（伴生测试模组）调试入口。

## 2. 当前状态

当前实现分成两块：

- 主模组 `Gizmo_TrionStatus` 绘制 `Trion` 能量条，并支持顶部右侧扩展徽标。
- `DevHarness` 通过 `DevHarnessTriggerGizmoProvider` 注册独立的触发体芯片 Gizmo。
- 独立芯片 Gizmo 当前只预览主/副两侧代表槽位，完整操作需要打开详情窗口。

已有正式接口足够支撑新面板：

- `ITriggerLoadoutReader`：读取主、副、特殊侧槽位和激活状态。
- `ITriggerInteractionReader`：读取槽位当前应被理解为激活、关闭、切换、镜像受控或不可用。
- `ITriggerLoadoutCommands`：提交正式激活、关闭、装入、卸下、销毁等请求。
- `ITrionGizmoExtensionProvider`：现有 `Trion` 徽标扩展口，目前只支持徽标，不支持外部绘制面板。

结论：不需要改 Trigger 真值、不需要绕过现有切换服务，核心改动是把 `Trion` Gizmo 扩展点从“只能塞图标”升级为“可塞图标，也可塞右侧面板”。

## 3. 边界原则

### 3.1 主模组边界

主模组只做架构基础设施：

- 提供可横向扩展的 `Trion` Gizmo 容器。
- 提供右侧面板扩展接口。
- 负责统一背景、高度、宽度、鼠标事件传递和基础 tooltip（提示文本）规则。
- 保留现有徽标扩展能力。

主模组不得：

- 直接引用 `Trigger` 芯片业务 UI。
- 直接读取芯片 Def（定义）来决定显示。
- 直接提交芯片激活/关闭命令。
- 把 `DevHarness` 测试芯片概念写进主模组。

### 3.2 DevHarness 边界

`DevHarness` 实现具体芯片面板：

- 从当前 Pawn（角色）解析触发体正式读取口、交互口、命令口。
- 按主/副槽位绘制格子。
- 根据正式交互语义提交激活、关闭、切换。
- 只在有 `Trion` 且有手持触发体时插入右侧面板。

`DevHarness` 不反向污染主模组；未来正式业务模组也可以复用同一面板扩展口。

## 4. 总体设计

布局结构：

```text
+--------------------------+---------------------------------------+
| Trion 能量                | 特殊侧/状态徽标区                    |
| [======================]  | 主 [槽][槽][槽][槽]                 |
| 可用 / 恢复或消耗         | 副 [槽][槽][槽][槽]                 |
+--------------------------+---------------------------------------+
```

显示条件：

- 没有 `Trion`：不显示 `Trion` Gizmo，保持现状。
- 有 `Trion`，没有触发体：只显示左侧 `Trion` 能量条。
- 有 `Trion`，有触发体：显示左侧 `Trion` 能量条 + 右侧芯片面板。

## 5. 主模组扩展设计

### 5.1 保留现有徽标扩展

现有 `ITrionGizmoExtensionProvider.GetBadges(...)` 继续有效。紧急脱离等已经接入顶部图标区的表现不重做、不迁移，只保证新版布局不会覆盖它。

特殊侧芯片显示规则：

- 主/副槽位行只显示 `TriggerSide.Main` 和 `TriggerSide.Sub`。
- `TriggerSide.Special` 不作为槽位格显示。
- 特殊侧相关状态继续走 `Trion` 顶部扩展徽标区。
- 紧急脱离芯片已有图标/徽标时，本轮只保留和协调位置，不重写其业务判定。

### 5.2 新增右侧面板扩展口

新增一个中性接口，职责只限 UI 面板：

```csharp
public interface ITrionGizmoPanelExtensionProvider
{
    float GetWidth(TrionGizmoExtensionContext context);

    GizmoResult DrawPanel(
        TrionGizmoExtensionContext context,
        Rect panelRect,
        GizmoRenderParms parms);
}
```

说明：

- `GetWidth` 返回当前扩展面板需要的宽度。
- 返回 `0` 或负数表示当前不显示。
- `DrawPanel` 只绘制给定矩形，并返回是否发生交互。
- 主模组不理解面板内容，只负责把矩形交给扩展。

### 5.3 Registry（注册表）调整

现有 `TrionGizmoExtensionRegistry` 扩展为两类读取：

- `GetBadges(...)`：继续返回徽标。
- `GetPanels(...)`：返回可显示的右侧面板提供器。

如果未来多个面板同时注册，第一版只支持“第一个有效面板”。原因：

- 当前需求只有芯片槽面板一个右侧大面板。
- 多面板横向叠加会很快挤占原版 Gizmo 区域。
- 先保持最小必要性，后续确有需求再加优先级或分栏。

## 6. Trion Gizmo 布局调整

### 6.1 尺寸

基础 `Trion` 区域维持当前视觉比例：

- 高度继续接近原版 Gizmo 高度。
- 左侧能量条区域宽度保持稳定，避免玩家熟悉的资源条跳动。
- 右侧面板按槽位数量请求宽度。

建议第一版尺寸：

- `Trion` 左侧基础宽度：沿用当前 `228f`。
- 面板左边距：`4f`。
- 槽位格：`32f`。
- 槽位间距：`5f`。
- 行高：`34f`。
- 主副两行共用当前卡片高度，避免整体变高。

宽度计算示例：

```text
总宽度 = Trion基础宽度 + 面板间距 + 面板宽度
面板宽度 = 行标签宽度 + max(主槽数, 副槽数) * 槽位格宽 + 间距
```

### 6.2 徽标区

顶部右侧徽标区仍属于 `Trion` 主卡内部，而不是芯片槽面板内部。

这样特殊侧芯片、战斗体状态、恢复冻结等状态仍然集中在同一块“状态徽标区”，不会和主/副槽位混在一起。

### 6.3 视觉风格

风格应贴近 RimWorld 原版：

- 深色窗口背景。
- 简单细边框。
- 少量高饱和色只用于状态强调。
- 不做大面积渐变、动画装饰或复杂材质。

目标是“战斗中一眼能懂”，不是做装备编辑器。

## 7. DevHarness 芯片面板设计

### 7.1 数据来源

面板每帧只读正式面：

- `TriggerSurfaceAccess.ResolveLoadoutReader(ownerPawn)`
- `TriggerSurfaceAccess.ResolveInteractionReader(ownerPawn)`
- `TriggerSurfaceAccess.ResolveLoadoutCommands(ownerPawn)`

显示槽位时：

- 主行读取 `loadoutReader.GetSlots(TriggerSide.Main)`。
- 副行读取 `loadoutReader.GetSlots(TriggerSide.Sub)`。
- 特殊侧不读取为槽位行，只允许徽标提供器处理。

### 7.2 槽位格内容

每个槽位格显示：

- 空槽：灰色空框。
- 已装芯片：芯片 `uiIcon`。
- 被禁用：图标透明度降低，边框使用禁用色。
- 镜像受控：使用镜像色边框，不作为独立操作入口。
- 当前激活：使用激活色边框。
- 可切换目标：使用待命色边框。
- 不可用：降低亮度，tooltip 显示原因。

槽位内部不显示长文本，避免拥挤。芯片名、状态、原因放入 tooltip。

### 7.3 状态颜色

第一版推荐颜色语义：

- 空槽：暗灰。
- 已装未激活：低饱和黄灰。
- 激活：青绿或绿色高亮。
- 禁用：暗红。
- 镜像受控：蓝色。
- 切入中：蓝青进度条。
- 退场中：橙色进度条。

颜色只是提示，正式行为仍以 `ITriggerInteractionReader` 返回的语义为准。

### 7.4 切换进度条

槽位格底部保留细进度条：

- `WarmingUp`（切入前摇）：从左向右增长。
- `WindingDown`（关闭/被切走后摇）：从右向左退。

后摇不能再表现成“从左往右跑满”，因为视觉语义会像“正在充能”。右往左退更符合“退出、收回、冷却离场”。

实现方式：

- 切入：`FillableBar(rect, progress)`，进度从 `0 -> 1`。
- 退场：用右对齐填充矩形，宽度为剩余比例或 `1 - progress`。

退场条语义：

```text
退场开始: [########]
退场中段: [    ####]
退场结束: [        ]
```

### 7.5 点击交互

点击槽位格时，不直接猜动作，必须读取交互语义：

- `Activate` 或 `SwitchTo`：调用 `RequestActivate(controlSide, controlSlotIndex)`。
- `Deactivate`：调用 `RequestDeactivate(controlSide)`。
- `Mirror`：不直接操作，tooltip 指向控制槽。
- `Unavailable`：不给命令，只给轻量提示。
- 空槽：第一版不在正式 UI 中提供装入测试芯片能力。

右键第一版不做额外菜单。卸下、装入测试芯片仍留在 DevMode 诊断窗口或现有装配流程里，避免正式战斗 UI 承担编辑器职责。

## 8. Tooltip 设计

整块面板 tooltip：

- 显示当前触发体主/副侧槽位摘要。
- 显示当前是否有切换中状态。

单槽 tooltip：

- 侧别和槽位编号。
- 芯片名称。
- 激活状态。
- 禁用状态与禁用原因。
- 镜像关系。
- 当前点击会执行的动作。
- 切换进度和阶段。

tooltip 只解释当前状态，不写长篇教程。

## 9. 特殊侧与紧急脱离

特殊侧不进主/副槽位网格，原因：

- 特殊侧通常不是玩家在主副手之间频繁切换的武器槽。
- 它更像被动、附加段、状态能力。
- 当前 `Trion` 顶部徽标区已经是为这类状态预留的区域。

紧急脱离芯片已有图标/状态提示时：

- 继续显示在顶部徽标区。
- 新芯片面板不得重复显示成槽位格。
- 如果顶部徽标区空间不足，优先保留高价值状态：战斗体状态、紧急脱离、恢复冻结。

本轮不重做紧急脱离解析，不改 `CombatBodyEmergencyEscapeResolver`。

## 10. 方案对比

### 方案 A：把 Trigger Gizmo 直接塞进 Trion Gizmo

优点：

- 改动看似少。

缺点：

- 主模组会认识 Trigger 芯片 UI。
- 破坏主模组中性边界。
- 后续其它模组难以复用。

结论：不采用。

### 方案 B：主模组提供 Trion 面板扩展口，DevHarness 实现芯片面板

优点：

- 主模组只做通用设施。
- DevHarness 承担业务 UI。
- 与现有徽标扩展思路一致。
- 后续正式业务模组可复用。

缺点：

- 需要新增一个面板扩展接口。

结论：采用。

### 方案 C：继续独立两个 Gizmo，只优化芯片 Gizmo

优点：

- 对 `Trion` Gizmo 改动最少。

缺点：

- 不满足合并需求。
- 玩家仍要在两个信息块之间切换视线。
- 特殊侧和主副槽关系不够清楚。

结论：不采用。

## 11. 文件影响预估

主模组：

- `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
  - 改为左侧基础区 + 右侧扩展面板区。
  - 保留现有能量条、徽标、tooltip。
- `Source/BDP/Core/Trion/External/ITrionGizmoExtensionProvider.cs`
  - 可保留原接口，或新增独立面板接口。
- `Source/BDP/Core/Trion/External/TrionGizmoExtensionRegistry.cs`
  - 增加面板提供器注册与读取。
- `Source/BDP/Core/Trion/External/TrionGizmoExtensionContext.cs`
  - 需要补充面板绘制所需上下文时，只加中性字段。

DevHarness：

- `Source/BDP.DevHarness/DevHarnessBootstrap.cs`
  - 注册芯片面板提供器。
- 新增 `Source/BDP.DevHarness/Gizmo_TrionTriggerLoadoutPanelProvider.cs`
  - 负责判断是否显示与绘制芯片面板。
- 可复用或迁移 `Gizmo_LegacyTriggerStatus` / `Window_LegacyTriggerSlots` 中的颜色、tooltip、切换进度计算逻辑。

测试：

- 新增主模组 smoke test（烟测）锁定 `Trion` 面板扩展接口存在，且主模组不引用 DevHarness。
- 新增 DevHarness smoke test 锁定芯片面板使用正式 Trigger surface，不直接操作内部槽位状态。
- 新增 UI 语义 smoke test 锁定后摇进度条存在右向左绘制逻辑。

## 12. 不做范围

本轮不做：

- 不重写 Trigger 切换流程。
- 不改芯片装载、卸载、绑定规则。
- 不把特殊侧芯片画进主/副槽位行。
- 不重做紧急脱离图标或判定。
- 不在正式 UI 中提供测试芯片装入菜单。
- 不移植旧版大窗口作为正式入口。
- 不改 `Legacy`（旧版模组）。

## 13. 风险与应对

### 13.1 Gizmo 宽度过宽

风险：槽位数量增加后，Gizmo 可能占据太多横向空间。

应对：

- 第一版完整显示所有槽位，满足当前需求。
- 如果未来触发体槽位极多，再单独设计折叠或分页，不在本轮预做。

### 13.2 点击语义误判

风险：UI 自己判断激活/关闭，可能绕开正式切换规则。

应对：

- 面板只读 `ITriggerInteractionReader`。
- 点击只根据正式 `OperationKind` 调用 `ITriggerLoadoutCommands`。

### 13.3 主模组业务污染

风险：为了画芯片面板，把 Trigger 业务塞进 `Trion`。

应对：

- 主模组只新增通用面板扩展口。
- 芯片面板在 DevHarness 注册。
- 主模组 smoke test 锁定不引用 DevHarness 命名空间。

### 13.4 特殊侧重复显示

风险：特殊侧既显示为徽标，又显示为槽位，造成理解混乱。

应对：

- DevHarness 面板只画主/副。
- 特殊侧只走徽标区。
- 文档和测试明确这一条。

## 14. 验证口径

静态验证：

- 主模组 Release 构建通过。
- DevHarness Release 构建通过。
- 新增 smoke tests 通过。
- `rg "BDP.DevHarness" Source/BDP/Core/Trion` 无结果。

游戏内验证：

1. Pawn 有 Trion、无触发体：只显示 Trion 能量条。
2. Pawn 有 Trion、有触发体：显示合并面板。
3. 主/副各多个槽位：全部槽位显示，向右延伸。
4. 点击未激活芯片：提交激活或切换。
5. 点击已激活芯片：提交关闭。
6. 切入前摇：槽位底部进度条从左向右。
7. 关闭/切走后摇：槽位底部进度条从右向左退。
8. 禁用槽：颜色变暗/偏红，点击不给非法命令。
9. 镜像槽：颜色偏蓝，tooltip 指向控制槽。
10. 特殊侧紧急脱离芯片：仍在顶部徽标区显示，不进入主/副槽位行。

## 15. 实施顺序建议

1. 主模组新增 `Trion` 右侧面板扩展接口和注册表能力。
2. 调整 `Gizmo_TrionStatus` 布局，让它能容纳一个右侧面板。
3. DevHarness 新增芯片面板提供器，只读正式 Trigger surface。
4. 迁移槽位颜色、tooltip、点击交互、切换进度条。
5. 移除或隐藏旧的独立芯片状态 Gizmo，避免重复显示。
6. 补 smoke tests 和游戏内实测。

## 16. 最终判断

采用“主模组预留 Trion 面板扩展口，DevHarness 插入芯片面板”的方案。

这符合 BDP 当前边界：主模组做中性设施，伴生测试模组承载芯片业务 UI。它也能满足玩家正式 UI 的可读性：资源、状态、主副槽、特殊侧徽标都集中在一个 Gizmo 内，但不把所有业务揉进主模组。
