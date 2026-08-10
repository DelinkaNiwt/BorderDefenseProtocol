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

$entryConfigPath = Join-Path $bdpSourceRoot 'Expressions\Config\ChipExpressionEntryConfig.cs'
$entryContractPath = Join-Path $bdpSourceRoot 'Expressions\Contract\ChipExpressionEntryContract.cs'
$contractInterpreterPath = Join-Path $bdpSourceRoot 'Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$declarationPath = Join-Path $bdpSourceRoot 'Expressions\Model\ExpressionSourceDeclaration.cs'
$materialPath = Join-Path $bdpSourceRoot 'Expressions\Model\ExpressionSourceMaterial.cs'
$resultPath = Join-Path $bdpSourceRoot 'Expressions\Model\FormalExpressionResult.cs'
$sourceProviderPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs'
$collectorPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'
$singleSideBuilderPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\SingleSideExpressionBuilder.cs'
$compositeResolverPath = Join-Path $bdpSourceRoot 'Expressions\Pipeline\CompositeExpressionResolver.cs'
$moduleMountConfigPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Config\RangedModuleMountConfig.cs'
$moduleDefPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Config\BdpRangedAttackModuleDef.cs'
$moduleConfigNodePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Config\RangedModuleConfigNode.cs'

$entryConfigText = Get-Content -LiteralPath $entryConfigPath -Raw -Encoding utf8
$entryContractText = Get-Content -LiteralPath $entryContractPath -Raw -Encoding utf8
$contractInterpreterText = Get-Content -LiteralPath $contractInterpreterPath -Raw -Encoding utf8
$declarationText = Get-Content -LiteralPath $declarationPath -Raw -Encoding utf8
$materialText = Get-Content -LiteralPath $materialPath -Raw -Encoding utf8
$resultText = Get-Content -LiteralPath $resultPath -Raw -Encoding utf8
$sourceProviderText = Get-Content -LiteralPath $sourceProviderPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8
$singleSideBuilderText = Get-Content -LiteralPath $singleSideBuilderPath -Raw -Encoding utf8
$compositeResolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8
$moduleMountConfigText = if (Test-Path -LiteralPath $moduleMountConfigPath) { Get-Content -LiteralPath $moduleMountConfigPath -Raw -Encoding utf8 } else { '' }
$moduleDefText = if (Test-Path -LiteralPath $moduleDefPath) { Get-Content -LiteralPath $moduleDefPath -Raw -Encoding utf8 } else { '' }
$moduleConfigNodeText = if (Test-Path -LiteralPath $moduleConfigNodePath) { Get-Content -LiteralPath $moduleConfigNodePath -Raw -Encoding utf8 } else { '' }

Assert-True (
    Test-Path -LiteralPath $moduleMountConfigPath
) 'RangedModuleMountConfig.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $moduleDefPath
) 'BdpRangedAttackModuleDef.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $moduleConfigNodePath
) 'RangedModuleConfigNode.cs must exist.'

Assert-True (
    ($entryConfigText -match 'List<RangedModuleMountConfig>\s+RangedModules')
) 'ChipExpressionEntryConfig must expose List<RangedModuleMountConfig> RangedModules.'

Assert-True (
    ($entryContractText -match 'IReadOnlyList<RangedModuleMountConfig>\s+RangedModules')
) 'ChipExpressionEntryContract must expose IReadOnlyList<RangedModuleMountConfig> RangedModules.'

Assert-True (
    ($declarationText -match 'IReadOnlyList<RangedModuleMountConfig>\s+RangedModules\s*\{\s*get;\s*set;\s*\}')
) 'ExpressionSourceDeclaration must expose a RangedModules snapshot.'

Assert-True (
    ($materialText -match 'IReadOnlyList<RangedModuleMountConfig>\s+RangedModules\s*\{\s*get;\s*set;\s*\}')
) 'ExpressionSourceMaterial must expose a RangedModules snapshot.'

Assert-True (
    ($resultText -match 'IReadOnlyList<RangedModuleMountConfig>\s+RangedModules\s*\{\s*get;\s*set;\s*\}')
) 'FormalExpressionResult must expose a RangedModules snapshot.'

Assert-True (
    ($moduleMountConfigText -match 'BdpRangedAttackModuleDef\s+moduleDef') -and
    ($moduleMountConfigText -match 'bool\s+enabled') -and
    ($moduleMountConfigText -match 'RangedModuleConfigNode\s+config')
) 'RangedModuleMountConfig must expose moduleDef, enabled, and config.'

Assert-True (
    $moduleMountConfigText -notmatch '\border\b'
) 'RangedModuleMountConfig must not declare order.'

Assert-True (
    ($moduleDefText -match 'class\s+BdpRangedAttackModuleDef\s*:\s*Def') -and
    ($moduleDefText -match 'Type\s+runtimeClass') -and
    ($moduleDefText -match 'RangedModuleConfigNode\s+defaultConfig')
) 'BdpRangedAttackModuleDef must expose runtimeClass and defaultConfig.'

Assert-True (
    $moduleConfigNodeText -match 'class\s+RangedModuleConfigNode'
) 'RangedModuleConfigNode must exist as the neutral config node root.'

Assert-True (
    $contractInterpreterText -match 'RangedModules\s*=\s*config\.RangedModules != null'
) 'DefaultChipExpressionContractInterpreter must copy entry config RangedModules into the formal contract.'

Assert-True (
    $sourceProviderText -match 'RangedModules\s*=\s*entry\.RangedModules'
) 'DefaultExpressionSourceDeclarationProvider must forward RangedModules from contract to declaration.'

Assert-True (
    $collectorText -match 'RangedModules\s*=\s*declaration\.RangedModules'
) 'ExpressionSourceCollector must forward RangedModules from declaration to material.'

Assert-True (
    $singleSideBuilderText -match 'RangedModules\s*=\s*material\.RangedModules'
) 'SingleSideExpressionBuilder must forward RangedModules from material to formal result.'

Assert-True (
    ($compositeResolverText -match 'RangedModules\s*=') -and
    ($compositeResolverText -match 'entry\.RangedModules|mainPrimary\.RangedModules|subPrimary\.RangedModules')
) 'CompositeExpressionResolver must build composite result RangedModules snapshots from formal sources.'

Write-Output 'RangedModuleMountConfigSmokeTests PASS'
