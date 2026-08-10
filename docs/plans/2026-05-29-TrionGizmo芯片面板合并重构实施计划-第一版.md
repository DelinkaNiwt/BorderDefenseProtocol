# Trion Gizmo 芯片面板合并重构实施计划（第一版）

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把 `Trion` 能量 Gizmo（小控件）扩展成可插入右侧面板的正式 UI（界面），并由 DevHarness（伴生测试模组）插入主/副触发体芯片槽面板。

**Architecture:** 主模组只新增中性的 `Trion` Gizmo 面板扩展口和布局承载能力，不认识芯片业务。DevHarness 注册面板扩展，读取现有 `TriggerSurfaceAccess` 正式 surface（接口面）来显示槽位并提交激活、关闭、切换。特殊侧芯片继续走 `Trion` 顶部徽标区，不进入主/副槽位行。

**Tech Stack:** RimWorld 1.6 Mod、C# 7.3、Verse/RimWorld Gizmo、PowerShell smoke tests（烟测）、`dotnet build`。

**Project Constraint:** 直接在当前工程执行，不创建 worktree（工作树），不新建 git 分支；每个任务只做计划内改动并单独提交。

---

## 0. 执行前确认

从仓库根目录执行：

```powershell
git status --short
```

预期：

- 工作区可能已有大量无关改动。
- 不回退、不清理、不覆盖无关改动。
- 本计划每个任务只 `git add` 自己列出的文件。

参考设计文档：

- `模组工程/BorderDefenseProtocol/docs/plans/2026-05-29-TrionGizmo芯片面板合并重构设计-第一版.md`

## Task 1: 主模组新增 Trion 面板扩展契约

**Files:**

- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/External/ITrionGizmoPanelExtensionProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/External/TrionGizmoExtensionRegistry.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoPanelExtensionContractSmokeTests.ps1`

**Step 1: Write the failing test**

创建 `TrionGizmoPanelExtensionContractSmokeTests.ps1`：

```powershell
$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$externalRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion\External'

$badgeProviderPath = Join-Path $externalRoot 'ITrionGizmoExtensionProvider.cs'
$panelProviderPath = Join-Path $externalRoot 'ITrionGizmoPanelExtensionProvider.cs'
$registryPath = Join-Path $externalRoot 'TrionGizmoExtensionRegistry.cs'

Assert-True (Test-Path -LiteralPath $badgeProviderPath) 'Existing badge provider interface must remain.'
Assert-True (Test-Path -LiteralPath $panelProviderPath) 'Trion panel extension provider interface must exist.'
Assert-True (Test-Path -LiteralPath $registryPath) 'Trion extension registry must exist.'

$badgeText = Get-Content -LiteralPath $badgeProviderPath -Raw -Encoding utf8
$panelText = Get-Content -LiteralPath $panelProviderPath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding utf8
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Recurse -Filter '*.cs' | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"

Assert-True ($badgeText -match 'IEnumerable<TrionGizmoExtensionBadge>\s+GetBadges') 'Badge provider GetBadges contract must remain unchanged.'
Assert-True ($panelText -match 'interface\s+ITrionGizmoPanelExtensionProvider') 'Panel provider interface must be named ITrionGizmoPanelExtensionProvider.'
Assert-True ($panelText -match 'float\s+GetWidth\s*\(\s*TrionGizmoExtensionContext\s+context\s*\)') 'Panel provider must expose GetWidth(context).'
Assert-True ($panelText -match 'GizmoResult\s+DrawPanel\s*\(') 'Panel provider must expose DrawPanel(...).'
Assert-True ($panelText -match 'Rect\s+panelRect') 'DrawPanel must receive a panel rect from the Trion Gizmo container.'
Assert-True ($panelText -match 'GizmoRenderParms\s+parms') 'DrawPanel must receive Gizmo render parms.'

Assert-True ($registryText -match 'List<ITrionGizmoExtensionProvider>') 'Registry must keep badge providers.'
Assert-True ($registryText -match 'List<ITrionGizmoPanelExtensionProvider>') 'Registry must keep panel providers separately.'
Assert-True ($registryText -match 'RegisterPanel\s*\(\s*ITrionGizmoPanelExtensionProvider') 'Registry must expose RegisterPanel for panel providers.'
Assert-True ($registryText -match 'UnregisterPanel\s*\(\s*ITrionGizmoPanelExtensionProvider') 'Registry must expose UnregisterPanel for panel providers.'
Assert-True ($registryText -match 'GetPanelProviders\s*\(') 'Registry must expose panel provider enumeration.'

