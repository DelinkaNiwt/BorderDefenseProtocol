# 冲锋枪武装构型设计

## 目标

为 BDP（Border Defense Protocol，边境防御协议）枪手职业新增一个冲锋枪型武装构型，使用 P90 风格 512×512 贴图。该构型定位为中近程、高射速、低单发威力的轻型远程武装，并支持单持与双持视觉。

## 已确认事实

- 当前枪手武装构型由 `ChipArmamentFormDef`（芯片武装构型定义）承载，正式业务内容位于 `1.6/Content/Defs`。
- 当前突击步枪构型使用 `BDP_Visual_RangedWeaponReference`（单持）及其双持预设，并使用中型枪械基准姿态。
- 中型枪械基准使用 512×512 画布中心作为姿态基准；突击步枪枪口锚点为中心向枪口侧 250 像素，即 `MuzzleOffset.Z = 0.48828125`。
- 用户提供的 P90 贴图为 512×512 正侧视图，枪口约位于画布 `x=440` 像素。
- 用户确认每次攻击使用 6～8 发连发；本设计选择 7 发作为标准方案。
- 当前 BDP 普通弹基础伤害为 13，突击步枪为 4 发、伤害倍率 1.0；突击步枪总连发基准约为 52 点。
- 原版 `Gun_HeavySMG`（重型冲锋枪）提供中近程、高贴身精度和较短射程的参数参考；本构型不直接复制原版武器 Def，而是沿用 BDP 的表达和投射物系统。

## 设计方案

### 玩家可见定位

冲锋枪是突击步枪的近距离机动侧向选择：

- 近距离命中率更高，远距离衰减明显。
- 预热与冷却更短，连续交战反应更快。
- 单发威力和停止力低于突击步枪。
- 通过 7 发连发维持稳定的近距离压制力。
- 不新增投射物 Def（定义），沿用当前芯片功能自身的投射物，只通过武装构型覆盖倍率调整火力。

### 标准参数

| 字段 | 值 | 说明 |
|---|---:|---|
| `range`（射程） | 24.0 | 低于突击步枪 30.9，高于极近程手枪 |
| `accuracyTouch`（贴身精度） | 0.83 | 近距离可靠 |
| `accuracyShort`（近距精度） | 0.72 | 保持主要作战区间 |
| `accuracyMedium`（中距精度） | 0.44 | 开始明显衰减 |
| `accuracyLong`（远距精度） | 0.22 | 不承担远程狙击定位 |
| `warmupTime`（预热时间） | 0.7 | 快于突击步枪 1.0 秒 |
| `defaultCooldownTime`（冷却时间） | 1.15 | 快于突击步枪 1.7 秒 |
| `burstShotCount`（连发数量） | 7 | 用户确认的 6～8 发区间中取中值 |
| `ticksBetweenBurstShots`（连发间隔） | 7 | 形成紧凑连续火力 |
| `rhythm`（攻击节奏） | `Sequential`（逐发） | 沿用枪手现有连续射击表达 |
| `damageMultiplier`（伤害倍率） | 0.55 | 每发约 7.15 点，7 发总量约 50 点 |
| `stoppingPowerMultiplier`（停止力倍率） | 0.75 | 每发停止力低于突击步枪，避免高射速过度击退 |
| `soundCast`（开火音效） | `Shot_MachinePistol` | 沿用原版轻型高射速枪声 |
| `noiseRadius`（噪声半径） | 14 | 小于突击步枪 18 |
| `muzzleFlashScale`（枪口闪光倍率） | 0.8 | 保持高射速下的可见性但不超过霰弹枪 |
| `additionalCost/Steel`（追加钢材） | 30 | 位于左轮手枪与突击步枪之间 |
| `additionalWorkAmount`（追加工作量） | 900 | 位于左轮手枪与突击步枪之间 |

### 视觉与枪口锚点

新增独立视觉预设，避免修改突击步枪现有贴图：

- 单持：`BDP_Visual_SubmachineGun`。
- 双持：`BDP_Visual_SubmachineGun_Dual`。
- 贴图路径：`Things/Trigger/Visual/P90Reference`。
- 贴图来源：`参考资源/贴图工程/P90_校正.png`。
- 继续继承 `BDP_VisualBase_RangedMedium`（中型枪械单持基准）和 `BDP_VisualBase_RangedMedium_Dual`（中型枪械双持基准）。
- 保持 `DrawScale`（绘制缩放）为 1，不修改 P90 贴图比例。
- 单持与双持都将枪口锚点设置为：

  `MuzzleOffset = (0, 0, 0.359375)`

  计算依据为 `(440 - 256) / 512 = 0.359375`；其中 `Z` 是武器朝向目标的前后轴，不是直接填写像素坐标。

- 暂时沿用中型枪械基准的握持锚点。若进游戏后手位不贴合，只调整冲锋枪视觉预设自己的 `GripOffset`（握持偏移），不修改共享基准。

## 结构与数据流

1. 制造台读取新的 `ChipArmamentFormDef`，将其限制在 `BDP_ChipProfession_Gunner`（枪手职业）。
2. 武装构型解析服务把上述射程、精度、节奏、音效和投射物倍率覆盖到芯片功能表达条目。
3. 视觉解析服务根据单持/双持状态选择对应 P90 视觉预设。
4. 枪口解析服务从视觉预设读取 `MuzzleOffset`，按最终姿态、朝向和镜像计算实际发射基准。
5. 现有投射物、拖尾、芯片功能和 Core 远程攻击链保持不变。

## 最小改动范围

- 修改 `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`：新增冲锋枪武装构型。
- 修改 `1.6/Content/Defs/ExpressionDef/Visual.xml`：新增单持/双持 P90 视觉及枪口锚点。
- 修改 `Languages/ChineseSimplified (简体中文)/DefInjected/ChipArmamentFormDef/Presets.xml`：新增中文名称和说明。
- 新增 `1.6/Textures/Things/Trigger/Visual/P90Reference.png`：复制已确认的 P90 贴图。
- 新增或扩展现有 Content 冒烟检查，覆盖 Def 引用、参数、贴图、单持/双持和锚点。
- 不修改 `Core.dll`、`Development.dll`、远程攻击运行时代码或共享中型枪械基准。

## 验证与进游戏试用

### 静态验证

- XML 可解析，DefName 唯一。
- 新构型只允许枪手职业，且出现在制造台。
- 单持/双持视觉预设均能解析，贴图路径存在且为 512×512。
- `MuzzleOffset.Z` 为 `0.359375`，突击步枪原值保持 `0.48828125`。
- 解析后的攻击参数为 24.0 射程、7 发连发、0.7 秒预热、1.15 秒冷却和 0.55 伤害倍率。

### 游戏内验证

使用现有 Development（开发诊断）枪口/握持标记检查：

- 南、北、东、西四个朝向的单持枪口位置。
- 双持两把 P90 的枪口是否分别跟随各自武器。
- 预热、连续射击和冷却阶段枪口是否始终从贴图右端约 440 像素位置发射。
- 近距离命中率、7 发连发节奏、单发伤害、停止力和冷却手感是否符合预期。

若实际体验中火力偏强或偏弱，下一轮只优先调整 `damageMultiplier`、`burstShotCount` 或冷却时间，不改变视觉锚点与模块边界。
