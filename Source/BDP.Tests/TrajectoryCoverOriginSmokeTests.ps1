# 本脚本使用 UTF-8 编码，验证终端掩体始终由最终飞行段起点决定。
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

function Read-OptionalSource {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modelRoot = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model'
$flightPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\ProjectileFlightPathSnapshot.cs'
$coverPath = Join-Path $modelRoot 'TrajectoryCoverSnapshot.cs'
$planPath = Join-Path $modelRoot 'ProjectileInitPlan.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$flightText = Read-OptionalSource $flightPath
$coverText = Read-OptionalSource $coverPath
$planText = Read-OptionalSource $planPath
$verbText = Read-OptionalSource $verbPath

Assert-True (
    ($flightText -match 'FinalApproachStart') -and
    ($flightText -match 'HasFinalApproach')
) 'The neutral path snapshot must expose the final approach start and whether it is valid.'

Assert-True (
    ($coverText -match 'class\s+TrajectoryCoverSnapshot') -and
    ($coverText -match 'CoverOriginCell') -and
    ($coverText -match 'OverallBlockChance') -and
    ($coverText -match 'CoverInfo')
) 'The cover snapshot must carry its geometric origin, original cover information, and combined block chance.'

Assert-True (
    ($planText -match 'TrajectoryCoverSnapshot\s+TrajectoryCover') -and
    ($planText -match 'FinalApproachStart')
) 'ProjectileInitPlan must persist the trajectory-derived terminal cover facts.'

Assert-True (
    ($coverText -match 'CoverUtility\.CalculateCoverGiverSet') -and
    ($coverText -match 'CoverUtility\.CalculateOverallBlockChance') -and
    ($verbText -match 'coverOriginCell') -and
    ($verbText -match 'TrajectoryCoverSnapshot\.Create')
) 'BdpVerb_Shoot must use the original CoverUtility formulas with the trajectory final-segment origin.'

Assert-True (
    ($verbText -notmatch 'CalculateCoverGiverSet\(target,\s*caster\.Position') -and
    ($verbText -notmatch 'CalculateOverallBlockChance\(target,\s*caster\.Position')
) 'The trajectory-aware cover path must not reintroduce caster.Position as the cover origin.'

Assert-True (
    ($coverText -match 'GetRandomCoverToMissInto|TryGetRandomCover|RandomCover') -and
    ($verbText -match 'TrajectoryCover')
) 'The random cover interception target must come from the same trajectory cover snapshot as the probability.'

Write-Output 'TrajectoryCoverOriginSmokeTests PASS'
