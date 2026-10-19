# BDP 小型枪械视觉预设 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 增加小型枪械单持/双持视觉基类，让左轮继承小型基类，按白圈调整南北双持握持目标，并将突击步枪实际使用的视觉预设改名。

**Architecture:** 继续使用 RimWorld Def XML 继承，不修改 Core 运行时解算器。新增 `BDP_VisualBase_RangedSmall` 与 `BDP_VisualBase_RangedSmall_Dual` 作为中型基类的同级抽象预设；左轮具体预设只保留左轮贴图并继承小型公共枪口、握持和姿态数据。突击步枪把当前实际使用的参考贴图预设改名为 `BDP_Visual_AssaultRifle` 与 `BDP_Visual_AssaultRifle_Dual`，删除原先同名但未被消费的旧定义。

**Tech Stack:** RimWorld 1.6 Def XML（定义配置）、PowerShell（静态冒烟测试）、MSBuild（微软构建工具）、Git（版本控制）。

---

### Task 1: 扩展视觉继承与消费者的失败测试

**Files:**
- Modify: `Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualEastWestElevationParitySmokeTests.ps1`
- Create: `Source/BDP.Tests/SmallRangedVisualPresetSmokeTests.ps1`

**Step 1: 写入小型基类和左轮继承的失败断言。**

在新测试中读取 `1.6/Content/Defs/ExpressionDef/Visual.xml`，断言：

- 存在 `BDP_VisualBase_RangedSmall` 与 `BDP_VisualBase_RangedSmall_Dual`，且两者为抽象定义。
- `BDP_VisualBase_RangedSmall` 是同级根定义，不继承中型基类。
- 小型基类的握持偏移为 `(0, 0, -0.1953125)`。
- 小型基类的枪口偏移为 `(0, 0, 0.3203125)`，且 `IsRangedWeapon=true`。
- 小型双持基类继承小型单持基类，南北偏移为 `(0.36, 0, 0.13)`，`NorthZAdjust=0.26`，并启用 `UseAsPoseOrigin=true`。
- 小型双持东西姿态与中型双持姿态保持一致。
- `BDP_Visual_Pistol` 与 `BDP_Visual_Pistol_Dual` 分别继承小型单持/双持基类。

**Step 2: 更新左轮测试的预期继承和枪口来源。**

将左轮测试从检查子预设直接持有 `<Muzzle>`，改为检查小型基类提供 `(0, 0, 0.3203125)`，并断言左轮子预设只保留贴图、没有重复的 `Grip`、`Muzzle` 或姿态块。

**Step 3: 更新突击步枪名称和引用的失败断言。**

将相关测试中的 `BDP_Visual_RangedWeaponReference` 与 `_Dual` 预期改为 `BDP_Visual_AssaultRifle` 与 `_Dual`，并断言旧名称及旧的未引用定义不再存在。

**Step 4: 运行测试确认在实现前失败。**

Run:

```powershell
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/SmallRangedVisualPresetSmokeTests.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1'
```

Expected: FAIL，原因应是小型基类不存在或左轮仍继承中型/冲锋枪；不得因脚本语法错误失败。

**Step 5: Commit the failing contract tests.**

```powershell
git add -- 'Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1' 'Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1' 'Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1' 'Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1' 'Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1' 'Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1' 'Source/BDP.Tests/VisualEastWestElevationParitySmokeTests.ps1' 'Source/BDP.Tests/SmallRangedVisualPresetSmokeTests.ps1'
git commit -m "test: 锁定小型枪械视觉预设契约"
```

### Task 2: 增加小型枪械视觉基类

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml:261-298`

**Step 1: 添加同级小型单持抽象预设。**

在 `BDP_VisualBase_RangedMedium` 后添加 `BDP_VisualBase_RangedSmall`，不设置 `ParentName`，声明：

```xml
<DrawScale>1</DrawScale>
<Grip>
  <GripOffset>(0, 0, -0.1953125)</GripOffset>
</Grip>
<Muzzle>
  <IsRangedWeapon>true</IsRangedWeapon>
  <MuzzleOffset>(0, 0, 0.3203125)</MuzzleOffset>
</Muzzle>
```

**Step 2: 添加小型双持抽象预设。**

添加 `BDP_VisualBase_RangedSmall_Dual`，继承 `BDP_VisualBase_RangedSmall`，复制中型双持的东西姿态规则，仅将南北握持目标调整为：

```xml
<DefaultOffset>(0.36, 0, 0.13)</DefaultOffset>
<NorthZAdjust>0.26</NorthZAdjust>
<HandMirrorOnlyWhenIdle>true</HandMirrorOnlyWhenIdle>
```

保留 `UseAsPoseOrigin=true`，使偏移表示握持目标而不是贴图中心。

**Step 3: 运行新测试确认基类实现通过。**

Run:

```powershell
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/SmallRangedVisualPresetSmokeTests.ps1'
```

Expected: PASS。

### Task 3: 切换左轮到小型基类

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml:355-382`
- Modify: `Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1`

**Step 1: 修改左轮单持父级。**

将 `BDP_Visual_Pistol` 的 `ParentName` 改为 `BDP_VisualBase_RangedSmall`，保留左轮贴图，并删除其重复的 `<Muzzle>` 块。

