# 冲锋枪武装构型 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在 BDP 枪手职业中新增使用 P90 贴图的中近程 7 发冲锋枪武装构型，并将枪口锚点设置到贴图约 440px（像素）位置。

**Architecture:** 复用现有 `ChipArmamentFormDef`（芯片武装构型定义）、中型远程视觉基准和投射物覆盖机制。新增冲锋枪只在 Content（正式业务内容）的 Def/XML、语言包和纹理资源中声明；单持与双持各有独立视觉预设，避免改变突击步枪及共享基准。Core.dll 不改动。

**Tech Stack:** RimWorld 1.6 Def/XML、C#（C#语言）已有视觉与武装构型解析、PowerShell（PowerShell脚本）冒烟检查、现有 BDP Release（发布）构建脚本。

---

### Task 1: 先写冲锋枪构型回归检查

**Files:**
- Create: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1`

**Step 1: Write the failing test**

新增 PowerShell 检查，读取以下文件：

- `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`
- `1.6/Content/Defs/ExpressionDef/Visual.xml`
- `Languages/ChineseSimplified (简体中文)/DefInjected/ChipArmamentFormDef/Presets.xml`
- `Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/SubmachineGun.xml`
- `1.6/Textures/Things/Trigger/Visual/P90Reference.png`

检查内容必须包括：

- `BDP_GunClass_SubmachineGun` 存在并限制到 `BDP_ChipProfession_Gunner`。
- `range=24.0`、四档精度 `0.83/0.72/0.44/0.22`、`warmupTime=0.7`、`defaultCooldownTime=1.15`。
- `burstShotCount=7`、`ticksBetweenBurstShots=7`、`rhythm=Sequential`。
- `damageMultiplier=0.55`、`stoppingPowerMultiplier=0.75`、`noiseRadius=14`、`muzzleFlashScale=0.8`。
- 单持绑定 `BDP_Visual_SubmachineGun`，双持绑定 `BDP_Visual_SubmachineGun_Dual`。
- 两个视觉预设都引用 `P90Reference`，两个 `MuzzleOffset` 都是 `(0, 0, 0.359375)`。
- 突击步枪共享基准的 `0.48828125` 锚点仍存在，确保未被改写。
- 中文语言包包含冲锋枪名称、说明和两个视觉预设名称。
- P90 纹理存在、尺寸为 512×512。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\SubmachineGunArmamentFormSmokeTests.ps1"
```

Expected: FAIL，因为冲锋枪 Def、视觉预设、语言包和目标纹理尚未添加。

**Step 3: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1"
git commit -m "test: 增加冲锋枪武装构型检查"
```

### Task 2: 添加 P90 单持与双持视觉预设

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/1.6/Content/Defs/ExpressionDef/Visual.xml`
- Create: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/SubmachineGun.xml`
- Create: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/1.6/Textures/Things/Trigger/Visual/P90Reference.png`

**Step 1: Add single-weapon visual**

在远程武器视觉预设附近增加 `BDP_Visual_SubmachineGun`，继承 `BDP_VisualBase_RangedMedium`，主贴图引用 `Things/Trigger/Visual/P90Reference`，不改绘制缩放、握持偏移或动作阶段。

为该预设增加：

```xml
<Muzzle>
  <IsRangedWeapon>true</IsRangedWeapon>
  <MuzzleOffset>(0, 0, 0.359375)</MuzzleOffset>
</Muzzle>
```

**Step 2: Add dual-weapon visual**

增加 `BDP_Visual_SubmachineGun_Dual`，继承 `BDP_VisualBase_RangedMedium_Dual`，使用同一张 P90 贴图和同一枪口锚点；保留双持基准的手侧分离、前后景和镜像行为。

**Step 3: Add localized visual labels**

新建语言包文件，提供：

- `BDP_Visual_SubmachineGun.label`：冲锋枪手持视觉
- `BDP_Visual_SubmachineGun_Dual.label`：冲锋枪双武器手持视觉

**Step 4: Copy the approved texture**

确认目标不存在或与当前目标一致后，将 `C:\NiwtDatas\Projects\RimworldModStudio\参考资源\贴图工程\P90_校正.png` 复制为 `1.6/Textures/Things/Trigger/Visual/P90Reference.png`。不修改源文件，不把截图标注或其它临时文件复制进模组。

