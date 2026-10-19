# High-Angle Roof Constraint Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 BDP 高角射击模块增加屋顶约束，并允许每个模块通过配置决定是否允许普通薄屋顶下的目标，默认禁止。

**Architecture:** 规则继续归属 `BDP.Content` 的 `HighAngleShootingModule`，不向 `BDP.Core` 注入高角或迫击炮业务。手动攻击在 `Confirm`（确认）阶段拒绝并显示语言包文案；自动攻击和正式计划在现有 `ProjectileInit`（投射物初始化）阶段提交停止请求，确保不会回退发射普通子弹。两个阶段共用同一套屋顶判断，且只在当前攻击需要高角路径时生效。

**Tech Stack:** C#（C#语言）、RimWorld 1.6 `RoofGrid/RoofDef`（屋顶网格/屋顶定义）、XML Def（定义配置）、PowerShell 冒烟测试、MSBuild/.NET Release 构建。

---

## 设计基线

- 发射者所在格存在任意屋顶：高角路径拒绝。
- 目标所在格的屋顶 `isThickRoof=true`：高角路径拒绝；山顶屋顶按此规则识别。
- 目标所在格是普通薄屋顶：只有 `AllowTargetUnderThinRoof=true` 才允许；默认 `false`。
- 目标所在格没有屋顶：允许。
- 目标有原版直视线时不切换高角，不受这条高角约束影响。
- 不实现 `ITargetingStageModule` 的鼠标悬停即时拒绝，不修改目标交互边界。
- 双来源攻击继续按既有单侧泳道分别裁定；被拒绝的高角来源不连带修改其他合法来源。

## 文件范围

只允许修改下列正式源码、Def、语言和测试文件：

- `模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingConfig.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingModule.cs`
- `模组工程/BorderDefenseProtocol/1.6/Content/Defs/RangedModuleDef/HighAngleShooting.xml`
- `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/RangedModules.xml`
- `模组工程/BorderDefenseProtocol/Source/BDP.Tests/HighAngleShootingModuleSmokeTests.ps1`

不修改 `BDP.Core`、`BdpProjectile`、追踪模块、路线模块和投射物 Def。

### Task 1: 先扩展高角约束回归测试

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/HighAngleShootingModuleSmokeTests.ps1:6-96`

**Step 1: 添加约束文件读取入口**

在已有 Def 路径变量中加入高角模块 Def 路径：

```powershell
$highAngleDefPath = Join-Path $modRoot "1.6\Content\Defs\RangedModuleDef\HighAngleShooting.xml"
```

在已有路径变量中加入语言包路径：

```powershell
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\RangedModules.xml"
```

在已有源码读取区加入：

```powershell
$languageSource = Get-Content -Raw -LiteralPath $languagePath -Encoding UTF8
```

**Step 2: 添加预期失败断言**

在现有高角模块契约断言附近增加以下约束：

```powershell
Assert-True ($moduleSource -match "IConfirmStageModule") "高角模块必须能在 Confirm 阶段拒绝手动下单。"
Assert-True ($moduleSource -match "AllowTargetUnderThinRoof") "高角模块必须读取薄屋顶目标开关。"
Assert-True ($moduleSource -match "Roofed") "高角模块必须检查发射者所在格是否有屋顶。"
Assert-True ($moduleSource -match "isThickRoof") "高角模块必须使用原版厚屋顶标记判断目标。"
Assert-True ($moduleSource -match "Stop\.IsRequested") "高角模块必须在正式初始化阶段提交硬拒绝。"
Assert-True ($configSource -match "AllowTargetUnderThinRoof") "高角配置必须声明薄屋顶目标开关。"
Assert-True ($configSource -match "allowTargetUnderThinRoof") "高角配置必须持久化薄屋顶目标开关。"
Assert-True ($projectilesDef -notmatch "AllowTargetUnderThinRoof") "薄屋顶开关必须归属高角模块配置，不得写入投射物 Def。"
Assert-True ($highAngleDef -match "<AllowTargetUnderThinRoof>false</AllowTargetUnderThinRoof>") "高角 Def 默认必须禁止薄屋顶目标。"
Assert-True ($languageSource -match "BDP_HighAngle_SourceUnderRoof") "缺少发射者屋顶拒绝文案。"
Assert-True ($languageSource -match "BDP_HighAngle_TargetUnderThickRoof") "缺少厚屋顶目标拒绝文案。"
Assert-True ($languageSource -match "BDP_HighAngle_TargetUnderThinRoof") "缺少薄屋顶目标拒绝文案。"
```

同时把：

```powershell
$highAngleDef = Get-Content -Raw -LiteralPath $highAngleDefPath -Encoding UTF8
```

加入 XML 读取区，并保持原有 `ITargetingStageModule` 禁止断言，确保本次不扩张到悬停目标交互。

**Step 3: 运行测试确认先失败**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\HighAngleShootingModuleSmokeTests.ps1"
```

Expected: FAIL，原因是配置、屋顶判断、确认阶段和语言键尚未存在。

