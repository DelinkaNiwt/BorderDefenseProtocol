# 光魂推进与推进器组合技实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将光魂推进改为“光魂 + 推进器”组合技，并实现贴地直线冲刺、路径建筑阻挡、敌人链式推挤、体型停止力和撞击晕眩，同时让蚱蜢与光魂推进共同复用中性地面冲刺设施。

**Architecture:** 在 `BDP.Content` 内新增不带业务名称的 `GroundDash`（地面冲刺）设施，承载贴地 `PawnFlyer`（单位飞行承载体）、生成工具、有效终点解析和基础目标预览。蚱蜢保留自己的 Shift 多段路径、链式跳跃和专属特效；光魂推进使用独立的 Verb、Effect 和 Flyer，不继承任何蚱蜢业务类。光魂推进的碰撞、推挤和伤害只由自己的效果/飞行器负责，范围预览只读取地图几何阻挡，不读取 Pawn 体型。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 `PawnFlyer`/`Verb`/`Ability` API、XML Def、PowerShell 冒烟测试、`dotnet msbuild`。

---

### Task 1: 先建立失败测试和边界契约

**Files:**
- Create: `Source/BDP.Tests/LightSoulPropulsionDefinitionSmokeTests.ps1`
- Create: `Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1`
- Test: `1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml`
- Test: `1.6/Content/Defs/ChipFunctionDef/Presets.xml`
- Test: `1.6/Content/Defs/ComboDef/LightSoulPropulsion.xml`

**Step 1: Write the failing definition test**

用现有 `TestSupport/ChipManufacturingTestSupport.ps1` 的 UTF-8 读取和断言方式，检查以下契约：

- `BDP_ChipFunction_Thruster` 存在，名称为“推进器”，分类为 `BDP_ChipCategory_Accessory`，槽区为 `MainSub`，占用为 `Single`；
- 推进器只有被动组合来源，不直接生成独立主动能力；
- `BDP_Combo_LightSoulPropulsion` 的第一来源是 `BDP_ChipFunction_LightSoul`，第二来源是 `BDP_ChipFunction_Thruster`，结果是 `BDP_Ability_LightSoulPropulsion`；
- 光魂的两个形态不再引用 `light_soul_propulsion`；
- 光魂推进不再引用 `CastJump`、`Longjump_Jump`、`Longjump_Land` 或原短距跳跃 Verb；
- 光魂推进使用中性地面冲刺 Verb 和光魂专属 Effect；
- 新增能力、组合技、推进器的玩家可见文案均有简体中文语言键。

**Step 2: Write the failing source boundary test**

检查源码结构契约：

- `GroundDash` 目录存在 `PawnFlyer_GroundDash`、`GroundDashUtility`、`Verb_CastAbilityGroundDash`；
- `Verb_CastAbilityGrasshopper` 可以引用/继承中性地面冲刺入口，但光魂推进源码不得出现 `Grasshopper` 类型名；
- `PawnFlyer_BounceSlash` 不再把公共地面位移设施绑定到蚱蜢专属基类；
- 光魂推进的运行时源码包含路径逐格检查、伤害去重、`BodySize` 停止力判定和 `StaggerFor` 撞击处理；
- Core 工程不新增 Content 命名空间或业务引用。

**Step 3: Run tests to verify they fail**