### Task 3: 添加冲锋枪武装构型与中文语言包

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/DefInjected/ChipArmamentFormDef/Presets.xml`

**Step 1: Add the armament form**

新增 `BDP_GunClass_SubmachineGun`，使用英文回退标签，限制职业为 `BDP_ChipProfession_Gunner`，`maxFunctionCount=2`，制造台可见并写入成品动态名称。

在 `overrides` 中写入：

```xml
<range>24.0</range>
<rhythm>Sequential</rhythm>
<accuracyTouch>0.83</accuracyTouch>
<accuracyShort>0.72</accuracyShort>
<accuracyMedium>0.44</accuracyMedium>
<accuracyLong>0.22</accuracyLong>
<warmupTime>0.7</warmupTime>
<defaultCooldownTime>1.15</defaultCooldownTime>
<burstShotCount>7</burstShotCount>
<ticksBetweenBurstShots>7</ticksBetweenBurstShots>
<soundCast>Shot_MachinePistol</soundCast>
<noiseRadius>14</noiseRadius>
<muzzleFlashScale>0.8</muzzleFlashScale>
<visualPresetDefName>BDP_Visual_SubmachineGun</visualPresetDefName>
<compositeVisualPresetDefName>BDP_Visual_SubmachineGun_Dual</compositeVisualPresetDefName>
```

追加制造成本使用 30 Steel（钢材），追加工作量使用 900。`projectileOverrides` 写入 `damageMultiplier=0.55` 和 `stoppingPowerMultiplier=0.75`，不创建新投射物 Def。

**Step 2: Add localized armament text**

在武装构型语言包中增加：

- 名称：冲锋枪
- 说明：中近程高速型武装，七发连射，近距离命中可靠，远距离衰减明显。

### Task 4: Run the focused test and static checks

**Files:**
- Test: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1`
- Inspect: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`
- Inspect: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Run the new regression test**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\SubmachineGunArmamentFormSmokeTests.ps1"
```

Expected: PASS，所有冲锋枪参数、视觉绑定、语言包和纹理尺寸检查通过。

**Step 2: Parse all relevant XML**

使用 PowerShell `[xml]` 解析两个正式 Def 文件和两个语言包文件，确认没有 XML 结构错误；检查同类型 DefName 无重复。

**Step 3: Run adjacent visual and armament checks**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\ChipArmamentFormDefinitionBoundarySmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\VisualMuzzleGraphicTransformSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\AssaultRifleWeaponStageVisualSmokeTests.ps1"
```

Expected: PASS；突击步枪视觉、共享枪口计算和通用武装型边界不受影响。

### Task 5: Build and prepare game testing

**Files:**
- Build script: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/Build/Verify-BdpCoreIsolation.ps1`
- Build script: `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/Build/Verify-BdpDevelopmentBoundary.ps1`
- Project files under `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source`

**Step 1: Run boundary checks**

运行 Core 隔离和 Development 边界检查，确认本次没有把业务内容或开发依赖引入 Core。

**Step 2: Build the current Release configuration**

按仓库现有构建入口编译 Content/Core/Development；本次没有 C# 变更，编译主要用于确认工作区和输出目录保持可用。

**Step 3: Record the game-only checks**

进入游戏后使用现有 Development 枪口/握持标记，检查四朝向单持、双持、预热和连续射击阶段；确认实际发射点位于 P90 贴图枪口约 440px 处，并观察 7 发连射、射程衰减、伤害和停止力手感。

### Task 6: Record and commit the implementation

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志80.md` 或按当前日志规则创建下一份日志文件

**Step 1: Review the diff**

确认只包含冲锋枪相关 XML、语言包、P90 目标贴图、冒烟测试和本次日志；不暂存用户已有的其它未跟踪资源。

**Step 2: Write the reverse-chronological work log**

记录本次新增构型、7 发参数、P90 贴图、440px 锚点、自动化检查结果和仍需进游戏验证的项目；单个日志文件不超过 20 条。

**Step 3: Commit the implementation**

```powershell
git add -- "模组工程/BorderDefenseProtocol/1.6/Content/Defs/ChipArmamentFormDef/Presets.xml" "模组工程/BorderDefenseProtocol/1.6/Content/Defs/ExpressionDef/Visual.xml" "模组工程/BorderDefenseProtocol/1.6/Textures/Things/Trigger/Visual/P90Reference.png" "模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/DefInjected/ChipArmamentFormDef/Presets.xml" "模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/DefInjected/ExpressionVisualPresetDef/SubmachineGun.xml" "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SubmachineGunArmamentFormSmokeTests.ps1" "日志/Agent工作日志/Agent日志80.md"
git commit -m "feat: 新增枪手冲锋枪武装构型"
```