**Step 2: 修改左轮双持父级。**

将 `BDP_Visual_Pistol_Dual` 的 `ParentName` 改为 `BDP_VisualBase_RangedSmall_Dual`，保留左轮贴图，并删除其重复的 `<Muzzle>` 块。

**Step 3: 运行左轮和继承测试。**

Run:

```powershell
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/SmallRangedVisualPresetSmokeTests.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1'
```

Expected: PASS；冲锋枪仍保持自己的 `(0, 0, 0.359375)` 枪口偏移，中型基类仍保持 `(0, 0, 0.48828125)`。

### Task 4: 重命名突击步枪实际视觉预设

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml:251-318`
- Modify: `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml:94-96`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/AssaultRifle.xml`
- Modify: `Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualEastWestElevationParitySmokeTests.ps1`

**Step 1: 删除旧的未引用 `BDP_Visual_AssaultRifle` 定义。**

移除使用 `Things/Item/Equipment/WeaponRanged/AssaultRifle` 贴图、当前没有 XML 消费引用的旧定义。

**Step 2: 重命名当前参考视觉定义。**

将：

```text
BDP_Visual_RangedWeaponReference
BDP_Visual_RangedWeaponReference_Dual
```

改为：

```text
BDP_Visual_AssaultRifle
BDP_Visual_AssaultRifle_Dual
```

保留原有参考贴图、父级和双持姿态，不引入动作阶段视觉。

**Step 3: 更新突击步枪构型引用。**

在 `BDP_GunClass_AssaultRifle/overrides` 中把单持与双持引用改为新的两个 DefName。

**Step 4: 添加中文诊断标签。**

在语言包中增加：

```xml
<BDP_Visual_AssaultRifle.label>突击步枪手持视觉</BDP_Visual_AssaultRifle.label>
<BDP_Visual_AssaultRifle_Dual.label>突击步枪双武器手持视觉</BDP_Visual_AssaultRifle_Dual.label>
```

**Step 5: 运行突击步枪、霰弹枪和引用边界测试。**

Run:

```powershell
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/AssaultRifleWeaponStageVisualSmokeTests.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File 'Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1'
```

Expected: PASS；突击步枪构型消费新名称，旧名称全局 XML 引用为零。

### Task 5: 完成回归验证

**Files:**
- Test: `Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`
- Test: `Source/BDP.Tests/VisualEastWestElevationParitySmokeTests.ps1`
- Test: `Source/BDP.Tests/VisualGripAnchorSmokeTests.ps1`
- Test: `Source/BDP.Tests/TriggerVisualGripMarkerSmokeTests.ps1`
- Test: `Source/BDP.Tests/SingleWeaponMuzzleAnchorSmokeTests.ps1`
- Test: `Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1`

**Step 1: 运行全部相关 PowerShell 冒烟测试。**

Run：

```powershell
$tests = @(
  'SmallRangedVisualPresetSmokeTests.ps1',
  'MediumRangedVisualPresetInheritanceSmokeTests.ps1',
  'RevolverWeaponVisualSmokeTests.ps1',
  'RangedWeaponReferenceVisualSmokeTests.ps1',
  'AssaultRifleWeaponStageVisualSmokeTests.ps1',
  'ShotgunLanReferenceVisualSmokeTests.ps1',
  'VisualNorthElevationParitySmokeTests.ps1',
  'VisualEastWestElevationParitySmokeTests.ps1',
  'VisualGripAnchorSmokeTests.ps1',
  'TriggerVisualGripMarkerSmokeTests.ps1',
  'SingleWeaponMuzzleAnchorSmokeTests.ps1',
  'SubmachineGunArmamentFormSmokeTests.ps1'
)
foreach ($test in $tests) {
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path 'Source/BDP.Tests' $test)
  if ($LASTEXITCODE -ne 0) { throw "测试失败：$test" }
}
```

Expected: 全部 PASS。

**Step 2: 检查 XML 引用和重复 DefName。**

确认：

- `BDP_Visual_RangedWeaponReference` 及 `_Dual` 不再被主模组 XML 引用。
- `BDP_Visual_AssaultRifle` 及 `_Dual` 各只有一个定义。
- 小型基类只有一套标准枪口和握持数据。

**Step 3: 构建 Core、Content 和 Development。**

使用现有 `.csproj` 构建 Release 配置，确认本需求没有破坏程序集边界；不要把构建产物或无关输出纳入提交。

### Task 6: 记录、提交和交付

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/<当前日志文件>.md`

**Step 1: 进行最终状态检查。**

确认只暂存本需求的 XML、语言包、测试、设计/实现计划和工作日志；保留用户原有未提交改动。

**Step 2: 写入倒序工作日志。**

记录小型基类、左轮继承、南北姿态调整、突击步枪重命名、测试和构建结果；每个日志文件保持不超过 20 条。

**Step 3: 提交实现。**

```powershell
git add -- <本需求文件列表>
git commit -m "feat: 增加小型枪械视觉预设"
```

**Step 4: 交付说明。**

说明修改文件、测试结果、构建结果、未纳入的既有工作区改动，以及仍需在游戏内确认的南北双持视觉效果。
