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
    $jobDriverText -match 'ResolveLiveValidationTarget\s*\('
) 'Ranged continuous jobs must resolve the live validation target instead of always validating TargetA.'

Assert-True (
    $jobDriverText -match 'ResolveLiveValidationTarget\s*\([\s\S]*TryResolveSemanticValidationTarget\s*\('
) 'Ranged continuous jobs must prefer the semantic entity target before falling back to TargetA.'

Assert-True (
    $jobDriverText -match 'TryValidateLiveTarget\s*\([\s\S]*ResolveLiveValidationTarget\s*\('
) 'TryValidateLiveTarget must validate the resolved semantic-first target.'

Assert-True (
    $jobDriverText -match 'TryResolveSemanticValidationTarget\s*\([\s\S]*TryResolveCurrentSemanticTarget\s*\('
) 'Semantic validation must read the current BdpVerb_Shoot semantic target.'

Assert-True (
    $jobDriverText -match 'private\s+static\s+bool\s+IsLiveValidationTargetStillUsable\s*\('
) 'Live target usability checks must be centralized so semantic targets and TargetA share the same dead/downed/invisible rules.'

Write-Output 'RangedSemanticTargetPrioritySmokeTests PASS'