Run:

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
```

Expected: FAIL，原因是新 Def、组合技和 GroundDash 源码尚未存在。失败必须来自缺失的新契约，不应先修改旧测试来消除失败。

**Step 4: Commit the test contract**

```powershell
git add -- Source/BDP.Tests/LightSoulPropulsionDefinitionSmokeTests.ps1 Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1
git commit -m "test: define light soul propulsion contracts"
```

### Task 2: 抽出中性地面冲刺飞行器

**Files:**
- Create: `Source/BDP.Content/GroundDash/PawnFlyer_GroundDash.cs`
- Modify: `Source/BDP.Content/Grasshopper/PawnFlyer_Grasshopper.cs`
- Modify: `Source/BDP.Content/BounceSlash/PawnFlyer_BounceSlash.cs`

**Step 1: Write the minimal neutral flyer**

从 `PawnFlyer_Grasshopper` 中移动真正与业务无关的成员到 `PawnFlyer_GroundDash`：

- 读取 `PawnFlyerProperties.progressCurve` 并直接调用 `SimpleCurve.Evaluate()`，避免原版动画曲线在斜率变化处过冲；
- `DrawPos` 使用起点到终点的单调线性位置；
- 保留贴地高度、Pawn 绘制、携带物绘制、阴影绘制和 `RespawnPawn` 的通用落地回调；
- 提供 `protected virtual void OnDashSegment(IntVec3 from, IntVec3 to)` 作为逐帧碰撞扩展点，不在基类放入任何敌人伤害或蚱蜢逻辑；
- 公开中性 `Action onLanded`，只用于落地后的通用回调；
- C# 成员逐个补充中文 XML 注释，避免复制蚱蜢业务注释进入中性类。

**Step 2: 保留蚱蜢专属视觉和路径状态**

令 `PawnFlyer_Grasshopper` 继承 `PawnFlyer_GroundDash`，保留踏板、气浪和蚱蜢专属状态；其多段回调继续使用基类公开的 `onLanded`。不要让 `PawnFlyer_GroundDash` 反向认识 `Grasshopper`。

**Step 3: 迁移弹射砍击的公共基类**

让 `PawnFlyer_BounceSlash` 继承 `PawnFlyer_GroundDash`，保留现有沿线段伤害去重逻辑，并通过 `OnDashSegment` 或等价的基类 Tick 钩子保持原有行为。它不得因为迁移而获得光魂推挤、推力或大体型判定。

**Step 4: Run the Content build**

Run:

```powershell
dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected: PASS，且只验证新增基类和两个旧消费者的类型迁移；若失败，先修正继承链/API，不继续新增业务逻辑。

**Step 5: Commit the neutral flyer extraction**

```powershell
git add -- Source/BDP.Content/GroundDash Source/BDP.Content/Grasshopper/PawnFlyer_Grasshopper.cs Source/BDP.Content/BounceSlash/PawnFlyer_BounceSlash.cs
git commit -m "refactor: extract neutral ground dash flyer"
```

### Task 3: 抽出中性路径解析、生成和预览

**Files:**
- Create: `Source/BDP.Content/GroundDash/GroundDashUtility.cs`
- Create: `Source/BDP.Content/GroundDash/Verb_CastAbilityGroundDash.cs`
- Modify: `Source/BDP.Content/Grasshopper/GrasshopperUtility.cs`
- Modify: `Source/BDP.Content/Grasshopper/Verb_CastAbilityGrasshopper.cs`

**Step 1: Implement the shared path result**

在 `GroundDashUtility` 中提供中性、无状态的有效终点解析方法，至少包含：

- 最大距离裁剪；
- 地图边界检查；
- 沿起点到请求点的直线逐格遍历；
- 第一处 `Impassable`、不可 `Walkable`、关闭门或不可通行建筑前停止；
- 返回最后一个可用格、实际方向和是否被几何物阻挡；
- 不检查 Pawn 体型、不把 Pawn 当作范围预览阻挡物、不绕路。

所有 preview（预览）、`ValidateTarget`（目标验证）和施法实际终点都使用这一结果，避免三套判定漂移。若起点到目标中间无可用落点，目标验证失败。

**Step 2: Implement the neutral ground-dash Verb**

从蚱蜢 Verb 中迁移仅与单段地面冲刺相关的能力：

- `EffectiveRange` 读取能力 Verb 的最大射程；
- `CanHitTargetFrom` 使用距离、地图边界和视线；
- `ValidateTarget` 允许按请求点解析有效终点，拒绝没有有效终点的目标；
- `OnGUI` 显示可用/不可用鼠标指示；
- `DrawHighlight` 绘制射程圈，并让每个候选格经过同一套建筑截断解析；
- 目标高亮显示有效终点，不显示墙后不可达格；
- 不实现 Shift、多段锚点、路径队列或蚱蜢专属文案。

**Step 3: Route Grasshopper through the shared facility**

