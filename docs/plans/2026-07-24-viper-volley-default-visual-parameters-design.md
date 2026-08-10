# 毒蛇齐射默认视觉参数设计

## 目标

只调整 DevHarness（伴生测试模组）中 `BDP_TestVisual_PathLatchVolley` 的以下三组显式配置：

1. `DrawScale`（绘制缩放）。
2. `SouthNorthPose`（南北姿态）。
3. `EastWestPose`（东西姿态）。

原来显式存在的字段继续显式保留；原来没有写的字段不新增。

## 最小改动

- `DrawScale` 改为 `1`。
- `SouthNorthPose` 中偏离类默认值的字段改回：
  - `DefaultOffset=(0, 0, 0)`
  - `DefaultAngle=0`
  - `DefaultAltitudeOffset=0.1`
  - `SouthZAdjust=0`
  - `NorthZAdjust=0`
  - `SubHandAngleOffset=30`
  - `MirrorOnNorth=false`
- `SouthNorthPose` 中本来已经等于默认值的 `AimMirror=true`、`HandMirror=true` 保持不变。
- `EastWestPose` 中偏离类默认值的字段改回：
  - `SideBaseX=0`
  - `SideDeltaX=0`
  - `SideDeltaZ=0`
  - `FrontAltitudeOffset=0.1`
  - `BackAltitudeOffset=-0.1`
  - `SubHandAngleOffset=30`
- `EastWestPose` 中本来已经等于默认值的 `DefaultAngle=0`、`AimMirror=true`、`HandMirror=false` 保持不变。

## 不改内容

- `GraphicData`（图形数据）及 `viper_salvo` 贴图路径。
- `Presentation`（表现入口）。
- `Muzzle`（枪口）全部字段。
- 其它视觉预设、芯片功能、射击数值和运行时代码。

只同步修正目标预设内因缩放与姿态恢复默认而失实的注释。

## 验证

先在现有追踪模块烟雾测试中加入目标预设默认参数断言，并确认当前配置按预期失败；再修改 XML（可扩展标记语言）使其通过。随后执行 XML 解析、相关视觉/追踪烟雾测试和差异检查。
