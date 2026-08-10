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

$diagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionDiagnostics.cs'
$stagesPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionService.Stages.cs'
$formalHostShootPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostShoot.cs'
$diagnosticsCorePath = Join-Path $bdpSourceRoot 'Support\Diagnostics\BdpDiagnostics.cs'
$protocolPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$diagnosticsText = Read-Source $diagnosticsPath
$stagesText = Read-Source $stagesPath
$formalHostShootText = Read-Source $formalHostShootPath
$diagnosticsCoreText = Read-Source $diagnosticsCorePath
$protocolText = Read-Source $protocolPath

Assert-True (
    ($diagnosticsText -match 'LogDualRangedPlanStart\(') -and
    ($diagnosticsText -match 'event=dual_ranged_plan_start') -and
    ($stagesText -match 'LogDualRangedPlanStart\(')
) 'Dual ranged plan build must log the composite/source ids and semantic target once per plan build.'

Assert-True (
    ($diagnosticsText -match 'LogDualRangedSideLegality\(') -and
    ($diagnosticsText -match 'event=dual_ranged_side_legality') -and
    ($diagnosticsText -match 'requiresDirectTargetLos=') -and
    ($diagnosticsText -match 'requiresVerbLos=') -and
    ($stagesText -match 'LogDualRangedSideLegality\(')
) 'Dual ranged side pruning must log each side necessary-LOS decision and the generic verb LOS truth separately.'

Assert-True (
    ($diagnosticsText -match 'LogDualRangedPlanResult\(') -and
    ($diagnosticsText -match 'event=dual_ranged_plan_result') -and
    ($diagnosticsText -match 'survivorCount=') -and
    ($diagnosticsText -match 'castCount=') -and
    ($stagesText -match 'LogDualRangedPlanResult\(')
) 'Dual ranged plan build must log final survivor count and cast count, including single-side fallback.'

Assert-True (
    ($diagnosticsText -match 'LogDualRangedHostLosProbe\(') -and
    ($diagnosticsText -match 'event=dual_ranged_host_los_probe') -and
    ($diagnosticsCoreText -match 'AttackExecutionThrottled\(') -and
    ($formalHostShootText -match 'override bool CanHitTargetFrom\(') -and
    ($formalHostShootText -match 'LogDualRangedHostLosProbe\(')
) 'Composite formal host CanHitTargetFrom must emit throttled diagnostics for auto-chain LOS probes without changing the result.'

Assert-True (
    ($protocolText -match 'protocolResult = new RangedAttackProtocolResult') -or
    ($protocolText -match 'protocolResult = firstFailedProtocol')
) 'Dual ranged protocol rebuild must still surface a protocol result instead of collapsing to null.'

Write-Output 'DualRangedExecutionDiagnosticsSmokeTests PASS'
