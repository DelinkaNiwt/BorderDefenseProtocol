# 毒蛇路线中间续段散布减半设计

## 目标

在保留毒蛇首段原版发射命中链、最终段独立小散布收束、原版精度事实影响和路径安全约束的前提下，降低中间续段的视觉偏航幅度。

## 方案

仅调整路线引导模块的中间续段最大散布半径：

- `IntermediateSpreadRadius`：`1.25` 格 → `0.625` 格
- `FinalSpreadRadius`：保持 `0.30` 格
- `HighAccuracySpreadScale`：保持 `0.25`
- `SpreadSafetyShrinkSteps`：保持 `4`
- 首段、末段、原版射击报告快照、天气/技能/姿态/掩体影响链均不改

中间段实际散布仍按既有公式计算：

`中间段基准半径 × 原版精度缩放 × 稳定随机偏移 × 安全约束`

因此本次只压低基准半径，不改变随机分布、精度响应或碰撞判定。

## 修改边界

同步修改以下同义默认值，避免配置、存档快照和缺省回退不一致：

1. `RoutePathConfig.IntermediateSpreadRadius`
2. `RoutePathContext` 的默认值与重置值
3. `RoutePathModule` 的缺省回退值
4. 毒蛇路线引导 XML Def 的 `<IntermediateSpreadRadius>`
5. 对应静态检查中的期望值

不修改用户当前未提交的 BeamTrail（光束轨迹）相关文件。

## 验证

1. 运行毒蛇路线散布静态检查，确认中间段为 `0.625`、最终段仍为 `0.30`。
2. Release（发布配置）编译 Core.dll 与 Content.dll，要求 0 警告、0 错误，Core 隔离门禁通过。
3. 检查 Git 工作区时只纳入本次设计/实现相关文件，保留用户现有 BeamTrail 改动。

