# 本脚本使用 UTF-8 BOM，确保 Windows PowerShell 正确读取中文断言。
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
$routeRoot = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath'
$configPath = Join-Path $routeRoot 'RoutePathConfig.cs'
$statePath = Join-Path $routeRoot 'RoutePathState.cs'
$resolverPath = Join-Path $routeRoot 'RouteSegmentResolver.cs'
$modulePath = Join-Path $routeRoot 'RoutePathModule.cs'
$xmlPath = Join-Path $repoRoot 'Common\Content\Defs\RangedModuleDef\RoutePath.xml'
$spreadPath = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\OrderedSpread\OrderedSpreadSampler.cs'

$configText = Read-Source $configPath
$stateText = Read-Source $statePath
$resolverText = Read-Source $resolverPath
$moduleText = Read-Source $modulePath
$xmlText = Read-Source $xmlPath
$spreadText = Read-Source $spreadPath
$contentText = $configText + $stateText + $resolverText + $moduleText + $spreadText

Assert-True (
    ($configText -match 'public\s+float\s+IntermediateSpreadRadius\s*=\s*0\.625f') -and
    ($configText -match 'public\s+float\s+FinalSpreadRadius\s*=\s*0\.30f') -and
    ($configText -match 'public\s+float\s+HighAccuracySpreadScale\s*=\s*0\.25f') -and
    ($configText -notmatch 'SpreadSafetyShrinkSteps|ArrivalTolerance')
) 'RoutePathConfig must expose ordered-spread parameters.'

Assert-True (
    ($configText -notmatch 'LowSkillSpreadMultiplier|HighSkillSpreadMultiplier|AnchorSpreadRadius') -and
    ($moduleText -notmatch 'SkillDefOf\.Shooting|ResolveSkillSpreadMultiplier')
) 'RoutePath must not calculate Shooting skill a second time.'

Assert-True (
    ($stateText -match 'public\s+float\s+IntermediateSpreadRadius') -and
    ($stateText -match 'public\s+float\s+FinalSpreadRadius') -and
    ($stateText -match 'public\s+float\s+HighAccuracySpreadScale') -and
    ($stateText -notmatch 'SpreadSafetyShrinkSteps|ArrivalTolerance') -and
    ($stateText -notmatch 'AnchorSpreadRadius')
) 'RoutePathContext must persist the ordered-spread snapshot without legacy aliases.'

Assert-True (
    ($resolverText -match 'float\s+intermediateSpreadRadius') -and
    ($resolverText -match 'float\s+finalSpreadRadius') -and
    ($resolverText -match 'float\s+highAccuracySpreadScale')
) 'RouteSegmentResolver must freeze every ordered-spread parameter.'

Assert-True (
    ($spreadText -match 'ProjectileAccuracySnapshot') -and
    ($spreadText -match 'StandardAimChance') -and
    ($resolverText -match 'IntermediateSpreadRadius') -and
    ($resolverText -match 'FinalSpreadRadius') -and
    ($resolverText -match 'HighAccuracySpreadScale') -and
    ($resolverText -notmatch 'GenSight|CanBeSeenOverFast|Map\s+map|TryResolveSafeSpreadDestination|IsExactSegmentClear')
) '路线偏移只消费精度与稳定种子，不能感知墙体回收散布。'

Assert-True (
    ($spreadText -match 'ResolveScaledRadius') -and
    ($spreadText -match 'SampleOffset') -and
    ($spreadText -match 'Gen\.HashCombineInt') -and
    ($spreadText -match 'Rand\.PushState') -and
    ($spreadText -match 'Rand\.PopState') -and
    ($resolverText -match 'OrderedSpreadSampler\.ResolveScaledRadius') -and
    ($resolverText -match 'OrderedSpreadSampler\.SampleOffset')
) 'Intermediate and final samples must use one shared stable-seed utility.'

Assert-True (
    ($resolverText -match 'TryResolveContinuation') -and
    ($resolverText -notmatch 'out\s+bool\s+advanceLeg|IsWithinArrivalTolerance') -and
    ($resolverText -match 'CurrentLegIndex')
) '续段应正常推进，不再保留为避墙而回接名义点的分支。'

Assert-True (
    ($moduleText -match 'context\.AccuracySnapshot') -and
    ($moduleText -match 'TryAdvanceLeg')
) 'Arrival must consume accuracy facts and advance only for a normal next leg.'

Assert-True (
    ($moduleText -match 'bool\s+hasFrozenFinalDestination\s*=\s*state\.PathSnapshot\.HasFrozenFinalDestination') -and
    ($moduleText -match 'Vector3\s+frozenFinalDestination\s*=\s*state\.PathSnapshot\.FrozenFinalDestination') -and
    ($moduleText -match 'state\.PathSnapshot\.HasFrozenFinalDestination\s*=\s*hasFrozenFinalDestination') -and
    ($moduleText -match 'state\.PathSnapshot\.FrozenFinalDestination\s*=\s*frozenFinalDestination')
) 'Automatic emit-route selection must preserve the final destination frozen at launch.'

Assert-True (
    ($xmlText -match '<IntermediateSpreadRadius>0\.625</IntermediateSpreadRadius>') -and
    ($xmlText -match '<FinalSpreadRadius>0\.30</FinalSpreadRadius>') -and
    ($xmlText -match '<HighAccuracySpreadScale>0\.25</HighAccuracySpreadScale>') -and
    ($xmlText -notmatch 'SpreadSafetyShrinkSteps|ArrivalTolerance')
) 'RoutePath Def must declare ordered-spread defaults.'

Assert-True (
    ($contentText -notmatch 'ShotReport\.HitReportFor') -and
    ($moduleText -notmatch 'SkillDefOf\.Shooting') -and
    ($moduleText -notmatch 'ProjectileHitFlags') -and
    ($moduleText -match 'LaunchResolution')
) 'Content route business must consume frozen facts without rerunning or rewriting original hit logic.'

Write-Output 'RoutePathOrderedSpreadSmokeTests PASS'
