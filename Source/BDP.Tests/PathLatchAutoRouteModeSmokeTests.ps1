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

function Read-SourceOrEmpty {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Read-Source $Path
}

function Read-AllCs {
    param([string]$Root)

    $builder = New-Object System.Text.StringBuilder
    Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.cs' |
        ForEach-Object {
            [void]$builder.AppendLine((Read-Source $_.FullName))
        }

    return $builder.ToString()
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$workspaceRoot = Split-Path -Parent $repoRoot
$mainSourceRoot = Join-Path $sourceRoot 'BDP'
$devHarnessRoot = Join-Path $workspaceRoot 'BorderDefenseProtocol.DevHarness'
$samplesRoot = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples'
$moduleDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'

$configPath = Join-Path $samplesRoot 'PathLatchConfig.cs'
$statePath = Join-Path $samplesRoot 'PathLatchState.cs'
$modulePath = Join-Path $samplesRoot 'PathLatchModule.cs'
$resolverPath = Join-Path $samplesRoot 'PathLatchAutoRouteResolver.cs'

$mainSourceText = Read-AllCs $mainSourceRoot
$configText = Read-Source $configPath
$stateText = Read-Source $statePath
$moduleText = Read-Source $modulePath
$resolverText = Read-SourceOrEmpty $resolverPath
$moduleDefsText = Read-Source $moduleDefsPath

Assert-True (
    $mainSourceText -notmatch 'PathLatchAutoRoute|ObstacleRouter|AutoRouteResolver'
) 'Main BDP source must not implement PathLatch auto-route business.'

Assert-True (
    Test-Path -LiteralPath $resolverPath
) 'DevHarness must add PathLatchAutoRouteResolver.cs for PathLatch auto-route business.'

Assert-True (
    ($configText -match 'public\s+bool\s+EnableAutoRoute') -and
    ($configText -match 'public\s+int\s+AutoRouteMaxDepth') -and
    ($configText -match 'public\s+int\s+AutoRouteAnchorsPerWall') -and
    ($configText -match 'public\s+int\s+AutoRouteMaxObstacleCells') -and
    ($configText -match 'EnableAutoRoute\s*=\s*EnableAutoRoute') -and
    ($configText -match 'AutoRouteMaxDepth\s*=\s*AutoRouteMaxDepth') -and
    ($configText -match 'AutoRouteAnchorsPerWall\s*=\s*AutoRouteAnchorsPerWall') -and
    ($configText -match 'AutoRouteMaxObstacleCells\s*=\s*AutoRouteMaxObstacleCells')
) 'PathLatchConfig must expose and clone auto-route configuration.'

Assert-True (
    ($stateText -match 'enum\s+PathLatchPathSource') -and
    ($stateText -match 'Direct') -and
    ($stateText -match 'Manual') -and
    ($stateText -match 'Auto') -and
    ($stateText -match 'public\s+PathLatchPathSource\s+PathSource') -and
    ($stateText -match 'Scribe_Values\.Look\(ref pathSource')
) 'PathLatch state must persist Direct/Manual/Auto path source.'

Assert-True (
    ($stateText -match 'public\s+List<PathLatchAnchorPoint>\s+AutoLeftAnchors') -and
    ($stateText -match 'public\s+List<PathLatchAnchorPoint>\s+AutoRightAnchors') -and
    ($stateText -match 'Scribe_Collections\.Look\(ref autoLeftAnchors') -and
    ($stateText -match 'Scribe_Collections\.Look\(ref autoRightAnchors')
) 'PathLatch confirmed snapshot must preserve both left and right auto-route candidates.'

Assert-True (
    ($moduleDefsText -match '<EnableAutoRoute>true</EnableAutoRoute>') -and
    ($moduleDefsText -match '<AutoRouteMaxDepth>3</AutoRouteMaxDepth>') -and
    ($moduleDefsText -match '<AutoRouteAnchorsPerWall>3</AutoRouteAnchorsPerWall>') -and
    ($moduleDefsText -match '<AutoRouteMaxObstacleCells>200</AutoRouteMaxObstacleCells>')
) 'BDP_TestRangedPathLatchModule must declare auto-route config in XML.'

Assert-True (
    ($resolverText -match 'public\s+sealed\s+class\s+PathLatchAutoRouteResult') -and
    ($resolverText -match 'public\s+bool\s+Succeeded') -and
    ($resolverText -match 'public\s+List<IntVec3>\s+Anchors') -and
    ($resolverText -match 'public\s+List<IntVec3>\s+LeftAnchors') -and
    ($resolverText -match 'public\s+List<IntVec3>\s+RightAnchors') -and
    ($resolverText -match 'public\s+string\s+RejectReason') -and
    ($resolverText -match 'public\s+static\s+PathLatchAutoRouteResult\s+TryResolve')
) 'PathLatchAutoRouteResolver must expose a small result model and TryResolve entry.'

Assert-True (
    ($resolverText -match 'LeftAnchors\s*=\s*') -and
    ($resolverText -match 'RightAnchors\s*=\s*') -and
    ($resolverText -notmatch 'ChooseBestRoute')
) 'PathLatchAutoRouteResolver must return both legal route sides instead of collapsing them into one best route.'

Assert-True (
    ($resolverText -match 'GenSight\.LineOfSight') -and
    ($resolverText -match 'FindFirstBlockingCell') -and
    ($resolverText -match 'CollectObstacleCluster') -and
    ($resolverText -match 'CollectContourCandidates') -and
    ($resolverText -match 'ValidateRoute') -and
    ($resolverText -match 'AutoRouteMaxDepth') -and
    ($resolverText -match 'AutoRouteAnchorsPerWall') -and
    ($resolverText -match 'AutoRouteMaxObstacleCells')
) 'PathLatchAutoRouteResolver must find an obstacle cluster and validate every segment with vanilla LOS.'

Assert-True (
    ($moduleText -match 'HasManualAnchors') -and
    ($moduleText -match 'TryResolveAutoRouteForFinalTarget') -and
    ($moduleText -match 'EvaluateCurrentCandidateLegality[\s\S]*TryResolveAutoRouteForFinalTarget') -and
    ($moduleText -match 'AppendPreview[\s\S]*AppendAutoRoutePreview') -and
    ($moduleText -match 'HandleFinalTargetConfirm[\s\S]*TryResolveAutoRouteForFinalTarget')
) 'PathLatchModule must call auto-route only for no-manual-anchor targeting, preview, and final confirmation.'

Assert-True (
    ($moduleText -match 'TargetingAdvanceKind\.Reject') -and
    ($moduleText -match 'ConfirmedSnapshot\.Anchors\s*=\s*PathLatchSegmentResolver\.NormalizeAnchors\(') -and
    ($moduleText -match 'ConfirmedSnapshot\.AutoLeftAnchors\s*=') -and
    ($moduleText -match 'ConfirmedSnapshot\.AutoRightAnchors\s*=') -and
    ($moduleText -match 'PathSource\s*=\s*PathLatchPathSource\.Auto') -and
    ($moduleText -match 'PathSource\s*=\s*PathLatchPathSource\.Manual') -and
    ($moduleText -match 'PathSource\s*=\s*PathLatchPathSource\.Direct')
) 'PathLatch confirm must freeze auto-route anchors as ordinary anchors and reject failed auto-routes.'

Assert-True (
    ($moduleText -match 'SelectAutoRouteAnchorsForEmit') -and
    ($moduleText -match 'BuildConfirmedSnapshotForEmit') -and
    ($moduleText -match 'emitIndex\s*%\s*2') -and
    ($moduleText -match 'PathSource\s*==\s*PathLatchPathSource\.Auto[\s\S]*SelectAutoRouteAnchorsForEmit') -and
    ($moduleText -match 'HasManualAnchors\(state\)[\s\S]*pathSource\s*=\s*PathLatchPathSource\.Manual')
) 'PathLatch projectile init must alternate valid auto-route sides by emitIndex while preserving manual-anchor priority.'

Assert-True (
    ($moduleText -match 'OverrideLaunchTarget\s*=\s*firstTarget') -and
    ($moduleText -match 'OverrideAimTarget\s*=\s*confirmedSnapshot\.FinalTarget') -and
    ($moduleText -match 'OverrideCurrentTarget\s*=\s*confirmedSnapshot\.FinalTarget')
) 'Auto-route anchors must not pollute final aim/current target semantics.'

Write-Output 'PathLatchAutoRouteModeSmokeTests PASS'
