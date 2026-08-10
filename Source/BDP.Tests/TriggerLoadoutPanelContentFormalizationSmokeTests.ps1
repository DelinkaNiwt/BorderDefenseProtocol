# BDP 事项 05 触发体芯片控制面板正式化边界冒烟测试。
# 本测试锁定 Content 许可扩展、正式面板、候选去重和玩家文字边界。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Read-Source
{
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path))
    {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$candidateModRoot = Join-Path $modsRoot "BorderDefenseProtocol.DevHarness"
$contentUiRoot = Join-Path $sourceRoot "BDP.Content\Trigger\UI"

$extensionPath = Join-Path $contentUiRoot "TriggerLoadoutPanelExtension.cs"
$providerPath = Join-Path $contentUiRoot "TriggerLoadoutPanelProvider.cs"
$contentBootstrapPath = Join-Path $sourceRoot "BDP.Content\ContentBootstrap.cs"
$candidateProviderPath = Join-Path $candidateModRoot "Source\BDP.DevHarness\TrionTriggerLoadoutPanelProvider.cs"
$candidateBootstrapPath = Join-Path $candidateModRoot "Source\BDP.DevHarness\DevHarnessBootstrap.cs"
$mainTriggerDefPath = Join-Path $mainModRoot "1.6\Content\Defs\Things\Equipment\Trigger\ThingDefs_TriggerBodies.xml"
$candidateTriggerDefPath = Join-Path $candidateModRoot "1.6\Defs\Things\Equipment\Trigger\Test\ThingDefs_TestTriggerBody.xml"

Assert-True (Test-Path -LiteralPath $extensionPath) "Content 缺少正式触发体芯片面板许可扩展。"
Assert-True (Test-Path -LiteralPath $providerPath) "Content 缺少正式触发体芯片控制面板。"
Assert-True (-not (Test-Path -LiteralPath $candidateProviderPath)) "候选模组仍保留重复芯片面板源码。"
Assert-True (Test-Path -LiteralPath $mainTriggerDefPath) "主模组缺少正式边境标准触发体定义。"
Assert-True (-not (Test-Path -LiteralPath $candidateTriggerDefPath)) "候选模组仍保留已转正的测试触发体定义。"

$extensionText = Read-Source $extensionPath
$providerText = Read-Source $providerPath
$contentBootstrapText = Read-Source $contentBootstrapPath
$candidateBootstrapText = Read-Source $candidateBootstrapPath
$mainTriggerDefText = Read-Source $mainTriggerDefPath
$candidateTriggerDefText = Read-Source $candidateTriggerDefPath

Assert-True (
    ($extensionText -match "namespace\s+BDP\.Content\.Trigger\.UI") -and
    ($extensionText -match "sealed\s+class\s+TriggerLoadoutPanelExtension\s*:\s*DefModExtension")
) "许可扩展必须是 BDP.Content.Trigger.UI 下的无业务字段 DefModExtension。"

Assert-True (
    ($providerText -match "namespace\s+BDP\.Content\.Trigger\.UI") -and
    ($providerText -match "sealed\s+class\s+TriggerLoadoutPanelProvider\s*:\s*ITrionGizmoPanelExtensionProvider") -and
    ($providerText -notmatch "BDP\.DevHarness")
) "正式面板必须使用 Content 命名空间并去除 DevHarness 语义。"

Assert-True (
    ($contentBootstrapText -match "TrionGizmoExtensionRegistry\.RegisterPanel\s*\(\s*new\s+TriggerLoadoutPanelProvider\s*\(\s*\)\s*\)") -and
    ($candidateBootstrapText -notmatch "TrionGizmoExtensionRegistry\.RegisterPanel")
) "面板必须且只能由 Content 正式启动入口注册。"

Assert-True (
    ($mainTriggerDefText -match 'Class="BDP\.Content\.Trigger\.UI\.TriggerLoadoutPanelExtension"') -and
    ($mainTriggerDefText -match '<defName>BDP_TriggerBody_BorderStandard</defName>')
) "正式边境标准触发体必须显式挂载玩家芯片面板许可扩展。"

Assert-True (
    ($providerText -match "GetModExtension<TriggerLoadoutPanelExtension>") -and
    ($providerText -match "IsPanelAllowed") -and
    ($providerText -match "TriggerSurfaceAccess\.ResolveLoadoutReader") -and
    ($providerText -match "TriggerSurfaceAccess\.ResolveInteractionReader") -and
    ($providerText -match "TriggerSurfaceAccess\.ResolveLoadoutCommands") -and
    ($providerText -notmatch "TryGetComp<CompTriggerBody>") -and
    ($providerText -notmatch "\.mainSlots|\.subSlots|\.specialSlots")
) "面板必须先检查 Content 许可，再通过 Core 正式接口读写 Trigger 状态。"

Assert-True (
    ($providerText -match "GetPlayerSlotNumber") -and
    ($providerText -match "return\s+internalSlotIndex\s*\+\s*1\s*;") -and
    ($providerText -match "主侧槽位") -and
    ($providerText -match "点击启用") -and
    ($providerText -match "点击切换至此芯片") -and
    ($providerText -match "点击关闭") -and
    ($providerText -match "当前无法操作此芯片") -and
    ($providerText -notmatch "正式交互|正式请求面|控制=|旁观槽位|Main\[|Sub\[")
) "正式面板必须使用玩家中文和从 1 开始的显示编号。"

Assert-True (
    ($providerText -match "GetSlots\s*\(\s*TriggerSide\.Main\s*\)") -and
    ($providerText -match "GetSlots\s*\(\s*TriggerSide\.Sub\s*\)") -and
    ($providerText -notmatch "GetSlots\s*\(\s*TriggerSide\.Special\s*\)") -and
    ($providerText -match "RequestActivate") -and
    ($providerText -match "RequestDeactivate") -and
    ($providerText -notmatch "TryLoadChip|TryUnloadChip|TryDestroyLoadedChip")
) "正式面板只能显示主副槽，并只承担启用、关闭和切换。"

Write-Output "TriggerLoadoutPanelContentFormalizationSmokeTests PASS"
