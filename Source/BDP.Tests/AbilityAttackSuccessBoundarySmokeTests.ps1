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
$dispatcherPath = Join-Path $coreRoot 'AttackExecution\AttackActionSuccessDispatcher.cs'
$eventPath = Join-Path $coreRoot 'AttackExecution\AttackActionSuccess.cs'
$patchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Ability_Activate_BdpAttackActionSuccess.cs'

Assert-True (Test-Path -LiteralPath $dispatcherPath) `
    'Core must expose an attack action success dispatcher.'
Assert-True (Test-Path -LiteralPath $eventPath) `
    'Core must expose an attack action success payload.'
Assert-True (Test-Path -LiteralPath $patchPath) `
    'Core must observe standard Ability.Activate success.'

$dispatcherText = Get-Content -LiteralPath $dispatcherPath -Raw -Encoding utf8
$eventText = Get-Content -LiteralPath $eventPath -Raw -Encoding utf8
$patchText = Get-Content -LiteralPath $patchPath -Raw -Encoding utf8

Assert-True ($eventText -match 'Ability') `
    'Attack action success payload must carry Ability source data.'
Assert-True ($eventText -match 'Verb') `
    'Attack action success payload must carry Verb source data.'
Assert-True ($dispatcherText -match 'event\s+Action<') `
    'Attack action success must be published through a typed event.'
Assert-True ($patchText -match 'Ability\.Activate|Activate') `
    'Ability success integration must target Ability.Activate.'
Assert-True ($patchText -match 'hostile|violent') `
    'Ability success integration must distinguish offensive abilities.'

Write-Output 'AbilityAttackSuccessBoundarySmokeTests PASS'
