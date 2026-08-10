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

$attackContextPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContext.cs'
$attackContextSnapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContextSnapshot.cs'
$attackContextNodePath = Join-Path $bdpSourceRoot 'AttackExecution\Context\IAttackContextNode.cs'
$projectileInitPlanPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$requestPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionRequest.cs'
$preparedContextPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionPreparedContext.cs'
$moduleSnapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedModuleContextSnapshot.cs'

$projectileInitPlanText = Read-Source $projectileInitPlanPath
$requestText = Read-Source $requestPath
$preparedContextText = Read-Source $preparedContextPath

Assert-True (
    Test-Path -LiteralPath $attackContextPath
) 'AttackContext.cs must exist as the single runtime attack-context trunk.'

Assert-True (
    Test-Path -LiteralPath $attackContextSnapshotPath
) 'AttackContextSnapshot.cs must exist as the single frozen attack-context trunk.'

Assert-True (
    Test-Path -LiteralPath $attackContextNodePath
) 'IAttackContextNode.cs must exist as the neutral context-node protocol.'

Assert-True (
    $projectileInitPlanText -match 'AttackContextSnapshot'
) 'ProjectileInitPlan must carry AttackContextSnapshot for the frozen back-half pipeline.'

Assert-True (
    $projectileInitPlanText -notmatch 'RangedModuleContextSnapshot'
) 'ProjectileInitPlan must stop carrying RangedModuleContextSnapshot as a parallel trunk.'

Assert-True (
    $projectileInitPlanText -notmatch 'ConfirmedInteractionSnapshot'
) 'ProjectileInitPlan must stop carrying ConfirmedInteractionSnapshot as a parallel trunk.'

Assert-True (
    $projectileInitPlanText -notmatch 'RangedAttackModuleSession'
) 'ProjectileInitPlan must stop carrying runtime module session into the frozen back-half pipeline.'

Assert-True (
    $requestText -match 'AttackContextSnapshot'
) 'AttackExecutionRequest must carry AttackContextSnapshot instead of fragmented frozen payloads.'

Assert-True (
    $preparedContextText -match 'AttackContextSnapshot'
) 'AttackExecutionPreparedContext must expose AttackContextSnapshot as the single frozen trunk.'

Assert-True (
    -not (Test-Path -LiteralPath $moduleSnapshotPath)
) 'RangedModuleContextSnapshot.cs must be deleted after the unified attack-context trunk lands.'

Write-Output 'AttackContextBoundarySmokeTests PASS'
