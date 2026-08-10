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
$runtimePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'

Assert-True (Test-Path -LiteralPath $hostPath) 'CompCombatBodyHost.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundRuntime.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'

$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$runtimeText = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8

Assert-True ($runtimeText -match 'IExposable') 'Wound runtime must be save-load aware.'
Assert-True ($hostText -match 'Scribe_Deep\.Look\(ref woundRuntime') 'CompCombatBodyHost must scribe woundRuntime.'
Assert-True ($runtimeText -match 'RestoreAfterLoad\(Pawn pawn\)[\s\S]*trionBinding\.RestoreAfterLoad') 'RestoreAfterLoad must restore saved wound drains.'
Assert-True ($runtimeText -notmatch 'RestoreAfterLoad\(Pawn pawn\)[\s\S]*RebuildActiveWounds\(pawn\)') 'RestoreAfterLoad must not clear saved wound drains through rebuild.'
Assert-True ($bindingText -match 'ExposeData') 'Wound Trion binding must save active drain records.'
Assert-True ($bindingText -match 'RestoreAfterLoad') 'Wound Trion binding must republish active wound drains after load.'
Assert-True ($bindingText -match 'GetActiveHediffLoadIds') 'Wound Trion binding must expose active drain ids for spray rebuild.'
Assert-True ($bindingText -match 'RegisterDrain') 'Restore must republish saved wound drains to Trion.'
Assert-True ($bindingText -match 'FindWoundByLoadId') 'Wound Trion binding must match saved drain records back to live Hediff load IDs.'

Write-Output 'CombatBodyWoundTrionPersistenceSmokeTests PASS'