### Task 2: 增加薄屋顶配置并固定默认值

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingConfig.cs:10-79`
- Modify: `模组工程/BorderDefenseProtocol/1.6/Content/Defs/RangedModuleDef/HighAngleShooting.xml:4-14`

**Step 1: 增加配置字段**

在 `ArcHeight` 后增加带成员注释的字段：

```csharp
/// <summary>
/// 是否允许高角射击把普通薄屋顶下的目标作为目标。
/// 厚屋顶/山顶屋顶不受此开关影响，始终禁止。
/// </summary>
public bool AllowTargetUnderThinRoof = false;
```

**Step 2: 补齐配置克隆**

在 `Clone()` 返回对象中加入：

```csharp
AllowTargetUnderThinRoof = AllowTargetUnderThinRoof
```

保证运行时模块拿到的配置快照保留该值。

**Step 3: 补齐配置持久化**

在 `ExposeData()` 中加入局部变量、读取和回写：

```csharp
bool allowTargetUnderThinRoof = AllowTargetUnderThinRoof;
Scribe_Values.Look(ref allowTargetUnderThinRoof, "allowTargetUnderThinRoof", false);
AllowTargetUnderThinRoof = allowTargetUnderThinRoof;
```

**Step 4: 在高角 Def 显式写出默认值**

在 `HighAngleShooting.xml` 的 `defaultConfig` 中加入中文注释和：

```xml
<!-- 默认禁止普通薄屋顶下的目标；需要放行的模块单独改为 true。 -->
<AllowTargetUnderThinRoof>false</AllowTargetUnderThinRoof>
```

### Task 3: 添加语言包文案

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/RangedModules.xml:2-22`

**Step 1: 写入三个拒绝原因语言键**

加入：

```xml
<!-- 高角射击屋顶约束。 -->
<BDP_HighAngle_SourceUnderRoof>高角射击：发射者处于屋顶下，无法进行高角射击。</BDP_HighAngle_SourceUnderRoof>
<BDP_HighAngle_TargetUnderThickRoof>高角射击：目标处于山顶屋顶下，无法进行高角射击。</BDP_HighAngle_TargetUnderThickRoof>
<BDP_HighAngle_TargetUnderThinRoof>高角射击：当前配置不允许攻击普通屋顶下的目标。</BDP_HighAngle_TargetUnderThinRoof>
```

文案只放语言包，C# 只通过语言键的 `Translate()`（语言翻译读取）读取。

### Task 4: 在高角模块内实现共享屋顶裁定

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingModule.cs:1-100`

**Step 1: 扩展模块阶段接口**

类声明增加 `IConfirmStageModule`，保留现有 `IProjectileInitStageModule`；不增加 `ITargetingStageModule`。

**Step 2: 抽取高角路径判断**

把现有只接受 `ProjectileInitStageContext` 的直视线判断收敛为可复用的中性几何判断：

```csharp
private static bool IsHighAngleRequired(Pawn pawn, Map map, LocalTargetInfo target)
{
    return pawn != null
        && map != null
        && pawn.Spawned
        && target.IsValid
        && target.Cell.IsValid
        && target.Cell.InBounds(map)
        && !GenSight.LineOfSight(pawn.Position, target.Cell, map);
}
```

保留现有 `IsOutOfSight(in ProjectileInitStageContext)` 也可以，但它必须最终调用同一判断，不能复制另一套直视线条件。

**Step 3: 实现共享屋顶拒绝原因**

在模块内部增加一个实例方法，只有高角路径需要使用且配置有效时才返回拒绝原因：

```csharp
private bool TryResolveRoofRejectReason(
    Pawn pawn,
    Map map,
    LocalTargetInfo target,
    out string rejectReason)
{
    rejectReason = null;
    if (config == null || !IsHighAngleRequired(pawn, map, target))
    {
        return false;
    }

    if (map.roofGrid.Roofed(pawn.Position))
    {
        rejectReason = "BDP_HighAngle_SourceUnderRoof".Translate();
        return true;
    }

    RoofDef roof = map.roofGrid.RoofAt(target.Cell);
    if (roof == null)
    {
        return false;
    }

    if (roof.isThickRoof)
    {
        rejectReason = "BDP_HighAngle_TargetUnderThickRoof".Translate();
        return true;
    }

    if (!config.AllowTargetUnderThinRoof)
    {
        rejectReason = "BDP_HighAngle_TargetUnderThinRoof".Translate();
        return true;
    }

    return false;
}
```

**Step 4: 接入 Confirm 阶段**

实现显式接口：

```csharp
void IConfirmStageModule.Contribute(ConfirmRecord record)
{
    if (record == null)
    {
        return;
    }

    LocalTargetInfo target = record.SemanticTarget.IsValid
        ? record.SemanticTarget
        : record.Target;
    if (TryResolveRoofRejectReason(record.Pawn, record.Pawn?.Map, target, out string rejectReason))
    {
        record.IsAllowed = false;
        record.RejectReason = rejectReason;
    }
}
```

这条路径只负责手动确认反馈，不改变 Targeting 悬停判断。

**Step 5: 接入 ProjectileInit 阶段**

在现有高角投射物覆盖之前调用共享判断：

```csharp
if (TryResolveRoofRejectReason(
        context.Pawn,
        context.Map,
        context.FinalTarget,
        out string rejectReason))
{
    contribution.Stop.IsRequested = true;
    contribution.Stop.Reason = rejectReason;
    contribution.Stop.Scope = RangedStageStopScope.Attack;
    return;
}
```

之后再执行原有的 emit 数量、直视线、高角 Def 和视觉轨迹提交。这样屋顶不满足时协议生成空计划，不会退回普通投射物。

### Task 5: 运行高角与边界冒烟测试

**Files:**

- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/HighAngleShootingModuleSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/RangedProtocolBoundarySmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/RangedModuleAuthorSurfaceBoundarySmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/RangedModuleRuntimeSessionSmokeTests.ps1`

