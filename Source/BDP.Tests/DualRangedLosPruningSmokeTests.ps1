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

$specPath = Join-Path $bdpSourceRoot 'Expressions\Model\ResolvedVerbSpec.cs'
$factoryPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\ResolvedVerbSpecFactory.cs'
$stagesPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionService.Stages.cs'

$specText = Read-Source $specPath
$factoryText = Read-Source $factoryPath
$stagesText = Read-Source $stagesPath

Assert-True (
    ($specText -match 'public bool RequireLineOfSight') -and
    ($specText -match 'public bool RequiresDirectTargetLineOfSight') -and
    ($specText -match 'public bool StopBurstWithoutLos')
) 'ResolvedVerbSpec must carry generic LOS and necessary direct-target LOS truth explicitly.'

Assert-True (
    ($factoryText -match 'RequireLineOfSight\s*=\s*verbProps\.requireLineOfSight') -and
    ($factoryText -match 'RequiresDirectTargetLineOfSight\s*=\s*ResolveDirectTargetLineOfSightRequirement') -and
    ($factoryText -match 'StopBurstWithoutLos\s*=\s*verbProps\.stopBurstWithoutLos')
) 'ResolvedVerbSpecFactory must normalize generic LOS and necessary direct-target LOS truth.'

Assert-True (
    $stagesText -match 'FilterDualRangedSidesByLegality'
) 'AttackExecutionService.Stages must explicitly filter dual ranged sides by legality.'

Assert-True (
    $stagesText -match 'if\s*\(resolvedSpec\s*!=\s*null\s*&&\s*resolvedSpec\.RequiresDirectTargetLineOfSight'
) 'Dual ranged legality filter must branch on per-side necessary direct-target LOS truth.'

Assert-True (
    ($stagesText -match 'return BuildSingleResultCasts\(request,\s*survivingResult') -or
    ($stagesText -match 'single_side_fallback')
) 'Dual ranged legality filter must degrade to a surviving single side when only one side remains legal.'

Write-Output 'DualRangedLosPruningSmokeTests PASS'
