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
$jobDriverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'

$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8

Assert-True (
    $jobDriverText -match 'cleanupOwnedAttackInstanceId'
) 'JobDriver_BdpRangedAttackExecution must track the owned cleanup attack instance id.'

Assert-True (
    $jobDriverText -match 'cleanupOwnedResultId'
) 'JobDriver_BdpRangedAttackExecution must track the owned cleanup result id.'

Assert-True (
    $jobDriverText -match 'cleanupOwnedProjectionVersion'
) 'JobDriver_BdpRangedAttackExecution must track the owned cleanup projection version.'

Assert-True (
    $jobDriverText -match 'cleanupOwnedOwnerPawnThingId'
) 'JobDriver_BdpRangedAttackExecution must track the owned cleanup owner pawn id.'

Assert-True (
    $jobDriverText -match 'CaptureCleanupOwnedSessionIfNeeded'
) 'JobDriver_BdpRangedAttackExecution must capture its owned session token for cleanup gating.'

Assert-True (
    $jobDriverText -match 'ShouldCleanupCurrentVerbSession'
) 'JobDriver_BdpRangedAttackExecution must gate cleanup reset by owned session identity.'

Assert-True (
    $jobDriverText -match 'CaptureCleanupOwnedSessionIfNeeded\(verb\)'
) 'TryTickExecution must capture the owned session token before it can be replaced by a later session.'

Assert-True (
    $jobDriverText -match 'CaptureCleanupOwnedSessionIfNeeded\(job != null \? job\.verbToUse : null\)'
) 'Job init must attempt an early owned-session capture for cleanup gating.'

Assert-True (
    $jobDriverText -match 'cleanupOwnedAttackInstanceId = token\.AttackInstanceId'
) 'Owned cleanup snapshot must store the attack instance id from the current token.'

Assert-True (
    $jobDriverText -match 'cleanupOwnedResultId = token\.ResultId'
) 'Owned cleanup snapshot must store the result id from the current token.'

Assert-True (
    $jobDriverText -match 'if \(ShouldCleanupCurrentVerbSession\(shootVerb\)\)'
) 'CleanupAttackSessionOnJobExit must only reset when the current verb session still belongs to this job generation.'

Assert-True (
    $jobDriverText -notmatch 'if \(\s*verb is BdpVerb_Shoot shootVerb\s*\)\s*\{\s*shootVerb\.Reset\(\);'
) 'CleanupAttackSessionOnJobExit must not directly reset the shared formal host shell without generation gating.'

Write-Output 'RangedJobGenerationScopedCleanupSmokeTests PASS'
