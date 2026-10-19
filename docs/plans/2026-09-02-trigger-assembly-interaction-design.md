# 触发器装配台交互逻辑设计

## 目标

修正装配台三处交互语义：槽位选择不应意外筛选芯片库；芯片预占用只反映实际已装配芯片；拖拽时只高亮可接受当前芯片的槽位。

## 已确认的根因

1. 中栏 `ShouldShowChip` 把选中槽位兼容性作为隐藏条件，兼容性筛选入口仍存在，所以点击左栏槽位会让不兼容芯片从列表消失。
2. `TriggerAssemblyPreviewService.BuildSnapshot` 将当前选中芯片当作“预览替换”加到 `PreviewReserved`，导致选择库存芯片也改变资源条。
3. `Panel_SlotLayout.DrawSlot` 只判断是否存在拖拽状态，没有判断拖拽芯片的槽位类型，因此所有非镜像槽位都会显示可放置高亮。

## 设计决策

- 中栏列表只允许搜索框和芯片分类按钮筛选；移除“显示不可用”兼容性筛选开关及其筛选条件，左栏选中槽位不得改变列表内容。
- 预览服务不再接收选中槽位、选中芯片等 UI 选择参数。`Reserved` 与 `PreviewReserved` 都直接取 `CalculateLoadedCapacity`，该方法只遍历正式读取口中的已装芯片，并对双持镜像去重。
- 新增装配窗口范围内的芯片槽位兼容性小工具，统一库存卡片兼容性视觉与左栏拖拽目标判断：`Main`、`Sub` 视为 `MainSub`，只有 `Special` 对应 `Special`。
- 本次只调整高亮显示，不改变事务层已有的装配校验与失败提示；不兼容目标不会被视觉误导为可放置目标。

## 影响范围

- `Source/BDP.Content/Assembly/Window/Panel_ChipInventory.cs`
- `Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- `Source/BDP.Content/Assembly/Window/TriggerAssemblyPreviewService.cs`
- `Source/BDP.Content/Assembly/Window/Window_TriggerAssembly.cs`
- 新增 `Source/BDP.Content/Assembly/Window/TriggerAssemblyChipCompatibility.cs`
- `Source/BDP.Tests/TriggerAssemblyWindowSmokeTests.ps1`
- `Source/BDP.Tests/TriggerAssemblerLeftMiddlePanelSmokeTests.ps1`

## 验收标准

- 点击任意左栏槽位后，中栏仍显示全部搜索/分类匹配芯片，不再存在按当前槽位兼容性隐藏的路径。
- 选中任意库存芯片或槽位芯片，资源条数值不发生选择性变化；只有成功实际装配/卸载后，下一帧累计值变化。
- 拖拽主副槽芯片时只有主副槽高亮，拖拽特殊槽芯片时只有特殊槽高亮；绑定镜像槽位仍不作为目标高亮。
- 契约测试、`BDP.Content` Release 构建、差异空白检查全部通过。
