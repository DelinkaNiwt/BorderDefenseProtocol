# Weapon Action Stage Visual Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在现有 BDP 武器绘制链上，按 `Idle（静止）`、`Warmup（预热）`、`Firing（射击）`、`Cooldown（冷却）` 为实际参与攻击的武器选择阶段贴图或整套隐藏，并为未来独立部件动画稳定提供 `Progress01（零到一阶段进度）`。

**Architecture:** Core（核心程序集）新增无持久化的阶段解析器和视觉预设阶段覆盖；阶段从原版 `Stance（姿态）`、正式宿主 `Verb（攻击行为）` 与 BDP 已发布攻击来源实时推导。绘制、枪口和诊断共用同一阶段快照；具体“预热换图、射击与冷却隐藏”只由主模组 Content（正式内容层）的视觉预设声明，不把业务配置写进 Core。

**Tech Stack:** RimWorld 1.6、C# 7.3、Harmony（方法补丁库）、Verse／RimWorld 原版程序集、XML（可扩展标记语言）Def、PowerShell 烟雾测试、.NET Framework 4.8。

---

## 实施约束

- 直接在当前工作区执行，不创建 worktree（工作树）或新分支。
- 每个提交只显式暂存本任务列出的文件；提交前运行 `git status --short`，不得夹带用户或其他任务的改动。
- 所有新增 C# 类型、字段、属性和方法逐成员写中文 XML 文档注释；新增／修改的 Def XML 每个阶段条目写中文注释；统一 UTF-8（编码）。
- 不新增视觉阶段存档字段，不在 tick（游戏刻）中维护第二套状态机，不修改原版攻击节奏。
- 不把静态切分贴图伪装成 `OverlayLayers（附加绘制层）`；未来部件动画另行扩展 `Parts（视觉部件）`。
- `BorderDefenseProtocol.DevHarness（伴生测试模组）` 已退役，本计划不得读取、修改或依赖它。
- `Source/BDP.Tests` 是主工程内的自动回归测试目录，不是游戏加载模组，继续用于结构与配置测试。
- 当前尚未确认正式“切分小立方体”贴图及目标武器预设，因此不创建占位业务内容；先完成 Core 设施，正式资源确认后再接入 `1.6/Content/Defs/ExpressionDef/Visual.xml`。

## Task 1：统一来源芯片实例身份判断

**Files:**

- Create: `Source/BDP/Core/Expressions/Utilities/ExpressionSourceReferenceMatcher.cs`
- Modify: `Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs`
- Create: `Source/BDP.Tests/ExpressionSourceReferenceMatcherSmokeTests.ps1`
- Modify: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`

### Step 1：先写失败的结构测试

新增 `ExpressionSourceReferenceMatcherSmokeTests.ps1`，断言：

- 存在 `ExpressionSourceReferenceMatcher`。
- 存在语义明确的 `BuildChipInstanceKey(ExpressionSourceReference sourceReference)` 与 `AreSameChipInstance(...)`。
- 键优先使用 `ChipThingId（芯片实例标识）`，缺失时回退 `Side（侧别） + SlotIndex（槽位序号） + ChipDefName（芯片定义名）`。
- `DefaultVisualProjectionBuilder` 不再保留自己的 `BuildWeaponChipInstanceKey`，而是调用共享匹配器。

更新现有单武器测试，使它断言共享匹配器，而不再绑定旧私有方法名。

### Step 2：运行测试并确认失败

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/ExpressionSourceReferenceMatcherSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
```

Expected: 新测试因共享匹配器不存在而失败；旧测试因预期尚未更新完成或实现仍使用旧方法而失败。

### Step 3：实现最小共享身份设施

创建 `ExpressionSourceReferenceMatcher`：

- `BuildChipInstanceKey(...)` 返回稳定的来源芯片实例键。
- `AreSameChipInstance(...)` 比较两份来源引用的键。
- 空引用返回空键／不匹配。

将 `DefaultVisualProjectionBuilder.CountActiveWeaponChipInstances(...)` 改为调用共享方法，删除旧的重复私有实现。该类型保持表达来源层中性，不引用视觉阶段或具体武器业务。

