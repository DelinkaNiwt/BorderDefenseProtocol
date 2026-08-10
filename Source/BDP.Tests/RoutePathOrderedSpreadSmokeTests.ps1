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
$xmlPath = Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RoutePath.xml'

$configText = Read-Source $configPath
$stateText = Read-Source $statePath
$resolverText = Read-Source $resolverPath
$moduleText = Read-Source $modulePath
$xmlText = Read-Source $xmlPath
$contentText = $configText + $stateText + $resolverText + $moduleText

Assert-True (
    ($configText -match 'public\s+float\s+IntermediateSpreadRadius\s*=\s*0\.625f') -and
    ($configText -match 'public\s+float\s+FinalSpreadRadius\s*=\s*0\.30f') -and
    ($configText -match 'public\s+float\s+HighAccuracySpreadScale\s*=\s*0\.25f') -and
    ($configText -match 'public\s+int\s+SpreadSafetyShrinkSteps\s*=\s*4')
) 'RoutePathConfig must expose ordered-spread parameters.'

Assert-True (
    ($configText -notmatch 'LowSkillSpreadMultiplier|HighSkillSpreadMultiplier|AnchorSpreadRadius') -and
    ($moduleText -notmatch 'SkillDefOf\.Shooting|ResolveSkillSpreadMultiplier')
) 'RoutePath must not calculate Shooting skill a second time.'

Assert-True (
    ($stateText -match 'public\s+float\s+IntermediateSpreadRadius') -and
    ($stateText -match 'public\s+float\s+FinalSpreadRadius') -and
    ($stateText -match 'public\s+float\s+HighAccuracySpreadScale') -and
    ($stateText -match 'public\s+int\s+SpreadSafetyShrinkSteps') -and
    ($stateText -notmatch 'AnchorSpreadRadius')
) 'RoutePathContext must persist the ordered-spread snapshot without legacy aliases.'

Assert-True (
    ($resolverText -match 'float\s+intermediateSpreadRadius') -and
    ($resolverText -match 'float\s+finalSpreadRadius') -and
    ($resolverText -match 'float\s+highAccuracySpreadScale') -and
    ($resolverText -match 'int\s+spreadSafetyShrinkSteps')
) 'RouteSegmentResolver must freeze every ordered-spread parameter.'

Assert-True (
    ($resolverText -match 'ProjectileAccuracySnapshot') -and
    ($resolverText -match 'ResolveAccuracyQuality') -and
    ($resolverText -match 'IntermediateSpreadRadius') -and
    ($resolverText -match 'FinalSpreadRadius') -and
    ($resolverText -match 'HighAccuracySpreadScale') -and
    ($resolverText -match 'SpreadSafetyShrinkSteps') -and
    ($resolverText -match 'GenSight\.LineOfSight') -and
    ($resolverText -match 'CanBeSeenOverFast') -and
    ($resolverText -match 'offset\s*\*=\s*0\.5f')
) 'Route resolver must build stable accuracy-driven safe spread.'

Assert-True (
    ($resolverText -match 'SampleKind') -and
    ($resolverText -match 'Gen\.HashCombineInt') -and
    ($resolverText -match 'Rand\.PushState') -and
    ($resolverText -match 'Rand\.PopState')
) 'Intermediate and final samples must use distinct stable seeds.'

Assert-True (
    ($resolverText -match 'TryResolveContinuation') -and
    ($resolverText -match 'out\s+bool\s+advanceLeg') -and
    ($resolverText -match 'ArrivalTolerance') -and
    ($resolverText -match 'CurrentLegIndex')
) 'Route resolver must distinguish normal advancement from recovery continuation.'

Assert-True (
    ($moduleText -match 'context\.AccuracySnapshot') -and
    ($moduleText -match 'if\s*\(advanceLeg\)') -and
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
    ($xmlText -match '<SpreadSafetyShrinkSteps>4</SpreadSafetyShrinkSteps>')
) 'RoutePath Def must declare ordered-spread defaults.'

Assert-True (
    ($contentText -notmatch 'ShotReport\.HitReportFor') -and
    ($moduleText -notmatch 'SkillDefOf\.Shooting') -and
    ($moduleText -notmatch 'ProjectileHitFlags')
) 'Content route business must consume frozen facts without rerunning or rewriting original hit logic.'

Write-Output 'RoutePathOrderedSpreadSmokeTests PASS'
