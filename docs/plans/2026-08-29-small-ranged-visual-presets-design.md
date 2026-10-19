# BDP 小型枪械视觉预设设计

## 目标

为 BDP 双武器绘制系统增加与中型枪械基类同级的小型枪械单持、双持视觉预设；让左轮手枪改为继承小型枪械预设；把南北朝向的双持握持目标向外、向上调整到用户标注的白圈附近；并将当前突击步枪实际使用的参考预设改名为突击步枪预设。

## 已确认事实

- 当前中型枪械单持基类为 `BDP_VisualBase_RangedMedium`。
- 当前中型枪械双持基类为 `BDP_VisualBase_RangedMedium_Dual`，继承中型单持基类并启用 `UseAsPoseOrigin`。
- 左轮单持预设 `BDP_Visual_Pistol` 当前继承 `BDP_Visual_SubmachineGun`。
- 左轮双持预设 `BDP_Visual_Pistol_Dual` 当前继承 `BDP_Visual_SubmachineGun_Dual`。
- 突击步枪构型当前实际引用 `BDP_Visual_RangedWeaponReference` 与 `BDP_Visual_RangedWeaponReference_Dual`。
- 工程中另有未被当前 XML 消费的旧 `BDP_Visual_AssaultRifle`，它使用原版突击步枪贴图。

## 设计方案

### 小型枪械基类

新增两个与中型枪械基类同级的抽象预设：

- `BDP_VisualBase_RangedSmall`
- `BDP_VisualBase_RangedSmall_Dual`

小型单持基类承载小型远程武器的公共数据：

- `DrawScale = 1`
- `GripOffset = (0, 0, -0.1953125)`
- `MuzzleOffset = (0, 0, 0.3203125)`
- `IsRangedWeapon = true`

小型双持基类承载双持姿态数据，东西朝向沿用中型双持基类的既有规则；南北朝向调整为：

- `DefaultOffset = (0.36, 0, 0.13)`
- `NorthZAdjust = 0.26`
- `HandMirrorOnlyWhenIdle = true`
- `UseAsPoseOrigin = true`

这使南北两侧的握持目标相对中型双持基类同时向外、向上移动，南北两向保持对称。

### 左轮继承关系

调整为：

```text
BDP_Visual_Pistol
    → BDP_VisualBase_RangedSmall

BDP_Visual_Pistol_Dual
    → BDP_VisualBase_RangedSmall_Dual
```

左轮预设继续使用自己的贴图；小型枪口偏移由小型基类提供，避免单持和双持重复声明。

### 突击步枪预设改名

将当前实际使用的预设改名：

```text
BDP_Visual_RangedWeaponReference
    → BDP_Visual_AssaultRifle

BDP_Visual_RangedWeaponReference_Dual
    → BDP_Visual_AssaultRifle_Dual
```

同步更新突击步枪构型引用、相关测试和本地化标签。删除原先未被消费的旧 `BDP_Visual_AssaultRifle` 定义，避免同名冲突。

## 边界

- 不修改握持诊断点颜色；颜色仍由 Development 诊断绘制器负责。
- 不修改中型枪械现有姿态和锚点参数。
- 不修改 Core 运行时解算逻辑；复用现有 XML 继承与握持原点机制。
- 不纳入已退役 `BorderDefenseProtocol.DevHarness` 与 Legacy 模组。
- 不触碰工作区中与本需求无关的既有未提交改动。

## 验证

- 增加或更新 XML 继承与消费者引用冒烟测试。
- 运行握持锚点、南北高度、双持视觉和相关武器预设测试。
- 检查 XML 结构、构建 Content 与 Development 程序集。
- 最终需要在游戏中分别观察左轮南北双持和突击步枪单双持的握持点、枪口点与贴图位置。
