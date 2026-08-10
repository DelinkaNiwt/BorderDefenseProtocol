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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$addonContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedStageAddonContext.cs'
$attackExecutionRoot = Join-Path $bdpSourceRoot 'AttackExecution'
$projectileRoot = Join-Path $bdpSourceRoot 'Projectiles'

$sessionText = Read-Source $sessionPath
$addonContextText = Read-Source $addonContextPath
$attackExecutionFiles = Get-ChildItem -LiteralPath $attackExecutionRoot -Recurse -Filter '*.cs'
$projectileFiles = Get-ChildItem -LiteralPath $projectileRoot -Recurse -Filter '*.cs'
$combinedText = ($attackExecutionFiles + $projectileFiles | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
    }) -join "`n"

Assert-True (
    ($sessionText -match 'AttackContext\s+AttackContext') -and
    ($sessionText -notmatch 'SharedState')
) 'RangedAttackModuleSession must keep AttackContext and must not keep SharedState.'

Assert-True (
    ($addonContextText -match 'AttackContextSnapshot') -and
    ($addonContextText -notmatch 'SharedState')
) 'RangedStageAddonContext must expose AttackContextSnapshot and must not expose SharedState.'

Assert-True (
    $combinedText -notmatch '\.SharedState\b'
) 'AttackExecution and projectile pipeline must stop reading or writing SharedState.'

Write-Output 'AttackContextNoSharedStateSmokeTests PASS'