### Step 4：运行定向测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/ExpressionSourceReferenceMatcherSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
```

Expected: 两项均输出 `PASS`。

### Step 5：提交

```powershell
git status --short
git add Source/BDP/Core/Expressions/Utilities/ExpressionSourceReferenceMatcher.cs Source/BDP/Core/Expressions/Projection/DefaultVisualProjectionBuilder.cs Source/BDP.Tests/ExpressionSourceReferenceMatcherSmokeTests.ps1 Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
git commit -m "refactor: unify expression source identity"
```

## Task 2：建立阶段契约和视觉覆盖配置

**Files:**

- Create: `Source/BDP/Core/Trigger/Visual/WeaponVisualActionStage.cs`
- Create: `Source/BDP/Core/Trigger/Visual/WeaponVisualStageSnapshot.cs`
- Create: `Source/BDP/Core/Expressions/Config/ExpressionVisualStageOverrideConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs`
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Messages.xml`
- Create: `Source/BDP.Tests/WeaponVisualStageConfigSmokeTests.ps1`

### Step 1：先写失败的配置契约测试

断言以下契约存在且命名完整：

- `WeaponVisualActionStage` 仅有 `Idle`、`Warmup`、`Firing`、`Cooldown`。
- `WeaponVisualStageSnapshot` 含 `Stage`、`Progress01`、`StageTicksRemaining`、`MatchedSourceResultId`、`HostResultId`、`AttackInstanceId`、`ProjectionVersion`。
- `ExpressionVisualStageOverrideConfig` 含 `Stage`、默认 `true` 的 `Visible`、可空 `GraphicData` 及阶段 Graphic（图像）缓存解析。
- `ExpressionVisualPresetDef.StageVisuals`、`ResolveStageOverride(...)`、`ResolveStageVisibility(...)` 和带阶段参数的 `ResolveGraphic(...)` 存在。
- 阶段 `GraphicData` 优先于原有 `ActiveGraphicData／GraphicData`，阶段未配置时完全回退旧规则。
- 重复阶段通过 `ConfigErrors()` 报错，运行时稳定使用首条；错误文案来自语言键 `BDP_ConfigError_DuplicateWeaponVisualStage`。
- 新增成员均有中文 XML 文档注释。

### Step 2：运行测试并确认失败

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageConfigSmokeTests.ps1
```

Expected: 因阶段契约和配置尚不存在而失败。

### Step 3：实现最小阶段契约

实现公开枚举和内部只读快照。枚举必须公开，因为公开的 XML 配置类型会引用它；快照只服务 Core（核心程序集）运行时，不扩大公开表面。快照提供明确的 `Idle(...)` 工厂或等价初始化入口，统一完成：

- `Stage = Idle`
- `Progress01 = 0f`
- `StageTicksRemaining = 0`
- 保留可用于诊断的宿主、攻击实例和投影身份

不实现 `IExposable（可存档接口）`，不增加任何 `Scribe（存档读写）` 调用。

### Step 4：实现阶段视觉覆盖

在预设中加入 `StageVisuals`，并实现：

```csharp
public ExpressionVisualStageOverrideConfig ResolveStageOverride(WeaponVisualActionStage stage)
public bool ResolveStageVisibility(WeaponVisualActionStage stage)
public Graphic ResolveGraphic(bool isExecutionActive, WeaponVisualActionStage stage, Thing sourceThing)
```

规则：

- 命中阶段且配置了 `GraphicData`：使用阶段贴图。
- 命中阶段但没配置 `GraphicData`：继续使用原执行焦点／默认贴图。
- 没命中阶段：旧行为不变。
- `Visible=false` 只负责显隐，不让贴图或姿态解析失败。

在 `ConfigErrors()` 中用 `HashSet<WeaponVisualActionStage>` 检查重复阶段，错误文案调用语言键，空条目安全跳过。

### Step 5：运行定向测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageConfigSmokeTests.ps1
```

Expected: 输出 `WeaponVisualStageConfigSmokeTests PASS`。

### Step 6：提交