Assert-True ($trionText -notmatch 'BDP\.DevHarness') 'Main Trion code must not reference DevHarness.'

Write-Output 'TrionGizmoPanelExtensionContractSmokeTests PASS'
```

**Step 2: Run test to verify it fails**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelExtensionContractSmokeTests.ps1"
```

Expected: FAIL，因为 `ITrionGizmoPanelExtensionProvider.cs` 还不存在。

**Step 3: Create panel provider interface**

新增：

```csharp
using UnityEngine;
using Verse;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态卡右侧面板扩展提供器。
    /// 它只负责向 Trion Gizmo 提供可选的右侧绘制面板，不承载 Trion 结算或业务命令。
    /// </summary>
    public interface ITrionGizmoPanelExtensionProvider
    {
        /// <summary>
        /// 返回当前上下文需要的面板宽度。
        /// 返回 0 或负数表示当前不显示面板。
        /// </summary>
        float GetWidth(TrionGizmoExtensionContext context);

        /// <summary>
        /// 在 Trion Gizmo 分配的右侧区域内绘制面板并处理输入。
        /// </summary>
        GizmoResult DrawPanel(
            TrionGizmoExtensionContext context,
            Rect panelRect,
            GizmoRenderParms parms);
    }
}
```

**Step 4: Extend registry**

在 `TrionGizmoExtensionRegistry.cs` 中保留现有徽标 providers，并新增面板 providers：

```csharp
private static readonly List<ITrionGizmoPanelExtensionProvider> panelProviders =
    new List<ITrionGizmoPanelExtensionProvider>();
```

新增成员：

```csharp
/// <summary>
/// 注册右侧面板扩展提供器。
/// </summary>
public static void RegisterPanel(ITrionGizmoPanelExtensionProvider provider)
{
    if (provider == null || panelProviders.Contains(provider))
    {
        return;
    }

    panelProviders.Add(provider);
}

/// <summary>
/// 反注册右侧面板扩展提供器。
/// </summary>
public static void UnregisterPanel(ITrionGizmoPanelExtensionProvider provider)
{
    if (provider == null)
    {
        return;
    }

    panelProviders.Remove(provider);
}

/// <summary>
/// 获取当前已注册的右侧面板扩展提供器。
/// 第一版由 Gizmo 容器只消费第一个有效面板。
/// </summary>
public static IEnumerable<ITrionGizmoPanelExtensionProvider> GetPanelProviders()
{
    for (int i = 0; i < panelProviders.Count; i++)
    {
        if (panelProviders[i] != null)
        {
            yield return panelProviders[i];
        }
    }
}
```

**Step 5: Run test to verify it passes**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelExtensionContractSmokeTests.ps1"
```

Expected: PASS.

**Step 6: Build main mod**

```powershell
dotnet build "模组工程/BorderDefenseProtocol/Source/BDP/BDP.csproj" -c Release
```

Expected: build succeeds.

**Step 7: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/External/ITrionGizmoPanelExtensionProvider.cs" `
          "模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/External/TrionGizmoExtensionRegistry.cs" `
          "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoPanelExtensionContractSmokeTests.ps1"
git commit -m "feat: 添加 Trion Gizmo 面板扩展契约"
```

## Task 2: Trion Gizmo 接入右侧面板布局

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoPanelLayoutSmokeTests.ps1`

**Step 1: Write the failing test**

创建 `TrionGizmoPanelLayoutSmokeTests.ps1`：

```powershell
$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'

Assert-True (Test-Path -LiteralPath $gizmoPath) 'Gizmo_TrionStatus.cs must exist.'

$text = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8

