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

$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$hostStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\HostState.cs'
$snapshotStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotState.cs'
$snapshotPolicyPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotPolicy.cs'
$snapshotServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotService.cs'
$snapshotConfigDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotConfigDef.cs'
$snapshotConfigXmlPath = Join-Path $repoRoot '1.6\Defs\CombatBodyDef\Config.xml'
$hediffRecordPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotHediffRecord.cs'
$needRecordPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotNeedRecord.cs'
$activeHediffPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\Hediff_BdpCombatBodyActive.cs'
$activeHediffXmlPath = Join-Path $repoRoot '1.6\Defs\HediffDef\CombatBody.xml'

Assert-True -Condition (Test-Path -LiteralPath $hostStatePath) -Message 'HostState must exist.'
Assert-True -Condition ((Test-Path -LiteralPath $snapshotStatePath) -and (Test-Path -LiteralPath $snapshotPolicyPath) -and (Test-Path -LiteralPath $snapshotServicePath)) -Message 'CombatBodySnapshot state, policy, and service must exist.'
Assert-True -Condition ((Test-Path -LiteralPath $snapshotConfigDefPath) -and (Test-Path -LiteralPath $snapshotConfigXmlPath)) -Message 'CombatBodySnapshot config def and xml must exist.'
Assert-True -Condition ((Test-Path -LiteralPath $hediffRecordPath) -and (Test-Path -LiteralPath $needRecordPath)) -Message 'CombatBodySnapshot hediff and need record types must exist.'
Assert-True -Condition ((Test-Path -LiteralPath $activeHediffPath) -and (Test-Path -LiteralPath $activeHediffXmlPath)) -Message 'CombatBody active hediff code and xml must exist.'

$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$snapshotPolicyText = Get-Content -LiteralPath $snapshotPolicyPath -Raw -Encoding utf8
$snapshotServiceText = Get-Content -LiteralPath $snapshotServicePath -Raw -Encoding utf8
$snapshotStateText = Get-Content -LiteralPath $snapshotStatePath -Raw -Encoding utf8
$hediffRecordText = Get-Content -LiteralPath $hediffRecordPath -Raw -Encoding utf8
$needRecordText = Get-Content -LiteralPath $needRecordPath -Raw -Encoding utf8
$activeHediffText = Get-Content -LiteralPath $activeHediffPath -Raw -Encoding utf8
$activeHediffXmlText = Get-Content -LiteralPath $activeHediffXmlPath -Raw -Encoding utf8