```powershell
git status --short
git add Source/BDP/Core/Trigger/Visual/WeaponVisualActionStage.cs Source/BDP/Core/Trigger/Visual/WeaponVisualStageSnapshot.cs Source/BDP/Core/Expressions/Config/ExpressionVisualStageOverrideConfig.cs Source/BDP/Core/Expressions/Config/ExpressionVisualPresetDef.cs "Languages/ChineseSimplified (简体中文)/Keyed/Messages.xml" Source/BDP.Tests/WeaponVisualStageConfigSmokeTests.ps1
git commit -m "feat: define weapon visual stage overrides"
```

## Task 3：按原版时序解析动作阶段和阶段进度

**Files:**

- Create: `Source/BDP/Core/Trigger/Visual/WeaponVisualStageResolver.cs`
- Create: `Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1`
- Modify: `Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1`

### Step 1：先写失败的阶段解析测试

测试以源码结构约束关键根因规则：

- 解析器只读 `Pawn.stances.curStance`、`BdpVerb_FormalHostShoot.HostSessionToken`、已发布战斗投影和视觉运行态。
- 不含 `Scribe`、可变阶段字段、tick 更新器或第二状态机。
- 参与来源正常路径使用 `ActiveCastResultIds`，并通过 `ResultIndex` 回溯 `ExpressionSourceReference`。
- 复合结果通过 `CompositeReferenceIndex` 展开来源；读档恢复窗口可从 `HostSessionToken.ResultId` 回退展开。
- 条目与参与来源通过 `ExpressionSourceReferenceMatcher.AreSameChipInstance(...)` 比较，而非只比 `entry.ResultId`。
- 阶段判断顺序必须是 `Stance_Warmup` → `Bursting` → `Stance_Cooldown`，保证 burst（连射）间隔冷却仍归 `Firing`。
- 宿主会话核对 Pawn、正式宿主类型、投影版本、宿主结果和可用的攻击实例标识。
- 预热进度读取原版 `WarmupProgress／WarmupTicksLeft`；最终冷却总时长读取 `verbProps.AdjustedCooldownTicks(...)`，并把进度限制到 `0–1`。

在读档回归测试中补充约束：阶段设施不得新增视觉阶段持久化，只能使用已恢复的正式宿主会话与原版姿态。

### Step 2：运行测试并确认失败

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
```

Expected: 新阶段测试因解析器不存在而失败；原读档测试继续通过或在新增断言后明确失败。

### Step 3：实现宿主会话解析

`WeaponVisualStageResolver.Resolve(...)` 建议签名：

```csharp
internal WeaponVisualStageSnapshot Resolve(
    Pawn pawn,
    VisualResidentEntry entry,
    TriggerCombatProjectionState combatProjection,
    TriggerVisualRuntimeState visualRuntimeState)
```

先从 `Stance_Busy.verb` 取得 `BdpVerb_FormalHostShoot`，再核对：

- `HostSessionToken.IsValid`
- `HostSessionToken.BelongsTo(pawn)`
- `HostSessionToken.ProjectionVersion == combatProjection.ProjectionVersion`
- 姿态握着的就是该正式宿主实例
- 视觉运行态有执行真值时，其投影版本、宿主结果和非空攻击实例必须与令牌一致

任一矛盾都返回 `Idle`，不尝试猜测。

### Step 4：实现参与来源解析

正常路径：读取 `ActiveCastResultIds`；每个结果若是复合结果，则递归或迭代展开 `CompositeReferenceIndex.SourceResultIds`，并用已访问集合阻止循环。

读档恢复路径：视觉运行态无有效施放来源时，从 `HostSessionToken.ResultId` 使用同一展开逻辑。只读取已发布投影，不调用 `TryPreparePlan`、`TryExecute` 或续射规划器。

对每个最终来源结果，从 `ResultIndex` 读取 `SourceReference`，用共享匹配器与 `entry.SourceReference` 比较。命中时把实际来源结果写入 `MatchedSourceResultId`。

### Step 5：实现阶段与进度

- `Stance_Warmup` 且宿主匹配：`Warmup`，`Progress01 = Mathf.Clamp01(hostVerb.WarmupProgress)`，剩余刻取 `WarmupTicksLeft`。
- `hostVerb.Bursting`：`Firing`，首版 `Progress01 = 0f`，剩余刻在可用时取忙姿态剩余值，否则为 `0`。
- `Stance_Cooldown` 且宿主匹配：`Cooldown`，总刻取 `hostVerb.verbProps.AdjustedCooldownTicks(hostVerb, pawn)`，进度为 `1 - remaining / total` 并限制范围。
- 其他：`Idle`。

不通过 `Find.TickManager.TicksGame - startedTick` 计算总时长，避免暂停或眩晕造成进度漂移。

### Step 6：运行定向测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
```

