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
    $jobDriverText -match 'AddFinishAction\s*\(\s*CleanupAttackSessionOnJobExit\s*\)'
) 'Manual ranged execution job must register a finish action that clears the bound verb session when the job exits.'

Assert-True (
    $jobDriverText -match 'private\s+void\s+CleanupAttackSessionOnJobExit\s*\(\s*JobCondition\s+condition\s*\)'
) 'Manual ranged execution job must expose a dedicated finish-action cleanup hook for attack session teardown.'

Assert-True (
    $jobDriverText -match 'CleanupAttackSessionOnJobExit\s*\(\s*JobCondition\s+condition\s*\)[\s\S]*job\s*!=\s*null\s*\?\s*job\.verbToUse\s*:\s*null[\s\S]*shootVerb\.Reset\s*\(\s*\)'
) 'Manual ranged execution job teardown must reset the bound BdpVerb_Shoot so stale manual host sessions cannot bleed into later auto attacks.'

Write-Output 'ManualRangedSessionCleanup PASS'
