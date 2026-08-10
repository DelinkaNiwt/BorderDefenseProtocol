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

$requestPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionRequest.cs'
$preparedContextPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionPreparedContext.cs'
$entryPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\RangedAttackEntry.cs'
$planPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$protocolServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$projectileInitStageServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$attackContextKeysPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContextKeys.cs'

$requestText = Read-Source $requestPath
$preparedContextText = Read-Source $preparedContextPath
$entryText = Read-Source $entryPath
$planText = Read-Source $planPath
$targetingSourceText = Read-Source $targetingSourcePath
$protocolServiceText = Read-Source $protocolServicePath
$projectileInitStageServiceText = Read-Source $projectileInitStageServicePath
$attackContextKeysText = Read-Source $attackContextKeysPath

Assert-True (
    $requestText -match 'AttackContextSnapshot'
) 'AttackExecutionRequest must carry AttackContextSnapshot.'

Assert-True (
    $preparedContextText -match 'AttackContextSnapshot'
) 'AttackExecutionPreparedContext must expose AttackContextSnapshot.'

Assert-True (
    $entryText -match 'ConfirmedInputSnapshot'
) 'RangedAttackEntry must carry the frozen confirmed input snapshot.'

Assert-True (
    $entryText -match 'ConfirmedInteractionSnapshot'
) 'RangedAttackEntry must carry the frozen confirmed interaction snapshot.'

Assert-True (
    ($targetingSourceText -match 'BuildConfirmedInputSnapshot') -and
    ($targetingSourceText -match 'BuildConfirmedInteractionSnapshot')
) 'Targeting bridge must freeze confirm-stage facts from AttackContext before dispatch.'

Assert-True (
    $planText -match 'AttackContextSnapshot\s+AttackContextSnapshot'
) 'ProjectileInitPlan must carry unified AttackContextSnapshot.'

Assert-True (
    ($projectileInitStageServiceText -match 'CreateAttackContextSnapshot') -and
    ($projectileInitStageServiceText -notmatch 'CreateSnapshot')
) 'ProjectileInitStageService must freeze unified AttackContextSnapshot into ProjectileInitPlan.'

Assert-True (
    ($protocolServiceText -match 'ConfirmedInputSnapshot') -and
    ($protocolServiceText -match 'entry\.ConfirmedInput')
) 'RangedAttackProtocolService must continue carrying the frozen confirmed input into the ranged entry.'

Assert-True (
    ($protocolServiceText -match 'ConfirmedInteractionSnapshot') -and
    ($protocolServiceText -match 'entry\.ConfirmedInteraction')
) 'RangedAttackProtocolService must continue carrying the frozen confirmed interaction into the ranged entry.'

Assert-True (
    $attackContextKeysText -match 'ConfirmedInput' -and
    $attackContextKeysText -match 'ConfirmedInteraction'
) 'AttackContextKeys must define the neutral confirmed-node keys.'

Write-Output 'RangedModulePrivateContextFreezeSmokeTests PASS'
