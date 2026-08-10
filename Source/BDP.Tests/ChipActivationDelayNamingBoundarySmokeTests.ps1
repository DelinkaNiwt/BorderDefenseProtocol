$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'

$configText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Chips\Config\ChipLoadoutConfig.cs') -Raw -Encoding utf8
$contractText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Chips\Contract\ChipLoadoutContract.cs') -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs') -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs') -Raw -Encoding utf8
$payloadText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs') -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs') -Raw -Encoding utf8
$detailPanelText = Get-Content -LiteralPath (
    Join-Path $contentRoot 'Assembly\Window\Panel_ChipDetail.cs') -Raw -Encoding utf8
$contentPanelText = Get-Content -LiteralPath (
    Join-Path $contentRoot 'Trigger\UI\TriggerLoadoutPanelProvider.cs') -Raw -Encoding utf8
$triggerText = (
    Get-ChildItem -LiteralPath (Join-Path $coreRoot 'Trigger') -Filter '*.cs' -File -Recurse |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
) -join "`n"
$attackWarmupText = Get-Content -LiteralPath (
    Join-Path $coreRoot 'AttackExecution\RangedProtocol\Model\PrepareRecord.cs') -Raw -Encoding utf8
$chipXmlText = (
    Get-ChildItem -LiteralPath (
        Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test') -Filter '*.xml' -File -Recurse |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
) -join "`n"

Assert-True (
    ($configText -match 'public int ActivationDelayTicks = -1;') -and
    ($configText -match 'public int DeactivationDelayTicks = -1;')
) 'ChipLoadoutConfig must expose the two formal chip activation delay fields.'

Assert-True (
    ($contractText -match 'public int ActivationDelayTicks;') -and
    ($contractText -match 'public int DeactivationDelayTicks;')
) 'ChipLoadoutContract must carry the two formal chip activation delay fields.'

Assert-True (
    ($resolverText -match 'ActivationDelayTicks = config\.ActivationDelayTicks') -and
    ($resolverText -match 'DeactivationDelayTicks = config\.DeactivationDelayTicks')
) 'The chip contract resolver must translate the formal delay fields without aliases.'

Assert-True (
    ($validatorText -match '"ActivationDelayTicksInvalid"') -and
    ($validatorText -match '"DeactivationDelayTicksInvalid"') -and
    ($validatorText -match 'loadout\.ActivationDelayTicks < -1') -and
    ($validatorText -match 'loadout\.DeactivationDelayTicks < -1')
) 'Definition validation must reject activation delay values below the -1 missing sentinel.'

Assert-True (
    ($payloadText -notmatch '\bWarmupTicks\b|\bWinddownTicks\b|ActivationDelayTicks|DeactivationDelayTicks') -and
    ($collectorText -notmatch 'WarmupTicks\s*=\s*loadout|WinddownTicks\s*=\s*loadout|ActivationDelayTicks\s*=\s*loadout|DeactivationDelayTicks\s*=\s*loadout')
) 'Expression chip snapshots must not copy Trigger activation delays.'

Assert-True (
    $triggerText -notmatch 'DefaultChipWarmupTicks|DefaultChipWinddownTicks|ResolveChipWarmupTicks|ResolveChipWinddownTicks|resolveChipWarmupTicks|resolveChipWinddownTicks|warmupDuration|winddownDuration|windingDownSlotIndex|WarmingUp|WindingDown'
) 'The Trigger subsystem must not retain the old warmup or winddown naming.'

Assert-True (
    ($triggerText -match 'DefaultChipActivationDelayTicks = 60') -and
    ($triggerText -match 'DefaultChipDeactivationDelayTicks = 30') -and
    ($triggerText -match 'ResolveChipActivationDelayTicks') -and
    ($triggerText -match 'ResolveChipDeactivationDelayTicks') -and
    ($triggerText -match 'SwitchPhase\.Activating') -and
    ($triggerText -match 'SwitchPhase\.Deactivating') -and
    ($triggerText -match 'activationDelayDuration') -and
    ($triggerText -match 'deactivationDelayDuration') -and
    ($triggerText -match 'deactivatingSlotIndex')
) 'The Trigger subsystem must use activation and deactivation delay terminology end to end.'

Assert-True (
    ($contentPanelText -notmatch 'SwitchPhase\.WarmingUp|SwitchPhase\.WindingDown|WindingDownSlotIndex|WarmupDuration|WinddownDuration') -and
    ($contentPanelText -match 'SwitchPhase\.Activating') -and
    ($contentPanelText -match 'SwitchPhase\.Deactivating') -and
    ($contentPanelText -match 'DeactivatingSlotIndex') -and
    ($contentPanelText -match 'ActivationDelayDuration') -and
    ($contentPanelText -match 'DeactivationDelayDuration')
) 'The content panel must read the formal activation delay state names.'

Assert-True (
    ($detailPanelText -match '"启用延迟/停用延迟"') -and
    ($detailPanelText -match 'snapshot\.ActivationDelayTicks') -and
    ($detailPanelText -match 'snapshot\.DeactivationDelayTicks')
) 'The chip detail panel must present the formal activation delay names.'

Assert-True (
    ($chipXmlText -notmatch '<WarmupTicks>|<WinddownTicks>') -and
    ([regex]::Matches($chipXmlText, '<ActivationDelayTicks>').Count -eq 10) -and
    ([regex]::Matches($chipXmlText, '<DeactivationDelayTicks>').Count -eq 10)
) 'DevHarness must migrate all ten chip delay pairs without retaining old tags.'

Assert-True (
    $attackWarmupText -match 'public int WarmupTicks'
) 'The ranged attack protocol must retain its real attack warmup field.'

Write-Output 'ChipActivationDelayNamingBoundarySmokeTests PASS'