Assert-True ($text -match 'BaseCardWidth') 'Trion gizmo must keep a named base card width for the left resource area.'
Assert-True ($text -match 'PanelSpacing') 'Trion gizmo must define spacing between Trion base area and extension panel.'
Assert-True ($text -match 'ResolvePanelExtension') 'Trion gizmo must resolve a panel extension provider.'
Assert-True ($text -match 'GetPanelProviders\(\)') 'Trion gizmo must read panel providers from TrionGizmoExtensionRegistry.'
Assert-True ($text -match 'DrawPanelExtension') 'Trion gizmo must isolate panel drawing in a helper.'
Assert-True ($text -match 'provider\.DrawPanel') 'Trion gizmo must delegate right-side panel rendering to the provider.'
Assert-True ($text -match 'baseRect') 'Trion gizmo must keep a dedicated baseRect for the Trion resource area.'
Assert-True ($text -match 'panelRect') 'Trion gizmo must create a dedicated panelRect for extension content.'
Assert-True ($text -match 'GetWidth\(maxWidth\)') 'GizmoOnGUI must size outerRect from GetWidth(maxWidth).'
Assert-True ($text -match 'BaseCardWidth\s*\+\s*PanelSpacing\s*\+\s*panelWidth') 'GetWidth must add panel width to the base Trion width.'

Assert-True ($text -match 'CollectBadges') 'Existing badge collection must remain.'
Assert-True ($text -match 'CreateFrozenBadge') 'Existing frozen badge must remain.'
Assert-True ($text -match 'BuildTooltip') 'Existing Trion tooltip must remain.'
Assert-True ($text -notmatch 'BDP\.Core\.Trigger') 'Main Trion gizmo must not reference Trigger namespace.'
Assert-True ($text -notmatch 'BDP\.DevHarness') 'Main Trion gizmo must not reference DevHarness namespace.'

Write-Output 'TrionGizmoPanelLayoutSmokeTests PASS'
```

**Step 2: Run test to verify it fails**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelLayoutSmokeTests.ps1"
```

Expected: FAIL，因为 `Gizmo_TrionStatus` 还没有右侧面板布局。

**Step 3: Refactor constants**

在 `Gizmo_TrionStatus.cs` 中：

- 把 `CardWidth` 改名为 `BaseCardWidth`。
- 新增 `PanelSpacing = 4f`。
- 保持 `CardHeight = 75f` 不变。

关键点：

- `BaseCardWidth` 只代表左侧 `Trion` 能量条区域。
- 外层总宽度由 `GetWidth` 计算。

**Step 4: Add panel resolution helper**

新增私有方法：

```csharp
/// <summary>
/// 解析当前第一个有效右侧面板扩展。
/// 第一版只消费第一个返回正宽度的面板，避免多个大面板挤占 Gizmo 区域。
/// </summary>
private ITrionGizmoPanelExtensionProvider ResolvePanelExtension(
    TrionGizmoExtensionContext context,
    out float panelWidth)
{
    panelWidth = 0f;
    foreach (ITrionGizmoPanelExtensionProvider provider in TrionGizmoExtensionRegistry.GetPanelProviders())
    {
        if (provider == null)
        {
            continue;
        }

        float requestedWidth = Mathf.Max(0f, provider.GetWidth(context));
        if (requestedWidth <= 0f)
        {
            continue;
        }

        panelWidth = requestedWidth;
        return provider;
    }

    return null;
}
```

**Step 5: Update GetWidth**

改为：

```csharp
public override float GetWidth(float maxWidth)
{
    TrionGizmoExtensionContext context = new TrionGizmoExtensionContext(
        owner,
        reader,
        new Rect(0f, 0f, BaseCardWidth, CardHeight));

    float panelWidth;
    ResolvePanelExtension(context, out panelWidth);
    return panelWidth > 0f ? BaseCardWidth + PanelSpacing + panelWidth : BaseCardWidth;
}
```

**Step 6: Split GizmoOnGUI layout**

核心结构：

