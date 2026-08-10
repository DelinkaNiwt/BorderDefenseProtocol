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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$samplesRoot = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples'
$moduleDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'

$configPath = Join-Path $samplesRoot 'PathLatchConfig.cs'
$statePath = Join-Path $samplesRoot 'PathLatchState.cs'
$resolverPath = Join-Path $samplesRoot 'PathLatchSegmentResolver.cs'
$modulePath = Join-Path $samplesRoot 'PathLatchModule.cs'

$configText = Read-Source $configPath
$stateText = Read-Source $statePath
$resolverText = Read-Source $resolverPath
$moduleText = Read-Source $modulePath
$moduleDefsText = Read-Source $moduleDefsPath
$moduleDefBlock = Get-ModuleDefBlock $moduleDefsText 'BDP_TestRangedPathLatchModule'

Assert-True (
    ($configText -match 'public\s+float\s+AnchorSpreadRadius') -and
    ($configText -match 'public\s+float\s+LowSkillSpreadMultiplier') -and
    ($configText -match 'public\s+float\s+HighSkillSpreadMultiplier') -and
    ($configText -match 'AnchorSpreadRadius\s*=\s*AnchorSpreadRadius') -and
    ($configText -match 'LowSkillSpreadMultiplier\s*=\s*LowSkillSpreadMultiplier') -and
    ($configText -match 'HighSkillSpreadMultiplier\s*=\s*HighSkillSpreadMultiplier')
) 'PathLatchConfig must expose AnchorSpreadRadius plus low/high skill spread multipliers and clone them.'

Assert-True (
    ($stateText -match 'public\s+float\s+AnchorSpreadRadius') -and
    ($stateText -match 'public\s+bool\s+HasFrozenFinalDestination') -and
    ($stateText -match 'public\s+Vector3\s+FrozenFinalDestination') -and
    ($stateText -match 'public\s+PathLatchPathSource\s+PathSource') -and
    ($stateText -match 'Scribe_Values\.Look\(ref anchorSpreadRadius') -and
    ($stateText -match 'Scribe_Values\.Look\(ref hasFrozenFinalDestination') -and
    ($stateText -match 'Scribe_Values\.Look\(ref frozenFinalDestination') -and
    ($stateText -match 'Scribe_Values\.Look\(ref pathSource')
) 'PathLatchPathSnapshotContext must persist AnchorSpreadRadius, FrozenFinalDestination, and PathSource.'

Assert-True (
    ($resolverText -match 'Gen\.HashCombineInt') -and
    ($resolverText -match 'Rand\.PushState') -and
    ($resolverText -match 'Rand\.PopState') -and
    ($resolverText -match 'ProjectileFlightPathUtility\.CreateLinear') -and
    ($resolverText -match 'FrozenFinalDestination') -and
    ($resolverText -match 'AnchorSpreadRadius') -and
    ($resolverText -match 'target\.PathSource\s*=\s*snapshot\.PathSource') -and
    ($resolverText -notmatch '\(\s*float\s*\)\s*\(\s*segmentIndex\s*\+\s*1\s*\)\s*/\s*segmentCount') -and
    ($resolverText -notmatch 'segmentIndex\s*>?=\s*segmentCount\s*-\s*1\s*\?\s*1f')
) 'PathLatchSegmentResolver must carry source and build deterministic per-leg spread snapshots without progressive segment-index scaling.'

Assert-True (
    ($moduleText -match 'ResolveEffectiveAnchorSpreadRadius') -and
    ($moduleText -match 'SkillDefOf\.Shooting') -and
    ($moduleText -match 'LowSkillSpreadMultiplier') -and
    ($moduleText -match 'HighSkillSpreadMultiplier')
) 'PathLatchModule must freeze continuation spread from the pawn Shooting skill and configured multipliers.'

Assert-True (
    ($moduleText -notmatch '\bHasInitialFlightPathSnapshot\s*=') -and
    ($moduleText -notmatch '\bInitialFlightPathSnapshot\s*=')
) 'PathLatchModule ProjectileInit must not prewrite InitialFlightPathSnapshot for the first leg.'

Assert-True (
    ($moduleText -match 'HasNextFlightPathSnapshot\s*=\s*true') -and
    ($moduleText -match 'NextFlightPathSnapshot\s*=\s*nextSnapshot')
) 'PathLatchModule Arrival must provide NextFlightPathSnapshot.'

Assert-True (
    ($moduleDefBlock -ne $null) -and
    ($moduleDefBlock -match '<AnchorSpreadRadius>0\.3</AnchorSpreadRadius>') -and
    ($moduleDefBlock -match '<LowSkillSpreadMultiplier>1\.35</LowSkillSpreadMultiplier>') -and
    ($moduleDefBlock -match '<HighSkillSpreadMultiplier>0\.45</HighSkillSpreadMultiplier>')
) 'BDP_TestRangedPathLatchModule must declare AnchorSpreadRadius plus low/high skill spread multipliers in XML.'

Write-Output 'PathLatchWaypointSpreadSmokeTests PASS'