将 `GrasshopperUtility.DoJump` 中的 `PawnFlyer.MakeFlyer`、保存地图/起点/选中状态、生成和恢复选中状态抽为 `GroundDashUtility.DoDash`。蚱蜢工具只保留兼容的业务入口和 `JobDefOf.CastJump` 指令；蚱蜢 Verb 的 Shift 多段逻辑继续留在蚱蜢命名空间。

让蚱蜢的单段落点校验和射程高亮改用共享路径解析；多段锚点仍由蚱蜢自己的路径输入规则控制。不要把光魂推进接入 `PathInputState`。

**Step 4: Run boundary and build tests**

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected: 首条测试仍可能因光魂实现未完成而 FAIL，但中性设施和蚱蜢迁移相关源码必须可编译；不修改测试去掩盖编译错误。

**Step 5: Commit shared targeting facilities**

```powershell
git add -- Source/BDP.Content/GroundDash Source/BDP.Content/Grasshopper/GrasshopperUtility.cs Source/BDP.Content/Grasshopper/Verb_CastAbilityGrasshopper.cs
git commit -m "refactor: share ground dash targeting facilities"
```

### Task 4: 实现光魂推进的独立碰撞与推挤链

**Files:**
- Create: `Source/BDP.Content/LightSoul/Propulsion/CompAbilityEffect_LightSoulPropulsion.cs`
- Create: `Source/BDP.Content/LightSoul/Propulsion/PawnFlyer_LightSoulPropulsion.cs`

**Step 1: Add the failing behavior assertions**

在 `LightSoulPropulsionSourceBoundarySmokeTests.ps1` 中补充对关键逻辑形状的断言：

- 使用 Bresenham（布雷森汉姆栅格线）或同等逐格线段扫描；
- 每次施放有已处理 Pawn 集合；
- 伤害类型为 `Blunt`；
- 先伤害，再按 `BodySize <= pushStoppingPower + 0.001f` 判断可推动；
- 不可推动目标会停止当前推挤链但不调用晕眩；
- 已推动敌人撞建筑或大体型目标时调用 `StaggerFor`；
- 推挤目标前进格必须 `Walkable` 且不能与其它 Thing/Pawn 重叠。

