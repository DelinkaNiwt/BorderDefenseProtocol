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

$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$activationPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$exitPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$patchRoot = Join-Path $repoRoot 'Source\BDP\Patches'
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'

$addDirectPath = Join-Path $patchRoot 'Patch_HediffSet_AddDirect_CombatBodyWounds.cs'
$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$activationText = Get-Content -LiteralPath $activationPath -Raw -Encoding utf8
$exitText = Get-Content -LiteralPath $exitPath -Raw -Encoding utf8
$addDirectText = Get-Content -LiteralPath $addDirectPath -Raw -Encoding utf8
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True ($hostText -match 'CombatBodyWoundRuntime') 'CompCombatBodyHost must own wound runtime.'
Assert-True ($hostText -match 'RestoreAfterLoad') 'Post-load must restore wound runtime.'
Assert-True ($hostText -match '\.Tick\s*\(') 'CompTick must allow wound runtime calibration.'
Assert-True ($activationText -match 'RebuildActiveWounds') 'Activation transaction must rebuild wound runtime.'
Assert-True ($exitText -match 'ClearActiveRuntime') 'Exit transaction must clear wound runtime.'
Assert-True ($addDirectText -match 'IsCombatBodyWoundRuntimeApplicable') 'New wound events must use Active and Collapsing wound runtime applicability.'

$expectedPatches = @(
  'Patch_HediffSet_AddDirect_CombatBodyWounds.cs',
  'Patch_Pawn_HealthTracker_RemoveHediff_CombatBodyWounds.cs',
  'Patch_Pawn_HealthTracker_NotifyHediffChanged_CombatBodyWounds.cs',
  'Patch_Hediff_Injury_TryMergeWith_CombatBodyWounds.cs'
)

foreach ($file in $expectedPatches) {
    Assert-True (Test-Path -LiteralPath (Join-Path $patchRoot $file)) "$file must exist."
}

Assert-True ($trionText -notmatch 'CombatBodyWound') 'CompTrion and Trion core must not reference wound business.'

Write-Output 'CombatBodyWoundLifecycleSmokeTests PASS'
