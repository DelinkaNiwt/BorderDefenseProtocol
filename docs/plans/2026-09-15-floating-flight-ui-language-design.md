# 浮空芯片名称与按钮文案设计

## 已确认事实

- `ChipFunctionDef.ResolvedLabel` 读取 `labelKey.Translate()`，因此需要 Keyed 语言键 `BDP_ChipFunction_Floating`。
- 当前简体中文包只有 `BDP_ChipFunction_Floating.label` 的 DefInjected 键，不能满足自定义制造界面的裸键查询。
- 浮空按钮当前把完整当前模式和下一模式都放进按钮标签，造成标签过长；详细信息已经有鼠标提示和右键菜单承载。
- 联合重工影子已完成对比，本轮保留 BDP 当前直接绘制方案，不修改影子代码。

## 批准设计

1. 在简体中文 `Keyed/Gameplay.xml` 增加 `BDP_ChipFunction_Floating` 的中文名称键。
2. 增加三条短模式名语言键：`关闭`、`移动`、`始终`。
3. 浮空按钮改为 `浮空：{短模式名}`；当前/下一模式和屋顶限制继续留在描述文本中。
4. 右键模式菜单继续显示完整模式名称，不改变模式逻辑、聚合逻辑或影子实现。

## 验证范围

- 浮空定义烟测必须确认 Keyed 芯片名称键和三条短模式键存在。
- 浮空按钮烟测必须确认按钮只传入当前短模式标签，描述仍接收当前/下一模式。
- Content Release 编译和全部 FloatingFlight 烟测必须通过。
