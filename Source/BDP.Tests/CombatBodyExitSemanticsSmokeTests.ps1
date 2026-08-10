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

$commandsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Access\Contracts\ICombatBodyCommands.cs'
$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$exitModePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionExitMode.cs'
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'

$commandsText = Get-Content -LiteralPath $commandsPath -Raw -Encoding utf8
$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$exitModeText = Get-Content -LiteralPath $exitModePath -Raw -Encoding utf8
$exitTransactionText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8

Assert-True -Condition ($commandsText -match 'void RequestRelease\(\);') -Message 'ICombatBodyCommands must expose RequestRelease.'
Assert-True -Condition ($commandsText -match 'void FinalizeCollapse\(\);') -Message 'ICombatBodyCommands must expose FinalizeCollapse.'
Assert-True -Condition ($commandsText -notmatch 'RequestDeactivate\s*\(') -Message 'ICombatBodyCommands must drop RequestDeactivate(bool emergency = false).'
Assert-True -Condition ($exitModeText -match '\bRelease\b') -Message 'CombatBodySessionExitMode must include Release.'
Assert-True -Condition ($exitModeText -match '\bCollapse\b') -Message 'CombatBodySessionExitMode must include Collapse.'
Assert-True -Condition ($exitModeText -notmatch '\bManual\b') -Message 'CombatBodySessionExitMode must not include Manual.'
Assert-True -Condition ($exitModeText -notmatch '\bEmergency\b') -Message 'CombatBodySessionExitMode must not include Emergency.'
Assert-True -Condition ($serviceText -match 'public void RequestRelease\(\)\s*\{') -Message 'CombatBodySessionService must expose RequestRelease.'
Assert-True -Condition ($serviceText -match 'public void FinalizeCollapse\(\)\s*\{') -Message 'CombatBodySessionService must expose FinalizeCollapse.'
Assert-True -Condition ($serviceText -notmatch 'RequestDeactivate\s*\(') -Message 'CombatBodySessionService must drop RequestDeactivate(bool emergency = false).'
Assert-True -Condition ($hostText -match 'CompTick\(\)[\s\S]*FinalizeCollapse\(') -Message 'CompCombatBodyHost.CompTick() must call FinalizeCollapse().'
Assert-True -Condition ($hostText -notmatch 'RequestDeactivate\(true\)') -Message 'CompCombatBodyHost must not call RequestDeactivate(true).'
Assert-True -Condition ($exitTransactionText -match 'CombatBodySessionExitMode\.Collapse') -Message 'CombatBodyExitTransaction must handle Collapse.'
Assert-True -Condition ($exitTransactionText -notmatch 'CombatBodySessionExitMode\.Emergency') -Message 'CombatBodyExitTransaction must drop Emergency.'

Write-Output 'CombatBodyExitSemantics PASS'
