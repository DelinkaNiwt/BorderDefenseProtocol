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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6'

$categoryDefPath = Join-Path $coreRoot 'Chips\Defs\ChipCategoryDef.cs'
$profileConfigPath = Join-Path $coreRoot 'Chips\Config\ChipProfileConfig.cs'
$profileContractPath = Join-Path $coreRoot 'Chips\Contract\ChipProfileContract.cs'
$contractResolverPath = Join-Path $coreRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$runtimePayloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$sourceCollectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'
$candidateCategoryDefsPath = Join-Path $devHarnessRoot 'Defs\Things\Items\Chips\Test\ChipCategoryDefs_Test.xml'
$candidateChipDefsRoot = Join-Path $devHarnessRoot 'Defs\Things\Items\Chips\Test'

Assert-True (Test-Path -LiteralPath $categoryDefPath) 'Core must define ChipCategoryDef as the neutral registered chip category type.'
Assert-True (Test-Path -LiteralPath $candidateCategoryDefsPath) 'DevHarness must define its isolated unreviewed-chip category.'

$categoryDefText = Get-Content -LiteralPath $categoryDefPath -Raw -Encoding utf8
$profileConfigText = Get-Content -LiteralPath $profileConfigPath -Raw -Encoding utf8
$profileContractText = Get-Content -LiteralPath $profileContractPath -Raw -Encoding utf8
$contractResolverText = Get-Content -LiteralPath $contractResolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$runtimePayloadText = Get-Content -LiteralPath $runtimePayloadPath -Raw -Encoding utf8
$sourceCollectorText = Get-Content -LiteralPath $sourceCollectorPath -Raw -Encoding utf8
$candidateCategoryDefsText = Get-Content -LiteralPath $candidateCategoryDefsPath -Raw -Encoding utf8

Assert-True (
    $categoryDefText -match 'public\s+sealed\s+class\s+ChipCategoryDef\s*:\s*Def'
) 'ChipCategoryDef must be a public RimWorld Def type without business-specific category members.'

Assert-True (
    ($profileConfigText -match 'public\s+ChipCategoryDef\s+Category\s*;') -and
    ($profileConfigText -notmatch '\bPrimaryCategory\b') -and
    ($profileConfigText -match 'List<ChipTagDef>\s+Tags')
) 'Chip profile authoring must expose one registered Category and typed ChipTagDef tags.'

Assert-True (
    ($profileContractText -match 'public\s+ChipCategoryDef\s+Category\s*;') -and
    ($profileContractText -notmatch '\bPrimaryCategory\b') -and
    ($profileContractText -match 'IReadOnlyList<ChipTagDef>\s+Tags')
) 'Resolved chip profile contracts must carry one category and typed profile tags.'

Assert-True (
    ($contractResolverText -match 'Category\s*=\s*config\.Category') -and
    ($contractResolverText -notmatch 'config\.PrimaryCategory') -and
    ($contractResolverText -match 'Tags\s*=\s*config\.Tags')
) 'Chip contract resolution must preserve the registered category and copy typed tags.'

Assert-True (
    ($validatorText -match 'profile\.Category\s*==\s*null') -and
    ($validatorText -match '"ChipCategoryMissing"') -and
    ($validatorText -notmatch '"PrimaryCategoryMissing"')
) 'Chip validation must reject a missing category Def reference with the new stable diagnostic code.'

Assert-True (
    ($runtimePayloadText -match 'ProfileCategoryDefName') -and
    ($runtimePayloadText -notmatch 'ProfilePrimaryCategory') -and
    ($runtimePayloadText -match 'ProfileTagDefNames') -and
    ($sourceCollectorText -match 'profile\.Category\.defName') -and
    ($sourceCollectorText -match 'profile\.Tags')
) 'Expression source snapshots must carry the resolved category and tag DefNames.'

Assert-True (
    ($candidateCategoryDefsText -match '<BDP\.Core\.Chips\.ChipCategoryDef>') -and
    ($candidateCategoryDefsText -match '<defName>BDP_Dev_UnreviewedChip</defName>') -and
    ($candidateCategoryDefsText -match '<label>未审定芯片</label>')
) 'DevHarness must own exactly one clearly named unreviewed category for candidate content.'

$candidateChipDefPaths = Get-ChildItem -LiteralPath $candidateChipDefsRoot -Filter '*.xml' -File
$normalChipCount = 0
foreach ($candidateChipDefPath in $candidateChipDefPaths) {
    [xml]$document = Get-Content -LiteralPath $candidateChipDefPath.FullName -Raw -Encoding utf8
    $chipDefs = $document.SelectNodes(
        "/Defs/ThingDef[modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']]")

    foreach ($chipDef in $chipDefs) {
        $defName = [string]$chipDef.defName
        $profile = $chipDef.modExtensions.li.Profile

        Assert-True (
            $null -eq $profile.PrimaryCategory
        ) "$defName must not retain the legacy PrimaryCategory field."

        $normalChipCount++
        Assert-True (
            [string]$profile.Category -eq 'BDP_Dev_UnreviewedChip'
        ) "$defName must reference the DevHarness-only unreviewed category until individually reviewed."
    }
}

Assert-True ($normalChipCount -gt 0) 'The category migration test must inspect at least one normal candidate chip.'

Write-Output 'ChipCategoryDefinitionBoundarySmokeTests PASS'
