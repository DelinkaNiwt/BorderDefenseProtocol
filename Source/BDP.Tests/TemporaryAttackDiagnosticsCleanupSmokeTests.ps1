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

$diagnosticsPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionDiagnostics.cs'
$surfacePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionSurfaceAccess.cs'
$postLoadRecoveryPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionPostLoadRecovery.cs'
$formalHostShootPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_FormalHostShoot.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'

$checkedText = @(
    Read-Source $diagnosticsPath
    Read-Source $surfacePath
    Read-Source $postLoadRecoveryPath
    Read-Source $formalHostShootPath
    Read-Source $continuationPlannerPath
) -join "`n"

$temporaryPatterns = @(
    ('auto' + '_ranged_entry_staging'),
    ('ranged_formal_host' + '_resume_check'),
    ('ranged_formal_host' + '_minimum_truth'),
    ('ranged_continuation' + '_prepare_failed'),
    ('LogAutoRanged' + 'EntryStagingDecision'),
    ('LogRangedFormalHost' + 'ResumeCheck'),
    ('LogRangedFormalHost' + 'MinimumTruth'),
    ('LogRangedContinuation' + 'PrepareFailed')
)

foreach ($pattern in $temporaryPatterns) {
    Assert-True (
        $checkedText -notmatch [regex]::Escape($pattern)
    ) "Temporary attack diagnostic marker must be removed from main source: $pattern"
}

Assert-True (
    -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'AutoRangedFailureDiagnosticsSmokeTests.ps1'))
) 'Temporary auto-ranged failure diagnostics smoke test must be removed.'

Assert-True (
    -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'RangedContinuationFailureDiagnosticsSmokeTests.ps1'))
) 'Temporary ranged-continuation failure diagnostics smoke test must be removed.'

Write-Output 'TemporaryAttackDiagnosticsCleanupSmokeTests PASS'
