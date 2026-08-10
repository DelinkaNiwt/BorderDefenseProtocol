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

$planPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$stageServicePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$diagnosticsPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionDiagnostics.cs'
$projectilePath = Join-Path $bdpSourceRoot 'Projectiles\BdpProjectile.cs'

$planText = Read-Source $planPath
$stageServiceText = Read-Source $stageServicePath
$diagnosticsText = Read-Source $diagnosticsPath
$projectileText = Read-Source $projectilePath

Assert-True (
    ($planText -match 'RangedProjectileTargetSemantics\s+TargetSemantics') -and
    ($planText -match 'SyncTargetSemanticsFromLegacyTargets') -and
    ($planText -match 'Scribe_Deep\.Look\(ref\s+targetSemantics,\s*"targetSemantics"\)')
) 'ProjectileInitPlan must carry and save an independent target semantics snapshot.'

Assert-True (
    ($stageServiceText -match 'SyncTargetSemanticsFromLegacyTargets\(\)') -and
    ($planText -match 'RangedProjectileTargetSemantics\.CreateFromTargets') -and
    ($stageServiceText -match 'plan\.TargetSemantics\.Clone\(\)')
) 'ProjectileInitStageService must create target semantics after module target overrides and clone it per projectile plan.'

Assert-True (
    ($diagnosticsText -match 'target_semantics_projectile_plan') -and
    ($diagnosticsText -match 'target_semantics_live_update') -and
    ($diagnosticsText -match 'DescribeTargetSemantics') -and
    ($diagnosticsText -match 'intentFinalTarget') -and
    ($diagnosticsText -match 'liveNextPoint')
) 'AttackExecution diagnostics must expose projectile target semantics in a removable diagnostic event.'

Assert-True (
    ($projectileText -match 'ApplyLiveTargetSemantics') -and
    ($projectileText -match 'semantics\.LiveFinalTarget') -and
    ($projectileText -match 'semantics\.LiveNextTarget') -and
    ($projectileText -match 'semantics\.LiveNextPoint') -and
    ($projectileText -match 'LogTargetSemanticsLiveUpdate')
) 'BdpProjectile must update only the live target semantics during flight and arrival continuation.'

Write-Output 'RangedTargetSemanticsCarrySmokeTests PASS'
