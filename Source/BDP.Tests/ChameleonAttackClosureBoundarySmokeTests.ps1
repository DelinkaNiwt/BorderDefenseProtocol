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
Assert-True (-not (Test-Path -LiteralPath $shutdownPath)) `
    'Chameleon must not keep the obsolete chip shutdown service.'

$adapterText = Get-Content -LiteralPath $adapterPath -Raw -Encoding utf8

Assert-True ($adapterText -match 'HediffComp_Invisibility') `
    'Chameleon invisibility must reuse the original invisibility component.'
Assert-True ($adapterText -match 'attackTargetsCache|UpdateTarget') `
    'Chameleon invisibility must refresh attack target cache without a DLC guard.'
Assert-True ($adapterText -match 'AttackActionSuccess') `
    'Chameleon invisibility must subscribe to the neutral attack success event.'
Assert-True ($adapterText -match 'RemoveHediff\s*\(\s*parent\s*\)') `
    'Chameleon attack break must remove only the current Hediff.'
Assert-True ($adapterText -notmatch 'RequestDeactivate|ChameleonAttackShutdownService|TryDeactivateImmediately') `
    'Chameleon attack break must not deactivate the chip.'
Assert-True ($adapterText -notmatch 'ResolvePublishedProjection|GetActiveSlot|LoadedChip') `
    'Chameleon attack break must not scan expression projection or chip slots.'

Write-Output 'ChameleonAttackClosureBoundarySmokeTests PASS'
