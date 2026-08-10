$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$comboRoot = Join-Path $repoRoot 'Source\BDP\Core\Combos'
$candidateComboRoot = Join-Path (
    Split-Path -Parent $repoRoot
) 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Combos'

$defText = Get-Content -LiteralPath (Join-Path $comboRoot 'Defs\ComboDef.cs') -Raw -Encoding utf8
$configText = Get-Content -LiteralPath (Join-Path $comboRoot 'Config\ComboDefinitionConfig.cs') -Raw -Encoding utf8
$contractText = Get-Content -LiteralPath (Join-Path $comboRoot 'Contract\ComboDefinitionContract.cs') -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath (Join-Path $comboRoot 'Contract\ComboDefinitionContractResolver.cs') -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath (Join-Path $comboRoot 'Validation\ComboDefinitionValidator.cs') -Raw -Encoding utf8
$servicePath = Join-Path $comboRoot 'Requirements\ComboUseRequirementService.cs'

foreach ($text in @($defText,$configText,$contractText)) {
    Assert-True (
        $text -match '(?:List|IReadOnlyList)<PawnRequirement>\s+UseRequirements'
    ) 'Combo Def, config, and contract must carry ordered neutral UseRequirements.'
}
Assert-True (
    ($resolverText -match 'UseRequirements') -and
    ($validatorText -match 'PawnRequirementListValidator')
) 'Combo contract resolution and validation must preserve and validate UseRequirements.'
Assert-True (
    Test-Path -LiteralPath $servicePath
) 'Core must provide the Combo use-requirement adapter service.'

$candidateComboText = (
    Get-ChildItem -LiteralPath $candidateComboRoot -Recurse -Filter '*.xml' |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }
) -join "`n"
Assert-True (
    $candidateComboText -notmatch '<UseRequirements>'
) 'No current concrete Combo may receive an invented use threshold.'

Write-Output 'ComboUseRequirementDefinitionSmokeTests PASS'
