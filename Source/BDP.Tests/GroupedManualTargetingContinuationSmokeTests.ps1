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

$groupedSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\GroupedAttackExecutionTargetingSource.cs'
$diagnosticsPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionDiagnostics.cs'

$groupedSourceText = Read-Source $groupedSourcePath
$diagnosticsText = Read-Source $diagnosticsPath

Assert-True (
    ($groupedSourceText -match 'public ITargetingSource DestinationSelector') -and
    ($groupedSourceText -notmatch 'DestinationSelector\s*=>\s*null') -and
    ($groupedSourceText -match 'HasActiveContinuation') -and
    ($groupedSourceText -match 'source\.DestinationSelector\s*!=\s*null') -and
    ($groupedSourceText -match 'return\s+this\s*;')
) 'Grouped manual targeting source must keep RimWorld Targeter alive when any member source requests continued targeting.'

Assert-True (
    ($groupedSourceText -match 'LogGroupedManualTargetingContinuation') -and
    ($diagnosticsText -match 'LogGroupedManualTargetingContinuation')
) 'Grouped manual targeting continuation must be visible in attack-execution diagnostics.'

Write-Output 'GroupedManualTargetingContinuationSmokeTests PASS'