Assert-True -Condition ($hostText -match 'private HostState hostState;') -Message 'CompCombatBodyHost must hold HostState.'
Assert-True -Condition ($hostText -match 'private CombatBodySnapshotPolicy snapshotPolicy;') -Message 'CompCombatBodyHost must hold CombatBodySnapshotPolicy.'
Assert-True -Condition ($hostText -match 'private CombatBodySnapshotService snapshotService;') -Message 'CompCombatBodyHost must hold CombatBodySnapshotService.'
Assert-True -Condition ($hostText -match 'Scribe_Deep\.Look\(ref hostState, "hostState"\);') -Message 'CompCombatBodyHost must persist HostState.'
Assert-True -Condition ($hostText -match 'hostState\.EnsureFrontState\(this\);') -Message 'CompCombatBodyHost must ensure front state.'
Assert-True -Condition ($hostText -match 'hostState\.FrontState\?\.GetChildHolders\(outChildren\);') -Message 'CompCombatBodyHost must append front state child holders.'
Assert-True -Condition ($bridgeText -match 'snapshotService') -Message 'PawnCombatBodyBridge must depend on snapshotService.'
Assert-True -Condition ($bridgeText -match 'ApplyCombatBodyTransformation\(\)[\s\S]*Capture') -Message 'PawnCombatBodyBridge.ApplyCombatBodyTransformation() must capture snapshot first.'
Assert-True -Condition ($bridgeText -match 'ApplyCombatBodyTransformation\(\)[\s\S]*RemoveCombatBodyEntryHediffs\(\)') -Message 'PawnCombatBodyBridge.ApplyCombatBodyTransformation() must clear original non-excluded hediffs.'
Assert-True -Condition ($bridgeText -match 'ApplyCombatBodyTransformation\(\)[\s\S]*AddCombatBodyActiveHediff\(\)') -Message 'PawnCombatBodyBridge.ApplyCombatBodyTransformation() must add BDP_CombatBodyActive.'
Assert-True -Condition ($bridgeText -match 'RestoreFromCombatBody\(\)[\s\S]*Restore') -Message 'PawnCombatBodyBridge.RestoreFromCombatBody() must restore via snapshotService.'
Assert-True -Condition ($bridgeText -match 'RestoreFromCombatBody\(\)[\s\S]*if \(!HasValidRollbackSnapshot\(\)\)[\s\S]*RestoreInvalidLegacyCombatBody\(\)[\s\S]*return;[\s\S]*RestoreFrontReplacement\(hostState\.FrontState\)[\s\S]*snapshotService\?\.Restore\(Pawn, hostState\)') -Message 'PawnCombatBodyBridge.RestoreFromCombatBody() must reserve full host rollback for a valid saved snapshot.'
Assert-True -Condition ($bridgeText -match 'RestoreFromCombatBody\(\)[\s\S]*if \(!HasValidRollbackSnapshot\(\)\)[\s\S]*return;[\s\S]*ExtinguishFire\(\)[\s\S]*RemoveCombatBodyEntryHediffs\(\)[\s\S]*RemoveCombatBodyActiveHediff\(\)[\s\S]*CopyHediffBaselineForFinalCleanup\(\)[\s\S]*RestoreFrontReplacement\([\s\S]*Restore\([\s\S]*FinalCleanupResidualHediffs\(restoredHediffBaseline\)') -Message 'PawnCombatBodyBridge.RestoreFromCombatBody() must keep full cleanup order behind the valid-snapshot guard.'
Assert-True -Condition ($snapshotPolicyText -match 'DefDatabase<CombatBodySnapshotConfigDef>\.AllDefsListForReading') -Message 'CombatBodySnapshotPolicy must read config defs from DefDatabase.'
Assert-True -Condition ($snapshotPolicyText -match 'public bool IsExcluded\(Hediff hediff\)') -Message 'CombatBodySnapshotPolicy must expose formal exclusion judgement.'
Assert-True -Condition ($snapshotServiceText -match 'policy\.GetExcludedHediffDefNames\(\)') -Message 'CombatBodySnapshotService must consume policy config output during capture.'
Assert-True -Condition ($snapshotStateText -match 'ThingOwner<Apparel> originalApparelContainer;') -Message 'CombatBodySnapshotState must hold original apparel container.'
Assert-True -Condition ($snapshotStateText -match 'ThingOwner<Thing> originalInventoryContainer;') -Message 'CombatBodySnapshotState must hold original inventory container.'
Assert-True -Condition ($snapshotStateText -match 'List<CombatBodySnapshotHediffRecord> hediffSnapshots') -Message 'CombatBodySnapshotState must hold hediff snapshots.'
Assert-True -Condition ($snapshotStateText -match 'List<CombatBodySnapshotNeedRecord> needSnapshots') -Message 'CombatBodySnapshotState must hold need snapshots.'
Assert-True -Condition ($snapshotStateText -match 'public void ClearSessionContainers\(\)') -Message 'CombatBodySnapshotState must expose session container reset.'
Assert-True -Condition ($snapshotServiceText -match 'CaptureApparel\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must capture apparel through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'CaptureInventory\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must capture inventory through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'CaptureHediffs\(pawn, hostState\.SnapshotState\)[\s\S]*CaptureNeeds\(pawn, hostState\.SnapshotState\)[\s\S]*CaptureApparel\(pawn, hostState\.SnapshotState\)[\s\S]*CaptureInventory\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService capture order must be hediffs -> needs -> apparel -> inventory.'
Assert-True -Condition ($snapshotServiceText -match 'ResetSessionContainersForCapture\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must reset session containers before a new capture.'
Assert-True -Condition ($snapshotServiceText -match 'RestoreApparel\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must restore apparel through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'RestoreInventory\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must restore inventory through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'pawn\.apparel\.WornApparel\.Contains\(apparel\)') -Message 'CombatBodySnapshotService must guard already-worn apparel during restore.'
Assert-True -Condition ($snapshotServiceText -match 'CaptureNeeds\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must capture needs through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'CaptureHediffs\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must capture hediffs through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'RestoreNeeds\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must restore needs through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'RestoreHediffs\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService must restore hediffs through a dedicated helper.'
Assert-True -Condition ($snapshotServiceText -match 'item\.holdingOwner == pawn\.inventory\.innerContainer') -Message 'CombatBodySnapshotService must guard already-owned inventory items during restore.'
Assert-True -Condition ($snapshotServiceText -match 'RestoreApparel\(pawn, hostState\.SnapshotState\)[\s\S]*RestoreInventory\(pawn, hostState\.SnapshotState\)[\s\S]*RestoreNeeds\(pawn, hostState\.SnapshotState\)[\s\S]*RestoreHediffs\(pawn, hostState\.SnapshotState\)') -Message 'CombatBodySnapshotService restore order must be apparel -> inventory -> needs -> hediffs.'
Assert-True -Condition ($hediffRecordText -match 'public string defName;') -Message 'CombatBodySnapshotHediffRecord must carry defName.'
Assert-True -Condition ($hediffRecordText -match 'public string lastInjuryDefName;') -Message 'CombatBodySnapshotHediffRecord must carry lastInjuryDefName.'
Assert-True -Condition ($needRecordText -match 'public string needDefName;') -Message 'CombatBodySnapshotNeedRecord must carry needDefName.'
Assert-True -Condition ($needRecordText -match 'public float curLevel;') -Message 'CombatBodySnapshotNeedRecord must carry curLevel.'
Assert-True -Condition ($activeHediffText -match 'class Hediff_BdpCombatBodyActive') -Message 'Active hediff code must define Hediff_BdpCombatBodyActive.'
Assert-True -Condition ($activeHediffXmlText -match '<defName>BDP_CombatBodyActive</defName>') -Message 'Active hediff xml must define BDP_CombatBodyActive.'
Assert-True -Condition ($activeHediffXmlText -match '<preventsDeath>true</preventsDeath>') -Message 'Active hediff xml must preserve preventsDeath.'
Assert-True -Condition ($serviceText -notmatch 'snapshotService') -Message 'CombatBodySessionService must not hold snapshotService.'
Assert-True -Condition ($serviceText -notmatch 'SnapshotState') -Message 'CombatBodySessionService must not read or write snapshot state.'
Assert-True -Condition ($serviceText -notmatch 'FrontState') -Message 'CombatBodySessionService must not read or write front state.'
Assert-True -Condition ($serviceText -notmatch 'CachedCollapseEmergencyEscape|CombatBodyEmergencyEscapeResolution') -Message 'CombatBodySessionService must not touch business-specific collapse state.'

Write-Output 'CombatBodyHostBridgeSnapshot PASS'