Expected: 两项均输出 `PASS`。

### Step 7：提交

```powershell
git status --short
git add Source/BDP/Core/Trigger/Visual/WeaponVisualStageResolver.cs Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1 Source/BDP.Tests/PostLoadAttackSessionRecoverySmokeTests.ps1
git commit -m "feat: resolve weapon visual action stages"
```

## Task 4：让姿态、枪口与诊断共用阶段快照

**Files:**

- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseRequest.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/TriggerVisualLaunchOriginResolver.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs`
- Modify: `Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsSnapshot.cs`
- Create: `Source/BDP.Tests/WeaponVisualStagePoseIntegrationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/TriggerVisualPoseDiagnosticsSmokeTests.ps1`

### Step 1：先写失败的共用链路测试

断言：

- `VisualPoseRequest` 使用 `WeaponStageSnapshot`，不只传一个模糊布尔值。
- `VisualPoseResolver.Resolve(...)` 和 `ResolveTextureOnly(...)` 均把 `WeaponStageSnapshot.Stage` 交给预设主贴图解析。
- `TriggerVisualLaunchOriginResolver` 也调用同一个 `WeaponVisualStageResolver` 并把同一快照放进请求。
- 阶段不改握持和枪口配置；锚点仍由现有 `ResolveGripAnchor／ResolveMuzzleAnchor` 计算。
- 诊断快照公开 `WeaponActionStage`、`WeaponStageProgress01`、`WeaponStageTicksRemaining`、`WeaponStageVisible`，并来自同一解析器。

### Step 2：运行测试并确认失败

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStagePoseIntegrationSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/TriggerVisualPoseDiagnosticsSmokeTests.ps1
```

Expected: 新测试因请求尚无阶段快照而失败；现有测试保持可定位状态。

### Step 3：扩展姿态请求与主贴图选择

在 `VisualPoseRequest` 增加 `WeaponStageSnapshot`，默认空时按 `Idle` 处理。两个主贴图解析入口统一调用：

```csharp
request.Preset.ResolveGraphic(
    request.IsExecutionActive,
    resolvedWeaponStage,
    request.SourceThing)
```

不要把 `Visible` 写入 `ResolvedVisualPose.IsValid`。隐藏阶段仍必须能解析枪口锚点。

### Step 4：接入实时枪口解析

`TriggerVisualLaunchOriginResolver` 在构造 `VisualPoseRequest` 前读取当前战斗投影并解析阶段。即使 `Firing／Cooldown` 配置为隐藏，`TryResolveMuzzleAnchor` 仍使用有效姿态和锚点，不退回 Pawn（角色）中心。

### Step 5：接入现有诊断快照

`TriggerVisualPoseDiagnosticsAccess` 对每个 resident（常驻视觉）条目调用同一阶段解析器，写入公开诊断字段。字段使用可读字符串和数值，不把内部枚举类型扩散到 Development（开发程序集）边界。

### Step 6：运行定向测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStagePoseIntegrationSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/TriggerVisualPoseDiagnosticsSmokeTests.ps1
```

Expected: 三项均输出 `PASS`。

### Step 7：提交

```powershell
git status --short
git add Source/BDP/Core/Trigger/Visual/VisualPoseRequest.cs Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs Source/BDP/Core/Trigger/Visual/TriggerVisualLaunchOriginResolver.cs Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsAccess.cs Source/BDP/Core/Trigger/Visual/Diagnostics/TriggerVisualPoseDiagnosticsSnapshot.cs Source/BDP.Tests/WeaponVisualStagePoseIntegrationSmokeTests.ps1 Source/BDP.Tests/VisualPoseResolverBoundarySmokeTests.ps1 Source/BDP.Tests/TriggerVisualPoseDiagnosticsSmokeTests.ps1
git commit -m "feat: carry weapon stages through visual poses"
```

## Task 5：在两条装备绘制路径实现换图与整套隐藏

**Files:**

- Modify: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`
- Create: `Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1`
- Modify: `Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1`
- Modify: `Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1`