```csharp
public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
{
    Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), CardHeight);
    Rect baseRect = new Rect(topLeft.x, topLeft.y, BaseCardWidth, CardHeight);
    Rect baseInnerRect = baseRect.ContractedBy(InnerPadding);

    TrionGizmoExtensionContext context = new TrionGizmoExtensionContext(owner, reader, baseRect);
    float panelWidth;
    ITrionGizmoPanelExtensionProvider panelProvider = ResolvePanelExtension(context, out panelWidth);
    Rect panelRect = panelProvider != null && panelWidth > 0f
        ? new Rect(baseRect.xMax + PanelSpacing, topLeft.y, panelWidth, CardHeight)
        : Rect.zero;

    Widgets.DrawWindowBackground(outerRect);

    // titleRect / barRect / bottomRect 从 baseInnerRect 计算。
    // 现有 DrawTitleRow / DrawBar / DrawBottomRow 保持只使用左侧 Trion 区。

    GizmoResult panelResult = DrawPanelExtension(panelProvider, context, panelRect, parms);

    if (Mouse.IsOver(baseRect))
    {
        Widgets.DrawHighlight(baseRect);
        TooltipHandler.TipRegion(baseRect, new TipSignal(BuildTooltip(), BuildTooltipId(1937421)));
    }

    return panelResult.State == GizmoState.Interacted
        ? panelResult
        : new GizmoResult(GizmoState.Clear);
}
```

注意：

- `BuildTooltip` 只挂在 `baseRect`，避免覆盖右侧芯片面板 tooltip。
- `CollectBadges` 继续只服务左侧 Trion 标题行。
- `outerRect` 只负责整体背景。

**Step 7: Add DrawPanelExtension helper**

```csharp
/// <summary>
/// 绘制右侧面板扩展。
/// </summary>
private GizmoResult DrawPanelExtension(
    ITrionGizmoPanelExtensionProvider provider,
    TrionGizmoExtensionContext context,
    Rect panelRect,
    GizmoRenderParms parms)
{
    if (provider == null || panelRect.width <= 0f)
    {
        return new GizmoResult(GizmoState.Clear);
    }

    TrionGizmoExtensionContext panelContext = new TrionGizmoExtensionContext(owner, reader, panelRect);
    return provider.DrawPanel(panelContext, panelRect, parms);
}
```

**Step 8: Run tests**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelLayoutSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoDrainDetailsSmokeTests.ps1"
```

Expected: both PASS.

**Step 9: Build main mod**

```powershell
dotnet build "模组工程/BorderDefenseProtocol/Source/BDP/BDP.csproj" -c Release
```

Expected: build succeeds.

**Step 10: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion/Gizmo_TrionStatus.cs" `
          "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionGizmoPanelLayoutSmokeTests.ps1"
git commit -m "feat: 扩展 Trion Gizmo 右侧面板布局"
```

## Task 3: DevHarness 新增 Trion 芯片槽面板提供器

**Files:**

- Create: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/TrionTriggerLoadoutPanelProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/DevHarnessBootstrap.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1`

**Step 1: Write the failing test**

创建 `DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1`：

```powershell
$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$providerPath = Join-Path $devHarnessRoot 'TrionTriggerLoadoutPanelProvider.cs'
$bootstrapPath = Join-Path $devHarnessRoot 'DevHarnessBootstrap.cs'

Assert-True (Test-Path -LiteralPath $providerPath) 'DevHarness Trion trigger loadout panel provider must exist.'
Assert-True (Test-Path -LiteralPath $bootstrapPath) 'DevHarnessBootstrap.cs must exist.'

$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8

Assert-True ($providerText -match 'class\s+TrionTriggerLoadoutPanelProvider\s*:\s*ITrionGizmoPanelExtensionProvider') 'Provider must implement ITrionGizmoPanelExtensionProvider.'
Assert-True ($bootstrapText -match 'TrionGizmoExtensionRegistry\.RegisterPanel\s*\(\s*new\s+TrionTriggerLoadoutPanelProvider\s*\(') 'DevHarness must register the Trion panel provider.'

Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveLoadoutReader') 'Panel must read Trigger loadout through formal surface.'
Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveInteractionReader') 'Panel must read Trigger interaction through formal surface.'
Assert-True ($providerText -match 'TriggerSurfaceAccess\.ResolveLoadoutCommands') 'Panel must submit commands through formal surface.'
Assert-True ($providerText -notmatch 'TryGetComp<CompTriggerBody>') 'Panel must not resolve CompTriggerBody directly.'
Assert-True ($providerText -notmatch '\.mainSlots|\.subSlots|\.specialSlots') 'Panel must not touch internal slot lists.'

Assert-True ($providerText -match 'GetSlots\s*\(\s*TriggerSide\.Main\s*\)') 'Panel must draw main slots.'
Assert-True ($providerText -match 'GetSlots\s*\(\s*TriggerSide\.Sub\s*\)') 'Panel must draw sub slots.'
Assert-True ($providerText -notmatch 'GetSlots\s*\(\s*TriggerSide\.Special\s*\)') 'Panel must not draw special side as slot cells.'

Assert-True ($providerText -match 'RequestActivate') 'Panel must support activation/switch through RequestActivate.'
Assert-True ($providerText -match 'RequestDeactivate') 'Panel must support deactivation through RequestDeactivate.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Activate') 'Panel must interpret Activate operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.SwitchTo') 'Panel must interpret SwitchTo operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Deactivate') 'Panel must interpret Deactivate operation.'
Assert-True ($providerText -match 'TriggerInteractionOperationKind\.Mirror') 'Panel must handle mirror slots without direct command.'

Assert-True ($providerText -match 'DrawWarmupProgressBar') 'Panel must keep warmup progress drawing isolated.'
Assert-True ($providerText -match 'DrawWinddownProgressBar') 'Panel must keep winddown progress drawing isolated.'
Assert-True ($providerText -match 'xMax\s*-\s*width') 'Winddown progress must fill from right to left.'
Assert-True ($providerText -match 'BuildSlotTooltip') 'Panel must provide per-slot tooltip.'
Assert-True ($providerText -notmatch 'Prefs\.DevMode') 'Panel visibility must not be gated by DevMode.'

Write-Output 'DevHarnessTrionTriggerLoadoutPanelSmokeTests PASS'
```

