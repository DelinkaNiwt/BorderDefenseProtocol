$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs'
$contextPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs'
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$contextText = Get-Content -LiteralPath $contextPath -Raw -Encoding utf8

Assert-True (
    ($serviceText -match 'EvaluateActivationRequirements') -and
    ($contextText -match 'EvaluateActivationRequirements')
) 'The formal Trigger command context must carry one Core activation-requirement evaluator.'

$activeIndex = $serviceText.IndexOf('nextSlot.IsActive')
$samePendingIndex = $serviceText.IndexOf('IsSamePendingTarget')
$requirementIndex = $serviceText.IndexOf('EvaluateActivationRequirements')
$cancelIndex = $serviceText.IndexOf('CancelConflictingPendingTargets')
$blockerIndex = $serviceText.IndexOf('FindActivationBlockers')

Assert-True (
    $activeIndex -ge 0 -and
    $samePendingIndex -gt $activeIndex -and
    $requirementIndex -gt $samePendingIndex -and
    $cancelIndex -gt $requirementIndex -and
    $blockerIndex -gt $requirementIndex
) 'Active and same-transaction targets must return before requirements; new requests must check before cancellation and blocker shutdown.'

Assert-True (
    ($serviceText -match 'requirementResult\.Satisfied') -and
    ($serviceText -match 'MessageTypeDefOf\.RejectInput') -and
    ($serviceText -match 'return\s+false')
) 'An unmet requirement must reject through the Core command and show one player-facing rejection.'

Write-Output 'TriggerActivationRequirementGateSmokeTests PASS'
