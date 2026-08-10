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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'

$loadoutConfigPath = Join-Path $coreRoot 'Chips\Config\ChipLoadoutConfig.cs'
$loadoutContractPath = Join-Path $coreRoot 'Chips\Contract\ChipLoadoutContract.cs'
$expressionConfigPath = Join-Path $coreRoot 'Expressions\Config\ChipExpressionConfig.cs'
$modeConfigPath = Join-Path $coreRoot 'Expressions\Config\ChipExpressionModeConfig.cs'
$expressionContractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionContract.cs'
$modeContractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionModeContract.cs'
$interpreterPath = Join-Path $coreRoot 'Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$structureValidationPath = Join-Path $coreRoot 'Expressions\Validation\ChipExpressionStructureValidation.cs'

$loadoutConfigText = Get-Content -LiteralPath $loadoutConfigPath -Raw -Encoding utf8
$loadoutContractText = Get-Content -LiteralPath $loadoutContractPath -Raw -Encoding utf8
$expressionConfigText = Get-Content -LiteralPath $expressionConfigPath -Raw -Encoding utf8
$modeConfigText = Get-Content -LiteralPath $modeConfigPath -Raw -Encoding utf8
$expressionContractText = Get-Content -LiteralPath $expressionContractPath -Raw -Encoding utf8
$modeContractText = Get-Content -LiteralPath $modeContractPath -Raw -Encoding utf8
$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$structureValidationText = if (Test-Path -LiteralPath $structureValidationPath) {
    Get-Content -LiteralPath $structureValidationPath -Raw -Encoding utf8
} else {
    ''
}

Assert-True (
    ($loadoutConfigText -notmatch '\bInitialModeKey\b') -and
    ($loadoutContractText -notmatch '\bInitialModeKey\b')
) 'Loadout config and contract must stop carrying InitialModeKey.'

Assert-True (
    ($expressionConfigText -match '\bstring\s+DefaultModeKey\b') -and
    ($expressionContractText -match '\bstring\s+DefaultModeKey\b')
) 'Expression config and contract must own DefaultModeKey.'

Assert-True (
    ($modeConfigText -match 'List<string>\s+ActiveEntryIds') -and
    ($modeContractText -match 'List<string>\s+ActiveEntryIds') -and
    ($modeConfigText -notmatch '\bOperations\b') -and
    ($modeContractText -notmatch '\bOperations\b')
) 'Mode config and contract must select entries through ActiveEntryIds only.'

Assert-True (
    ($modeConfigText -match 'string\s+DisplayLabel') -and
    ($modeConfigText -match 'string\s+GizmoIconTexPath') -and
    ($modeContractText -match 'string\s+DisplayLabel') -and
    ($modeContractText -match 'string\s+GizmoIconTexPath')
) 'Mode config and contract must carry required player label and optional gizmo icon.'

$removedOperationFiles = @(
    (Join-Path $coreRoot 'Expressions\Config\ChipExpressionModeOperationConfig.cs'),
    (Join-Path $coreRoot 'Expressions\Config\ChipExpressionModeOperationKindConfig.cs'),
    (Join-Path $coreRoot 'Expressions\Contract\ChipExpressionModeOperationContract.cs'),
    (Join-Path $coreRoot 'Expressions\Contract\ChipExpressionModeOperationKind.cs')
)
foreach ($removedOperationFile in $removedOperationFiles) {
    Assert-True (-not (Test-Path -LiteralPath $removedOperationFile)) `
        ("Legacy mode operation file must be removed: " + $removedOperationFile)
}

Assert-True (
    ($interpreterText -notmatch 'ChipExpressionModeOperation') -and
    ($interpreterText -notmatch '\.Operations\b') -and
    ($interpreterText -notmatch '\bTargetEntryId\b') -and
    ($validatorText -notmatch 'ChipExpressionModeOperation') -and
    ($validatorText -notmatch '\.Operations\b')
) 'Interpreter and definition validator must stop consuming the legacy operation schema.'

Assert-True (
    (Test-Path -LiteralPath $structureValidationPath) -and
    ($structureValidationText -match 'ChipExpressionStructureValidator') -and
    ($interpreterText -match 'ChipExpressionStructureValidator\.Validate') -and
    ($validatorText -match 'ChipExpressionStructureValidator\.Validate')
) 'Definition validation and runtime interpretation must share one expression structure rule set.'

$versionRoots = @(
    (Join-Path $repoRoot '1.6'),
    (Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6')
)
$versionXmlText = ($versionRoots | ForEach-Object {
    Get-ChildItem -LiteralPath $_ -Recurse -Filter '*.xml' | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
    }
}) -join "`n"

Assert-True (
    ($versionXmlText -notmatch '<InitialModeKey>') -and
    ($versionXmlText -notmatch '<Operations>')
) 'Current version XML must not use the removed initial-mode or operation authoring schema.'

Write-Output 'ChipExpressionModeDefinitionBoundarySmokeTests PASS'