**Step 2: Run test to verify it fails**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1"
```

Expected: FAIL，因为 provider 文件还不存在。

**Step 3: Register provider in DevHarnessBootstrap**

修改 `DevHarnessBootstrap.cs`：

```csharp
using BDP.Core.Trion.External;
```

在静态构造中追加：

```csharp
TrionGizmoExtensionRegistry.RegisterPanel(new TrionTriggerLoadoutPanelProvider());
```

保留：

```csharp
TriggerExternalGizmoRegistry.Register(new DevHarnessTriggerGizmoProvider());
```

原因：后者仍提供 DevMode 诊断按钮；正式芯片面板走 Trion 面板扩展。

**Step 4: Create provider skeleton**

新增文件：

```csharp
using System.Collections.Generic;
using BDP.Core.Trion.External;
using BDP.Core.Trigger;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.DevHarness
{
    /// <summary>
    /// DevHarness 插入 Trion Gizmo 右侧的触发体芯片槽面板。
    /// 只通过 Trigger 正式 surface 读写，不直接访问 Trigger 内部槽位状态。
    /// </summary>
    public sealed class TrionTriggerLoadoutPanelProvider : ITrionGizmoPanelExtensionProvider
    {
        private const float PanelPadding = 4f;
        private const float RowLabelWidth = 18f;
        private const float SlotSize = 30f;
        private const float SlotGap = 5f;
        private const float RowHeight = 32f;
        private const float RowGap = 4f;

        private static readonly Color EmptySlotColor = new Color(0.16f, 0.17f, 0.18f, 0.90f);
        private static readonly Color LoadedBorderColor = new Color(0.68f, 0.58f, 0.26f, 0.95f);
        private static readonly Color ActiveBorderColor = new Color(0.34f, 0.86f, 0.58f, 1f);
        private static readonly Color DisabledBorderColor = new Color(0.72f, 0.24f, 0.22f, 1f);
        private static readonly Color MirrorBorderColor = new Color(0.42f, 0.62f, 0.92f, 1f);
        private static readonly Color WarmupBarColor = new Color(0.28f, 0.70f, 0.94f, 1f);
        private static readonly Color WinddownBarColor = new Color(0.92f, 0.50f, 0.22f, 1f);

        public float GetWidth(TrionGizmoExtensionContext context)
        {
            Pawn pawn = ResolvePawn(context);
            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            if (reader == null)
            {
                return 0f;
            }

            int columns = Mathf.Max(CountSlots(reader, TriggerSide.Main), CountSlots(reader, TriggerSide.Sub));
            if (columns <= 0)
            {
                return 0f;
            }

            return PanelPadding * 2f
                 + RowLabelWidth
                 + columns * SlotSize
                 + Mathf.Max(0, columns - 1) * SlotGap;
        }

        public GizmoResult DrawPanel(TrionGizmoExtensionContext context, Rect panelRect, GizmoRenderParms parms)
        {
            Pawn pawn = ResolvePawn(context);
            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            ITriggerInteractionReader interactionReader = TriggerSurfaceAccess.ResolveInteractionReader(pawn);
            ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
            if (reader == null)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            Widgets.DrawBox(panelRect, 1);

            Rect innerRect = panelRect.ContractedBy(PanelPadding);
            Rect mainRect = new Rect(innerRect.x, innerRect.y, innerRect.width, RowHeight);
            Rect subRect = new Rect(innerRect.x, mainRect.yMax + RowGap, innerRect.width, RowHeight);

            bool interacted = false;
            interacted |= DrawSideRow(mainRect, "主", TriggerSide.Main, reader, interactionReader, commands);
            interacted |= DrawSideRow(subRect, "副", TriggerSide.Sub, reader, interactionReader, commands);

            return interacted ? new GizmoResult(GizmoState.Interacted) : new GizmoResult(GizmoState.Clear);
        }
    }
}
```

**Step 5: Add helper methods**

在同一类中补齐：

- `ResolvePawn`
- `CountSlots`
- `DrawSideRow`
- `DrawSlotCell`
- `SubmitSlotInteraction`
- `DrawSwitchProgress`
- `DrawWarmupProgressBar`
- `DrawWinddownProgressBar`
- `CalculateSwitchProgress`
- `BuildSlotTooltip`
- `BuildSideLabel`
- `DescribeInteractionOperation`
- `DescribeInteractionReason`

核心约束：

```csharp
private static Pawn ResolvePawn(TrionGizmoExtensionContext context)
{
    return context != null ? context.Owner as Pawn : null;
}
```

```csharp
private static int CountSlots(ITriggerLoadoutReader reader, TriggerSide side)
{
    int count = 0;
    foreach (ITriggerSlotState ignored in reader.GetSlots(side))
    {
        count++;
    }

    return count;
}
```

槽位点击：

```csharp
private static bool SubmitSlotInteraction(
    ITriggerSlotState slot,
    ITriggerSlotInteractionState interaction,
    ITriggerLoadoutCommands commands)
{
    if (slot == null || slot.LoadedChip == null || interaction == null || commands == null)
    {
        return false;
    }

    if (interaction.Availability != TriggerInteractionAvailability.Available)
    {
        Messages.Message("当前槽位不可操作：" + DescribeInteractionReason(interaction.Reason), MessageTypeDefOf.NeutralEvent, false);
        return false;
    }

    if (interaction.OperationKind == TriggerInteractionOperationKind.Activate
        || interaction.OperationKind == TriggerInteractionOperationKind.SwitchTo)
    {
        return commands.RequestActivate(interaction.ControlSide, interaction.ControlSlotIndex);
    }

    if (interaction.OperationKind == TriggerInteractionOperationKind.Deactivate)
    {
        return commands.RequestDeactivate(interaction.ControlSide);
    }

    if (interaction.OperationKind == TriggerInteractionOperationKind.Mirror)
    {
        Messages.Message("这是镜像槽位，请操作主槽位。", MessageTypeDefOf.NeutralEvent, false);
        return false;
    }

    return false;
}
```

后摇右向左：

```csharp
private static void DrawWinddownProgressBar(Rect rect, float progress)
{
    float remaining = Mathf.Clamp01(1f - progress);
    float width = rect.width * remaining;
    if (width <= 0f)
    {
        return;
    }

    Widgets.DrawBoxSolid(new Rect(rect.xMax - width, rect.y, width, rect.height), WinddownBarColor);
}
```

前摇左向右：

```csharp
private static void DrawWarmupProgressBar(Rect rect, float progress)
{
    Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height), WarmupBarColor);
}
```

**Step 6: Run panel smoke test**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1"
```

