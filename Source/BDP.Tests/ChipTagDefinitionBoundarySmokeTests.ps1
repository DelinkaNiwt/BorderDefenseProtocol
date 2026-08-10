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
$tagDefPath = Join-Path $coreRoot 'Chips\Defs\ChipTagDef.cs'
$profileConfigPath = Join-Path $coreRoot 'Chips\Config\ChipProfileConfig.cs'
$profileContractPath = Join-Path $coreRoot 'Chips\Contract\ChipProfileContract.cs'
$resolverPath = Join-Path $coreRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$payloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'

Assert-True (Test-Path -LiteralPath $tagDefPath) 'Core must provide a neutral ChipTagDef type.'

$tagDefText = Get-Content -LiteralPath $tagDefPath -Raw -Encoding utf8
$profileConfigText = Get-Content -LiteralPath $profileConfigPath -Raw -Encoding utf8
$profileContractText = Get-Content -LiteralPath $profileContractPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8

Assert-True ($tagDefText -match 'public\s+sealed\s+class\s+ChipTagDef\s*:\s*Def') `
    'ChipTagDef must be a neutral public RimWorld Def type.'
Assert-True ($profileConfigText -match 'List<ChipTagDef>\s+Tags') `
    'ChipProfileConfig must support zero or more registered chip tags.'
Assert-True ($profileContractText -match 'IReadOnlyList<ChipTagDef>\s+Tags') `
    'ChipProfileContract must expose resolved chip tags as a read-only list.'
Assert-True ($resolverText -match 'Tags\s*=\s*config\.Tags') `
    'The chip contract resolver must copy author-declared chip tags.'
Assert-True (($validatorText -match 'ChipTagMissing') -and ($validatorText -match 'ChipTagDuplicate')) `
    'Chip validation must reject empty and duplicate tag entries.'
Assert-True ($payloadText -match 'ProfileTagDefNames') `
    'Expression source snapshots must carry chip tag DefNames.'
Assert-True ($collectorText -match 'profile\.Tags') `
    'Expression source collection must preserve chip tags.'

Write-Output 'ChipTagDefinitionBoundarySmokeTests PASS'
