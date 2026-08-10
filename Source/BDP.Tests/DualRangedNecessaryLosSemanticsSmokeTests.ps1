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
$modRoot = Split-Path -Parent $repoRoot

$bdpCoreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$devHarnessRoot = Join-Path $modRoot 'BorderDefenseProtocol.DevHarness'

$configEnumPath = Join-Path $bdpCoreRoot 'Expressions\Config\DirectTargetLineOfSightRequirementConfig.cs'
$entryConfigPath = Join-Path $bdpCoreRoot 'Expressions\Config\ChipExpressionEntryConfig.cs'
$comboEntryConfigPath = Join-Path $bdpCoreRoot 'Combos\Config\ComboExpressionEntryConfig.cs'
$specPath = Join-Path $bdpCoreRoot 'Expressions\Model\ResolvedVerbSpec.cs'
$factoryPath = Join-Path $bdpCoreRoot 'Expressions\Pipeline\ResolvedVerbSpecFactory.cs'
$interpreterPath = Join-Path $bdpCoreRoot 'Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$stagesPath = Join-Path $bdpCoreRoot 'AttackExecution\AttackExecutionService.Stages.cs'
$targetingSourcePath = Join-Path $bdpCoreRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$devHarnessCombatXmlPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'

$configEnumText = if (Test-Path -LiteralPath $configEnumPath) { Read-Source $configEnumPath } else { '' }
$entryConfigText = Read-Source $entryConfigPath
$comboEntryConfigText = Read-Source $comboEntryConfigPath
$specText = Read-Source $specPath
$factoryText = Read-Source $factoryPath
$interpreterText = Read-Source $interpreterPath
$stagesText = Read-Source $stagesPath
$targetingSourceText = Read-Source $targetingSourcePath
$devHarnessCombatXmlText = Read-Source $devHarnessCombatXmlPath

Assert-True (
    ($configEnumText -match 'enum\s+DirectTargetLineOfSightRequirementConfig') -and
    ($configEnumText -match 'FromVerb') -and
    ($configEnumText -match 'Required') -and
    ($configEnumText -match 'NotRequired')
) 'A dedicated direct-target LOS requirement config enum must exist.'

Assert-True (
    ($entryConfigText -match 'DirectTargetLineOfSightRequirementConfig\s+DirectTargetLineOfSight') -and
    ($comboEntryConfigText -match 'DirectTargetLineOfSightRequirementConfig\s+DirectTargetLineOfSight') -and
    ($comboEntryConfigText -match 'DirectTargetLineOfSight = DirectTargetLineOfSight')
) 'Chip and combo expression entries must expose the necessary direct-target LOS policy.'

Assert-True (
    $specText -match 'public bool RequiresDirectTargetLineOfSight'
) 'ResolvedVerbSpec must carry the necessary shooter-to-semantic-target LOS truth separately from RequireLineOfSight.'

Assert-True (
    ($factoryText -match 'ResolveDirectTargetLineOfSightRequirement') -and
    ($factoryText -match 'RequiresDirectTargetLineOfSight = ResolveDirectTargetLineOfSightRequirement') -and
    ($factoryText -match 'DirectTargetLineOfSightRequirementConfig\.NotRequired')
) 'ResolvedVerbSpecFactory must normalize the dedicated necessary LOS truth.'

Assert-True (
    $interpreterText -match 'config\.DirectTargetLineOfSight'
) 'Expression contract interpretation must feed the necessary LOS policy into ResolvedVerbSpec.'

Assert-True (
    ($stagesText -match 'resolvedSpec\.RequiresDirectTargetLineOfSight') -and
    ($stagesText -notmatch 'resolvedSpec\.RequireLineOfSight\)\s*\{\s*Pawn pawn = request')
) 'Dual execution pruning must use necessary direct-target LOS truth, not generic RequireLineOfSight.'

Assert-True (
    ($targetingSourceText -match 'resolvedSpec\.RequiresDirectTargetLineOfSight') -and
    ($targetingSourceText -notmatch 'resolvedSpec\.RequireLineOfSight\)\s*\{\s*return sourceVerb\.CanHitTarget')
) 'Manual dual target legality must use necessary direct-target LOS truth.'

Assert-True (
    ($devHarnessCombatXmlText -match '<Id>test_ranged_volley_primary</Id>(?s).*?<DirectTargetLineOfSight>Required</DirectTargetLineOfSight>') -and
    ($devHarnessCombatXmlText -match '<Id>test_path_latch_primary</Id>(?s).*?<DirectTargetLineOfSight>NotRequired</DirectTargetLineOfSight>')
) 'DevHarness volley must declare necessary direct LOS while path-latch must declare it not necessary.'

Write-Output 'DualRangedNecessaryLosSemanticsSmokeTests PASS'
