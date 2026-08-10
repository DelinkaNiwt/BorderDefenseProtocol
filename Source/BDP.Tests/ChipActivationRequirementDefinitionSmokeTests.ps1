$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$requirementRoot = Join-Path $repoRoot 'Source\BDP\Core\Requirements'
$chipServicePath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Services\ChipActivationRequirementService.cs'
$configPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipDefinitionConfig.cs'
$trionConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipTrionConfig.cs'
$trionContractPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\ChipTrionContract.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Validation\DefaultChipDefinitionValidator.cs'
$listValidatorPath = Join-Path $repoRoot 'Source\BDP\Core\Requirements\PawnRequirementListValidator.cs'
$skillRequirementPath = Join-Path $repoRoot 'Source\BDP\Core\Requirements\SkillLevelRequirement.cs'
$payloadPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs'

Assert-True (Test-Path -LiteralPath (Join-Path $requirementRoot 'PawnRequirement.cs')) `
    '07B must provide the common PawnRequirement base type.'
Assert-True (Test-Path -LiteralPath (Join-Path $requirementRoot 'TrionIntensityRequirement.cs')) `
    '07B must provide the standard TrionIntensityRequirement.'
Assert-True (Test-Path -LiteralPath (Join-Path $requirementRoot 'SkillLevelRequirement.cs')) `
    '07B must provide the standard SkillLevelRequirement.'
Assert-True (Test-Path -LiteralPath $chipServicePath) `
    '07B must provide one ordered activation-requirement service.'

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$trionText = (Get-Content -LiteralPath $trionConfigPath -Raw -Encoding utf8) +
    (Get-Content -LiteralPath $trionContractPath -Raw -Encoding utf8)
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$listValidatorText = Get-Content -LiteralPath $listValidatorPath -Raw -Encoding utf8
$skillRequirementText = Get-Content -LiteralPath $skillRequirementPath -Raw -Encoding utf8
$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8

Assert-True (
    ($configText -match 'List<PawnRequirement>\s+ActivationRequirements') -and
    ($resolverText -match 'ActivationRequirements')
) 'Chip definitions and contracts must retain the ordered ActivationRequirements list.'

Assert-True (
    ($validatorText -match 'TrionIntensityRequirementMissing') -and
    ($validatorText -match 'TrionIntensityRequirementDuplicate') -and
    ($validatorText -match 'SkillLevelRequirementDuplicate') -and
    ($validatorText -match 'PawnRequirementListValidator') -and
    ($listValidatorText -match 'SkillDuplicate') -and
    ($skillRequirementText -match 'MinimumLevel')
) 'Definition validation must enforce one positive integer intensity requirement and valid unique skills.'

Assert-True (
    ($trionText -notmatch 'PowerRequirement') -and
    ($payloadText -notmatch 'PowerRequirement') -and
    ($collectorText -notmatch 'PowerRequirement')
) 'The obsolete PowerRequirement channel must be physically removed from Trion and expression payloads.'

Write-Output 'ChipActivationRequirementDefinitionSmokeTests PASS'