Expected: PASS.

**Step 7: Build DevHarness**

```powershell
dotnet build "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj" -c Release
```

Expected: build succeeds.

**Step 8: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/TrionTriggerLoadoutPanelProvider.cs" `
          "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/DevHarnessBootstrap.cs" `
          "模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1"
git commit -m "feat: 在 Trion Gizmo 插入触发体芯片面板"
```

## Task 4: 隐藏旧独立芯片状态 Gizmo，保留 DevMode 诊断入口

**Files:**

- Modify: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/DevHarnessTriggerGizmoProvider.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/LegacyStyleTriggerGuiSmokeTests.ps1`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1`

**Step 1: Write the failing replacement test**

创建 `TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1`：

```powershell
$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$triggerProviderPath = Join-Path $devHarnessRoot 'DevHarnessTriggerGizmoProvider.cs'
$panelProviderPath = Join-Path $devHarnessRoot 'TrionTriggerLoadoutPanelProvider.cs'

$triggerProviderText = Get-Content -LiteralPath $triggerProviderPath -Raw -Encoding utf8
$panelProviderText = Get-Content -LiteralPath $panelProviderPath -Raw -Encoding utf8

Assert-True ($panelProviderText -match 'TrionTriggerLoadoutPanelProvider') 'Trion panel provider must exist before hiding legacy standalone gizmo.'
Assert-True ($triggerProviderText -notmatch 'yield return new Gizmo_LegacyTriggerStatus') 'DevHarness must stop yielding the old standalone chip status gizmo.'
Assert-True ($triggerProviderText -match 'Window_TriggerLoadoutDiagnostics') 'DevHarness diagnostics window must remain reachable in DevMode.'
Assert-True ($triggerProviderText -match 'Prefs\.DevMode') 'DevHarness diagnostic gizmo provider should remain DevMode-gated.'

Write-Output 'TrionTriggerPanelReplacesLegacyGizmoSmokeTests PASS'
```

