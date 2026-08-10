$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$panelPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'
$reasonPath = Join-Path $coreRoot 'Trigger\Interaction\TriggerInteractionReason.cs'
$interpreterPath = Join-Path $coreRoot 'Trigger\Interaction\TriggerInteractionInterpreter.cs'

$panelText = Get-Content -LiteralPath $panelPath -Raw -Encoding utf8
$reasonText = Get-Content -LiteralPath $reasonPath -Raw -Encoding utf8
$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding utf8

Assert-True (
    ($panelText -match 'WaitingBorderColor') -and
    ($panelText -match 'new\s+Color\(1f,\s*0\.76f,\s*0\.18f,\s*0\.96f\)')
) 'The panel must define the initial bright amber waiting border.'
Assert-True (
    ($panelText -match 'IsWaitingTarget') -and
    ($panelText -match 'GUI\.color\s*=\s*Color\.white')
) 'A waiting target icon must stay at normal brightness.'
Assert-True ($panelText -match '等待冲突芯片关闭') `
    'The waiting target tooltip must use the confirmed player wording.'
Assert-True (
    $panelText -match
        'switchState\.Phase\s*==\s*SwitchPhase\.WaitingForConflicts'
) 'The panel must identify waiting without drawing a fake fixed progress bar.'
Assert-True (
    ($reasonText -match '\bWaitingForConflicts\b') -and
    ($reasonText -notmatch '\bExclusionConflict\b')
) 'Interaction reasons must replace blocked exclusion with waiting information.'
Assert-True (
    ($interpreterText -notmatch 'hasExclusionConflict') -and
    ($interpreterText -match 'TriggerInteractionAvailability\.InformationalOnly') -and
    ($interpreterText -match 'TriggerInteractionAvailability\.Available')
) 'The interpreter must ignore repeated target clicks but allow selecting another target.'

Write-Output 'TriggerActivationExclusionWaitingPanelSmokeTests PASS'
