# 霰弹枪岚参考视觉 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为霰弹枪枪壳部署岚参考贴图，并让其单武器保持默认姿态、双武器复用当前突击步枪参考双武器的全部姿态数据。

**Architecture:** 新建独立霰弹枪双武器视觉 Def，避免改动突击步枪共享数据。单武器只替换现有霰弹枪视觉的贴图路径；双武器 Def 复制已验证的数据，并由霰弹枪枪壳的复合视觉字段选择。

**Tech Stack:** RimWorld XML 配置、PowerShell 烟雾测试、PNG 贴图、.NET 构建。

---

### Task 1: 写霰弹枪视觉失败测试

**Files:**
- Create: `Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

**Step 1: 编写断言**

测试必须确认：

- 源图和部署图均存在、均为 `512×512` 且 SHA256 相等；
- `BDP_Visual_Shotgun` 使用 `Things/Trigger/Visual/ShotgunReferenceLan`，且没有南北/东西姿态、握持点或枪口点；
- `BDP_Visual_Shotgun_Dual` 存在，使用同一岚贴图；
- 新双武器 Def 的 `GraphicData.graphicClass`、`SouthNorthPose`、`Grip`、`Muzzle` 与 `BDP_Visual_RangedWeaponReference_Dual` 相同；
- 霰弹枪枪壳分别绑定 `BDP_Visual_Shotgun` 和 `BDP_Visual_Shotgun_Dual`；
- 突击步枪枪壳绑定保持不变。

**Step 2: 运行确认失败**

Run: `& Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

Expected: FAIL，因为目标贴图、新双武器 Def 和枪壳绑定尚不存在。

### Task 2: 部署贴图和最小 XML 配置

**Files:**
- Create: `1.6/Textures/Things/Trigger/Visual/ShotgunReferenceLan.png`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `1.6/Content/Defs/ChipGunShellDef/Presets.xml`

**Step 1: 部署贴图**

原样复制：

```text
C:/NiwtDatas/Projects/RimworldModStudio/参考资源/通用资源/占位贴图/测试图-岚.png
```

到：

```text
1.6/Textures/Things/Trigger/Visual/ShotgunReferenceLan.png
```

**Step 2: 修改单武器视觉**

将 `BDP_Visual_Shotgun/GraphicData/texPath` 改为：

```xml
<texPath>Things/Trigger/Visual/ShotgunReferenceLan</texPath>
```

不添加其它配置。

**Step 3: 新增双武器视觉**

新增 `BDP_Visual_Shotgun_Dual`，其贴图使用 `ShotgunReferenceLan`，`SouthNorthPose`、`Grip` 和 `Muzzle` 内容逐项复制 `BDP_Visual_RangedWeaponReference_Dual`。

**Step 4: 绑定枪壳**

在 `BDP_GunClass_Shotgun/overrides` 中保留单武器字段，并新增：

```xml
<compositeVisualPresetDefName>BDP_Visual_Shotgun_Dual</compositeVisualPresetDefName>
```

**Step 5: 运行测试确认通过**

Run: `& Source/BDP.Tests/ShotgunLanReferenceVisualSmokeTests.ps1`

Expected: PASS。

### Task 3: 回归、构建与提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志43.md`

**Step 1: 运行视觉回归和 XML 解析**

运行霰弹枪新测试、远程参考预设、单武器默认姿态、镜像、握持点和点位可视化测试，并解析两个修改过的 XML。

Expected: 全部 PASS。

**Step 2: 构建部署**

Run: `dotnet build Source/BDP/BDP.csproj --no-restore -c Debug`

Expected: Core 隔离检查 PASS，0 警告、0 错误。

**Step 3: 更新日志**

在 `Agent日志43.md` 顶部新增倒序条目，保持不超过 20 条。

**Step 4: 精确提交**

只暂存新测试、新贴图、两个 XML 和工作日志，执行 `git diff --cached --check` 后提交：

```text
test: add shotgun dual reference visual
```

**Step 5: 提交后复验**

重跑新测试和相关回归，确认工作区只剩用户原有未跟踪文件。
