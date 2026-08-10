$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$monitorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerActivationRequirementMonitor.cs'
$ownerPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.ActivationRequirements.cs'
$coordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'

Assert-True (Test-Path -LiteralPath $monitorPath) `
    '07B must provide one low-frequency active requirement monitor.'
Assert-True (Test-Path -LiteralPath $ownerPath) `
    'CompTriggerBody must own the active-root requirement check boundary.'

$monitorText = Get-Content -LiteralPath $monitorPath -Raw -Encoding utf8
$ownerText = Get-Content -LiteralPath $ownerPath -Raw -Encoding utf8
$coordinatorText = Get-Content -LiteralPath $coordinatorPath -Raw -Encoding utf8

Assert-True (
    ($monitorText -match 'CheckIntervalTicks\s*=\s*60') -and
    ($monitorText -match '%\s*CheckIntervalTicks') -and
    ($monitorText -match 'IsBindingMirror') -and
    ($monitorText -match 'SwitchPhase\.Deactivating')
) 'The monitor must stagger 60-tick checks and skip mirrors and already-closing roots.'

Assert-True (
    ($ownerText -match 'RequestDeactivate') -and
    ($ownerText -notmatch 'DeactivateBoundSlotImmediate') -and
    ($ownerText -notmatch 'RequestActivate')
) 'Continuous failure must use the existing normal deactivation flow without forced removal or automatic reopen.'

$switchIndex = $coordinatorText.IndexOf('ResolveDueSwitchTransitionsForRuntimeTick')
$monitorIndex = $coordinatorText.IndexOf('CheckActiveActivationRequirementsForRuntimeTick')
$publishIndex = $coordinatorText.IndexOf('if (projectionDirty')
Assert-True (
    $switchIndex -ge 0 -and
    $monitorIndex -gt $switchIndex -and
    $publishIndex -gt $monitorIndex
) 'RuntimeTick must check active requirements after switch settlement and before projection publication.'

Write-Output 'TriggerActivationRequirementContinuousSmokeTests PASS'