Run:

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
```

Expected: FAIL，直到两个光魂推进类完成。

**Step 2: Implement the typed Effect properties**

新增 `CompProperties_LightSoulPropulsion`，由 Def 注入以下运行参数：

- `pawnFlyerDef`；
- `damageDef`，默认由运行期兜底为 `DamageDefOf.Blunt`；
- `damageAmount`；
- `armorPenetration`；
- `pushStoppingPower`；
- `staggerTicks`，初始值 95，沿用原版投射物默认值；
- `sourceLabelKey` 或等价语言键来源。

`CompAbilityEffect_LightSoulPropulsion.Apply` 只负责检查 Pawn/Map/Def、解析有效终点、生成 `PawnFlyer_LightSoulPropulsion` 并通过 `GroundDashUtility.DoDash` 发射；不复制蚱蜢的多段 Trion 扣费或路径队列。

**Step 3: Implement one-pass damage and collision scanning**

在 `PawnFlyer_LightSoulPropulsion` 中：

1. 每 Tick 取上一帧和当前帧的逐格线段，避免高速冲刺跨格漏检；
2. 对 `FlyingPawn` 自身、死亡 Pawn、非敌对 Pawn 跳过；
3. 使用 `HashSet<Pawn>` 保证一次施放每个敌人只受一次钝伤；
4. 伤害成功后立即计算该 Pawn 的体型；
5. 可推动目标沿冲刺方向尝试逐格向前递进，前方有可推动单位时先为其保留链位置；
6. 前方是不可通行建筑、动态障碍、大体型 Pawn 或无合法落点时停止，不绕路、不穿墙、不重叠；
7. 大体型 Pawn 只受一次钝伤并成为虚拟墙，不被推动、不被晕眩；
8. 已推动链中实际受到阻挡的单位调用 `target.stances?.stagger?.StaggerFor(staggerTicks)`；
9. 对每次施放记录链内碰撞状态，避免同一帧或连续帧重复推挤/重复晕眩。

移动 Pawn 时使用原版允许的地图位置更新和 `Notify_Teleported`/等价通知路径，清理旧路径并让 Pawn 的位置、占用格和绘制状态保持一致；禁止直接生成副本或用 `WipeMode.Vanish` 覆盖其它 Pawn。

**Step 4: Recheck dynamic geometry during flight**

飞行器每 Tick 重新检查当前移动段的地图几何阻挡。若冲刺开始后建筑或门状态改变，立即把飞行终点收缩到阻挡前最后可用格并结束冲刺，确保实际逻辑和视觉不穿墙。

**Step 5: Run the behavior-shape test and build**

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected: 测试 PASS，Content 编译 PASS。若原版位置通知 API 与预期不同，依据真实编译错误调整为 RimWorld 1.6 的实际 API，不新增自定义位置系统。

**Step 6: Commit the propulsion runtime**

```powershell
git add -- Source/BDP.Content/LightSoul/Propulsion Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1
git commit -m "feat: add light soul propulsion dash collision"
```

### Task 5: 接入 Def、组合技和语言包

**Files:**
- Modify: `1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml`
- Create: `1.6/Content/Defs/ComboDef/LightSoulPropulsion.xml`
- Modify: `1.6/Content/Defs/ChipFunctionDef/Presets.xml`
- Create: `1.6/Content/Defs/ThingDef/PawnFlyer_LightSoulPropulsion.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/AbilityDef/LightSoul.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/ComboDef/LightSoulPropulsion.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/ChipFunctionDef/Presets.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Gameplay.xml`

**Step 1: Convert the existing ability into a combo result host**

保留 `BDP_Ability_LightSoulPropulsion` 这个结果 Ability 的 DefName，避免在当前组合结果绑定上增加无必要的改名；将 `verbClass` 固定改为中性 `BDP.Content.GroundDash.Verb_CastAbilityGroundDash`，将 `jobDef` 从 `CastJump` 移除，并改用地面冲刺专用 `pawnFlyerDef` 和 `CompProperties_LightSoulPropulsion`。移除跳跃音效，使用已有中性音效或不配置新音效，避免将蚱蜢/长跳命名带入光魂。

配置 Blunt 伤害、最大射程、冷却、停止力、伤害、穿透和 95 ticks 晕眩参数；数值先保持设计阶段约定的最小可用默认值，平衡调整留给后续实测。

**Step 2: Add the Thruster function and combo**

在 `Presets.xml` 新增配件类 `BDP_ChipFunction_Thruster`，使用独立的被动身份键。新增 `BDP_Combo_LightSoulPropulsion`，来源顺序为光魂第一、推进器第二，Trion `UseCostResolve`/`MinimumRequiredResolve` 使用 `FollowFirstSource`，使原光魂推进的 20 Trion 语义保持不变。组合结果表达条目引用 `BDP_Ability_LightSoulPropulsion`，并配置组合条件所需的 Ability/Trion 组件。

从光魂表达目录、`shield`、`heavy_blade` 的 `ActiveEntryIds` 删除 `light_soul_propulsion`，确保没有推进器时绝不出现按钮。

**Step 3: Add ThingDef and localization**

新增 `PawnFlyer_LightSoulPropulsion` ThingDef，使用光魂专属 Flyer 类和贴地、单调的飞行参数；不引用 Grasshopper 平台/气浪视觉。

把推进器名称、组合技名称/说明、能力说明、推进器说明和新的战斗描述全部放入简体中文语言包；代码只读取语言键，不硬编码新增玩家文案。保留英文技术 DefName，并在中文注释中说明其含义。

**Step 4: Run definition tests**

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
```

Expected: PASS，能确认光魂直连入口已删除、组合来源正确、推进器类别正确、Ability 不再是跳跃定义、语言键齐全。

**Step 5: Build all formal projects in dependency order**

```powershell
dotnet msbuild .\Source\BDP\BDP.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
dotnet msbuild .\Source\BDP.Development\BDP.Development.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal
```

Expected: 三个项目均 PASS，且不把 `BDP.Development` 引用写入正式源码/Def。

**Step 6: Commit the formal content**

