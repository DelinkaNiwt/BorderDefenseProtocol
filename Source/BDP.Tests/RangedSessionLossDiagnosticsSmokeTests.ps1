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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$diagnosticsPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionDiagnostics.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$formalHostShootPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$jobDriverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'

$diagnosticsText = Get-Content -LiteralPath $diagnosticsPath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$formalHostShootText = Get-Content -LiteralPath $formalHostShootPath -Raw -Encoding utf8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8

Assert-True (
    $diagnosticsText -match 'LogFormalHostSessionTokenSync\('
) 'AttackExecutionDiagnostics must expose formal-host token sync diagnostics.'

Assert-True (
    $diagnosticsText -match 'event=formal_host_session_token_sync'
) 'Formal-host token sync diagnostics must use a stable event name.'

Assert-True (
    $diagnosticsText -match 'LogVerbSessionCleared\('
) 'AttackExecutionDiagnostics must expose verb session-clear diagnostics.'

Assert-True (
    $diagnosticsText -match 'event=verb_session_cleared'
) 'Verb session-clear diagnostics must use a stable event name.'

Assert-True (
    $diagnosticsText -match 'LogRangedJobSessionInvalid\('
) 'AttackExecutionDiagnostics must expose ranged job invalid-session diagnostics.'

Assert-True (
    $diagnosticsText -match 'event=ranged_job_session_invalid'
) 'Ranged job invalid-session diagnostics must use a stable event name.'

Assert-True (
    $shootVerbText -match 'LogVerbSessionCleared\('
) 'BdpVerb_Shoot.Reset and null context paths must log when they clear session truth.'

Assert-True (
    $formalHostShootText -match 'LogFormalHostSessionTokenSync\('
) 'BdpVerb_FormalHostShoot.SyncFormalBinding must log token sync boundary changes.'

Assert-True (
    $jobDriverText -match 'LogRangedJobSessionInvalid\('
) 'JobDriver_BdpRangedAttackExecution must log the job boundary when session validation fails.'

Assert-True (
    $diagnosticsText -match 'LogRangedJobCleanupDecision\('
) 'AttackExecutionDiagnostics must expose ranged job cleanup decision diagnostics.'

Assert-True (
    $diagnosticsText -match 'event=ranged_job_cleanup_decision'
) 'Ranged job cleanup decision diagnostics must use a stable event name.'

Assert-True (
    $jobDriverText -match 'LogRangedJobCleanupDecision\('
) 'JobDriver_BdpRangedAttackExecution must log whether cleanup reset is applied or skipped.'

Assert-True (
    $jobDriverText -match 'reason = "skip_generation_mismatch"'
) 'JobDriver_BdpRangedAttackExecution must explicitly log generation mismatch cleanup skips.'

Assert-True (
    $jobDriverText -match 'reason = "apply_owned_session_reset"'
) 'JobDriver_BdpRangedAttackExecution must explicitly log owned-session cleanup resets.'

Write-Output 'RangedSessionLossDiagnosticsSmokeTests PASS'
