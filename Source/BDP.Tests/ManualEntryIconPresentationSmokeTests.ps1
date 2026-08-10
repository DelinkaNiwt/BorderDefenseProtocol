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
$devHarnessRoot = Join-Path $repoRoot '..\BorderDefenseProtocol.DevHarness\1.6\Defs\Trigger'

$presentationConfigPath = Join-Path $bdpSourceRoot 'Expressions\Config\ExpressionPresentationConfig.cs'
$compositePresentationDefPath = Join-Path $bdpSourceRoot 'Expressions\Config\ExpressionCompositePresentationDef.cs'
$sourceConfigBasePath = Join-Path $bdpSourceRoot 'Expressions\Config\ExpressionSourceConfigBase.cs'
$entryContractPath = Join-Path $bdpSourceRoot 'Expressions\Contract\ChipExpressionEntryContract.cs'
$contractInterpreterPath = Join-Path $bdpSourceRoot 'Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$declarationPath = Join-Path $bdpSourceRoot 'Expressions\Model\ExpressionSourceDeclaration.cs'
$materialPath = Join-Path $bdpSourceRoot 'Expressions\Model\ExpressionSourceMaterial.cs'
$resultPath = Join-Path $bdpSourceRoot 'Expressions\Model\FormalExpressionResult.cs'
$groupPath = Join-Path $bdpSourceRoot 'Expressions\Model\ManualEntryProjectionGroup.cs'
$itemPath = Join-Path $bdpSourceRoot 'Expressions\Model\ManualEntryProjectionItem.cs'
$sourceProviderPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs'
$snapshotBuilderPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$projectorPath = Join-Path $bdpSourceRoot 'Expressions\Projection\DefaultManualEntryProjector.cs'
$resolverPath = Join-Path $bdpSourceRoot 'Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$comboDefsPath = Join-Path $devHarnessRoot 'ComboDefs_BDP_TestCombos.xml'
$compositePresentationDefsPath = Join-Path $devHarnessRoot 'ExpressionCompositePresentationDefs_BDP_Test.xml'

$presentationConfigText = Get-Content -LiteralPath $presentationConfigPath -Raw -Encoding utf8
$compositePresentationDefText = Get-Content -LiteralPath $compositePresentationDefPath -Raw -Encoding utf8
$sourceConfigBaseText = Get-Content -LiteralPath $sourceConfigBasePath -Raw -Encoding utf8
$entryContractText = Get-Content -LiteralPath $entryContractPath -Raw -Encoding utf8
$contractInterpreterText = Get-Content -LiteralPath $contractInterpreterPath -Raw -Encoding utf8
$declarationText = Get-Content -LiteralPath $declarationPath -Raw -Encoding utf8
$materialText = Get-Content -LiteralPath $materialPath -Raw -Encoding utf8
$resultText = Get-Content -LiteralPath $resultPath -Raw -Encoding utf8
$groupText = Get-Content -LiteralPath $groupPath -Raw -Encoding utf8
$itemText = Get-Content -LiteralPath $itemPath -Raw -Encoding utf8
$sourceProviderText = Get-Content -LiteralPath $sourceProviderPath -Raw -Encoding utf8
$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding utf8
$projectorText = Get-Content -LiteralPath $projectorPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8
$comboDefsText = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8
$compositePresentationDefsText = Get-Content -LiteralPath $compositePresentationDefsPath -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $presentationConfigPath
) 'ExpressionPresentationConfig must exist as the dedicated manual-entry icon presentation block.'

Assert-True (
    ($presentationConfigText -match 'class ExpressionPresentationConfig') -and
    ($presentationConfigText -match 'public string ManualEntryIconTexPath;')
) 'ExpressionPresentationConfig must declare ManualEntryIconTexPath.'

Assert-True (
    (Test-Path -LiteralPath $compositePresentationDefPath) -and
    ($compositePresentationDefText -match 'class ExpressionCompositePresentationDef') -and
    ($compositePresentationDefText -match 'public string CompositeKind;') -and
    ($compositePresentationDefText -match 'public string ManualEntryIconTexPath;')
) 'ExpressionCompositePresentationDef must exist for composite manual-entry icon authoring.'

