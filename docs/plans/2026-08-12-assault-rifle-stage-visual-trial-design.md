# 突击步枪阶段视觉试验设计

## 目标

只对 `BDP_GunClass_AssaultRifle`（突击步枪枪壳）当前独占使用的单武器与双武器视觉预设增加阶段表现：空闲保持原远程参考贴图，预热改用霰弹枪岚参考贴图，射击和最终冷却隐藏整套武器视觉。

## 配置归属

- 单武器预设：`BDP_Visual_RangedWeaponReference`
- 双武器预设：`BDP_Visual_RangedWeaponReference_Dual`
- 预热贴图：`Things/Trigger/Visual/ShotgunReferenceLan`
- 配置位置：`1.6/Content/Defs/ExpressionDef/Visual.xml`

这两个预设当前只被突击步枪枪壳引用，因此直接配置不会影响其它枪壳。Core（核心程序集）不改动。

## 阶段规则

- `Idle`（空闲）：不声明覆盖，继续使用原远程参考贴图。
- `Warmup`（预热）：可见，主贴图换为霰弹枪岚参考贴图。
- `Firing`（射击）：不可见；四连发内部间隔也保持隐藏。
- `Cooldown`（最终冷却）：不可见；结束后自动回到空闲贴图。

显隐只影响视觉绘制，不改变姿态、握持点、枪口锚点、后坐力、攻击节奏或投射物。

## 验证

新增正式配置回归，逐一验证两个预设的三个阶段条目、贴图路径、可见性、中文注释和未声明 `Idle`。同时解析 XML，并运行阶段视觉现有回归测试。