```powershell
git add -- `
  '1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml' `
  '1.6/Content/Defs/ComboDef/LightSoulPropulsion.xml' `
  '1.6/Content/Defs/ChipFunctionDef/Presets.xml' `
  '1.6/Content/Defs/ThingDef/PawnFlyer_LightSoulPropulsion.xml' `
  'Languages/ChineseSimplified (简体中文)/DefInjected/AbilityDef/LightSoul.xml' `
  'Languages/ChineseSimplified (简体中文)/DefInjected/ComboDef/LightSoulPropulsion.xml' `
  'Languages/ChineseSimplified (简体中文)/DefInjected/ChipFunctionDef/Presets.xml' `
  'Languages/ChineseSimplified (简体中文)/Keyed/Gameplay.xml'
git commit -m "feat: make light soul propulsion a combo ability"
```

提交前用 `git diff --cached --name-only` 确认命令没有暂存现有 DLL、PDB 或参考贴图；PowerShell 路径若因中文括号/空格解析失败，改用完整单引号路径逐项 `git add --`，不扩大暂存范围。

### Task 6: 回归检查和实机验证清单

**Files:**
- Modify: `Source/BDP.Tests/LightSoulPropulsionDefinitionSmokeTests.ps1`（仅在发现契约缺口时补测试）
- Modify: `Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1`（仅在发现契约缺口时补测试）
- Create or modify: `日志/Agent工作日志/` 下当前工作日志文件

**Step 1: Run all focused smoke tests**

```powershell
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
pwsh -NoProfile -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
pwsh -NoProfile -File .\Source\BDP.Tests\ChipCombinationResolverSmokeTests.ps1
pwsh -NoProfile -File .\Source\BDP.Tests\ChipCombinationSelectionRulesSmokeTests.ps1
pwsh -NoProfile -File .\Source\BDP.Tests\UnifiedModAssemblyLayoutSmokeTests.ps1
```

Expected: 全部 PASS；若某测试未接受新 DefName/类名，修正测试契约或实现，不能添加兼容旧跳跃入口的残留业务代码。

**Step 2: Run architecture boundary checks**

按脚本参数实际要求执行：

```powershell
pwsh -NoProfile -File .\Source\Build\Verify-BdpDevelopmentBoundary.ps1 -ModRoot .
pwsh -NoProfile -File .\Source\Build\Verify-BdpReleaseReady.ps1 -ModRoot .
```

Core 隔离检查使用当前 `Verify-BdpCoreIsolation.ps1` 的完整参数；预期 Core 仍只依赖外部 RimWorld/Unity/系统程序集，不认识 Content。

**Step 3: Verify the formal output boundary**

确认构建产生的 DLL/PDB 属于本次构建结果，但不把它们自动纳入提交；工作区原有二进制改动保持原状，最终提交只包含源码、Def、语言包、测试和文档。

**Step 4: Perform the in-game checklist**

在游戏中逐项确认：

- 只有光魂和推进器都在合法同侧 Main/Sub 时显示光魂推进；只装光魂时不显示；
- 近点施法只推进到选取点；超过射程按最大距离；
- 范围圈/路径预览在墙、不可通行建筑、关闭门前截断；大体型 Pawn 不截断预览；
- 实际冲刺不穿墙，飞行中动态出现的建筑也会阻挡；
- 路径上的敌人每个只受一次 Blunt 伤害；小体型敌人按冲刺方向连续推挤；
- 大体型目标只受伤害、不移动、不晕眩，并使已推动链在其前方停止；
- 被推动链撞墙或撞大体型目标时产生短暂晕眩；
- 不穿透、不重叠、不绕路；
- 光魂盾形态/重刃形态都能使用组合能力；
- 视觉上没有长跳音效、蚱蜢踏板或蚱蜢气浪，位置与范围预览一致。

**Step 5: Write the reverse-chronological work log**

在 `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志` 当前日志文件顶部增加本次最新记录；超过 20 条时新建日志文件。记录设计提交、实现提交、测试命令和实机验证结论，不记录未采用的试错路线。

**Step 6: Final verification before completion**

```powershell
git diff --check
git status --short
git log -5 --oneline
```

Expected: `git diff --check` 无输出；工作区只保留用户原有的 DLL/PDB/贴图改动，新增实现文件已提交；最终回复只声明实际执行并有证据支持的构建/测试结果，明确实机验证是否完成。