### Step 1：先写失败的绘制测试

断言：

- 完整 `Replace（替换）` 与单武器 `ReplaceTextureOnly（只替换贴图）` 都调用同一个阶段解析器。
- 单武器方法改名为 `TryHandleSingleWeaponTextureReplacement` 或同等明确名称，因为隐藏阶段“已处理但未绘制”仍需压制原装备。
- 完整替换路径区分 `handledAnyEntry（是否处理任何条目）` 和实际绘制，不能继续用 `drewAny` 决定是否恢复原版贴图。
- `Visible=false` 时跳过主贴图和全部附加层，但只有在姿态成功解析后才视为已处理。
- `Visible=true` 时仍沿用原后坐、镜像、姿态和附加层绘制。
- `Keep（保留）` 与 `Suppress（压制）` 的原政策不变。

### Step 2：运行测试并确认失败

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1
```

Expected: 新测试因绘制链尚未读取阶段可见性而失败。

### Step 3：接入完整替换路径

逐条目：

1. 解析 `WeaponVisualStageSnapshot`。
2. 放入 `VisualPoseRequest` 并解析姿态。
3. 姿态有效后将 `handledAnyEntry = true`。
4. `preset.ResolveStageVisibility(stage) == false` 时不调用后坐和绘制，继续处理下一条。
5. 可见时按旧逻辑应用后坐并绘制。

这样 `Replace` 在射击／冷却隐藏时仍压住原版宿主贴图；姿态本身不可解析时则安全回退原版。

### Step 4：接入单武器只替换贴图路径

把旧“是否画出来”的方法语义改为“是否成功处理”：

- 解析同一阶段快照。
- 姿态有效且阶段隐藏：返回 `true`，不绘制。
- 姿态有效且可见：绘制阶段贴图与现有附加层，再返回 `true`。
- 姿态／预设无效：返回 `false`，允许原版装备绘制。

不要把单武器 `IsExecutionActive` 硬编码为阶段参与信号；阶段与旧执行焦点继续保持独立。

### Step 5：运行定向测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/DualWeaponVanillaRecoilDrawSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/VisualAimMirrorVanillaParitySmokeTests.ps1
```

Expected: 五项均输出 `PASS`。

### Step 6：提交

```powershell
git status --short
git add Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1 Source/BDP.Tests/SingleWeaponTextureOnlyVisualSmokeTests.ps1 Source/BDP.Tests/SingleWeaponOverlayVisualSmokeTests.ps1
git commit -m "feat: draw weapon visuals by action stage"
```

## Task 6：为正式 Content 阶段配置建立接入边界

**Files:**

- Create: `Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1`
- Modify later, after the target weapon and texture are confirmed: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Add later, after the texture is supplied: `1.6/Textures/<正式切分贴图路径>.png`

### Step 1：先写内容边界测试

新增 `WeaponVisualStageContentBoundarySmokeTests.ps1`，先约束设施与业务的归属，而不虚构具体武器：

- Core（核心程序集）只定义 `StageVisuals（阶段视觉覆盖）` 配置能力，不硬编码 `Warmup／Firing／Cooldown` 的具体贴图或显隐业务。
- 正式业务阶段配置只能出现在 `1.6/Content/Defs/ExpressionDef/Visual.xml` 的 `ExpressionVisualPresetDef（表达视觉预设）` 中。
- 本功能新增或修改的 C#、XML 和测试命令不读取、修改或调用 `BorderDefenseProtocol.DevHarness` 路径；历史退役记录不在该断言范围内。
- 没有 `StageVisuals` 的既有正式预设继续合法，不要求全量迁移。
- 后续接入的每个阶段 `<li>` 必须有中文注释。

