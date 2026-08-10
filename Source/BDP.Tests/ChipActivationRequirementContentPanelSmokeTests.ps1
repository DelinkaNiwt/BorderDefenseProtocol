$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$interfacePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Access\Contracts\ITriggerSlotInteractionState.cs'
$snapshotPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerSlotInteractionSnapshot.cs'
$reasonPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerInteractionReason.cs'
$detailPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\Panel_ChipDetail.cs'
$contentPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'

$interfaceText = Get-Content -LiteralPath $interfacePath -Raw -Encoding utf8
$snapshotText = Get-Content -LiteralPath $snapshotPath -Raw -Encoding utf8
$reasonText = Get-Content -LiteralPath $reasonPath -Raw -Encoding utf8
$detailText = Get-Content -LiteralPath $detailPath -Raw -Encoding utf8
$contentText = Get-Content -LiteralPath $contentPath -Raw -Encoding utf8

Assert-True (
    ($interfaceText -match 'IReadOnlyList<PawnRequirementSnapshot>\s+ActivationRequirements') -and
    ($snapshotText -match 'ActivationRequirements')
) 'Content must receive activation requirements through the public read-only interaction snapshot.'

Assert-True (
    ($reasonText -match 'ActivationRequirementsUnmet') -and
    ($contentText -match 'ActivationRequirementsUnmet')
) 'The player control must remain visible but blocked with one formal unmet-requirement reason.'

Assert-True (
    ([regex]::Matches($detailText, '"激活条件"').Count -eq 1) -and
    ($detailText -notmatch '"功率要求"')
) 'The Core static detail must replace the old power line with one tidy activation-requirement section.'

Assert-True (
    ([regex]::Matches($contentText, '"激活条件"').Count -eq 1) -and
    ($contentText -match '<color=') -and
    ($contentText -notmatch 'TrionIntensityRequirement|SkillLevelRequirement')
) 'The Content tooltip must render one colored section from snapshots without reimplementing concrete rules.'

Write-Output 'ChipActivationRequirementContentPanelSmokeTests PASS'
