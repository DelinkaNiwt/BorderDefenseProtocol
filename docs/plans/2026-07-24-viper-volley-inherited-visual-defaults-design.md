# 毒蛇齐射继承视觉默认值设计

## 目标

让 BDP 双武器视觉的默认姿态保持中性：主侧和副侧都不附加默认装饰角。毒蛇齐射视觉预设不再重复抄写框架默认绘制与姿态参数，统一通过缺省配置回退。

本设计取代 `2026-07-24-viper-volley-default-visual-parameters-design.md` 中“毒蛇齐射显式写出全部默认姿态字段”的决定。

## 方案选择

已放弃：

1. 只把 C#（C#语言）默认值改成 0，但保留毒蛇显式 30；这会让毒蛇继续旋转。
2. 把毒蛇显式 30 改成显式 0；虽然画面正确，但仍重复维护框架默认值。

采用：

3. 把南北、东西姿态的 `SubHandAngleOffset`（副侧角度偏移）类默认值都改为 0；同时从毒蛇视觉预设删除 `DrawScale`（绘制缩放）、`SouthNorthPose`（南北姿态）、`EastWestPose`（东西姿态）三个显式配置块。

## 影响范围

- 主模组：
  - `ExpressionVisualSouthNorthPoseConfig.SubHandAngleOffset` 默认值由 `30f` 改为 `0f`。
  - `ExpressionVisualEastWestPoseConfig.SubHandAngleOffset` 默认值由 `30f` 改为 `0f`。
  - 同步注释为“默认不附加角度；作者可显式配置”。
- DevHarness（伴生测试模组）：
  - 毒蛇齐射视觉预设删除 `DrawScale`、`SouthNorthPose`、`EastWestPose`。
  - 保留 `GraphicData`（图形数据）与 `Muzzle`（枪口）原值。
- 其它视觉预设：
  - 已显式配置的 `8` 或 `10` 保持不变，不受新默认值影响。

## 运行时结果

- 缺少 `DrawScale` 时，字段初始化默认值为 `1`。
- 缺少南北/东西姿态块时，解析器分别创建默认配置对象。
- 默认主侧装饰角为 0，副侧额外装饰角也为 0。
- 南北高度层、手侧镜像和东西前后高度层等其它默认值保持不变。

## 验证

修改聚焦烟雾测试，使其先要求：

1. 两个 C# 默认值均为 `0f`。
2. 毒蛇预设不存在 `DrawScale`、`SouthNorthPose`、`EastWestPose` 节点。
3. 毒蛇贴图和枪口值保持不变。
4. 其它视觉预设中显式的 `8`、`10` 不被改动。

先确认测试因现有默认 30 和显式姿态块而失败，再实施最小修改并验证转绿。
