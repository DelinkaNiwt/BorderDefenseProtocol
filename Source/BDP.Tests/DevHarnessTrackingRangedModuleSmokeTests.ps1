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

function Get-FileTextOrEmpty {
    param(
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Test-WellFormedXml {
    param(
        [string]$Path
    )

    try {
        $null = [xml](Get-Content -LiteralPath $Path -Raw -Encoding utf8)
        return $true
    }
    catch {
        return $false
    }
}

function Get-ClassDeclaration {
    param(
        [string]$Text,
        [string]$ClassName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)public\s+sealed\s+class\s+$ClassName\s*:(.*?)\{")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-ModuleDefBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<BDP\.Core\.AttackExecution\.BdpRangedAttackModuleDef>.*?<defName>$DefName</defName>.*?</BDP\.Core\.AttackExecution\.BdpRangedAttackModuleDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-VisualPresetBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<BDP\.Core\.Expressions\.ExpressionVisualPresetDef>.*?<defName>$DefName</defName>.*?</BDP\.Core\.Expressions\.ExpressionVisualPresetDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-ChipBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $matches = [regex]::Matches(
        $Text,
        '(?s)<ThingDef\s+ParentName="ResourceBase">.*?</ThingDef>')

    foreach ($match in $matches) {
        if ($match.Value -match "<defName>$DefName</defName>") {
            return $match.Value
        }
    }

    return $null
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modProjectsRoot = Split-Path -Parent $repoRoot
$devHarnessRoot = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness'
$samplesRoot = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples'
$moduleDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'
$visualPresetDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\ExpressionVisualPresetDefs_Test.xml'
$moduleSourcePath = Join-Path $samplesRoot 'TrackingModule.cs'
$configSourcePath = Join-Path $samplesRoot 'TrackingModuleConfig.cs'
$stateSourcePath = Join-Path $samplesRoot 'TrackingModuleState.cs'
$pathBuilderSourcePath = Join-Path $samplesRoot 'TrackingPathBuilder.cs'

$moduleSourceText = Get-FileTextOrEmpty $moduleSourcePath
$configSourceText = Get-FileTextOrEmpty $configSourcePath
$stateSourceText = Get-FileTextOrEmpty $stateSourcePath
$pathBuilderSourceText = Get-FileTextOrEmpty $pathBuilderSourcePath
$moduleDefsText = Get-FileTextOrEmpty $moduleDefsPath
$chipDefsText = Get-FileTextOrEmpty $chipDefsPath
$visualPresetDefsText = Get-FileTextOrEmpty $visualPresetDefsPath

$moduleDeclaration = Get-ClassDeclaration $moduleSourceText 'TrackingModule'
$moduleDefBlock = Get-ModuleDefBlock $moduleDefsText 'BDP_TestRangedTrackingModule'
$singleChipBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipTracking'
$volleyChipBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipTrackingVolley'
$pathLatchVolleyVisualBlock = Get-VisualPresetBlock $visualPresetDefsText 'BDP_TestVisual_PathLatchVolley'

Assert-True (
    (Test-Path -LiteralPath $moduleDefsPath) -and
    (Test-WellFormedXml $moduleDefsPath)
) 'DevHarness RangedAttackModuleDefs_Test.xml must remain well-formed XML after adding tracking module definitions.'

Assert-True (
    (Test-Path -LiteralPath $chipDefsPath) -and
    (Test-WellFormedXml $chipDefsPath)
) 'DevHarness ThingDefs_TestChips_Combat.xml must remain well-formed XML after adding tracking test chips.'

Assert-True (
    (Test-Path -LiteralPath $visualPresetDefsPath) -and
    (Test-WellFormedXml $visualPresetDefsPath)
) 'DevHarness ExpressionVisualPresetDefs_Test.xml must remain well-formed XML after adding tracking visual reuse.'

Assert-True (
    (Test-Path -LiteralPath $moduleSourcePath) -and
    ($moduleDeclaration -ne $null) -and
    ($moduleDeclaration -match 'IRangedAttackModuleRuntime') -and
    ($moduleDeclaration -match 'IProjectileInitStageModule') -and
    ($moduleDeclaration -match 'IArrivalStageModule') -and
    ($moduleDeclaration -match 'IHitStageModule') -and
    ($moduleDeclaration -notmatch 'IAimStageModule') -and
    ($moduleDeclaration -notmatch 'IPrepareStageModule') -and
    ($moduleDeclaration -notmatch 'IFireStageModule') -and
    ($moduleDeclaration -notmatch 'IFlightStageModule') -and
    ($moduleDeclaration -notmatch 'IImpactStageModule')
) 'DevHarness must provide TrackingModule.cs with only runtime, projectile init, arrival, and hit stage interfaces.'

Assert-True (
    (Test-Path -LiteralPath $configSourcePath) -and
    ($configSourceText -match 'class\s+TrackingModuleConfig') -and
    ($configSourceText -match '\bMaxRelocks\b') -and
    ($configSourceText -match '\bHitWindow\b') -and
    ($configSourceText -match '\bRelockWindow\b') -and
    ($configSourceText -match '\bMinClosingDistance\b') -and
    ($configSourceText -match '\bMaxTurnAngleWhenEvading\b') -and
    ($configSourceText -match '\bStaticTargetAngleBypassDistance\b') -and
    ($configSourceText -match '\bPredictionLeadRatio\b') -and
    ($configSourceText -match '\bCurveArcStrength\b') -and
    ($configSourceText -match '\bInertiaWeight\b') -and
    ($configSourceText -match '\bCaptureWeight\b') -and
    ($configSourceText -match '\bTerminalFlyAwayDistance\b') -and
    ($configSourceText -match '\bPerEmitVariance\b') -and
    ($configSourceText -match 'override\s+RangedModuleConfigNode\s+Clone\s*\(') -and
    ($configSourceText -match '\bCloneTyped\s*\(')
) 'DevHarness must provide TrackingModuleConfig.cs with the agreed tracking config surface and cloning entry points.'

Assert-True (
    (Test-Path -LiteralPath $stateSourcePath) -and
    ($stateSourceText -match 'class\s+TrackingModuleState') -and
    ($stateSourceText -match '\bTrackingPhase\b') -and
    ($stateSourceText -match '\bLockedTarget\b') -and
    ($stateSourceText -match '\bPhase\b') -and
    ($stateSourceText -match '\bRelocksUsed\b') -and
    ($stateSourceText -match '\bLastObservedTargetPos\b') -and
    ($stateSourceText -match '\bLastDistanceToTarget\b') -and
    ($stateSourceText -match '\bSeed\b') -and
    ($stateSourceText -match '\bFlyAwayIssued\b') -and
    ($stateSourceText -match '\bFlyAwayEnd\b') -and
    ($stateSourceText -match '\bClone\s*\(') -and
    ($stateSourceText -match '\bExposeData\s*\(')
) 'DevHarness must provide TrackingModuleState.cs with the agreed tracking state payload, phase enum, clone, and persistence surface.'

Assert-True (
    (Test-Path -LiteralPath $pathBuilderSourcePath) -and
    ($pathBuilderSourceText -match 'class\s+TrackingPathBuilder') -and
    ($pathBuilderSourceText -match '\bTryResolveTargetPosition\s*\(') -and
    ($pathBuilderSourceText -match '\bComputeDistanceToTarget\s*\(') -and
    ($pathBuilderSourceText -match '\bComputeTurnAngleToTarget\s*\(') -and
    ($pathBuilderSourceText -match '\bComputePredictedTargetPosition\s*\(') -and
    ($pathBuilderSourceText -match '\bBuildTrackingPath\s*\(') -and
    ($pathBuilderSourceText -match '\bBuildFlyAwayPath\s*\(') -and
    ($pathBuilderSourceText -match 'ProjectileFlightPathUtility\.CreateLinear') -and
    ($pathBuilderSourceText -match 'ProjectileFlightPathUtility\.CreateCubicBezier')
) 'DevHarness must provide TrackingPathBuilder.cs with the agreed helper surface and only use the shared linear/bezier path utilities.'

Assert-True (
    ($moduleSourceText -match '\bHitCheckPending\b') -and
    ($moduleSourceText -match '\bFlyAway\b') -and
    ($moduleSourceText -match 'HasOverrideContinueFlight') -and
    ($moduleSourceText -match 'OverrideContinueFlight\s*=\s*false') -and
    ($moduleSourceText -match 'OverrideContinueFlight\s*=\s*true') -and
    ($moduleSourceText -match 'NextFlightPathSnapshot') -and
    ($moduleSourceText -match 'BuildTrackingPath') -and
    ($moduleSourceText -match 'BuildFlyAwayPath')
) 'TrackingModule.cs must contain the arrival-stage hit, relock, and fly-away arbitration wiring.'

Assert-True (
    ($moduleSourceText -match 'HasOverrideHitThing') -and
    ($moduleSourceText -match 'OverrideHitThing\s*=\s*null') -and
    ($moduleSourceText -match 'ForceGround\s*=\s*true')
) 'TrackingModule.cs must contain the final hit review override path.'

Assert-True (
    ($moduleDefBlock -ne $null) -and
    ($moduleDefBlock -match '<runtimeClass>BDP\.DevHarness\.RangedModules\.Samples\.TrackingModule</runtimeClass>') -and
    ($moduleDefBlock -match '<defaultConfig Class="BDP\.DevHarness\.RangedModules\.Samples\.TrackingModuleConfig">')
) 'DevHarness must define BDP_TestRangedTrackingModule and bind it to TrackingModule plus TrackingModuleConfig.'

Assert-True (
    ($singleChipBlock -ne $null) -and
    ($singleChipBlock -match '<defaultProjectile>BDP_TestBulletSemantic</defaultProjectile>') -and
    ($singleChipBlock -match '<moduleDef>BDP_TestRangedTrackingModule</moduleDef>')
) 'DevHarness must define BDP_TestChipTracking and mount the tracking module on the shared ranged test projectile.'

Assert-True (
    ($volleyChipBlock -ne $null) -and
    ($volleyChipBlock -match '<defaultProjectile>BDP_TestBulletSemantic</defaultProjectile>') -and
    ($volleyChipBlock -match '<Presentation>\s*<VisualPresetDefName>BDP_TestVisual_PathLatchVolley</VisualPresetDefName>\s*<ForceSuppressHostEquipment>true</ForceSuppressHostEquipment>\s*<VisualPriority>20</VisualPriority>\s*</Presentation>') -and
    ($volleyChipBlock -match '<moduleDef>BDP_TestRangedPathLatchModule</moduleDef>') -and
    ($volleyChipBlock -match '<moduleDef>BDP_TestRangedTrackingModule</moduleDef>') -and
    ($volleyChipBlock -match '<DirectTargetLineOfSight>NotRequired</DirectTargetLineOfSight>') -and
    ($volleyChipBlock -match '<burstShotCount>5</burstShotCount>') -and
    ($volleyChipBlock -match '<Rhythm>Simultaneous</Rhythm>')
) 'DevHarness must define BDP_TestChipTrackingVolley with path-latch navigation, viper volley visual preset, NotRequired direct-target LOS, simultaneous volley rhythm, and tracking mounted.'

Assert-True (
    ($pathLatchVolleyVisualBlock -ne $null) -and
    ($pathLatchVolleyVisualBlock -match '<texPath>Things/Trigger/Chip/viper_salvo</texPath>') -and
    ($pathLatchVolleyVisualBlock -match '<MuzzleOffset>\(0, 0, 0\.68\)</MuzzleOffset>') -and
    ($pathLatchVolleyVisualBlock -notmatch '<HasSubHandMuzzleOffsetOverride>') -and
    ($pathLatchVolleyVisualBlock -notmatch '<SubHandMuzzleOffsetOverride>')
) 'BDP_TestVisual_PathLatchVolley（毒蛇齐射视觉预设）应保留贴图和枪口前向距离，主副侧均不添加横向偏移。'

Write-Output 'DevHarnessTrackingRangedModuleSmokeTests PASS'