### Step 2：运行测试并确认设施边界

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1
```

Expected: 若 Core 出现具体业务硬编码、本功能文件访问退役测试模组或 Content 路径不正确则失败；否则输出 `WeaponVisualStageContentBoundarySmokeTests PASS`。

### Step 3：提交内容边界测试

```powershell
git status --short
git add Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1
git commit -m "test: guard weapon stage content boundary"
```

### Step 4：正式资源确认后再写业务配置

这一步有明确前置条件：用户已确认目标 `ExpressionVisualPresetDef（表达视觉预设）` 与正式静态切分 PNG（便携式网络图形）路径。未满足时停止在 Core 设施完成状态，不创建原版贴图占位或虚构 Def。

在目标正式预设中声明：

- `Warmup`：`Visible=true`，`GraphicData.texPath` 指向正式静态切分贴图。
- `Firing`：`Visible=false`。
- `Cooldown`：`Visible=false`。
- 不重复声明 `Idle`，让静止阶段沿用当前完整武器贴图。
- 单武器预设和对应双武器复合预设需要相同行为时分别明确配置，不用隐式猜测。

### Step 5：为已确认的正式预设写失败测试

在 `WeaponVisualStageContentBoundarySmokeTests.ps1` 增加针对确切 `defName（定义名）` 与 `texPath（贴图路径）` 的断言，并先运行确认它因 XML 尚未接入而失败。

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1
```

Expected: 因目标正式预设尚无阶段配置而失败。

### Step 6：写入 Content XML 与正式贴图

只修改确认过的目标预设和对应贴图文件，不改其它现有视觉预设。XML 至少包含：

```xml
<!-- 当前正式武器的动作阶段视觉覆盖。 -->
<StageVisuals>
  <!-- 预热阶段显示正式静态切分贴图。 -->
  <li>
    <Stage>Warmup</Stage>
    <GraphicData>
      <texPath>确认后的正式贴图路径</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </GraphicData>
  </li>
  <!-- 正式开火及连射间隔隐藏整套武器视觉。 -->
  <li>
    <Stage>Firing</Stage>
    <Visible>false</Visible>
  </li>
  <!-- 最终冷却阶段继续隐藏整套武器视觉。 -->
  <li>
    <Stage>Cooldown</Stage>
    <Visible>false</Visible>
  </li>
</StageVisuals>
```

