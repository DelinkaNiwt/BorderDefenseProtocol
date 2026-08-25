# 光魂举盾有限瞄准旋转实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为视觉预设增加可选的瞄准旋转限幅，让光魂举盾连续跟随目标但保持自然的四向盾面姿态。

**Architecture:** `BDP.Core`（核心层）在 `ExpressionVisualPresetDef` 上提供默认关闭的 `AimRotationLimit`，`VisualPoseResolver` 只在该字段启用时把真实目标角压缩到当前四向原版持械基准附近。`BDP.Content`（内容层）仅在光魂举盾单持和双武器 XML 预设中配置 15 度；警戒补丁仍负责连续位置，配对武器仍使用完整原版瞄准。

**Tech Stack:** C# 7.3、RimWorld 1.6、Harmony、PowerShell 冒烟测试、MSBuild Release。

---

### Task 1: 写限幅行为失败测试

**Files:**
- Create: `Source/BDP.Tests/LightSoulShieldBoundedAimRotationSmokeTests.ps1`

**Step 1: Write the failing test**

断言 `ExpressionVisualPresetDef` 存在默认值为 `0` 的 `AimRotationLimit`；断言 `VisualPoseResolver` 使用 `Mathf.DeltaAngle`、`Mathf.Clamp`、四向 `143/217` 基准和限幅值参与姿态角计算；断言两个光魂举盾 XML 预设配置 `15`，并且位置仍使用样本 `AimAngle`。

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulShieldBoundedAimRotationSmokeTests.ps1`

Expected: FAIL，因为新配置字段、解析路径和 XML 配置尚未存在。

### Task 2: 实现 Core 限幅解析

**Files:**
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`

**Step 1: Add the minimal configuration**

在预设中新增 `public float AimRotationLimit = 0f;`，逐成员中文注释，说明 0 表示完整沿用原版瞄准角。

**Step 2: Add the minimal resolver behavior**

在 `CalculatePose` 中保留 `request.PoseSample.DrawLoc + offset.WorldOffset` 的位置计算；调用 `ResolveAimAngle` 得到绘制角输入。限幅启用时按 `Rot4` 映射人物中心角 `North=0/East=90/South=180/West=270`，计算目标相对角并限制为 `±45°`，再映射到非西向 `143°` 或西向 `217°` 的原版持械基准。限幅为 0 时直接返回原始 `AimAngle`，确保其它视觉不变。

**Step 3: Run the focused test**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulShieldBoundedAimRotationSmokeTests.ps1`

Expected: FAIL 仅剩 XML 未配置断言。

### Task 3: 配置光魂举盾并转绿

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1: Configure both shield presets**

在 `BDP_Visual_LightSoulShieldGuard` 和 `BDP_Visual_LightSoulShieldGuard_Dual` 根节点加入 `<AimRotationLimit>15</AimRotationLimit>`；不修改已有位置、角度、镜像和贴图字段。

**Step 2: Run the focused test**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulShieldBoundedAimRotationSmokeTests.ps1`

Expected: `LightSoulShieldBoundedAimRotationSmokeTests PASS`。

### Task 4: 回归验证与提交

**Files:**
- Test: `Source/BDP.Tests/LightSoul*.ps1`
- Test: `Source/BDP.Tests/Visual*Pose*.ps1`
- Build: `Source/BDP/BDP.csproj`, `Source/BDP.Content/BDP.Content.csproj`
- Log: `日志/Agent工作日志/Agent日志47.md`

**Step 1: Run related tests**

运行限幅专项、`LightSoulGuardDirectionalPoseSmokeTests.ps1`、`LightSoulGuardAimVisualSmokeTests.ps1`、`LightSoulRealWeaponBoundarySmokeTests.ps1`、`VisualPoseResolverBoundarySmokeTests.ps1` 和相关 XML 解析测试；确认已有方向、镜像、位置和真实武器边界没有回归。

**Step 2: Build Release**

Run sequentially:

```powershell
dotnet build Source/BDP/BDP.csproj -c Release
dotnet build Source/BDP.Content/BDP.Content.csproj -c Release
```

Expected: BDP.Core.dll 与 BDP.Content.dll 构建成功，0 警告、0 错误。

**Step 3: Update work log**

按时间倒序记录限幅设计、测试和构建结果；超过 20 条时新建日志文件。

**Step 4: Commit**

提交设计、计划、测试、Core 解析、Content XML 和工作日志，提交信息：`fix: 限制举盾目标跟随视觉旋转`。
