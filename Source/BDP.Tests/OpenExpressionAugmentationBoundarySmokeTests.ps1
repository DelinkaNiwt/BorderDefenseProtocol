# 开放式表达增强边界测试：先验证动态增强设施尚未存在，再进入实现。

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
$coreRoot = Join-Path $sourceRoot "BDP\Core"

$entryConfigPath = Join-Path $coreRoot "Expressions\Config\ChipExpressionEntryConfig.cs"
$entryContractPath = Join-Path $coreRoot "Expressions\Contract\ChipExpressionEntryContract.cs"
$sourceDeclarationPath = Join-Path $coreRoot "Expressions\Model\ExpressionSourceDeclaration.cs"
$sourceMaterialPath = Join-Path $coreRoot "Expressions\Model\ExpressionSourceMaterial.cs"
$formalResultPath = Join-Path $coreRoot "Expressions\Model\FormalExpressionResult.cs"
$providerPath = Join-Path $coreRoot "Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs"
$collectorPath = Join-Path $coreRoot "Expressions\Pipeline\ExpressionSourceCollector.cs"
$snapshotBuilderPath = Join-Path $coreRoot "Expressions\Pipeline\ExpressionSnapshotBuilder.cs"
$manualProjectorPath = Join-Path $coreRoot "Expressions\Projection\DefaultManualEntryProjector.cs"

$entryConfigText = Get-Content -LiteralPath $entryConfigPath -Raw -Encoding UTF8
$entryContractText = Get-Content -LiteralPath $entryContractPath -Raw -Encoding UTF8
$sourceDeclarationText = Get-Content -LiteralPath $sourceDeclarationPath -Raw -Encoding UTF8
$sourceMaterialText = Get-Content -LiteralPath $sourceMaterialPath -Raw -Encoding UTF8
$formalResultText = Get-Content -LiteralPath $formalResultPath -Raw -Encoding UTF8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding UTF8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding UTF8
$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding UTF8
$manualProjectorText = Get-Content -LiteralPath $manualProjectorPath -Raw -Encoding UTF8

Assert-True ($entryConfigText -match "RangedModuleAugmentations") `
    "表达条目配置必须声明独立的开放式远程模块增强列表。"
Assert-True ($entryContractText -match "RangedModuleAugmentations") `
    "表达条目契约必须传递开放式远程模块增强列表。"
Assert-True ($sourceDeclarationText -match "RangedModuleAugmentations") `
    "来源声明必须保存被动增强声明，不能只保存当前结果自身模块。"
Assert-True ($sourceMaterialText -match "RangedModuleAugmentations") `
    "来源材料必须把被动增强声明带入表达快照构建。"
Assert-True ($formalResultText -match "DisplayLabelPrefix") `
    "正式结果必须拥有独立的名称前缀修饰，不得破坏枪壳变体字段。"
Assert-True ($providerText -match "RangedModuleAugmentations") `
    "来源声明提供器必须复制开放式增强声明。"
Assert-True ($collectorText -match "RangedModuleAugmentations") `
    "来源收集器必须传递开放式增强声明。"
Assert-True ($snapshotBuilderText -match "ExpressionAugmentationResolver") `
    "表达快照构建器必须在最终结果冻结前调用开放式增强解析器。"
Assert-True ($manualProjectorText -match "ExpressionDisplayLabelResolver") `
    "手动入口投影必须消费最终结果的名称前缀，并继续保留枪壳后缀拼接。"

Write-Output "OpenExpressionAugmentationBoundarySmokeTests PASS"