Assert-True (
    $sourceConfigBaseText -match 'public ExpressionPresentationConfig Presentation;'
) 'ExpressionSourceConfigBase must expose a Presentation block for author-defined entry visuals.'

Assert-True (
    ($entryContractText -match 'public string ManualEntryIconTexPath;') -and
    ($declarationText -match 'public string ManualEntryIconTexPath { get; set; }') -and
    ($materialText -match 'public string ManualEntryIconTexPath { get; set; }') -and
    ($resultText -match 'public string ManualEntryIconTexPath { get; set; }') -and
    ($groupText -match 'public string ManualEntryIconTexPath { get; set; }') -and
    ($itemText -match 'public string ManualEntryIconTexPath { get; set; }')
) 'Manual-entry icon tex path must flow through contract, declaration, material, result, group and item models.'

Assert-True (
    $contractInterpreterText -match 'ManualEntryIconTexPath = config\.Presentation != null \? config\.Presentation\.ManualEntryIconTexPath : null'
) 'DefaultChipExpressionContractInterpreter must carry the explicit icon tex path out of the config Presentation block.'

Assert-True (
    $sourceProviderText -match 'ManualEntryIconTexPath = entry\.ManualEntryIconTexPath'
) 'DefaultExpressionSourceDeclarationProvider must pass icon tex path into source declarations.'

Assert-True (
    ($snapshotBuilderText -match 'ManualEntryIconTexPath = ResolveManualEntryIconTexPath\(slot, declaration\)') -and
    ($snapshotBuilderText -match 'return declaration\.ManualEntryIconTexPath;') -and
    ($snapshotBuilderText -match 'return slot\.LoadedChip\.def\.graphicData != null \? slot\.LoadedChip\.def\.graphicData\.texPath : null;')
) 'ExpressionSnapshotBuilder must prefer explicit icon tex path and otherwise fall back to the loaded chip item texture path for single-chip entries.'

Assert-True (
    ($projectorText -match 'ManualEntryIconTexPath = result\.ManualEntryIconTexPath') -and
    ($projectorText -match 'ManualEntryIconTexPath = result\.ManualEntryIconTexPath')
) 'DefaultManualEntryProjector must stamp the resolved icon tex path onto both group and primary item projections.'

Assert-True (
    ($resolverText -match 'ResolveIconTexture') -and
    ($resolverText -match 'BuildCommandLabel') -and
    ($resolverText -match 'CompositeExpressionKind\.DualWeapon') -and
    ($resolverText -match 'DefDatabase<ExpressionCompositePresentationDef>') -and
    ($resolverText -match 'ContentFinder<Texture2D>\.Get') -and
    ($resolverText -notmatch 'pawn\.equipment\?\.Primary\?\.def\?\.uiIcon')
) 'DefaultManualEntryGizmoResolver must load configured textures, support composite icon defs, and stop falling back to the host equipment icon.'

Assert-True (
    ($targetingSourceText -match 'return context\.Verb != null \? context\.Verb\.UIIcon : null;') -and
    ($targetingSourceText -notmatch 'return pawn\?\.equipment\?\.Primary\?\.def\?\.uiIcon;')
) 'AttackExecutionTargetingSource UIIcon must read from the active formal host verb only.'

Assert-True (
    ($comboDefsText -match '<Presentation>') -and
    ($comboDefsText -match '<ManualEntryIconTexPath>')
) 'DevHarness combo sample must demonstrate the explicit Presentation.ManualEntryIconTexPath authoring path.'

Assert-True (
    ($compositePresentationDefsText -match '<BDP\.Core\.Expressions\.ExpressionCompositePresentationDef>') -and
    ($compositePresentationDefsText -match '<CompositeKind>DualWeapon</CompositeKind>') -and
    ($compositePresentationDefsText -match '<ManualEntryIconTexPath>')
) 'DevHarness must provide a composite icon def sample for DualWeapon manual-entry buttons.'

Write-Output 'ManualEntryIconPresentationSmokeTests PASS'
