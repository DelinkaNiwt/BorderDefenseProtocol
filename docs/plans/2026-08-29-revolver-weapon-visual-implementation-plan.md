# 左轮手枪单武器与双武器视觉实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为左轮手枪武装构型补齐单武器和双武器视觉，使用用户提供的左轮贴图，并把两种视觉的枪口锚点设为贴图约 x=420 像素。

**Architecture:** 复用现有武装构型视觉绑定和 `ExpressionVisualPresetDef`（表达视觉预设定义）继承机制。单武器预设直接继承 `BDP_Visual_SubmachineGun`，双武器预设直接继承 `BDP_Visual_SubmachineGun_Dual`，只覆盖左轮贴图和枪口锚点，不改 Core（核心）运行时设施。

**Tech Stack:** RimWorld 1.6 Def/XML（定义配置）、PNG（贴图）、PowerShell 冒烟测试、现有 BDP C#（C#语言）项目构建。

---

### Task 1: 记录并提交已批准设计

**Files:**
- Create: `docs/plans/2026-08-29-revolver-weapon-visual-design.md`
- Create: `docs/plans/2026-08-29-revolver-weapon-visual-implementation-plan.md`

**Step 1: 核对设计内容**

确认文档只包含左轮视觉配置、双武器绑定、贴图资源和验证范围，不扩展战斗属性或运行时代码。

**Step 2: Commit**

```powershell
git add -- docs/plans/2026-08-29-revolver-weapon-visual-design.md docs/plans/2026-08-29-revolver-weapon-visual-implementation-plan.md
git commit -m "docs: 记录左轮单双武器视觉设计"
```

### Task 2: 先写左轮视觉配置冒烟测试并确认失败

**Files:**
- Create: `Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1`

**Step 1: Write the failing test**

测试必须检查：

- `BDP_GunClass_Revolver` 的单武器预设仍为 `BDP_Visual_Pistol`。
- 左轮构型新增双武器预设 `BDP_Visual_Pistol_Dual`。
- 单武器预设继承 `BDP_Visual_SubmachineGun`。
- 双武器预设继承 `BDP_Visual_SubmachineGun_Dual`。
- 两个预设的贴图路径均为 `Things/Trigger/Visual/RevolverReference`，绘制类型均为 `Graphic_Single`。
- 两个预设均声明 `IsRangedWeapon=true`，枪口偏移均为 `(0, 0, 0.3203125)`。
- 冲锋枪锚点仍为 `(0, 0, 0.359375)`，中型基准锚点仍为 `(0, 0, 0.48828125)`。
- 目标贴图存在且为 512×512。

**Step 2: Run test to verify it fails**

Run:

```powershell
pwsh -File Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1
```

Expected: FAIL，原因是当前左轮构型没有 `compositeVisualPresetDefName`，也没有左轮双武器预设和自定义枪口锚点。

### Task 3: 实施左轮单武器和双武器配置

**Files:**
- Modify: `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/Pistol.xml`

**Step 1: 增加构型复合视觉绑定**

在 `BDP_GunClass_Revolver/overrides` 中保留：

```xml
<visualPresetDefName>BDP_Visual_Pistol</visualPresetDefName>
```

并增加：

```xml
<compositeVisualPresetDefName>BDP_Visual_Pistol_Dual</compositeVisualPresetDefName>
```

**Step 2: 改造单武器视觉预设**

给 `BDP_Visual_Pistol` 增加 `ParentName="BDP_Visual_SubmachineGun"`，将主贴图改为 `RevolverReference`，增加远程枪口声明和 `(0, 0, 0.3203125)` 偏移。

**Step 3: 增加双武器视觉预设**

新增 `BDP_Visual_Pistol_Dual`，继承 `BDP_Visual_SubmachineGun_Dual`，声明相同的左轮贴图和枪口偏移。

**Step 4: 增加诊断名称本地化**

在 `Pistol.xml` 中增加：

```xml
<BDP_Visual_Pistol_Dual.label>左轮手枪双武器手持视觉</BDP_Visual_Pistol_Dual.label>
```

### Task 4: 放入用户确认的左轮贴图

**Files:**
- Create: `1.6/Textures/Things/Trigger/Visual/RevolverReference.png`
- Source: `参考资源/贴图工程/崩雷左轮_校正.png`

**Step 1: Confirm source identity**

确认源文件与用户附件一致，保持 512×512 和透明背景。

**Step 2: Copy the asset**

使用文件复制将源贴图放入目标路径，不重新绘制、不改变像素内容。

### Task 5: 运行专项测试与回归验证

**Files:**
- Test: `Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1`
- Test: `Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1`
- Test: `Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`
- Test: `Source/BDP.Tests/SingleWeaponMuzzleAnchorSmokeTests.ps1`

**Step 1: Run the new test**

```powershell
pwsh -File Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1
```

Expected: PASS。

**Step 2: Run related visual and armament tests**

```powershell
pwsh -File Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1
pwsh -File Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1
pwsh -File Source/BDP.Tests/SingleWeaponMuzzleAnchorSmokeTests.ps1
```

Expected: 全部 PASS，且冲锋枪及共享中型基准原值不变。

**Step 3: Build the project**

使用项目现有构建脚本或解决方案命令构建 `BDP.Core.dll`、`BDP.Content.dll` 和 `BDP.Development.dll`，确认退出码为 0。

### Task 6: 写工作日志并提交实现

**Files:**
- Modify/Create: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/` 下的当前工作日志文件

**Step 1: Review the diff**

确认只包含本次左轮单/双武器视觉、贴图、语言包、测试和计划文档的改动，不纳入工作区既有无关修改。

**Step 2: Write the reverse-chronological work log**

记录配置字段、贴图资源、测试和构建结果；单个日志文件最多保留 20 条。

**Step 3: Commit**

```powershell
git add -- 1.6/Content/Defs/ChipArmamentFormDef/Presets.xml 1.6/Content/Defs/ExpressionDef/Visual.xml Languages Source/BDP.Tests/RevolverWeaponVisualSmokeTests.ps1 1.6/Textures/Things/Trigger/Visual/RevolverReference.png
git commit -m "feat: 增加左轮单双武器视觉"
```
