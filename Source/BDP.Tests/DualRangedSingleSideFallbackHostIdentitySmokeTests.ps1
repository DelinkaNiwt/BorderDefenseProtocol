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
$stagesPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
$protocolPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$stagesText = Read-Source $stagesPath
$protocolText = Read-Source $protocolPath

Assert-True (
    $stagesText -match 'dual[^\r\n]*formal host[^\r\n]*复合结果'
) 'Dual ranged runtime step host identity must stay on the composite formal host result.'

Assert-True (
    $stagesText -notmatch 'singleSourceResultId' -and
    $stagesText -notmatch 'singleCastResultId'
) 'Dual ranged single-side fallback must not lower HostResultId to the surviving source result.'

Assert-True (
    $protocolText -match 'StepSourceResultIds\s*=' -and
    $protocolText -match 'CollectMergedSourceResultIds' -and
    $protocolText -match 'CollectStepSourceResultIds'
) 'Actual ranged source identity must remain represented by StepSourceResultIds instead of HostResultId.'

Write-Output 'DualRangedSingleSideFallbackHostIdentitySmokeTests PASS'
