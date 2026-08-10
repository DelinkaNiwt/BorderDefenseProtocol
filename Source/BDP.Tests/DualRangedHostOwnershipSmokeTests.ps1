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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$stagesPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionService.Stages.cs'
$contextPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\RangedAttackExecutionContext.cs'

$stagesText = Read-Source $stagesPath
$contextText = Read-Source $contextPath

Assert-True (
    $stagesText -match 'ResolveRangedStepHostResultId'
) 'AttackExecutionService.Stages must define an explicit ranged step host resolution helper.'

Assert-True (
    $stagesText -match 'HostResultId = ResolveRangedStepHostResultId\(request, casts\)'
) 'Ranged runtime steps must bind HostResultId from resolved effective host ownership.'

Assert-True (
    $stagesText -match 'single_side_fallback'
) 'Dual ranged single-side degradation must remain visible to the host ownership path.'

Assert-True (
    $contextText -match 'string hostResultId = step != null && !string.IsNullOrWhiteSpace\(step.HostResultId\)'
) 'RangedAttackExecutionContext must prefer step.HostResultId before falling back to the entry result id.'

Assert-True (
    $contextText -match 'TryGetByResultId\(request.Request.Pawn, hostResultId'
) 'RangedAttackExecutionContext must bind the formal host shell using the effective host result id.'

Write-Output 'DualRangedHostOwnershipSmokeTests PASS'
