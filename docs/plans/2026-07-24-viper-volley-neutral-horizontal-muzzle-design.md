# 毒蛇齐射枪口横向偏移归零设计

## 目标

只取消 `BDP_TestVisual_PathLatchVolley`（毒蛇齐射视觉预设）的枪口横向偏移，保留枪口前向偏移 `0.68`，其它视觉参数不变。

## 原因

`MuzzleOffset`（主侧枪口偏移）和 `SubHandMuzzleOffsetOverride`（副侧枪口覆盖偏移）都是 `Vector3`（3维向量），XML（可扩展标记语言）不能只省略其中的横向 X 分量。副侧覆盖未启用时，原有解析逻辑会自动复用主侧 `MuzzleOffset`。

## 最小方案

- 主侧 `MuzzleOffset` 从 `(0.04, 0, 0.68)` 改为 `(0, 0, 0.68)`。
- 删除 `HasSubHandMuzzleOffsetOverride`（副侧枪口覆盖开关）。
- 删除 `SubHandMuzzleOffsetOverride`（副侧枪口覆盖偏移）。
- 保留 `IsRangedWeapon`（是否远程武器）和 `ExtraWorldOffset`（额外世界坐标偏移）的现有显式配置。

结果是主副侧都没有横向偏移，副侧通过原有默认回退逻辑继承主侧的前向 `0.68`。

## 验证

- 聚焦烟雾测试先对旧配置失败，再对新配置通过。
- 相关追踪齐射烟雾测试同步检查：主侧横向为零，副侧覆盖字段不存在。
- XML 文件保持可解析。
