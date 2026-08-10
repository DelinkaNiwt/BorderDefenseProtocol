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
$genePath = Join-Path $repoRoot 'Source\BDP\Core\Genes\Gene_TrionGland.cs'
$statDefOfPath = Join-Path $repoRoot 'Source\BDP\Core\Genes\TrionStatDefOf.cs'
$statDefPath = Join-Path $repoRoot '1.6\Defs\Stats\Trion\StatDefs_Trion.xml'
$compTrionPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'
$manualCommandPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\Command_BdpManualEntryTarget.cs'
$manualResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$manualProjectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultManualEntryProjector.cs'
$singleSideBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\SingleSideExpressionBuilder.cs'
$formalResultPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$combatBodySessionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$injectorPath = Join-Path $repoRoot 'Source\BDP\Core\Bootstrap\Injectors\PawnTrionCompInjector.cs'
$mainGeneDefPath = Join-Path $repoRoot '1.6\Defs\Genes\Trion\GeneDefs_TrionGland.xml'

Assert-True (
    Test-Path -LiteralPath $genePath
) 'Mainline BDP must define Gene_TrionGland as the Pawn-side Trion identity carrier.'

Assert-True (
    Test-Path -LiteralPath $statDefOfPath
) 'Mainline BDP must define TrionStatDefOf for Trion gene-backed stats.'

Assert-True (
    Test-Path -LiteralPath $mainGeneDefPath
) 'Mainline BDP must ship the formal Trion gland GeneDef.'

Assert-True (
    Test-Path -LiteralPath $statDefPath
) 'Mainline BDP must ship Stats_Trion.xml.'

Assert-True (
    Test-Path -LiteralPath $combatBodySessionPath
) 'CombatBodySessionService must continue to exist as the three-system coordinator.'

$geneText = Get-Content -LiteralPath $genePath -Raw -Encoding utf8
$statDefOfText = Get-Content -LiteralPath $statDefOfPath -Raw -Encoding utf8
$geneDefText = Get-Content -LiteralPath $mainGeneDefPath -Raw -Encoding utf8
$statDefText = Get-Content -LiteralPath $statDefPath -Raw -Encoding utf8
$compTrionText = Get-Content -LiteralPath $compTrionPath -Raw -Encoding utf8
$manualCommandText = Get-Content -LiteralPath $manualCommandPath -Raw -Encoding utf8
$manualResolverText = Get-Content -LiteralPath $manualResolverPath -Raw -Encoding utf8
$manualProjectorText = Get-Content -LiteralPath $manualProjectorPath -Raw -Encoding utf8
$singleSideBuilderText = Get-Content -LiteralPath $singleSideBuilderPath -Raw -Encoding utf8
$formalResultText = Get-Content -LiteralPath $formalResultPath -Raw -Encoding utf8
$injectorText = Get-Content -LiteralPath $injectorPath -Raw -Encoding utf8

Assert-True (
    $geneText -match 'public override IEnumerable<Gizmo> GetGizmos\(\)'
) 'Gene_TrionGland must carry the Pawn-side Trion GUI entry.'

Assert-True (
    $geneText -match 'public override void PostAdd\(\)'
) 'Gene_TrionGland must cooperate with Trion refresh when added.'

Assert-True (
    $geneText -match 'public override void PostRemove\(\)'
) 'Gene_TrionGland must cooperate with Trion refresh when removed.'

Assert-True (
    $geneText -notmatch 'Snapshot|Orchestrator|ActivateCombatBody|DeactivateCombatBody|Rollback|Emergency'
) 'New Gene_TrionGland must keep only true gene responsibilities and must not regress into the old God Object.'

Assert-True (
    $statDefOfText -match 'public static StatDef BDP_TrionCapacity;'
) 'TrionStatDefOf must expose BDP_TrionCapacity.'

Assert-True (
    $statDefOfText -match 'public static StatDef BDP_TrionRecoveryRate;'
) 'TrionStatDefOf must expose BDP_TrionRecoveryRate.'

Assert-True (
    ($geneDefText -match '<defName>BDP_Gene_TrionGland</defName>') -and
    ($geneDefText -match '<geneClass>BDP\.Core\.Genes\.Gene_TrionGland</geneClass>')
) 'GeneDef_TrionGland.xml must bind BDP_Gene_TrionGland to Gene_TrionGland.'

Assert-True (
    ($geneDefText -notmatch '<BDP_TrionCapacity>') -and
    ($geneDefText -match '<BDP_TrionRecoveryRate>500</BDP_TrionRecoveryRate>')
) 'The formal gland must unlock dynamic capacity and contribute only the fixed recovery stat.'

Assert-True (
    ($statDefText -match '<defName>BDP_TrionCapacity</defName>') -and
    ($statDefText -match '<defName>BDP_TrionRecoveryRate</defName>')
) 'Stats_Trion.xml must define the Trion capacity and recovery stats.'

Assert-True (
    $compTrionText -notmatch 'Gene_TrionGland|CombatBodySessionService|CompTriggerBody|CompCombatBodyHost'
) 'CompTrion must remain independent from Gene, CombatBodySession, Trigger, and CombatBody internals.'

Assert-True (
    ($injectorText -match 'baseMax = 0f') -and
    ($injectorText -notmatch 'baseMax = 100f')
) 'PawnTrionCompInjector must stop injecting a pre-activated 100-capacity pawn Trion host.'

Assert-True (
    ($manualCommandText -match 'groupKey\s*=') -and
    ($manualCommandText -match 'GenText\.StableStringHash')
) 'Command_BdpManualEntryTarget must group by a stable manual-entry key instead of only relying on label/icon coincidence.'

Assert-True (
    ($manualCommandText -match 'public override void ProcessGroupInput\(Event ev, List<Gizmo> group\)') -and
    ($manualCommandText -match 'BeginTargeting')
) 'Command_BdpManualEntryTarget must launch targeting through ProcessGroupInput for grouped manual entries.'

Assert-True (
    $manualCommandText -match 'public override void ProcessInput\(Event ev\)\s*\{\s*base\.ProcessInput\(ev\);\s*\}'
) 'Command_BdpManualEntryTarget.ProcessInput must stop directly starting targeting before group resolution.'

Assert-True (
    $manualResolverText -match 'group\.GroupId'
) 'DefaultManualEntryGizmoResolver must pass the manual-entry group identity into Command_BdpManualEntryTarget.'

Assert-True (
    $formalResultText -match 'ManualEntryAggregationKey'
) 'FormalExpressionResult must publish a stable manual-entry aggregation key.'

Assert-True (
    ($singleSideBuilderText -match 'ManualEntryAggregationKey =') -and
    ($singleSideBuilderText -match 'ReasonKey')
) 'SingleSideExpressionBuilder must derive manual-entry aggregation identity from declaration-level semantics rather than per-chip result.Id.'

Assert-True (
    ($manualProjectorText -match 'result\.ManualEntryAggregationKey') -and
    ($manualProjectorText -notmatch 'GroupId = result\.Id \+ ":group"')
) 'DefaultManualEntryProjector must stop deriving grouped manual-entry identity from per-chip result.Id.'

Write-Output 'TrionGeneGuiContracts PASS'