**Step 2: Run test to verify it fails**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1"
```

Expected: FAIL，因为旧 provider 仍 yield `Gizmo_LegacyTriggerStatus`。

**Step 3: Update DevHarnessTriggerGizmoProvider**

删除或注释掉：

```csharp
yield return new Gizmo_LegacyTriggerStatus(...);
```

保留 DevMode 诊断入口：

- `测 Trigger诊断`
- `测 画点诊断`
- `测 看战体`
- `测 姿态诊断`
- `测 开战体`
- `测 关战体`

注意：

- 这些仍只在 `Prefs.DevMode` 下显示。
- 正式玩家 UI 不再从这里显示芯片状态卡。
- `Gizmo_LegacyTriggerStatus.cs` 和 `Window_LegacyTriggerSlots.cs` 可以暂时保留，避免同轮删除过多调试工具；后续单独清理。

**Step 4: Update old smoke test**

`LegacyStyleTriggerGuiSmokeTests.ps1` 当前会断言旧状态 Gizmo 是主入口。改成新语义：

- 仍允许文件存在。
- 不再要求 `DevHarnessTriggerGizmoProvider` yield `Gizmo_LegacyTriggerStatus`。
- 诊断窗口仍可达。
- 正式芯片面板现在由 `TrionTriggerLoadoutPanelProvider` 承担。

修改重点：

```powershell
Assert-True (
    $providerText -notmatch 'yield return new Gizmo_LegacyTriggerStatus'
) 'Legacy standalone Trigger status gizmo must no longer be yielded after Trion panel migration.'

Assert-True (
    Test-Path -LiteralPath (Join-Path $devHarnessRoot 'TrionTriggerLoadoutPanelProvider.cs')
) 'Trion panel provider must replace the old standalone chip status gizmo as the formal UI.'
```

删除旧断言：

- `DevHarnessTriggerGizmoProvider must expose Gizmo_LegacyTriggerStatus as the primary Trigger GUI entry.`
- `DevHarnessTriggerGizmoProvider must stop using Gizmo_TriggerLoadoutSummary as the primary Trigger GUI entry.` 可保留但语义改为诊断不主入口。

**Step 5: Run tests**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\LegacyStyleTriggerGuiSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1"
```

Expected: all PASS.

**Step 6: Build DevHarness**

```powershell
dotnet build "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj" -c Release
```

Expected: build succeeds.

