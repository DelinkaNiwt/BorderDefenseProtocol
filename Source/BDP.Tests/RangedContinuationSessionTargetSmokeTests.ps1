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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$shootVerbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'
$executionContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedAttackExecutionContext.cs'

$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$executionContextText = Get-Content -LiteralPath $executionContextPath -Raw -Encoding utf8

Assert-True (
    ($shootVerbText -match 'LocalTargetInfo\s+sessionTarget') -and
    ($shootVerbText -match 'Scribe_TargetInfo\.Look\(ref\s+sessionTarget,\s*"sessionTarget"\)')
) 'BdpVerb_Shoot must persist a dedicated sessionTarget truth.'

Assert-True (
    ($shootVerbText -match 'sessionTarget\s*=\s*context\.SessionTarget') -and
    ($shootVerbText -match 'sessionTarget\s*=\s*LocalTargetInfo\.Invalid')
) 'BdpVerb_Shoot must bind and reset the dedicated session target.'

Assert-True (
    $shootVerbText -match 'WarmupComplete\(\)[\s\S]*TryPreparePendingEmission\(sessionTarget\.IsValid\s*\?\s*sessionTarget\s*:\s*currentTarget\)'
) 'WarmupComplete must continue with sessionTarget first, then only fall back to currentTarget.'

Assert-True (
    $shootVerbText -match 'TryCastShot\(\)[\s\S]*TryPreparePendingEmission\(sessionTarget\.IsValid\s*\?\s*sessionTarget\s*:\s*currentTarget\)'
) 'TryCastShot must continue with sessionTarget first, then only fall back to currentTarget.'

Assert-True (
    ($executionContextText -match 'LocalTargetInfo\s+SessionTarget') -and
    ($executionContextText -match 'SessionTarget\s*=\s*request\.Request\.Target')
) 'RangedAttackExecutionContext must expose the stable session target from the formal request.'

Assert-True (
    $continuationPlannerText -match 'Target\s*=\s*sessionTarget'
) 'RangedVerbContinuationPlanner must rebuild follow-up requests from the stable sessionTarget.'

Write-Output 'RangedContinuationSessionTargetSmokeTests PASS'
