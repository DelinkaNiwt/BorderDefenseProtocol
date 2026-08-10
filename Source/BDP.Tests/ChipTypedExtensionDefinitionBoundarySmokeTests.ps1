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

$extensionConfigPath = Join-Path $coreRoot 'Chips\Config\ChipExtensionConfig.cs'
$legacyConfigPath = Join-Path $coreRoot 'Chips\Config\ChipExtensionBlockConfig.cs'
$legacyContractPath = Join-Path $coreRoot 'Chips\Contract\ChipExtensionBlockContract.cs'
$definitionConfigPath = Join-Path $coreRoot 'Chips\Config\ChipDefinitionConfig.cs'
$definitionContractPath = Join-Path $coreRoot 'Chips\Contract\ChipDefinitionContract.cs'
$contractResolverPath = Join-Path $coreRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'

Assert-True (Test-Path -LiteralPath $extensionConfigPath) `
    'Core must define ChipExtensionConfig as the neutral typed-extension base.'
Assert-True (-not (Test-Path -LiteralPath $legacyConfigPath)) `
    'Legacy ChipExtensionBlockConfig must be deleted.'
Assert-True (-not (Test-Path -LiteralPath $legacyContractPath)) `
    'Legacy ChipExtensionBlockContract must be deleted.'

$extensionConfigText = Get-Content -LiteralPath $extensionConfigPath -Raw -Encoding utf8
$definitionConfigText = Get-Content -LiteralPath $definitionConfigPath -Raw -Encoding utf8
$definitionContractText = Get-Content -LiteralPath $definitionContractPath -Raw -Encoding utf8
$contractResolverText = Get-Content -LiteralPath $contractResolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$chipCoreRoot = Join-Path $coreRoot 'Chips'
$allChipCoreText = (Get-ChildItem -LiteralPath $chipCoreRoot -Recurse -Filter '*.cs' -File |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"

Assert-True (
    $extensionConfigText -match
        'public\s+abstract\s+class\s+ChipExtensionConfig'
) 'ChipExtensionConfig must be a neutral public abstract base type.'

Assert-True (
    $extensionConfigText -notmatch 'IExposable'
) 'Static chip extension config must not implement an empty save-game interface.'

Assert-True (
    $definitionConfigText -match
        'List<ChipExtensionConfig>\s+Extensions'
) 'ChipDefinitionConfig must expose a typed extension list.'

Assert-True (
    $definitionContractText -match
        'IReadOnlyList<ChipExtensionConfig>\s+Extensions'
) 'The resolved chip contract must retain typed static extension references.'

Assert-True (
    ($contractResolverText -match
        'IReadOnlyList<ChipExtensionConfig>\s+TranslateExtensions') -and
    ($contractResolverText -match
        'new\s+List<ChipExtensionConfig>\(configs\)')
) 'Contract resolution must shallow-copy the static typed extension list.'

Assert-True (
    ($validatorText -match '"ChipExtensionEntryMissing"') -and
    ($validatorText -match '"ChipExtensionTypeDuplicated"') -and
    ($validatorText -match 'HashSet<Type>')
) 'Core must reject null entries and duplicate concrete extension types.'

Assert-True (
    ($allChipCoreText -notmatch 'ChipExtensionBlockConfig') -and
    ($allChipCoreText -notmatch 'ChipExtensionBlockContract') -and
    ($allChipCoreText -notmatch '\bBlockName\b') -and
    ($allChipCoreText -notmatch '\bTargetSystem\b') -and
    ($allChipCoreText -notmatch '\bPayloadText\b')
) 'Legacy free-text chip extension declarations must be removed from Core.'

Write-Output 'ChipTypedExtensionDefinitionBoundarySmokeTests PASS'