**Step 7: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/DevHarnessTriggerGizmoProvider.cs" `
          "模组工程/BorderDefenseProtocol/Source/BDP.Tests/LegacyStyleTriggerGuiSmokeTests.ps1" `
          "模组工程/BorderDefenseProtocol/Source/BDP.Tests/TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1"
git commit -m "refactor: 用 Trion 面板替代独立芯片状态 Gizmo"
```

## Task 5: 收口验证和游戏内实测清单

**Files:**

- Modify: `日志/Agent工作日志/Agent日志08.md` 或最新未满 20 条的日志文件

**Step 1: Run focused smoke tests**

```powershell
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelExtensionContractSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoPanelLayoutSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionGizmoDrainDetailsSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\DevHarnessTrionTriggerLoadoutPanelSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\TrionTriggerPanelReplacesLegacyGizmoSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File "模组工程\BorderDefenseProtocol\Source\BDP.Tests\LegacyStyleTriggerGuiSmokeTests.ps1"
```

Expected: all PASS.

**Step 2: Run boundary grep checks**

```powershell
rg -n "BDP\.DevHarness|TrionTriggerLoadoutPanelProvider" "模组工程/BorderDefenseProtocol/Source/BDP/Core/Trion"
```

Expected: no output.

```powershell
rg -n "GetSlots\s*\(\s*TriggerSide\.Special\s*\)" "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/TrionTriggerLoadoutPanelProvider.cs"
```

Expected: no output.

**Step 3: Build both mods**

```powershell
dotnet build "模组工程/BorderDefenseProtocol/Source/BDP/BDP.csproj" -c Release
dotnet build "模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj" -c Release
```

Expected: both builds succeed.

**Step 4: Game manual verification**

用 RimWorld 实测以下场景：

1. 有 Trion、无触发体：只显示 Trion 能量条，无右侧芯片面板。
2. 有 Trion、有触发体：Trion 能量条右侧出现主/副槽面板。
3. 主/副多槽位：所有槽位都显示，并向右延伸。
4. 空槽：暗灰空框。
5. 已装未激活芯片：显示芯片图标，边框为待命色。
6. 点击未激活芯片：走正式激活或切换。
7. 点击已激活芯片：走正式关闭。
8. 切入前摇：槽位底部进度条从左向右。
9. 关闭或切走后摇：槽位底部进度条从右向左退。
10. 禁用槽：颜色变暗/偏红，点击不提交非法命令。
11. 镜像槽：颜色偏蓝，tooltip 指向控制槽，不直接提交独立命令。
12. 特殊侧紧急脱离芯片：仍显示在顶部徽标区，不出现在主/副槽位行。
13. DevMode 开启：旧独立芯片状态 Gizmo 不再重复出现，诊断按钮仍可用。

**Step 5: Add work log**

在最新未满 20 条的日志文件顶部追加：

```markdown
YYYY-MM-DD HH:mm:ss

- 完成 Trion Gizmo 芯片面板合并重构：主模组新增右侧面板扩展口，DevHarness 插入正式芯片槽面板。
- 芯片面板只读取 Trigger 正式 surface，主/副槽位全部显示；特殊侧紧急脱离继续走 Trion 顶部徽标区。
- 已执行相关 smoke tests、主模组/DevHarness Release 构建，并完成游戏内基础实测。
```

如果游戏内实测未完成，把最后一句改成：

```markdown
- 已执行相关 smoke tests 和主模组/DevHarness Release 构建；游戏内实测待补。
```

**Step 6: Commit log**

```powershell
git add -- "日志/Agent工作日志/Agent日志08.md"
git commit -m "chore: 记录 Trion 芯片面板合并验证"
```

## Final Verification

最终交付前执行：

```powershell
git log -5 --oneline
git status --short
```

确认：

- 本任务相关提交存在。
- 工作区如仍有无关改动，不把它们纳入本任务说明为自己改动。
- 若游戏内实测未做，最终回复必须明确说明“未做游戏内实测”。

## Rollback Notes

若需要回退：

1. 回退 DevHarness 面板提交，可恢复旧独立芯片状态 Gizmo。
2. 回退 Trion 布局提交，会让面板扩展不再显示，但主模组扩展契约仍可保留。
3. 回退扩展契约提交，会彻底移除新扩展口。

推荐回退顺序从后往前按提交撤销，避免 DevHarness 引用不存在的主模组接口。