### Step 7：运行 XML 与正式内容测试

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1
[xml](Get-Content -Raw -Encoding UTF8 "1.6\Content\Defs\ExpressionDef\Visual.xml") | Out-Null
```

Expected: 测试输出 `PASS`，XML 解析无异常，正式贴图文件存在。

### Step 8：提交正式业务配置

```powershell
git status --short
git add 1.6/Content/Defs/ExpressionDef/Visual.xml "1.6/Textures/<正式切分贴图路径>.png" Source/BDP.Tests/WeaponVisualStageContentBoundarySmokeTests.ps1
git commit -m "feat: configure formal weapon stage visuals"
```

## Task 7：构建、回归和游戏内时序验证

**Files:**

- Modify only if needed: `Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1`
- Modify only if needed: `Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1`
- Modify only if needed: `Source/BDP.Tests/WeaponVisualStagePoseIntegrationSmokeTests.ps1`
- Build output: `1.6/Assemblies/BDP.Core.dll`
- Build output: `1.6/Assemblies/BDP.Core.pdb`
- Build output as changed by projects: `1.6/Assemblies/BDP.Content.dll`, `1.6/Assemblies/BDP.Development.dll` and matching PDB files

### Step 1：运行阶段定向测试组

Run:

```powershell
$tests = @(
  'ExpressionSourceReferenceMatcherSmokeTests.ps1',
  'WeaponVisualStageConfigSmokeTests.ps1',
  'WeaponVisualStageResolverSmokeTests.ps1',
  'WeaponVisualStagePoseIntegrationSmokeTests.ps1',
  'WeaponVisualStageDrawIntegrationSmokeTests.ps1',
  'WeaponVisualStageContentBoundarySmokeTests.ps1',
  'SingleWeaponTextureOnlyVisualSmokeTests.ps1',
  'SingleWeaponOverlayVisualSmokeTests.ps1',
  'VisualPoseResolverBoundarySmokeTests.ps1',
  'TriggerVisualPoseDiagnosticsSmokeTests.ps1',
  'PostLoadAttackSessionRecoverySmokeTests.ps1',
  'DualWeaponVanillaRecoilDrawSmokeTests.ps1',
  'VisualAimMirrorVanillaParitySmokeTests.ps1'
)
foreach ($test in $tests) {
  powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path 'Source/BDP.Tests' $test)
  if ($LASTEXITCODE -ne 0) { throw "$test failed" }
}
```

Expected: 全部输出 `PASS`，进程退出码均为 `0`。

### Step 2：构建三个程序集

Run:

```powershell
$env:DOTNET_CLI_HOME = (Resolve-Path '.dotnet').Path
dotnet build Source/BDP/BDP.csproj -c Release --no-restore
dotnet build Source/BDP.Content/BDP.Content.csproj -c Release --no-restore
dotnet build Source/BDP.Development/BDP.Development.csproj -c Release --no-restore
```

Expected: 三项均为 `0 Warning(s)`、`0 Error(s)`；Core 隔离校验通过。

### Step 3：做游戏内实测

正式 Content（正式内容层）目标预设和切分贴图接入后，使用主模组本体逐项验证并记录结果；在正式资源尚未接入前，本步骤明确保持待办，不用占位武器代替：

1. 单武器静止显示原贴图。
2. 起手预热立即切换正式静态切分贴图。
3. 发射与最终冷却整套隐藏，冷却结束恢复。
4. 预热中取消目标／移动／眩晕后恢复静止贴图。
5. 连射两发之间的 `Stance_Cooldown（冷却姿态）` 仍归 `Firing（射击）`，不闪回静止贴图。
6. 暂停时阶段和进度不跳变。
7. 在预热、连射间隔和最终冷却分别保存读档，恢复后阶段正确。
8. 双武器只让实际参与来源变化；共同参与时两把都响应；同芯片副攻击驱动同一把可见武器。
9. 武器隐藏时，投射物仍从原枪口锚点发射，不回退角色中心。
10. 无 `StageVisuals` 的既有正式预设完全不变。

若只在游戏时序中失败，先添加可插拔、带统一搜索前缀的临时诊断日志；定位后删除日志和无效尝试，只保留根因修复。

### Step 4：提交经过验证的构建产物

先检查构建实际改动，再只暂存本任务对应程序集：

```powershell
git status --short
git diff --stat
git add 1.6/Assemblies/BDP.Core.dll 1.6/Assemblies/BDP.Core.pdb
git add 1.6/Assemblies/BDP.Content.dll 1.6/Assemblies/BDP.Content.pdb 1.6/Assemblies/BDP.Development.dll 1.6/Assemblies/BDP.Development.pdb
git commit -m "build: deploy weapon visual stage support"
```

如果某个程序集未变化，不强行暂存或制造无意义改动。

## Task 8：更新工作日志并完成最终审计

**Files:**

- Modify: `../../日志/Agent工作日志/Agent日志43.md`（若已达 20 条则新建下一日志文件）

### Step 1：更新倒序工作日志

把 2026-08-12 顶部当前“只读分析”条目更新为最终完成记录，简洁写明：

- 新增的中性阶段设施与来源实例判断。
- 正式 Content 预设的预热换图、射击／冷却隐藏行为；若正式资源仍未接入，则明确报告“Core 设施完成、业务接入与游戏实测待资源确认”。
- 未新增视觉存档状态，枪口锚点保持有效。
- 定向测试、三个程序集构建和游戏内实测结果。

不要重复追加同一任务的分析条目与完成条目。

### Step 2：运行最终验证

Run:

```powershell
git status --short
git log -8 --oneline
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageConfigSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageResolverSmokeTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/WeaponVisualStageDrawIntegrationSmokeTests.ps1
```

Expected: 三项测试输出 `PASS`；状态中只剩用户原有或明确不属于本任务的改动。

### Step 3：提交日志

```powershell
git add "../../日志/Agent工作日志/Agent日志43.md"
git commit -m "docs: log weapon visual stage support"
```

### Step 4：交付说明

最终回复只报告：

- 已实现的阶段行为和边界。
- 正式切分小立方体贴图仍需替换的唯一 `texPath` 位置。
- 测试、构建、游戏实测证据。
- 建议下一主线：用户提供正式静态切分贴图后接入 Content；独立小方块移动属于未来单独任务，不在本次顺手展开。
