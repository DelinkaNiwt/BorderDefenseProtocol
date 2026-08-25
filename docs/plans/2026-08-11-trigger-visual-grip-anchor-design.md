# Trigger 视觉握持锚点设计

## 目标

为表达视觉预设增加正式握持锚点，并把主侧／右手与副侧／左手握持点接入现有点位可视化设施。首轮只增加数据、解算和诊断，不改变武器位置、角度、枪口或发射行为。

## 数据与解算

- `ExpressionVisualPresetDef` 增加可选 `Grip` 配置。
- `GripOffset` 使用与枪口一致的局部坐标：X 为左右、Y 为高度、Z 为武器前后。
- 基准参考图配置 `(0, 0, -0.1953125)`，对应图片 `(156, 255.5)`。
- `VisualPoseResolver` 按最终绘制角度与 Mesh（网格）镜像把握持偏移转换为世界坐标，输出 `ResolvedGripAnchor`。
- 未配置 `Grip` 时返回无效锚点；不影响旧预设。

## 现有诊断设施扩充

沿用现有链路：

`TriggerVisualPoseDiagnosticsAccess` → `TriggerVisualPoseDiagnosticsSnapshot` → `TriggerVisualMarkerOverlayDrawer`

- 在现有单武器诊断快照中增加握持点有效性、世界坐标和局部偏移。
- 在现有武器点位循环中绘制握持点以及武器中心到握持点的连线。
- 主侧／右手使用暖橙色，副侧／左手使用亮青色。
- 不新增 Gizmo（游戏按钮）、开关、地图组件、绘制入口或语言文案。

## 验证

- 基准 XML（可扩展标记语言）配置检查。
- Core（核心）握持锚点契约与解算链烟雾测试。
- Development（开发诊断）现有绘制器扩充烟雾测试。
- Core 与 Development 项目隔离输出构建。
