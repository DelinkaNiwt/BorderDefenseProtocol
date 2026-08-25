$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$chameleonRoot = Join-Path $repoRoot 'Source\BDP.Content\Chameleon'
$adapterPath = Join-Path $chameleonRoot 'HediffComp_BdpInvisibility.cs'
$shutdownPath = Join-Path $chameleonRoot 'ChameleonAttackShutdownService.cs'

Assert-True (Test-Path -LiteralPath $adapterPath) `
    'Chameleon must have a Content-side invisibility adapter.'
Assert-True (Test-Path -LiteralPath $shutdownPath) `
    'Chameleon must have a Content-side attack shutdown service.'

$adapterText = Get-Content -LiteralPath $adapterPath -Raw -Encoding utf8
$shutdownText = Get-Content -LiteralPath $shutdownPath -Raw -Encoding utf8

Assert-True ($adapterText -match 'HediffComp_Invisibility') `
    'Chameleon invisibility must reuse the original invisibility component.'
Assert-True ($adapterText -match 'attackTargetsCache|UpdateTarget') `
    'Chameleon invisibility must refresh attack target cache without a DLC guard.'
Assert-True ($shutdownText -match 'AttackActionSuccess') `
    'Chameleon shutdown must subscribe to the neutral attack success event.'
Assert-True ($shutdownText -match 'Deactivate.*Immediate|Immediate.*Deactivate') `
    'Chameleon shutdown must use an immediate deactivation path.'
Assert-True ($shutdownText -notmatch 'CapacityCost|Allocate|Release') `
    'Chameleon shutdown must not own battle-body capacity allocation or release.'

Write-Output 'ChameleonAttackClosureBoundarySmokeTests PASS'