**Step 1: 运行高角专项测试**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\HighAngleShootingModuleSmokeTests.ps1"
```

Expected: `PASS: 独立高角射击模块契约符合预期。`

**Step 2: 运行模块边界测试**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\RangedModuleAuthorSurfaceBoundarySmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\RangedModuleRuntimeSessionSmokeTests.ps1"
```

Expected: 三个脚本均输出 `PASS`，且确认阶段接口仍由 Core 提供、业务实现仍在 Content。

### Task 6: Release 构建与程序集边界验证

**Step 1: 构建正式程序集**

Run:

```powershell
dotnet build "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\BDP.csproj" -c Release
dotnet build "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Content\BDP.Content.csproj" -c Release
dotnet build "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Compatibility\BDP.Compatibility.csproj" -c Release
dotnet build "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Development\BDP.Development.csproj" -c Release
```

Expected: 四个工程均 `Build succeeded`，无 C# 编译错误。

**Step 2: 验证 Core 隔离**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\Build\Verify-BdpCoreIsolation.ps1" `
  -CoreProjectPath "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\BDP.csproj" `
  -CoreAssemblyPath "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\1.6\Assemblies\BDP.Core.dll" `
  -ModSourceRoot "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source" `
  -CoreSourceRoot "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\Core" `
  -CoreDefsRoot "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\1.6\Content\Defs"
```

Expected: `PASS: BDP Core 零本模组程序集依赖检查通过。`

**Step 3: 验证 Development 单向边界**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\Build\Verify-BdpDevelopmentBoundary.ps1" `
  -ModRoot "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol"
```

Expected: `PASS: BDP Development 单向依赖、正式源码/XML 和输出目录边界正确。`

### Task 7: 进游戏验证实际时序

**Files:**

- No source changes; use a Development-enabled test save and the current BDP release output.

**Step 1: 验证默认薄屋顶禁止**

使用默认 `<AllowTargetUnderThinRoof>false</AllowTargetUnderThinRoof>`，让目标位于普通薄屋顶下且与发射者无直视线。手动点击应收到薄屋顶拒绝提示，自动攻击不得生成投射物。

**Step 2: 验证配置放行**

将同一高角模块配置临时改为 `true` 并重新加载 Def，让目标仍位于普通薄屋顶下。应允许高角计划生成并保持原有高角视觉轨迹。

**Step 3: 验证厚屋顶与山顶屋顶**

无论配置开关取值，目标位于山顶屋顶下都应拒绝；发射者位于普通屋顶或山顶屋顶下也都应拒绝。

**Step 4: 验证普通射击和组合消费者**

确认有直视线的普通射击不被高角规则误伤；确认追踪模块的追踪强度、允许躲过判定、重锁/脱靶和高角视觉轨迹不发生行为变化；确认双来源攻击仍按来源泳道裁定。

**Step 5: 验证无普通子弹回退**

在每个拒绝场景观察实际投射物生成和诊断日志，确认结果是“本次攻击不发射”，而不是生成普通追踪弹。

### Task 8: 提交正式修改

**Files:**

- Add/Modify: 本计划列出的 5 个正式文件

**Step 1: 检查工作区范围**

Run:

```powershell
git status --short
git diff --check
```

确认不包含用户已有的程序集、临时目录、无关日志和其他未授权文件。

**Step 2: 提交**

Run:

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingConfig.cs" "模组工程/BorderDefenseProtocol/Source/BDP.Content/RangedModules/HighAngleShooting/HighAngleShootingModule.cs" "模组工程/BorderDefenseProtocol/1.6/Content/Defs/RangedModuleDef/HighAngleShooting.xml" "模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/RangedModules.xml" "模组工程/BorderDefenseProtocol/Source/BDP.Tests/HighAngleShootingModuleSmokeTests.ps1"
git diff --cached --check
git commit -m "feat: constrain high-angle shooting under roofs"
```

Expected: 只提交本功能的 5 个文件，提交成功后工作区其余既有变更保持原样。
