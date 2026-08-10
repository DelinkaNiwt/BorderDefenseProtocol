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

$protocolPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$diagnosticsPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionDiagnostics.cs'

$protocolText = Read-Source $protocolPath
$diagnosticsText = Read-Source $diagnosticsPath

Assert-True (
    ($diagnosticsText -match 'LogRangedProjectilePlanSummary') -and
    ($diagnosticsText -match 'planLaunchTarget') -and
    ($diagnosticsText -match 'planAimTarget') -and
    ($diagnosticsText -match 'planCurrentTarget') -and
    ($diagnosticsText -match 'planSourceResultId')
) 'AttackExecution diagnostics must expose ranged projectile plan source and target layering.'

Assert-True (
    ($protocolText -match 'LogRangedProjectilePlanSummary') -and
    ($protocolText -match '"single_or_lane"') -and
    ($protocolText -match '"dual_merged"')
) 'Ranged protocol service must log both single/lane and dual-merged projectile plan summaries.'

Write-Output 'RangedProjectilePlanDiagnosticsSmokeTests PASS'
