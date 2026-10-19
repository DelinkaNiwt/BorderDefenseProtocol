# 左轮手枪单武器与双武器视觉设计

## 目标

为 BDP（Border Defense Protocol，边境防御协议）枪手的左轮手枪武装构型补齐单武器与双武器绘制。两种状态都使用用户提供的 512×512 左轮贴图，并把枪口锚点放在贴图约 x=420 像素处。

## 已确认事实

- `BDP_GunClass_Revolver` 当前已经绑定 `BDP_Visual_Pistol`，但没有 `compositeVisualPresetDefName`（复合视觉预设名称）。
- `BDP_Visual_Pistol` 当前只声明原版 Revolver 贴图，没有继承冲锋枪视觉预设，也没有显式枪口锚点。
- 冲锋枪单武器预设为 `BDP_Visual_SubmachineGun`，双武器预设为 `BDP_Visual_SubmachineGun_Dual`。
- 中型枪械基准使用 512×512 画布中心 x=256 作为锚点换算基准。
- 用户提供的图片为 512×512、带透明背景的左轮贴图；工作区中的 `参考资源/贴图工程/崩雷左轮_校正.png` 与附件内容一致。

## 设计决策

### 构型绑定

保留现有单武器视觉名称，避免无意义的重命名：

- `visualPresetDefName` 保持 `BDP_Visual_Pistol`。
- 新增 `compositeVisualPresetDefName`，值为 `BDP_Visual_Pistol_Dual`。

单武器关系使用单侧预设；双武器关系由运行时解析器优先使用复合预设。

### 单武器预设

让 `BDP_Visual_Pistol` 直接继承 `BDP_Visual_SubmachineGun`，只覆盖左轮自己的贴图与枪口锚点：

- `ParentName = BDP_Visual_SubmachineGun`
- `GraphicData.texPath = Things/Trigger/Visual/RevolverReference`
- `GraphicData.graphicClass = Graphic_Single`
- `Muzzle.IsRangedWeapon = true`
- `Muzzle.MuzzleOffset = (0, 0, 0.3203125)`

这样会继承冲锋枪单武器的标准缩放和握持锚点，不新增南北/东西姿态覆盖，继续使用原版单武器姿态。

### 双武器预设

新增 `BDP_Visual_Pistol_Dual`，直接继承 `BDP_Visual_SubmachineGun_Dual`：

- `ParentName = BDP_Visual_SubmachineGun_Dual`
- `defName = BDP_Visual_Pistol_Dual`
- `GraphicData.texPath = Things/Trigger/Visual/RevolverReference`
- `GraphicData.graphicClass = Graphic_Single`
- `Muzzle.IsRangedWeapon = true`
- `Muzzle.MuzzleOffset = (0, 0, 0.3203125)`

这样会复用冲锋枪双武器的主副手分离、遮挡、镜像和握持定位规则，只替换贴图和枪口点。

### 枪口换算

```text
(420 - 256) / 512 = 0.3203125
```

这里的 `0.3203125` 写入 `MuzzleOffset.Z`；`Z` 是 BDP 视觉系统中沿武器朝向目标的局部轴，不是直接把像素值写入 XML。

### 非目标范围

- 不修改左轮射程、精度、连发、伤害、音效等战斗属性。
- 不修改冲锋枪现有预设或共享中型枪械基准。
- 不修改 `Core.dll`、`Development.dll` 或绘制运行时代码。
- 不新增组合技阶段视觉。

## 文件范围

- 修改 `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`：增加左轮双武器视觉绑定。
- 修改 `1.6/Content/Defs/ExpressionDef/Visual.xml`：改造左轮单武器预设，新增左轮双武器预设。
- 新增 `Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/Pistol.xml`：提供双武器视觉预设的中文诊断名称。
- 新增 `1.6/Textures/Things/Trigger/Visual/RevolverReference.png`：复制已确认的左轮贴图。
- 新增左轮视觉配置冒烟测试，覆盖单武器、双武器、贴图路径和枪口锚点。

## 验收标准

- 左轮构型同时声明单武器和双武器视觉预设。
- 单武器预设继承冲锋枪单武器预设；双武器预设继承冲锋枪双武器预设。
- 两种预设都使用 `RevolverReference` 贴图和 `(0, 0, 0.3203125)` 枪口偏移。
- 冲锋枪现有 `0.359375` 锚点和共享基准 `0.48828125` 锚点不变。
- 贴图为 512×512，XML 可解析，专项测试与相关回归测试通过。
