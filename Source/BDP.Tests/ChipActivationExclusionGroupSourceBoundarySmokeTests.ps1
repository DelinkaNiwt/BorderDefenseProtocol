$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$defPath = Join-Path $coreRoot 'Chips\Defs\ChipExclusionGroupDef.cs'
$configPath = Join-Path $coreRoot 'Chips\Config\ChipLoadoutConfig.cs'
$contractPath = Join-Path $coreRoot 'Chips\Contract\ChipLoadoutContract.cs'
$resolverPath = Join-Path $coreRoot 'Chips\Contract\ChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\ChipDefinitionValidator.cs'
$payloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'

Assert-True (Test-Path -LiteralPath $defPath) `
    'ChipExclusionGroupDef source must exist.'

$defText = Get-Content -LiteralPath $defPath -Raw -Encoding utf8
$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$contractText = Get-Content -LiteralPath $contractPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8

Assert-True (
    ($defText -match 'public\s+sealed\s+class\s+ChipExclusionGroupDef\s*:\s*Def') -and
    ($defText -notmatch 'Color|Weapon|Members|MemberDefs')
) 'ChipExclusionGroupDef must be a neutral identity-only RimWorld Def.'

Assert-True (
    $configText -match
        'List<ChipExclusionGroupDef>\s+ActivationExclusionGroups'
) 'ChipLoadoutConfig must publish strong typed activation exclusion groups.'
Assert-True (
    $contractText -match
        'IReadOnlyList<ChipExclusionGroupDef>\s+ActivationExclusionGroups'
) 'ChipLoadoutContract must carry read-only activation exclusion groups.'
Assert-True (
    ($resolverText -match 'ActivationExclusionGroups\s*=\s*config\.ActivationExclusionGroups[\s\S]*new\s+List<ChipExclusionGroupDef>') -and
    ($resolverText -match 'new\s+List<ChipExclusionGroupDef>')
) 'The contract resolver must copy Def references without converting them to strings.'
Assert-True (
    ($validatorText -match 'ActivationExclusionGroupMissing') -and
    ($validatorText -match 'ActivationExclusionGroupDuplicate')
) 'Definition validation must reject null and duplicate activation exclusion groups.'

$productionText = @(
    $configText,
    $contractText,
    $resolverText,
    $validatorText,
    $payloadText,
    $collectorText
) -join "`n"
Assert-True ($productionText -notmatch '\bExclusionTags\b') `
    'Current production sources must physically remove ExclusionTags.'
Assert-True (
    ($payloadText -notmatch 'ActivationExclusionGroups') -and
    ($collectorText -notmatch 'ActivationExclusionGroups')
) 'Expression runtime snapshots must not carry activation exclusion groups.'

Write-Output 'ChipActivationExclusionGroupSourceBoundarySmokeTests PASS'
