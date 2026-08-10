$ErrorActionPreference = "Stop"

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

$servicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$statePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\State\CombatBodyState.cs'
$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'

$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$stateText = Get-Content -LiteralPath $statePath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8

Assert-True (
    $serviceText -match 'private bool isExitInProgress;'
) 'CombatBodySessionService must own a per-session exit reentry guard.'

Assert-True (
    $serviceText -match 'public bool CanManualDeactivate\(\)\s*\{\s*return !isExitInProgress && rawCombatBodyService\.CanManualDeactivate\(\);\s*\}'
) 'CanManualDeactivate() must report false while an exit transaction is already running.'

Assert-True (
    $serviceText -match 'private void ExecuteExit\(CombatBodySessionExitMode exitMode\)\s*\{\s*if \(isExitInProgress\)\s*\{\s*return;\s*\}\s*isExitInProgress = true;\s*try\s*\{\s*exitTransaction\.Execute\(OwnerPawn, exitMode\);\s*\}\s*finally\s*\{\s*isExitInProgress = false;\s*\}\s*\}'
) 'ExecuteExit() must ignore nested exit requests and reset the guard in finally.'

Assert-True (
    $triggerBodyText -match 'public override void Notify_Unequipped\(Pawn pawn\)[\s\S]*CombatBodySurfaceAccess\.ResolveCommands\(pawn\)\?\.RequestRelease\(\);'
) 'Trigger detach should keep requesting release through the formal command surface; the session guard owns reentry suppression.'

Assert-True (
    $stateText -match 'throw new InvalidOperationException\("Only Active or Collapsing combat body can enter Cooldown\."\);'
) 'CombatBodyState.EnterCooldown() must keep its phase invariant instead of swallowing duplicate exits.'

Write-Output 'CombatBodyExitReentryGuard PASS'
